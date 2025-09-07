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
		private readonly ReaderWriterLockSlim _materialLock = new ReaderWriterLockSlim();

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
			SculptData sculptdata = new()
			{
				gameObject = gameObject,
				prim = prim,
			};

			//store the gameObject and prim data for the object that requested the mesh
			//so that it can be applied once the data is ready
			requestedMeshes.TryAdd(prim.Sculpt.SculptTexture, new List<SculptData>());
			requestedMeshes[prim.Sculpt.SculptTexture].Add(sculptdata);
			ClientManager.client.Assets.RequestImage(prim.Sculpt.SculptTexture, CallbackSculptTexture);
		}

		public void CallbackSculptTexture(TextureRequestState state, AssetTexture assetTexture)
		{
			if (state != TextureRequestState.Finished) return;

#if !UNITY_ANDROID && !UNITY_IOS && !UNITY_EDITOR_OSX
			UUID id = assetTexture.AssetID;

			MeshmerizerR mesher = new();

			//FIXME Replace this decode with the native code DLL version
			try
			{
				var _ = assetTexture.Decode();
			}
			catch (Exception ex)
			{
				_log.LogError("Exception Decoding Sculpt Texture. " + ex.ToString());
				throw;
			}

			FacetedMesh fmesh;
			Primitive prim;
			try
			{
				// Call a method that might throw an exception
				if (!requestedMeshes.TryGetValue(id, out var sculptDataList)) return;
				if (sculptDataList.Count < 1) return;
				prim = sculptDataList[0].prim;
				fmesh = mesher.GenerateFacetedSculptMesh(requestedMeshes[id][0].prim, assetTexture.Image.ExportBitmap(), DetailLevel.Highest);
			}
			catch (Exception e)
			{
				Debug.Log(e);
				return;
				// Catch all exception cases individually
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
			DebugStatsManager.AddStateUpdate(DebugStatsType.DecodedTextureProcess, uuid.ToString());

			if (components == 3)
			{
				materialContainer[uuid].texture.Reinitialize(width, height, TextureFormat.RGB24, false);
			}
			else
			{
				materialContainer[uuid].texture.Reinitialize(width, height, TextureFormat.RGBA32, false);
			}

			materialContainer[uuid].texture.SetPixelData(bytes, 0);
			materialContainer[uuid].texture.name = $"{uuid} Comp:{components}";
			materialContainer[uuid].texture.Apply();
			
			// compression was called way too often. reduced quality of images,
			// tanked framerate, and somehow increased render performance lol.
			// materialContainer[uuid].texture.Compress(false);
			materialContainer[uuid].components = (uint)components;

			List<Renderer> removeMaterials = new();
			DissolveIn dis;
			if (components == 4)
			{
				for (var i = 0; i < materials[uuid].Count; i++)
				{
					if (materials[uuid][i] == null) continue;

					dis = materials[uuid][i].GetComponent<DissolveIn>();

					Primitive.TextureEntryFace textureEntryFace;
					PrimInfo pi = materials[uuid][i].GetComponent<PrimInfo>();
					if (!ClientManager.simManager.scenePrims.ContainsKey(pi.localID))
					{
						removeMaterials.Add(materials[uuid][i]);
						continue;
					}

					textureEntryFace = ClientManager.simManager.scenePrims[pi.localID].prim.Textures.GetFace((uint)pi.face);

					if (ClientManager.simManager.scenePrims.ContainsKey(pi.localID))
					{
						materials[uuid][i].name += " alpha";
						if (dis == null)
						{
							materials[uuid][i].sharedMaterial = materialContainer[uuid].GetMaterialAlpha(textureEntryFace.RGBA.ToUnity(), textureEntryFace.Glow, textureEntryFace.Fullbright);
						}
						else
						{
							dis.newMat = materialContainer[uuid].GetMaterialAlpha(textureEntryFace.RGBA.ToUnity(), textureEntryFace.Glow, textureEntryFace.Fullbright);
						}
					}

				}
				foreach (Renderer r in removeMaterials)
				{
					materials[uuid].Remove(r);
				}
				//Resources.UnloadUnusedAssets();
			}
		}

		public void RequestMesh2(GameObject gameObject, Primitive primitive, UUID uuid, GameObject meshHolder)
		{
			if (gameObject.IsDestroyed())
			{
				// log warning?
				return;
			}
			if (meshHolder.IsDestroyed())
			{
				// log warning?
				return;
			}

			_assetManager.Meshes.RequestMesh(gameObject, primitive, uuid, meshHolder);
		}

		public void RequestAnimation(Primitive primitive, UUID animationId)
		{
			_assetManager.AnimationManager.RequestAnimation(primitive,animationId);
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
