using System;
using System.Collections.Concurrent;
using UnityEngine;
using OpenMetaverse;
using OpenMetaverse.Assets;
using OpenMetaverse.Rendering;
using CrystalFrost;
using Microsoft.Extensions.Logging;
using CrystalFrost.Services;
using CrystalFrost.Assets.Mesh;
using CSJ2K;

namespace CrystalFrost.Assets
{
    /// <summary>
    /// Data structure for mesh processing queue items
    /// </summary>
    public class MeshQueueItem
    {
        public GameObject GameObject { get; set; }
        public Primitive Primitive { get; set; }
        public UUID MeshUUID { get; set; }
        public GameObject MeshHolder { get; set; }
    }

    /// <summary>
    /// Data structure for sculpt processing
    /// </summary>
    public class SculptData
    {
        public GameObject GameObject { get; set; }
        public Primitive Primitive { get; set; }
        public byte[] ImageData { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    /// <summary>
    /// Specialized manager for mesh processing, sculpt handling, and mesh caching
    /// Extracted from CFAssetManager to follow Single Responsibility Principle
    /// </summary>
    public class MeshManager : IDisposable
    {
        private readonly ILogger<MeshManager> _logger;
        private readonly IClientManagerService _clientManagerService;
        
        // Mesh processing queue and cache
        private readonly ConcurrentQueue<MeshQueueItem> _meshQueue = new();
        private readonly ConcurrentDictionary<UUID, Mesh> _meshCache = new();
        private readonly ConcurrentQueue<SculptData> _sculptQueue = new();

        // Services for mesh processing
        private readonly IAssetManager _assetManager;
        private readonly ITransformTexCoords _transformTextureCoords;

        public MeshManager(ILogger<MeshManager> logger, IClientManagerService clientManagerService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clientManagerService = clientManagerService ?? throw new ArgumentNullException(nameof(clientManagerService));
            
            // Get required services
            _assetManager = Services.GetService<IAssetManager>();
            _transformTextureCoords = Services.GetService<ITransformTexCoords>();
        }

        public void RequestMesh(GameObject gameObject, Primitive primitive, UUID meshUuid, GameObject meshHolder)
        {
            if (gameObject == null || primitive == null)
            {
                _logger.LogWarning("Invalid parameters for mesh request");
                return;
            }

            // Check cache first
            if (_meshCache.TryGetValue(meshUuid, out Mesh cachedMesh))
            {
                ApplyMeshToObject(gameObject, cachedMesh, meshHolder);
                return;
            }

            // Add to processing queue
            var queueItem = new MeshQueueItem
            {
                GameObject = gameObject,
                Primitive = primitive,
                MeshUUID = meshUuid,
                MeshHolder = meshHolder
            };

            _meshQueue.Enqueue(queueItem);
            ProcessMeshQueue();
        }

        private void ProcessMeshQueue()
        {
            if (!_meshQueue.TryDequeue(out MeshQueueItem item))
                return;

            try
            {
                // Request mesh asset from server
                _clientManagerService.Client.Assets.RequestMesh(item.MeshUUID, (success, meshAsset) =>
                {
                    if (success && meshAsset != null)
                    {
                        ProcessMeshAsset(item, meshAsset);
                    }
                    else
                    {
                        _logger.LogWarning($"Failed to retrieve mesh asset {item.MeshUUID}");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing mesh request for {item.MeshUUID}");
            }
        }

        private void ProcessMeshAsset(MeshQueueItem item, AssetMesh meshAsset)
        {
            try
            {
                // Process mesh on main thread
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    ProcessMeshOnMainThread(item, meshAsset);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process mesh asset {item.MeshUUID}");
            }
        }

        private void ProcessMeshOnMainThread(MeshQueueItem item, AssetMesh meshAsset)
        {
            try
            {
                if (!meshAsset.Decode())
                {
                    _logger.LogWarning($"Failed to decode mesh asset {item.MeshUUID}");
                    return;
                }

                Mesh unityMesh = ConvertToUnityMesh(meshAsset, item.Primitive);
                if (unityMesh != null)
                {
                    unityMesh.name = item.MeshUUID.ToString();
                    
                    // Cache the mesh
                    _meshCache[item.MeshUUID] = unityMesh;
                    
                    // Apply to game object
                    ApplyMeshToObject(item.GameObject, unityMesh, item.MeshHolder);
                    
                    _logger.LogDebug($"Successfully processed mesh {item.MeshUUID}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing mesh {item.MeshUUID} on main thread");
            }
        }

        private Mesh ConvertToUnityMesh(AssetMesh meshAsset, Primitive primitive)
        {
            try
            {
                var mesh = new Mesh();
                
                // Convert vertices
                if (meshAsset.Positions != null && meshAsset.Positions.Count > 0)
                {
                    Vector3[] vertices = new Vector3[meshAsset.Positions.Count];
                    for (int i = 0; i < meshAsset.Positions.Count; i++)
                    {
                        var omvPos = meshAsset.Positions[i];
                        vertices[i] = new Vector3(omvPos.X, omvPos.Y, omvPos.Z);
                    }
                    mesh.vertices = vertices;
                }

                // Convert normals
                if (meshAsset.Normals != null && meshAsset.Normals.Count > 0)
                {
                    Vector3[] normals = new Vector3[meshAsset.Normals.Count];
                    for (int i = 0; i < meshAsset.Normals.Count; i++)
                    {
                        var omvNormal = meshAsset.Normals[i];
                        normals[i] = new Vector3(omvNormal.X, omvNormal.Y, omvNormal.Z);
                    }
                    mesh.normals = normals;
                }

                // Convert texture coordinates
                if (meshAsset.TexCoords != null && meshAsset.TexCoords.Count > 0)
                {
                    Vector2[] uvs = new Vector2[meshAsset.TexCoords.Count];
                    for (int i = 0; i < meshAsset.TexCoords.Count; i++)
                    {
                        var omvUV = meshAsset.TexCoords[i];
                        uvs[i] = new Vector2(omvUV.X, omvUV.Y);
                    }
                    mesh.uv = uvs;
                }

                // Convert triangles
                if (meshAsset.Indices != null && meshAsset.Indices.Count > 0)
                {
                    int[] triangles = new int[meshAsset.Indices.Count];
                    for (int i = 0; i < meshAsset.Indices.Count; i++)
                    {
                        triangles[i] = (int)meshAsset.Indices[i];
                    }
                    mesh.triangles = triangles;
                }

                mesh.RecalculateBounds();
                mesh.RecalculateTangents();

                return mesh;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert OpenMetaverse mesh to Unity mesh");
                return null;
            }
        }

        private void ApplyMeshToObject(GameObject gameObject, Mesh mesh, GameObject meshHolder)
        {
            try
            {
                GameObject targetObject = meshHolder ?? gameObject;
                
                var meshFilter = targetObject.GetComponent<MeshFilter>();
                if (meshFilter == null)
                {
                    meshFilter = targetObject.AddComponent<MeshFilter>();
                }
                
                var meshRenderer = targetObject.GetComponent<MeshRenderer>();
                if (meshRenderer == null)
                {
                    meshRenderer = targetObject.AddComponent<MeshRenderer>();
                }

                meshFilter.mesh = mesh;
                
                _logger.LogDebug($"Applied mesh to {targetObject.name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to apply mesh to game object");
            }
        }

        public void RequestSculpt(GameObject gameObject, Primitive primitive)
        {
            if (gameObject == null || primitive == null)
            {
                _logger.LogWarning("Invalid parameters for sculpt request");
                return;
            }

            try
            {
                UUID sculptTexture = primitive.Sculpt.SculptTexture;
                
                // Request the sculpt texture
                _clientManagerService.Client.Assets.RequestImage(sculptTexture, (state, assetTexture) =>
                {
                    if (state == TextureRequestState.Finished && assetTexture?.AssetData != null)
                    {
                        ProcessSculptTexture(gameObject, primitive, assetTexture);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to request sculpt texture");
            }
        }

        private void ProcessSculptTexture(GameObject gameObject, Primitive primitive, AssetTexture assetTexture)
        {
            try
            {
                // Process sculpt on main thread
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    var sculptData = new SculptData
                    {
                        GameObject = gameObject,
                        Primitive = primitive,
                        ImageData = assetTexture.AssetData
                    };

                    _sculptQueue.Enqueue(sculptData);
                    ProcessSculptQueue();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process sculpt texture");
            }
        }

        private void ProcessSculptQueue()
        {
            if (!_sculptQueue.TryDequeue(out SculptData sculptData))
                return;

            try
            {
                // Create mesh from sculpt data
                Mesh sculptMesh = CreateSculptMesh(sculptData);
                if (sculptMesh != null)
                {
                    ApplyMeshToObject(sculptData.GameObject, sculptMesh, null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process sculpt from queue");
            }
        }

		private Mesh CreateSculptMesh(SculptData sculptData)
		{
			try
			{
				// Decode the sculpt texture data using CSJ2K
				RawBytesImageCreator.Register();
				var pi = J2kImage.FromBytes(sculptData.ImageData);
				if (pi == null)
				{
					_logger.LogError("Failed to decode sculpt texture: J2kImage is null");
					return null;
				}
				var rawImage = pi.As<RawBytesImage>();
				int width = rawImage.Width;
				int height = rawImage.Height;
				byte[] imageData = rawImage.Data;

				// For now, we only support sphere sculpts
				if (sculptData.Primitive.Sculpt.Type != SculptType.Sphere)
				{
					_logger.LogWarning($"Unsupported sculpt type: {sculptData.Primitive.Sculpt.Type}. Only Sphere is supported for now.");
					return null;
				}

				// Generate the mesh from the heightmap
				int x, y;
				var vertices = new List<Vector3>();
				var uvs = new List<Vector2>();
				var triangles = new List<int>();

				for (y = 0; y < height; y++)
				{
					for (x = 0; x < width; x++)
					{
						// Get height from blue channel, as is standard for SL sculpts
						float z = imageData[(y * width + x) * 4 + 2] / 255.0f;

						// Map plane to sphere
						float lon = (x / (float)(width - 1)) * 2.0f * Mathf.PI;
						float lat = (y / (float)(height - 1)) * Mathf.PI;

						float radius = 0.5f * z; // Simple radius based on height

						vertices.Add(new Vector3(
							radius * Mathf.Sin(lat) * Mathf.Cos(lon),
							radius * Mathf.Cos(lat),
							radius * Mathf.Sin(lat) * Mathf.Sin(lon)
						));

						uvs.Add(new Vector2(x / (float)width, y / (float)height));
					}
				}

				for (y = 0; y < height - 1; y++)
				{
					for (x = 0; x < width - 1; x++)
					{
						int tl = y * width + x;
						int tr = tl + 1;
						int bl = (y + 1) * width + x;
						int br = bl + 1;

						triangles.Add(tl);
						triangles.Add(tr);
						triangles.Add(bl);

						triangles.Add(tr);
						triangles.Add(br);
						triangles.Add(bl);
					}
				}

				var mesh = new Mesh
				{
					name = "SculptMesh",
					vertices = vertices.ToArray(),
					uv = uvs.ToArray(),
					triangles = triangles.ToArray()
				};

				mesh.RecalculateNormals();
				mesh.RecalculateBounds();

				return mesh;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to create sculpt mesh");
				return null;
			}
		}

        public void ClearCache()
        {
            _logger.LogInformation("Clearing mesh cache");
            
            foreach (var mesh in _meshCache.Values)
            {
                if (mesh != null)
                {
                    UnityEngine.Object.Destroy(mesh);
                }
            }
            
            _meshCache.Clear();
        }

        public void Dispose()
        {
            _logger.LogInformation("Disposing MeshManager");
            
            ClearCache();
            
            // Clear queues
            while (_meshQueue.TryDequeue(out _)) { }
            while (_sculptQueue.TryDequeue(out _)) { }
            
            _logger.LogInformation("MeshManager disposed");
        }
    }
}