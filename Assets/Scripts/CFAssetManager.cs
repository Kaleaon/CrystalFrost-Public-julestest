using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using OpenMetaverse;
using OpenMetaverse.Assets;
using OpenMetaverse.Rendering;
using CrystalFrost.Assets;
using UnityEditor;
using OMVVector3 = OpenMetaverse.Vector3;
using Vector3 = UnityEngine.Vector3;
using OMVVector2 = OpenMetaverse.Vector2;
using Vector2 = UnityEngine.Vector2;
using Material = UnityEngine.Material;
using Mesh = UnityEngine.Mesh;
using CrystalFrost.Lib;
using CrystalFrost.Extensions;
using Microsoft.Extensions.Logging;
using CrystalFrost.Assets.Mesh;
using Temp;
using Unity.VisualScripting;

namespace CrystalFrost
{
	/// <summary>
	/// Manages asset caching, texture processing, and material creation for the Crystal Frost viewer.
	/// Implements proper disposal patterns to prevent memory leaks and thread safety for concurrent access.
	/// </summary>
	public class CFAssetManager : IDisposable
	{
		//it's faster to multiply by this than to divide by 255
		//to derive the float value from 0-255 pixel values
		//private float byteMult = 0.003921568627451f;

		public SimManager simManager;

		private readonly IAssetManager _assetManager;
		private readonly ITransformTexCoords _transformTextureCoords;
		private readonly ILogger<CFAssetManager> _log;

		/// <summary>
		/// Read-write lock for protecting critical sections during material operations.
		/// </summary>
		private readonly object _materialLock = new object();

		/// <summary>
		/// Pool of white textures to reduce instantiation overhead.
		/// Thread-safe using ConcurrentQueue for multi-threaded access.
		/// </summary>
		private readonly ConcurrentQueue<Texture2D> _whiteTexturePool = new ConcurrentQueue<Texture2D>();
		private const int MaxPoolSize = 100; // Limit pool size to prevent excessive memory usage

		public CFAssetManager()
		{
			_log = Services.GetService<ILogger<CFAssetManager>>();
			_transformTextureCoords = Services.GetService<ITransformTexCoords>();
			_assetManager = Services.GetService<IAssetManager>();
		}

		public class MeshQueueItem
		{
			public UUID uuid;
			public List<RawMeshData> meshData = new();
		}

		public ConcurrentQueue<MeshQueueItem> concurrentMeshQueue = new();

		public class SLMeshData
		{
			public Mesh[] meshHighest;
			public Mesh[] meshHigh;
			public Mesh[] meshMedium;
		}

		public ConcurrentDictionary<UUID, SLMeshData> meshCache = new();
		public ConcurrentDictionary<UUID, AudioClip> sounds = new();
		public ConcurrentDictionary<UUID, List<Renderer>> materials = new();
		public ConcurrentDictionary<UUID, int> componentsDict = new();
		public List<MeshRenderer> fullbrights = new();

		public Material zeroMaterial;

		public ConcurrentDictionary<UUID, MaterialContainer> materialContainer = new();

		/// <summary>
		/// Gets a white texture from the pool or creates a new one if the pool is empty.
		/// This reduces garbage collection pressure from repeated texture instantiation.
		/// Thread-safe implementation using ConcurrentQueue.
		/// </summary>
		/// <returns>A white texture instance</returns>
		private Texture2D GetPooledWhiteTexture()
		{
			if (_whiteTexturePool.TryDequeue(out Texture2D pooledTexture))
			{
				return pooledTexture;
			}

			// Create new texture if pool is empty
			return Texture2D.Instantiate(Texture2D.whiteTexture);
		}

		/// <summary>
		/// Returns a white texture to the pool for reuse, if the pool isn't full.
		/// Thread-safe implementation using ConcurrentQueue.
		/// </summary>
		/// <param name="texture">The texture to return to the pool</param>
		private void ReturnTextureToPool(Texture2D texture)
		{
			if (texture == null) return;

			if (_whiteTexturePool.Count < MaxPoolSize)
			{
				_whiteTexturePool.Enqueue(texture);
			}
			else
			{
				// Pool is full, destroy the texture
				if (Application.isPlaying)
					UnityEngine.Object.Destroy(texture);
				else
					UnityEngine.Object.DestroyImmediate(texture);
			}
		}

