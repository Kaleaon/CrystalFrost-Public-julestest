using System;
using System.Collections.Generic;
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
    /// Represents the loading state of a mesh request
    /// </summary>
    public enum MeshRequestState
    {
        Pending,
        Loaded,
        Failed
    }

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

        // Active request state tracking
        private readonly ConcurrentDictionary<string, MeshRequestState> _requestStates = new();

        // Static cached procedural fallback meshes (memory overhead < 5 KB, well below 1 MB guardrail)
        private static Mesh _cachedPlaceholderWireframeMesh;
        private static Mesh _cachedFallbackBoxMesh;

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

        #region Request State Tracking

        private string GetRequestKey(GameObject gameObject, UUID uuid)
        {
            int goId = gameObject != null ? gameObject.GetInstanceID() : 0;
            return $"{goId}_{uuid}";
        }

        public MeshRequestState GetRequestState(GameObject gameObject, UUID uuid)
        {
            string key = GetRequestKey(gameObject, uuid);
            if (_requestStates.TryGetValue(key, out var state))
            {
                return state;
            }
            return MeshRequestState.Pending;
        }

        public MeshRequestState GetRequestState(GameObject gameObject)
        {
            if (gameObject == null) return MeshRequestState.Pending;
            int goId = gameObject.GetInstanceID();
            foreach (var kvp in _requestStates)
            {
                if (kvp.Key.StartsWith($"{goId}_"))
                {
                    return kvp.Value;
                }
            }
            return MeshRequestState.Pending;
        }

        public MeshRequestState GetRequestState(UUID uuid)
        {
            foreach (var kvp in _requestStates)
            {
                if (kvp.Key.EndsWith($"_{uuid}"))
                {
                    return kvp.Value;
                }
            }
            return MeshRequestState.Pending;
        }

        private void SetRequestState(GameObject gameObject, UUID uuid, MeshRequestState state)
        {
            string key = GetRequestKey(gameObject, uuid);
            _requestStates[key] = state;
        }

        #endregion

        #region Procedural Mesh Generation & Helpers

        private void ExecuteOnMainThread(Action action)
        {
            if (action == null) return;
            if (UnityMainThreadDispatcher.Exists())
            {
                UnityMainThreadDispatcher.Instance().Enqueue(action);
            }
            else
            {
                action();
            }
        }

        private static Mesh GetOrCreatePlaceholderWireframeMesh()
        {
            if (_cachedPlaceholderWireframeMesh != null)
                return _cachedPlaceholderWireframeMesh;

            Mesh mesh = new Mesh { name = "PlaceholderWireframeMesh" };

            Vector3[] vertices = new Vector3[]
            {
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3( 0.5f, -0.5f, -0.5f),
                new Vector3( 0.5f,  0.5f, -0.5f),
                new Vector3(-0.5f,  0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f,  0.5f),
                new Vector3( 0.5f, -0.5f,  0.5f),
                new Vector3( 0.5f,  0.5f,  0.5f),
                new Vector3(-0.5f,  0.5f,  0.5f)
            };

            int[] lineIndices = new int[]
            {
                0, 1,  1, 2,  2, 3,  3, 0, // Bottom square
                4, 5,  5, 6,  6, 7,  7, 4, // Top square
                0, 4,  1, 5,  2, 6,  3, 7  // Vertical pillars
            };

            mesh.vertices = vertices;
            mesh.SetIndices(lineIndices, MeshTopology.Lines, 0);
            mesh.RecalculateBounds();

            _cachedPlaceholderWireframeMesh = mesh;
            return _cachedPlaceholderWireframeMesh;
        }

        private static Mesh GetOrCreateFallbackBoxMesh()
        {
            if (_cachedFallbackBoxMesh != null)
                return _cachedFallbackBoxMesh;

            Mesh mesh = new Mesh { name = "FallbackBoxMesh" };

            Vector3[] vertices = new Vector3[]
            {
                // Front face
                new Vector3(-0.5f, -0.5f,  0.5f), new Vector3( 0.5f, -0.5f,  0.5f), new Vector3( 0.5f,  0.5f,  0.5f), new Vector3(-0.5f,  0.5f,  0.5f),
                // Back face
                new Vector3( 0.5f, -0.5f, -0.5f), new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-0.5f,  0.5f, -0.5f), new Vector3( 0.5f,  0.5f, -0.5f),
                // Top face
                new Vector3(-0.5f,  0.5f,  0.5f), new Vector3( 0.5f,  0.5f,  0.5f), new Vector3( 0.5f,  0.5f, -0.5f), new Vector3(-0.5f,  0.5f, -0.5f),
                // Bottom face
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3( 0.5f, -0.5f, -0.5f), new Vector3( 0.5f, -0.5f,  0.5f), new Vector3(-0.5f, -0.5f,  0.5f),
                // Right face
                new Vector3( 0.5f, -0.5f,  0.5f), new Vector3( 0.5f, -0.5f, -0.5f), new Vector3( 0.5f,  0.5f, -0.5f), new Vector3( 0.5f,  0.5f,  0.5f),
                // Left face
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-0.5f, -0.5f,  0.5f), new Vector3(-0.5f,  0.5f,  0.5f), new Vector3(-0.5f,  0.5f, -0.5f)
            };

            int[] triangles = new int[]
            {
                0, 1, 2,  0, 2, 3,      // Front
                4, 5, 6,  4, 6, 7,      // Back
                8, 9, 10, 8, 10, 11,    // Top
                12, 13, 14, 12, 14, 15, // Bottom
                16, 17, 18, 16, 18, 19, // Right
                20, 21, 22, 20, 22, 23  // Left
            };

            Vector2[] uvs = new Vector2[24];
            for (int i = 0; i < 6; i++)
            {
                uvs[i * 4 + 0] = new Vector2(0, 0);
                uvs[i * 4 + 1] = new Vector2(1, 0);
                uvs[i * 4 + 2] = new Vector2(1, 1);
                uvs[i * 4 + 3] = new Vector2(0, 1);
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            _cachedFallbackBoxMesh = mesh;
            return _cachedFallbackBoxMesh;
        }

        private void ApplyPlaceholderMesh(GameObject targetObject)
        {
            ExecuteOnMainThread(() =>
            {
                if (targetObject == null) return;
                Mesh placeholder = GetOrCreatePlaceholderWireframeMesh();
                ApplyMeshToObject(targetObject, placeholder, null);
            });
        }

        private void ApplyFallbackMesh(GameObject gameObject, Primitive primitive, GameObject meshHolder = null)
        {
            ExecuteOnMainThread(() =>
            {
                GameObject targetObject = meshHolder ?? gameObject;
                if (targetObject == null) return;

                if (primitive != null && targetObject.transform.localScale == Vector3.one)
                {
                    var omvScale = primitive.Scale;
                    if (omvScale.X > 0 && omvScale.Y > 0 && omvScale.Z > 0)
                    {
                        targetObject.transform.localScale = new Vector3(omvScale.X, omvScale.Y, omvScale.Z);
                    }
                }

                Mesh fallbackBox = GetOrCreateFallbackBoxMesh();
                ApplyMeshToObject(targetObject, fallbackBox, meshHolder);
            });
        }

        #endregion

        #region Mesh Request Processing

        public void RequestMesh(GameObject gameObject, Primitive primitive, UUID meshUuid, GameObject meshHolder)
        {
            if (gameObject == null || primitive == null)
            {
                _logger.LogWarning("Invalid parameters for mesh request");
                return;
            }

            GameObject targetObject = meshHolder ?? gameObject;

            // Check cache first
            if (_meshCache.TryGetValue(meshUuid, out Mesh cachedMesh))
            {
                SetRequestState(targetObject, meshUuid, MeshRequestState.Loaded);
                ApplyMeshToObject(gameObject, cachedMesh, meshHolder);
                return;
            }

            // Requirement 1: Assign a procedural wireframe bounding box placeholder immediately
            // Requirement 2: Track mesh loading state as Pending
            SetRequestState(targetObject, meshUuid, MeshRequestState.Pending);
            ApplyPlaceholderMesh(targetObject);

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
                        // Requirement 3: Network asset retrieval failure -> set state to Failed and apply persistent fallback bounding box mesh
                        _logger.LogWarning($"Failed to retrieve mesh asset {item.MeshUUID}. Applying persistent fallback bounding box mesh.");
                        GameObject targetObject = item.MeshHolder ?? item.GameObject;
                        SetRequestState(targetObject, item.MeshUUID, MeshRequestState.Failed);
                        ApplyFallbackMesh(item.GameObject, item.Primitive, item.MeshHolder);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing mesh request for {item.MeshUUID}. Applying persistent fallback bounding box mesh.");
                GameObject targetObject = item.MeshHolder ?? item.GameObject;
                SetRequestState(targetObject, item.MeshUUID, MeshRequestState.Failed);
                ApplyFallbackMesh(item.GameObject, item.Primitive, item.MeshHolder);
            }
        }

        private void ProcessMeshAsset(MeshQueueItem item, AssetMesh meshAsset)
        {
            try
            {
                // Process mesh on main thread
                ExecuteOnMainThread(() =>
                {
                    ProcessMeshOnMainThread(item, meshAsset);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process mesh asset {item.MeshUUID}");
                GameObject targetObject = item.MeshHolder ?? item.GameObject;
                SetRequestState(targetObject, item.MeshUUID, MeshRequestState.Failed);
                ApplyFallbackMesh(item.GameObject, item.Primitive, item.MeshHolder);
            }
        }

        private void ProcessMeshOnMainThread(MeshQueueItem item, AssetMesh meshAsset)
        {
            GameObject targetObject = item.MeshHolder ?? item.GameObject;
            try
            {
                if (!meshAsset.Decode())
                {
                    // Requirement 3: Mesh decode failure -> set state to Failed and transition to persistent fallback bounding box mesh
                    _logger.LogWarning($"Failed to decode mesh asset {item.MeshUUID}. Transitioning target object to fallback error mesh state.");
                    SetRequestState(targetObject, item.MeshUUID, MeshRequestState.Failed);
                    ApplyFallbackMesh(item.GameObject, item.Primitive, item.MeshHolder);
                    return;
                }

                Mesh unityMesh = ConvertToUnityMesh(meshAsset, item.Primitive);
                if (unityMesh != null)
                {
                    unityMesh.name = item.MeshUUID.ToString();
                    
                    // Cache the mesh
                    _meshCache[item.MeshUUID] = unityMesh;
                    
                    // Requirement 2: Set state to Loaded
                    SetRequestState(targetObject, item.MeshUUID, MeshRequestState.Loaded);

                    // Apply to game object
                    ApplyMeshToObject(item.GameObject, unityMesh, item.MeshHolder);
                    
                    _logger.LogDebug($"Successfully processed mesh {item.MeshUUID}");
                }
                else
                {
                    _logger.LogWarning($"Failed to convert mesh asset {item.MeshUUID}. Transitioning target object to fallback error mesh state.");
                    SetRequestState(targetObject, item.MeshUUID, MeshRequestState.Failed);
                    ApplyFallbackMesh(item.GameObject, item.Primitive, item.MeshHolder);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing mesh {item.MeshUUID} on main thread. Transitioning to fallback error mesh state.");
                SetRequestState(targetObject, item.MeshUUID, MeshRequestState.Failed);
                ApplyFallbackMesh(item.GameObject, item.Primitive, item.MeshHolder);
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
                if (targetObject == null) return;
                
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

        #endregion

        #region Sculpt Request Processing

        public void RequestSculpt(GameObject gameObject, Primitive primitive)
        {
            if (gameObject == null || primitive == null)
            {
                _logger.LogWarning("Invalid parameters for sculpt request");
                return;
            }

            try
            {
                UUID sculptTexture = primitive.Sculpt?.SculptTexture ?? UUID.Zero;
                
                // Requirement 1: Assign a procedural wireframe bounding box placeholder immediately
                // Requirement 2: Track sculpt request state as Pending
                SetRequestState(gameObject, sculptTexture, MeshRequestState.Pending);
                ApplyPlaceholderMesh(gameObject);

                // Request the sculpt texture
                _clientManagerService.Client.Assets.RequestImage(sculptTexture, (state, assetTexture) =>
                {
                    if (state == TextureRequestState.Finished && assetTexture?.AssetData != null)
                    {
                        ProcessSculptTexture(gameObject, primitive, assetTexture);
                    }
                    else
                    {
                        // Requirement 3: Sculpt texture retrieval failure -> set state to Failed and apply persistent fallback bounding box mesh
                        _logger.LogWarning($"Failed to retrieve sculpt texture {sculptTexture} (state: {state}). Applying persistent fallback bounding box mesh.");
                        SetRequestState(gameObject, sculptTexture, MeshRequestState.Failed);
                        ApplyFallbackMesh(gameObject, primitive);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to request sculpt texture. Applying fallback bounding box mesh.");
                UUID sculptTexture = primitive.Sculpt?.SculptTexture ?? UUID.Zero;
                SetRequestState(gameObject, sculptTexture, MeshRequestState.Failed);
                ApplyFallbackMesh(gameObject, primitive);
            }
        }

        private void ProcessSculptTexture(GameObject gameObject, Primitive primitive, AssetTexture assetTexture)
        {
            try
            {
                // Process sculpt on main thread
                ExecuteOnMainThread(() =>
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
                UUID sculptTexture = primitive.Sculpt?.SculptTexture ?? UUID.Zero;
                SetRequestState(gameObject, sculptTexture, MeshRequestState.Failed);
                ApplyFallbackMesh(gameObject, primitive);
            }
        }

        private void ProcessSculptQueue()
        {
            if (!_sculptQueue.TryDequeue(out SculptData sculptData))
                return;

            UUID sculptTexture = sculptData.Primitive?.Sculpt?.SculptTexture ?? UUID.Zero;

            try
            {
                // Create mesh from sculpt data
                Mesh sculptMesh = CreateSculptMesh(sculptData);
                if (sculptMesh != null)
                {
                    SetRequestState(sculptData.GameObject, sculptTexture, MeshRequestState.Loaded);
                    ApplyMeshToObject(sculptData.GameObject, sculptMesh, null);
                }
                else
                {
                    _logger.LogWarning("Sculpt mesh creation failed. Applying persistent fallback bounding box mesh.");
                    SetRequestState(sculptData.GameObject, sculptTexture, MeshRequestState.Failed);
                    ApplyFallbackMesh(sculptData.GameObject, sculptData.Primitive);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process sculpt from queue. Applying persistent fallback bounding box mesh.");
                SetRequestState(sculptData.GameObject, sculptTexture, MeshRequestState.Failed);
                ApplyFallbackMesh(sculptData.GameObject, sculptData.Primitive);
            }
        }

        private Mesh CreateSculptMesh(SculptData sculptData)
        {
            try
            {
                // Requirement 4: When an unsupported sculpt primitive type occurs, render a procedural fallback primitive box corresponding to primitive bounds with clear logging
                if (sculptData.Primitive.Sculpt.Type != SculptType.Sphere)
                {
                    _logger.LogWarning($"Unsupported sculpt primitive type: {sculptData.Primitive.Sculpt.Type}. Rendering procedural fallback primitive box corresponding to primitive bounds.");
                    UUID sculptTexture = sculptData.Primitive?.Sculpt?.SculptTexture ?? UUID.Zero;
                    SetRequestState(sculptData.GameObject, sculptTexture, MeshRequestState.Failed);

                    if (sculptData.Primitive != null && sculptData.GameObject != null && sculptData.GameObject.transform.localScale == Vector3.one)
                    {
                        var omvScale = sculptData.Primitive.Scale;
                        if (omvScale.X > 0 && omvScale.Y > 0 && omvScale.Z > 0)
                        {
                            sculptData.GameObject.transform.localScale = new Vector3(omvScale.X, omvScale.Y, omvScale.Z);
                        }
                    }
                    return GetOrCreateFallbackBoxMesh();
                }

                // Decode the sculpt texture data using CSJ2K
                RawBytesImageCreator.Register();
                var pi = J2kImage.FromBytes(sculptData.ImageData);
                if (pi == null)
                {
                    _logger.LogError("Failed to decode sculpt texture: J2kImage is null. Applying persistent fallback bounding box mesh.");
                    return GetOrCreateFallbackBoxMesh();
                }
                var rawImage = pi.As<RawBytesImage>();
                int width = rawImage.Width;
                int height = rawImage.Height;
                byte[] imageData = rawImage.Data;

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
                _logger.LogError(ex, "Failed to create sculpt mesh. Returning fallback bounding box mesh.");
                return GetOrCreateFallbackBoxMesh();
            }
        }

        #endregion

        #region Cache and Disposal

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
            _requestStates.Clear();

            if (_cachedPlaceholderWireframeMesh != null)
            {
                UnityEngine.Object.Destroy(_cachedPlaceholderWireframeMesh);
                _cachedPlaceholderWireframeMesh = null;
            }

            if (_cachedFallbackBoxMesh != null)
            {
                UnityEngine.Object.Destroy(_cachedFallbackBoxMesh);
                _cachedFallbackBoxMesh = null;
            }
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

        #endregion
    }
}
