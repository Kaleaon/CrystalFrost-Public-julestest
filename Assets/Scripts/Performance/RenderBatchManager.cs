using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Microsoft.Extensions.Logging;
using OpenMetaverse;
using CrystalFrost.Services;

namespace CrystalFrost.Performance
{
    /// <summary>
    /// Batch information for objects that can be batched together
    /// </summary>
    public class RenderBatch
    {
        public Material Material { get; set; }
        public Mesh Mesh { get; set; }
        public List<Matrix4x4> Transforms { get; } = new List<Matrix4x4>();
        public List<GameObject> Objects { get; } = new List<GameObject>();
        public int MaxInstanceCount { get; set; } = 1023; // GPU instancing limit
    }

    /// <summary>
    /// Performance manager for reducing draw calls through batching and GPU instancing
    /// Analyzes scene objects and groups them for efficient rendering
    /// </summary>
    public class RenderBatchManager : MonoBehaviour
    {
        private readonly ILogger<RenderBatchManager> _logger;
        private readonly IClientManagerService _clientManagerService;

        // Batching configuration
        [SerializeField] private bool enableGPUInstancing = true;
        [SerializeField] private bool enableStaticBatching = true;
        [SerializeField] private bool enableDynamicBatching = true;
        [SerializeField] private int minObjectsForBatching = 3;
        [SerializeField] private float maxDistanceForBatching = 100f;

        // Batch tracking
        private Dictionary<string, RenderBatch> _renderBatches = new Dictionary<string, RenderBatch>();
        private Dictionary<GameObject, string> _objectToBatch = new Dictionary<GameObject, string>();
        
        // Performance monitoring
        private int _lastFrameDrawCalls = 0;
        private int _batchedDrawCalls = 0;

        private void Awake()
        {
            _logger = Services.GetService<ILogger<RenderBatchManager>>();
            _clientManagerService = ClientManager.GetService();
        }

        private void Start()
        {
            // Enable Unity's built-in batching optimizations
            if (enableStaticBatching)
            {
                StaticBatchingUtility.Combine(FindStaticGameObjects());
            }
            
            _logger.LogInformation("RenderBatchManager initialized");
        }

        public void RegisterObjectForBatching(GameObject obj, Material material, Mesh mesh)
        {
            if (obj == null || material == null || mesh == null)
                return;

            try
            {
                string batchKey = GenerateBatchKey(material, mesh);
                
                // Create batch if it doesn't exist
                if (!_renderBatches.TryGetValue(batchKey, out RenderBatch batch))
                {
                    batch = new RenderBatch
                    {
                        Material = material,
                        Mesh = mesh
                    };
                    _renderBatches[batchKey] = batch;
                }

                // Add object to batch
                batch.Objects.Add(obj);
                batch.Transforms.Add(obj.transform.localToWorldMatrix);
                _objectToBatch[obj] = batchKey;

                // Check if we should enable instancing for this batch
                if (batch.Objects.Count >= minObjectsForBatching && enableGPUInstancing)
                {
                    EnableGPUInstancing(batch);
                }

                _logger.LogDebug($"Registered {obj.name} for batching with key {batchKey}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to register object {obj.name} for batching");
            }
        }

        public void UnregisterObjectFromBatching(GameObject obj)
        {
            if (obj == null || !_objectToBatch.TryGetValue(obj, out string batchKey))
                return;

            try
            {
                if (_renderBatches.TryGetValue(batchKey, out RenderBatch batch))
                {
                    int index = batch.Objects.IndexOf(obj);
                    if (index >= 0)
                    {
                        batch.Objects.RemoveAt(index);
                        batch.Transforms.RemoveAt(index);
                    }

                    // Remove batch if empty
                    if (batch.Objects.Count == 0)
                    {
                        _renderBatches.Remove(batchKey);
                    }
                }

                _objectToBatch.Remove(obj);
                _logger.LogDebug($"Unregistered {obj.name} from batching");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to unregister object {obj.name} from batching");
            }
        }

        private string GenerateBatchKey(Material material, Mesh mesh)
        {
            // Create unique key for material+mesh combination
            int materialHash = material.GetInstanceID();
            int meshHash = mesh.GetInstanceID();
            return $"{materialHash}_{meshHash}";
        }