		//request non-fullbright texture from the server
		/*		public void RequestTexture(UUID uuid, Renderer rendr, Color color, float glow, bool fullbright)
				{
					if (uuid == UUID.Zero) return;
					if (!materials.ContainsKey(uuid))
					{
						materials.Add(uuid, new List<Renderer>());
					}
					if (!materials[uuid].Contains(rendr))
					{
						materials[uuid].Add(rendr);
					}

					DissolveIn dis = rendr.gameObject.GetComponent<DissolveIn>();
					//Don't bother requesting a texture if it's already cached in memory;
					if (materialContainer.ContainsKey(uuid))
					{
						rendr.sharedMaterial = materialContainer[uuid].GetMaterial(color, glow, fullbright);
						if (materialContainer[uuid].ready) return;
					}
					else
					{
						materialContainer.Add(uuid, new MaterialContainer(uuid, Texture2D.Instantiate(Texture2D.whiteTexture), 3));
						if (dis == null)
						{
							rendr.sharedMaterial = materialContainer[uuid].GetMaterial(color, glow, fullbright);
						}
						else
						{
							dis.newMat = materialContainer[uuid].GetMaterial(color, glow, fullbright);
						}
					}
					materialContainer[uuid].ready = true;
					_assetManager.Textures.RequestImage(uuid);
				}*/

		/// <summary>
		/// Requests a texture for rendering on a specific submesh of a renderer.
		/// Uses texture pooling to reduce memory allocation overhead.
		/// Thread-safe implementation with proper locking for concurrent access.
		/// </summary>
		/// <param name="uuid">The UUID of the texture to request</param>
		/// <param name="rendr">The renderer to apply the texture to</param>
		/// <param name="subMeshIndex">The submesh index to apply the material to</param>
		/// <param name="color">The color tint to apply</param>
		/// <param name="glow">The glow intensity</param>
		/// <param name="fullbright">Whether the material should be fullbright</param>
		/// <returns>The created or cached material</returns>
		public Material RequestTexture(UUID uuid, Renderer rendr, int subMeshIndex, Color color, float glow, bool fullbright)
		{
			if (uuid == UUID.Zero) return null;

			// Validate renderer is not null or destroyed
			if (rendr == null || rendr.gameObject == null)
			{
				_log.LogWarning("RequestTexture called with null or destroyed renderer");
				return null;
			}

			// Use read lock for checking if material already exists
			_materialLock.EnterReadLock();
			try
			{
				// Check if we already have material container
				if (materialContainer.TryGetValue(uuid, out MaterialContainer existingContainer))
				{
					// We have the container, get the material and apply it
					Material existingMaterial = existingContainer.GetMaterial(color, glow, fullbright);
					ApplyMaterialToRenderer(rendr, subMeshIndex, existingMaterial);
					
					// Add renderer to materials list if not already present
					AddRendererToMaterialsList(uuid, rendr);
					
					// Request texture if not ready
					if (!existingContainer.ready)
					{
						DebugStatsManager.AddStateUpdate(DebugStatsType.TextureDownloadRequest, uuid.ToString());
						_assetManager.Textures.RequestImage(uuid);
						existingContainer.ready = true;
					}
					
					return existingMaterial;
				}
			}
			finally
			{
				_materialLock.ExitReadLock();
			}

			// Need to create new material container - upgrade to write lock
			_materialLock.EnterWriteLock();
			try
			{
				// Double-check that another thread didn't create it while we were waiting
				if (materialContainer.TryGetValue(uuid, out MaterialContainer doubleCheckContainer))
				{
					Material existingMaterial = doubleCheckContainer.GetMaterial(color, glow, fullbright);
					ApplyMaterialToRenderer(rendr, subMeshIndex, existingMaterial);
					AddRendererToMaterialsList(uuid, rendr);
					
					if (!doubleCheckContainer.ready)
					{
						DebugStatsManager.AddStateUpdate(DebugStatsType.TextureDownloadRequest, uuid.ToString());
						_assetManager.Textures.RequestImage(uuid);
						doubleCheckContainer.ready = true;
					}
					
					return existingMaterial;
				}

				// Create new material container with pooled texture
				Texture2D pooledTexture = GetPooledWhiteTexture();
				var newContainer = new MaterialContainer(uuid, pooledTexture, 3);
				materialContainer.TryAdd(uuid, newContainer);

				Material newMaterial = newContainer.GetMaterial(color, glow, fullbright);
				ApplyMaterialToRenderer(rendr, subMeshIndex, newMaterial);
				AddRendererToMaterialsList(uuid, rendr);

				DebugStatsManager.AddStateUpdate(DebugStatsType.TextureDownloadRequest, uuid.ToString());
				_assetManager.Textures.RequestImage(uuid);
				newContainer.ready = true;

				return newMaterial;
			}
			finally
			{
				_materialLock.ExitWriteLock();
			}
		}

