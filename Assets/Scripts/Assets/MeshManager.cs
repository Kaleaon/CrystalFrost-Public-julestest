using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using OpenMetaverse;
using CrystalFrost;
using CrystalFrost.Exceptions;
using Microsoft.Extensions.Logging;
using CrystalFrost.Services;
using CrystalFrost.Assets.Mesh;

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
    /// Specialized manager for mesh processing and sculpt handling
    /// Delegates mesh requests to IAssetManager.Meshes and eliminates duplicate caches
    /// </summary>
    public class MeshManager : IDisposable
    {
        private readonly ILogger<MeshManager> _logger;
        private readonly IClientManagerService _clientManagerService;
        private readonly IUserFriendlyExceptionMapper _exceptionMapper;

        public event Action<UserFriendlyMessage> OnAssetError;

        // Active request state tracking
        private readonly ConcurrentDictionary<string, MeshRequestState> _requestStates = new();

        // Static cached procedural fallback meshes (memory overhead < 5 KB, well below 1 MB guardrail)
        private static Mesh _cachedPlaceholderWireframeMesh;
        private static Mesh _cachedFallbackBoxMesh;

        private IMeshManager _engineMeshManager;
        private IAssetManager _assetManager;

        public MeshManager(ILogger<MeshManager> logger, IClientManagerService clientManagerService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clientManagerService = clientManagerService ?? throw new ArgumentNullException(nameof(clientManagerService));
            try
            {
                _exceptionMapper = Services.GetService<IUserFriendlyExceptionMapper>() ?? new UserFriendlyExceptionMapper();
            }
            catch
            {
                _exceptionMapper = new UserFriendlyExceptionMapper();
            }
        }

        private IMeshManager GetEngineMeshManager()
        {
            if (_engineMeshManager == null)
            {
                try
                {
                    _engineMeshManager = Services.GetService<IMeshManager>();
                }
                catch (Exception)
                {
                    // May not be registered or during test execution
                }
            }
            return _engineMeshManager;
        }

        private IAssetManager GetAssetManager()
        {
            if (_assetManager == null)
            {
                try
                {
                    _assetManager = Services.GetService<IAssetManager>();
                }
                catch (Exception)
                {
                    // May not be registered or during test execution
                }
            }
            return _assetManager;
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

                meshFilter.sharedMesh = mesh;

                _logger.LogDebug($"Applied mesh to {targetObject.name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply mesh to game object");
            }
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

            // Assign a procedural wireframe bounding box placeholder immediately
            // Track mesh loading state as Pending
            SetRequestState(targetObject, meshUuid, MeshRequestState.Pending);
            ApplyPlaceholderMesh(targetObject);

            // Forward request to IAssetManager.Meshes
            try
            {
                var assetMgr = GetAssetManager();
                if (assetMgr?.Meshes != null)
                {
                    assetMgr.Meshes.RequestMesh(gameObject, primitive, meshUuid, meshHolder);
                }
                else
                {
                    GetEngineMeshManager()?.RequestMesh(gameObject, primitive, meshUuid, meshHolder);
                }

                _logger.LogDebug($"Delegated mesh request for {meshUuid} to IAssetManager.Meshes");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to delegate mesh request for {meshUuid}. Applying persistent fallback bounding box mesh.");
                SetRequestState(targetObject, meshUuid, MeshRequestState.Failed);
                ApplyFallbackMesh(gameObject, primitive, meshHolder);
                var userMsg = _exceptionMapper?.Map(ex) ?? new UserFriendlyMessage("Content Loading Notice", "Some world content could not be displayed completely. Re-entering the region or checking your network connection may help.");
                OnAssetError?.Invoke(userMsg);
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

                // When an unsupported sculpt primitive type occurs, render a procedural fallback primitive box corresponding to primitive bounds with clear logging
                if (primitive.Sculpt != null && primitive.Sculpt.Type != SculptType.Sphere)
                {
                    _logger.LogWarning($"Unsupported sculpt primitive type: {primitive.Sculpt.Type}. Rendering procedural fallback primitive box corresponding to primitive bounds.");
                    SetRequestState(gameObject, sculptTexture, MeshRequestState.Failed);

                    if (gameObject.transform.localScale == Vector3.one)
                    {
                        var omvScale = primitive.Scale;
                        if (omvScale.X > 0 && omvScale.Y > 0 && omvScale.Z > 0)
                        {
                            gameObject.transform.localScale = new Vector3(omvScale.X, omvScale.Y, omvScale.Z);
                        }
                    }

                    Mesh fallbackBox = GetOrCreateFallbackBoxMesh();
                    ApplyMeshToObject(gameObject, fallbackBox, null);
                    return;
                }

                // Assign a procedural wireframe bounding box placeholder immediately
                // Track sculpt request state as Pending
                SetRequestState(gameObject, sculptTexture, MeshRequestState.Pending);
                ApplyPlaceholderMesh(gameObject);

                // Forward sculpt texture request to IAssetManager.Textures
                var assetMgr = GetAssetManager();
                if (assetMgr?.Textures != null)
                {
                    assetMgr.Textures.RequestImage(sculptTexture);
                }
                else if (_clientManagerService?.Client?.Assets != null)
                {
                    _clientManagerService.Client.Assets.RequestImage(sculptTexture, (state, assetTexture) =>
                    {
                        if (state != TextureRequestState.Finished || assetTexture?.AssetData == null)
                        {
                            _logger.LogWarning($"Failed to retrieve sculpt texture {sculptTexture} (state: {state}). Applying persistent fallback bounding box mesh.");
                            SetRequestState(gameObject, sculptTexture, MeshRequestState.Failed);
                            ApplyFallbackMesh(gameObject, primitive);
                            var userMsg = _exceptionMapper?.Map($"Failed to retrieve sculpt texture {sculptTexture}") ?? new UserFriendlyMessage("Content Loading Notice", "Some world content could not be displayed completely. Re-entering the region or checking your network connection may help.");
                            OnAssetError?.Invoke(userMsg);
                        }
                    });
                }

                _logger.LogDebug($"Delegated sculpt request for texture {sculptTexture} to IAssetManager.Textures");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to request sculpt texture. Applying fallback bounding box mesh.");
                UUID sculptTexture = primitive.Sculpt?.SculptTexture ?? UUID.Zero;
                SetRequestState(gameObject, sculptTexture, MeshRequestState.Failed);
                ApplyFallbackMesh(gameObject, primitive);
                var userMsg = _exceptionMapper?.Map(ex) ?? new UserFriendlyMessage("Content Loading Notice", "Some world content could not be displayed completely. Re-entering the region or checking your network connection may help.");
                OnAssetError?.Invoke(userMsg);
            }
        }

        #endregion

        #region Cache and Disposal

        public void ClearCache()
        {
            _logger.LogInformation("Clearing mesh manager cache state");
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
            _logger.LogInformation("MeshManager disposed");
        }

        #endregion
    }
}