        private void EnableGPUInstancing(RenderBatch batch)
        {
            try
            {
                // Enable GPU instancing on the material if supported
                if (batch.Material.enableInstancing == false && SystemInfo.supportsInstancing)
                {
                    // Create instanced version of material
                    Material instancedMaterial = new Material(batch.Material);
                    instancedMaterial.enableInstancing = true;
                    instancedMaterial.name = batch.Material.name + "_Instanced";
                    
                    batch.Material = instancedMaterial;
                    
                    _logger.LogInformation($"Enabled GPU instancing for batch with {batch.Objects.Count} objects");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enable GPU instancing");
            }
        }

        private void Update()
        {
            if (enableGPUInstancing)
            {
                ProcessGPUInstancing();
            }
            
            MonitorPerformance();
        }

        private void ProcessGPUInstancing()
        {
            _batchedDrawCalls = 0;
            
            foreach (var batch in _renderBatches.Values)
            {
                if (batch.Objects.Count < minObjectsForBatching)
                    continue;

                try
                {
                    // Update transforms for moved objects
                    UpdateBatchTransforms(batch);
                    
                    // Render with GPU instancing in chunks
                    RenderBatchWithInstancing(batch);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing GPU instancing batch");
                }
            }
        }

        private void UpdateBatchTransforms(RenderBatch batch)
        {
            for (int i = 0; i < batch.Objects.Count; i++)
            {
                if (batch.Objects[i] != null)
                {
                    batch.Transforms[i] = batch.Objects[i].transform.localToWorldMatrix;
                }
            }
        }

        private void RenderBatchWithInstancing(RenderBatch batch)
        {
            if (batch.Objects.Count == 0) return;

            // Split into chunks if exceeding GPU instancing limits
            int objectCount = batch.Objects.Count;
            int chunks = Mathf.CeilToInt((float)objectCount / batch.MaxInstanceCount);

            for (int chunk = 0; chunk < chunks; chunk++)
            {
                int startIndex = chunk * batch.MaxInstanceCount;
                int count = Mathf.Min(batch.MaxInstanceCount, objectCount - startIndex);

                if (count <= 0) continue;

                // Get transform matrices for this chunk
                Matrix4x4[] matrices = new Matrix4x4[count];
                for (int i = 0; i < count; i++)
                {
                    matrices[i] = batch.Transforms[startIndex + i];
                }

                // Disable individual renderers to avoid double rendering
                DisableRenderersInBatch(batch, startIndex, count);

                // Render using GPU instancing
                Graphics.DrawMeshInstanced(
                    batch.Mesh,
                    0, // submesh index
                    batch.Material,
                    matrices,
                    count
                );

                _batchedDrawCalls++;
            }
        }

        private void DisableRenderersInBatch(RenderBatch batch, int startIndex, int count)
        {
            for (int i = startIndex; i < startIndex + count; i++)
            {
                if (batch.Objects[i] != null)
                {
                    var renderer = batch.Objects[i].GetComponent<Renderer>();
                    if (renderer != null && renderer.enabled)
                    {
                        renderer.enabled = false;
                    }
                }
            }
        }

        private GameObject[] FindStaticGameObjects()
        {
            // Find static objects suitable for static batching
            return GameObject.FindGameObjectsWithTag("Static")
                .Where(obj => obj.GetComponent<Renderer>() != null)
                .ToArray();
        }

        public void OptimizeMaterialsForBatching()
        {
            try
            {
                var materials = FindObjectsOfType<Renderer>()
                    .SelectMany(r => r.materials)
                    .Distinct()
                    .ToList();

                foreach (var material in materials)
                {
                    // Enable GPU instancing if supported
                    if (SystemInfo.supportsInstancing && !material.enableInstancing)
                    {
                        material.enableInstancing = true;
                        _logger.LogDebug($"Enabled instancing for material {material.name}");
                    }

                    // Optimize material properties for batching
                    OptimizeMaterialForBatching(material);
                }

                _logger.LogInformation($"Optimized {materials.Count} materials for batching");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to optimize materials for batching");
            }
        }

        private void OptimizeMaterialForBatching(Material material)
        {
            // Remove per-material variations that break batching
            if (material.HasProperty("_MainTex"))
            {
                // Ensure texture tiling is consistent
                Vector2 tiling = material.GetTextureScale("_MainTex");
                if (tiling != Vector2.one)
                {
                    material.SetTextureScale("_MainTex", Vector2.one);
                }
            }
        }

        private void MonitorPerformance()
        {
            // Simple draw call monitoring
            _lastFrameDrawCalls = UnityEngine.Profiling.Profiler.GetRuntimeMemorySize("Draw Calls");
            
            // Log performance metrics periodically
            if (Time.frameCount % 300 == 0) // Every 5 seconds at 60 FPS
            {
                _logger.LogDebug($"Batching Performance: {_batchedDrawCalls} batched draws, {_renderBatches.Count} active batches");
            }
        }

        public void ClearAllBatches()
        {
            try
            {
                _logger.LogInformation("Clearing all render batches");
                
                // Re-enable disabled renderers
                foreach (var batch in _renderBatches.Values)
                {
                    foreach (var obj in batch.Objects)
                    {
                        if (obj != null)
                        {
                            var renderer = obj.GetComponent<Renderer>();
                            if (renderer != null)
                            {
                                renderer.enabled = true;
                            }
                        }
                    }
                }

                _renderBatches.Clear();
                _objectToBatch.Clear();
                _batchedDrawCalls = 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing render batches");
            }
        }

        public void SetBatchingSettings(bool gpuInstancing, bool staticBatching, bool dynamicBatching)
        {
            enableGPUInstancing = gpuInstancing;
            enableStaticBatching = staticBatching;
            enableDynamicBatching = dynamicBatching;
            
            _logger.LogInformation($"Batching settings updated: GPU={gpuInstancing}, Static={staticBatching}, Dynamic={dynamicBatching}");
        }

        public (int batchCount, int totalObjects, int batchedDrawCalls) GetBatchingStats()
        {
            int totalObjects = _renderBatches.Values.Sum(b => b.Objects.Count);
            return (_renderBatches.Count, totalObjects, _batchedDrawCalls);
        }

        private void OnDestroy()
        {
            ClearAllBatches();
        }
    }
}