		/// <summary>
		/// Helper method to apply material to renderer with proper error handling.
		/// </summary>
		/// <param name="rendr">The renderer to apply material to</param>
		/// <param name="subMeshIndex">The submesh index</param>
		/// <param name="material">The material to apply</param>
		private void ApplyMaterialToRenderer(Renderer rendr, int subMeshIndex, Material material)
		{
			try
			{
				Material[] mats = rendr.materials;
				if (subMeshIndex >= 0 && subMeshIndex < mats.Length)
				{
					mats[subMeshIndex] = material;
					rendr.materials = mats;
				}
				else
				{
					_log.LogWarning($"SubMesh index {subMeshIndex} out of bounds for renderer with {mats.Length} materials");
				}
			}
			catch (Exception ex)
			{
				_log.LogError($"Error applying material to renderer: {ex.Message}");
			}
		}

		/// <summary>
		/// Helper method to add renderer to materials list thread-safely.
		/// </summary>
		/// <param name="uuid">The texture UUID</param>
		/// <param name="rendr">The renderer to add</param>
		private void AddRendererToMaterialsList(UUID uuid, Renderer rendr)
		{
			var rendererList = materials.GetOrAdd(uuid, _ => new List<Renderer>());
			
			lock (rendererList)
			{
				if (!rendererList.Contains(rendr))
				{
					rendererList.Add(rendr);
				}
			}
		}


		/// <summary>
		/// Requests a texture for terrain rendering.
		/// Uses texture pooling to reduce memory allocation overhead.
		/// Thread-safe implementation with proper locking.
		/// </summary>
		/// <param name="uuid">The UUID of the texture to request</param>
		/// <returns>The texture for terrain rendering</returns>
		public Texture2D RequestTexture(UUID uuid)
		{
			if (uuid == UUID.Zero) return null;

			// Use read lock to check if texture already exists
			_materialLock.EnterReadLock();
			try
			{
				if (materialContainer.TryGetValue(uuid, out MaterialContainer existingContainer))
				{
					// Ensure materials list exists for this UUID
					materials.GetOrAdd(uuid, _ => new List<Renderer>());
					return existingContainer.texture;
				}
			}
			finally
			{
				_materialLock.ExitReadLock();
			}

			// Need to create new container - upgrade to write lock
			_materialLock.EnterWriteLock();
			try
			{
				// Double-check that another thread didn't create it
				if (materialContainer.TryGetValue(uuid, out MaterialContainer doubleCheckContainer))
				{
					materials.GetOrAdd(uuid, _ => new List<Renderer>());
					return doubleCheckContainer.texture;
				}

				// Create new material container with pooled texture
				Texture2D pooledTexture = GetPooledWhiteTexture();
				var newContainer = new MaterialContainer(uuid, pooledTexture, 3);
				materialContainer.TryAdd(uuid, newContainer);
				materials.GetOrAdd(uuid, _ => new List<Renderer>());

				// Request the actual texture data
				_assetManager.Textures.RequestImage(uuid);

				return newContainer.texture;
			}
			finally
			{
				_materialLock.ExitWriteLock();
			}
		}



