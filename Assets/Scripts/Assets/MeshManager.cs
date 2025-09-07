using System;
using System.Collections.Concurrent;
using UnityEngine;
using OpenMetaverse;
using OpenMetaverse.Assets;
using OpenMetaverse.Rendering;
using Microsoft.Extensions.Logging;
using CrystalFrost.Services;
using CrystalFrost.Assets.Mesh;

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
                // This is a simplified sculpt mesh creation
                // In a full implementation, this would decode the sculpt texture
                // and generate proper mesh geometry based on the height map
                
                var mesh = new Mesh();
                mesh.name = "SculptMesh";
                
                // Create a simple plane as placeholder
                Vector3[] vertices = new Vector3[4]
                {
                    new Vector3(-0.5f, 0, -0.5f),
                    new Vector3(0.5f, 0, -0.5f),
                    new Vector3(-0.5f, 0, 0.5f),
                    new Vector3(0.5f, 0, 0.5f)
                };
                
                Vector2[] uv = new Vector2[4]
                {
                    new Vector2(0, 0),
                    new Vector2(1, 0),
                    new Vector2(0, 1),
                    new Vector2(1, 1)
                };
                
                int[] triangles = new int[6] { 0, 2, 1, 2, 3, 1 };
                
                mesh.vertices = vertices;
                mesh.uv = uv;
                mesh.triangles = triangles;
                mesh.RecalculateNormals();
                
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