		public class SculptData
		{
			public GameObject gameObject;
			public Primitive prim;
		}

		//request sculpt texture from server
		public void RequestSculpt(GameObject gameObject, Primitive prim)
		{
			if (gameObject == null)
			{
				_log.LogError("RequestSculpt called with null gameObject");
				return;
			}

			if (prim == null)
			{
				_log.LogError("RequestSculpt called with null primitive");
				return;
			}

			if (prim.Sculpt == null)
			{
				_log.LogError($"Primitive {prim.LocalID} has null sculpt data");
				return;
			}

			if (prim.Sculpt.SculptTexture == UUID.Zero)
			{
				_log.LogWarning($"Primitive {prim.LocalID} has zero UUID for sculpt texture");
				return;
			}

			try
			{
				SculptData sculptdata = new()
				{
					gameObject = gameObject,
					prim = prim,
				};

				//store the gameObject and prim data for the object that requested the mesh
				//so that it can be applied once the data is ready
				var sculptDataList = requestedMeshes.GetOrAdd(prim.Sculpt.SculptTexture, _ => new List<SculptData>());
				
				lock (sculptDataList)
				{
					sculptDataList.Add(sculptdata);
				}
				
				ClientManager.client.Assets.RequestImage(prim.Sculpt.SculptTexture, CallbackSculptTexture);
				_log.LogDebug($"Requested sculpt texture {prim.Sculpt.SculptTexture} for primitive {prim.LocalID}");
			}
			catch (Exception ex)
			{
				_log.LogError($"Error requesting sculpt texture for primitive {prim.LocalID}: {ex.Message}\nStack trace: {ex.StackTrace}");
			}
		}

		public void CallbackSculptTexture(TextureRequestState state, AssetTexture assetTexture)
		{
			if (state != TextureRequestState.Finished) 
			{
				_log.LogWarning($"Sculpt texture request not finished. State: {state}");
				return;
			}

			if (assetTexture == null)
			{
				_log.LogError("Received null assetTexture in CallbackSculptTexture");
				return;
			}

#if !UNITY_ANDROID && !UNITY_IOS && !UNITY_EDITOR_OSX
			UUID id = assetTexture.AssetID;
			_log.LogDebug($"Processing sculpt texture callback for ID: {id}");

			MeshmerizerR mesher = new();

			//FIXME Replace this decode with the native code DLL version
			try
			{
				var decodedImage = assetTexture.Decode();
				if (decodedImage == null)
				{
					_log.LogError($"Failed to decode sculpt texture {id}: decoded image is null");
					return;
				}
			}
			catch (OutOfMemoryException ex)
			{
				_log.LogError($"Out of memory when decoding sculpt texture {id}: {ex.Message}");
				return; // Don't rethrow, gracefully handle memory issues
			}
			catch (ArgumentException ex)
			{
				_log.LogError($"Invalid argument when decoding sculpt texture {id}: {ex.Message}");
				return; // Invalid texture data, can't proceed
			}
			catch (Exception ex)
			{
				_log.LogError($"Unexpected exception decoding sculpt texture {id}: {ex.Message}\nStack trace: {ex.StackTrace}");
				return; // Don't rethrow, log and continue
			}

			FacetedMesh fmesh;
			Primitive prim;
			try
			{
				// Validate that we have requested mesh data for this texture
				if (!requestedMeshes.TryGetValue(id, out var sculptDataList))
				{
					_log.LogWarning($"No requested mesh data found for sculpt texture {id}");
					return;
				}
				
				if (sculptDataList == null || sculptDataList.Count < 1)
				{
					_log.LogWarning($"Empty sculpt data list for texture {id}");
					return;
				}

				prim = sculptDataList[0].prim;
				if (prim == null)
				{
					_log.LogError($"Null primitive in sculpt data for texture {id}");
					return;
				}

				if (assetTexture.Image?.ExportBitmap() == null)
				{
					_log.LogError($"Failed to export bitmap from asset texture {id}");
					return;
				}

				fmesh = mesher.GenerateFacetedSculptMesh(prim, assetTexture.Image.ExportBitmap(), DetailLevel.Highest);
				
				if (fmesh == null || fmesh.Faces == null)
				{
					_log.LogError($"Failed to generate faceted mesh for sculpt texture {id}");
					return;
				}
			}
			catch (ArgumentNullException ex)
			{
				_log.LogError($"Null argument when generating sculpt mesh for {id}: {ex.Message}");
				return;
			}
			catch (InvalidOperationException ex)
			{
				_log.LogError($"Invalid operation when generating sculpt mesh for {id}: {ex.Message}");
				return;
			}
			catch (Exception ex)
			{
				_log.LogError($"Unexpected error generating sculpt mesh for {id}: {ex.Message}\nStack trace: {ex.StackTrace}");
				return;
			}


			for (var j = 0; j < fmesh.Faces.Count; j++)
			{

				if (fmesh.Faces[j].Vertices.Count == 0)
				{
					continue;
				}

				var item = new MeshQueueItem()
				{ uuid = prim.Sculpt.SculptTexture };

				for (j = 0; j < fmesh.Faces.Count; j++)
				{
					Primitive.TextureEntryFace textureEntryFace = prim.Textures.GetFace((uint)j);

					var face = fmesh.Faces[j];
					_transformTextureCoords.TransformTexCoords(face.Vertices, face.Center, textureEntryFace, prim.Scale);
					RawMeshData rmd = face.ToRawMeshData();
					item.meshData.Add(rmd);
				}
				concurrentMeshQueue.Enqueue(item);
				requestedMeshes.TryRemove(id, out var _);
			}
#endif
		}

		public void MainThreadTextureReinitialize(byte[] bytes, UUID uuid, int width, int height, int components)
		{
			if (bytes == null)
			{
				_log.LogError($"Null bytes array for texture {uuid}");
				return;
			}

			if (width <= 0 || height <= 0)
			{
				_log.LogError($"Invalid texture dimensions for {uuid}: {width}x{height}");
				return;
			}

			if (components != 3 && components != 4)
			{
				_log.LogError($"Invalid component count for texture {uuid}: {components}. Expected 3 or 4.");
				return;
			}

			DebugStatsManager.AddStateUpdate(DebugStatsType.DecodedTextureProcess, uuid.ToString());

			try
			{
				// Validate that we have a material container for this UUID
				if (!materialContainer.TryGetValue(uuid, out MaterialContainer container))
				{
					_log.LogWarning($"No material container found for texture {uuid}");
					return;
				}

				if (container?.texture == null)
				{
					_log.LogError($"Material container for {uuid} has null texture");
					return;
				}

				// Reinitialize texture with appropriate format
				TextureFormat format = components == 3 ? TextureFormat.RGB24 : TextureFormat.RGBA32;
				
				try
				{
					container.texture.Reinitialize(width, height, format, false);
				}
				catch (UnityException ex)
				{
					_log.LogError($"Failed to reinitialize texture {uuid}: {ex.Message}");
					return;
				}

				// Set pixel data with validation
				try
				{
					container.texture.SetPixelData(bytes, 0);
					container.texture.name = $"{uuid} Comp:{components}";
					container.texture.Apply();
				}
				catch (UnityException ex)
				{
					_log.LogError($"Failed to set pixel data for texture {uuid}: {ex.Message}");
					return;
				}
				catch (ArgumentException ex)
				{
					_log.LogError($"Invalid pixel data for texture {uuid}: {ex.Message}");
					return;
				}

				// compression was called way too often. reduced quality of images,
				// tanked framerate, and somehow increased render performance lol.
				// materialContainer[uuid].texture.Compress(false);
				container.components = (uint)components;

				// Process alpha textures if applicable
				if (components == 4)
				{
					ProcessAlphaTexture(uuid);
				}
			}
			catch (Exception ex)
			{
				_log.LogError($"Unexpected error reinitializing texture {uuid}: {ex.Message}\nStack trace: {ex.StackTrace}");
			}
		}

		/// <summary>
		/// Processes alpha textures and updates materials accordingly.
		/// Separated from MainThreadTextureReinitialize for better error handling.
		/// </summary>
		/// <param name="uuid">The texture UUID</param>
		private void ProcessAlphaTexture(UUID uuid)
		{
			try
			{
				if (!materials.TryGetValue(uuid, out List<Renderer> rendererList))
				{
					_log.LogWarning($"No renderer list found for alpha texture {uuid}");
					return;
				}

				List<Renderer> removeMaterials = new();
				
				// Create a copy of the list to avoid modification during iteration
				List<Renderer> renderersToProcess;
				lock (rendererList)
				{
					renderersToProcess = new List<Renderer>(rendererList);
				}

				foreach (var renderer in renderersToProcess)
				{
					try
					{
						if (renderer == null)
						{
							_log.LogWarning($"Null renderer found in materials list for texture {uuid}");
							continue;
						}

						DissolveIn dis = renderer.GetComponent<DissolveIn>();
						PrimInfo pi = renderer.GetComponent<PrimInfo>();
						
						if (pi == null)
						{
							_log.LogWarning($"No PrimInfo component found on renderer for texture {uuid}");
							removeMaterials.Add(renderer);
							continue;
						}

						if (!ClientManager.simManager.scenePrims.TryGetValue(pi.localID, out ScenePrimData scenePrim))
						{
							_log.LogDebug($"Scene prim {pi.localID} not found, marking renderer for removal");
							removeMaterials.Add(renderer);
							continue;
						}

						if (scenePrim.prim?.Textures == null)
						{
							_log.LogWarning($"Scene prim {pi.localID} has null textures");
							continue;
						}

						Primitive.TextureEntryFace textureEntryFace = scenePrim.prim.Textures.GetFace((uint)pi.face);
						if (textureEntryFace == null)
						{
							_log.LogWarning($"Failed to get texture face {pi.face} for prim {pi.localID}");
							continue;
						}

						renderer.name += " alpha";
						
						if (!materialContainer.TryGetValue(uuid, out MaterialContainer container))
						{
							_log.LogWarning($"Material container for {uuid} not found during alpha processing");
							continue;
						}

						Material alphaMaterial = container.GetMaterialAlpha(
							textureEntryFace.RGBA.ToUnity(), 
							textureEntryFace.Glow, 
							textureEntryFace.Fullbright);

						if (dis == null)
						{
							renderer.sharedMaterial = alphaMaterial;
						}
						else
						{
							dis.newMat = alphaMaterial;
						}
					}
					catch (Exception ex)
					{
						_log.LogError($"Error processing alpha material for renderer: {ex.Message}");
						removeMaterials.Add(renderer);
					}
				}

				// Remove invalid renderers from the list
				if (removeMaterials.Count > 0)
				{
					lock (rendererList)
					{
						foreach (Renderer r in removeMaterials)
						{
							rendererList.Remove(r);
						}
					}
					_log.LogDebug($"Removed {removeMaterials.Count} invalid renderers for texture {uuid}");
				}
			}
			catch (Exception ex)
			{
				_log.LogError($"Unexpected error processing alpha texture {uuid}: {ex.Message}\nStack trace: {ex.StackTrace}");
			}
		}

		public void RequestMesh2(GameObject gameObject, Primitive primitive, UUID uuid, GameObject meshHolder)
		{
			// Comprehensive null checks with proper logging
			if (gameObject == null)
			{
				_log.LogError("RequestMesh2 called with null gameObject");
				return;
			}

			if (primitive == null)
			{
				_log.LogError("RequestMesh2 called with null primitive");
				return;
			}

			if (meshHolder == null)
			{
				_log.LogError("RequestMesh2 called with null meshHolder");
				return;
			}

			if (uuid == UUID.Zero)
			{
				_log.LogWarning($"RequestMesh2 called with zero UUID for primitive {primitive.LocalID}");
				return;
			}

			// Check if objects are destroyed (Unity-specific check)
			if (gameObject.IsDestroyed())
			{
				_log.LogWarning($"GameObject for primitive {primitive.LocalID} is destroyed, skipping mesh request");
				return;
			}

			if (meshHolder.IsDestroyed())
			{
				_log.LogWarning($"MeshHolder for primitive {primitive.LocalID} is destroyed, skipping mesh request");
				return;
			}

			// Null check for asset manager
			if (_assetManager?.Meshes == null)
			{
				_log.LogError("Asset manager or mesh manager is null, cannot request mesh");
				return;
			}

			try
			{
				_assetManager.Meshes.RequestMesh(gameObject, primitive, uuid, meshHolder);
			}
			catch (Exception ex)
			{
				_log.LogError($"Error requesting mesh for primitive {primitive.LocalID}: {ex.Message}");
			}
		}

		public void RequestAnimation(Primitive primitive, UUID animationId)
		{
			// Null validation for parameters
			if (primitive == null)
			{
				_log.LogError("RequestAnimation called with null primitive");
				return;
			}

			if (animationId == UUID.Zero)
			{
				_log.LogWarning($"RequestAnimation called with zero UUID for primitive {primitive.LocalID}");
				return;
			}

			// Null check for asset manager
			if (_assetManager?.AnimationManager == null)
			{
				_log.LogError("Asset manager or animation manager is null, cannot request animation");
				return;
			}

			try
			{
				_assetManager.AnimationManager.RequestAnimation(primitive, animationId);
			}
			catch (Exception ex)
			{
				_log.LogError($"Error requesting animation {animationId} for primitive {primitive.LocalID}: {ex.Message}");
			}
		}

		private readonly ConcurrentDictionary<UUID, List<SculptData>> requestedMeshes = new();

        /// <summary>
        /// Disposes of all assets and clears caches to prevent memory leaks.
        /// This should be called when logging out or switching grids.
        /// </summary>
        public void Dispose()
        {
            _log.LogInformation("CFAssetManager disposing resources...");

            _materialLock.EnterWriteLock();
            try
            {
                // Dispose all MaterialContainer instances to prevent memory leaks
                foreach (var container in materialContainer.Values)
                {
                    try
                    {
                        container?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _log.LogError($"Error disposing MaterialContainer: {ex.Message}");
                    }
                }

                // Clear all caches and collections
                concurrentMeshQueue = new ConcurrentQueue<MeshQueueItem>();
                meshCache.Clear();
                sounds.Clear();
                materials.Clear();
                componentsDict.Clear();
                fullbrights.Clear();
                materialContainer.Clear();
                requestedMeshes.Clear();

                // Clear renderer lists to prevent holding references to destroyed objects
                foreach (var rendererList in materials.Values)
                {
                    rendererList.Clear();
                }
            }
            finally
            {
                _materialLock.ExitWriteLock();
            }

            // Dispose pooled textures
            while (_whiteTexturePool.TryDequeue(out Texture2D texture))
            {
                try
                {
                    if (texture != null)
                    {
                        if (Application.isPlaying)
                            UnityEngine.Object.Destroy(texture);
                        else
                            UnityEngine.Object.DestroyImmediate(texture);
                    }
                }
                catch (Exception ex)
                {
                    _log.LogError($"Error disposing pooled texture: {ex.Message}");
                }
            }

            // Dispose AudioClips to prevent memory leaks
            foreach (var audioClip in sounds.Values)
            {
                try
                {
                    if (audioClip != null)
                    {
                        if (Application.isPlaying)
                            UnityEngine.Object.Destroy(audioClip);
                        else
                            UnityEngine.Object.DestroyImmediate(audioClip);
                    }
                }
                catch (Exception ex)
                {
                    _log.LogError($"Error disposing AudioClip: {ex.Message}");
                }
            }

            // Dispose the lock
            _materialLock?.Dispose();

            _log.LogInformation("CFAssetManager disposed successfully.");
        }
    }
}
