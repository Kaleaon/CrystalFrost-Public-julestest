using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;
using OpenMetaverse;
using Microsoft.Extensions.Logging;
using CrystalFrost.Services;

namespace CrystalFrost.Assets
{
    /// <summary>
    /// Container for material and its associated renderers
    /// Enables efficient material updates and cleanup
    /// </summary>
    public class MaterialContainer : IDisposable
    {
        public Material Material { get; set; }
        public List<Renderer> Renderers { get; } = new List<Renderer>();
        public bool Disposed { get; private set; }

        public void Dispose()
        {
            if (!Disposed)
            {
                if (Material != null)
                {
                    if (Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(Material);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(Material);
                    }
                    Material = null;
                }
                Renderers.Clear();
                Disposed = true;
            }
        }
    }

    /// <summary>
    /// Specialized manager for material creation, caching, and renderer management
    /// Extracted from CFAssetManager to follow Single Responsibility Principle
    /// </summary>
    public class MaterialManager : IDisposable
    {
        private readonly ILogger<MaterialManager> _logger;
        private readonly IClientManagerService _clientManagerService;
        private readonly TextureManager _textureManager;

        // Thread-safe material container management
        private readonly ConcurrentDictionary<UUID, MaterialContainer> _materialContainers = new();
        private readonly ReaderWriterLockSlim _materialLock = new(LockRecursionPolicy.NoRecursion);

        // Thread-safe material binding queue for main-thread batch execution
        public ConcurrentQueue<MaterialBindingRequest> MaterialBindingQueue { get; } = new ConcurrentQueue<MaterialBindingRequest>();
        public ConcurrentQueue<MaterialBindingRequest> BindingQueue => MaterialBindingQueue;

        // Default materials
        public Material ZeroMaterial { get; private set; }

        public MaterialManager(ILogger<MaterialManager> logger, IClientManagerService clientManagerService, TextureManager textureManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clientManagerService = clientManagerService ?? throw new ArgumentNullException(nameof(clientManagerService));
            _textureManager = textureManager ?? throw new ArgumentNullException(nameof(textureManager));

            InitializeDefaultMaterials();
        }

        private void InitializeDefaultMaterials()
        {
            try
            {
                // Create default zero material
                ZeroMaterial = new Material(Shader.Find("Standard"));
                ZeroMaterial.name = "ZeroMaterial";
                ZeroMaterial.color = Color.white;

                _logger.LogInformation("Default materials initialized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize default materials");
                throw;
            }
        }

        public Material RequestMaterial(UUID textureUuid, Renderer renderer, int subMeshIndex, Color color, float glow, bool fullbright)
        {
            Material material = GetOrCreateMaterial(textureUuid, color, glow, fullbright);

            if (renderer != null)
            {
                // Enqueue request for main-thread application to prevent threading race conditions
                MaterialBindingQueue.Enqueue(new MaterialBindingRequest(renderer, subMeshIndex, material, textureUuid, color, glow, fullbright));
            }

            return material;
        }

        private Material GetOrCreateMaterial(UUID textureUuid, Color color, float glow, bool fullbright)
        {
            try
            {
                _materialLock.EnterReadLock();

                // Check if material container already exists
                if (_materialContainers.TryGetValue(textureUuid, out MaterialContainer container))
                {
                    if (container.Material != null)
                    {
                        return container.Material;
                    }
                }
            }
            finally
            {
                _materialLock.ExitReadLock();
            }

            // Material doesn't exist, create it
            return CreateNewMaterial(textureUuid, color, glow, fullbright);
        }

        private Material CreateNewMaterial(UUID textureUuid, Color color, float glow, bool fullbright)
        {
            try
            {
                _materialLock.EnterWriteLock();

                // Double-check pattern - another thread might have created it
                if (_materialContainers.TryGetValue(textureUuid, out MaterialContainer existingContainer) && existingContainer.Material != null)
                {
                    return existingContainer.Material;
                }

                // Create new material
                Material material = CreateMaterial(textureUuid, color, glow, fullbright);
                
                // Create or update material container
                MaterialContainer container = _materialContainers.GetOrAdd(textureUuid, _ => new MaterialContainer());
                container.Material = material;

                _logger.LogDebug($"Created new material for texture {textureUuid}");
                return material;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to create material for texture {textureUuid}");
                return ZeroMaterial;
            }
            finally
            {
                _materialLock.ExitWriteLock();
            }
        }

        private Material CreateMaterial(UUID textureUuid, Color color, float glow, bool fullbright)
        {
            // Get base shader name from ClientManager settings
            string shaderName = "Standard";
            
#if MK_GLOW_PRESENT
            shaderName = "MK/Glow/Standard";
#endif

            Material material = new Material(Shader.Find(shaderName));
            material.name = $"{_clientManagerService.MaterialNameModifier}{textureUuid}";

            // Get texture from TextureManager
            Texture2D texture = _textureManager.RequestTexture(textureUuid);
            if (texture != null)
            {
                material.SetTexture(_clientManagerService.DiffuseName, texture);
            }

            // Apply color and properties
            material.SetColor(_clientManagerService.ColorName, color);

            // Handle glow/emission
            if (glow > 0 || fullbright)
            {
                material.SetColor(_clientManagerService.EmissiveColorName, color * glow);
                if (texture != null)
                {
                    material.SetTexture(_clientManagerService.EmissiveMapName, texture);
                }
            }

            // Handle fullbright
            if (fullbright)
            {
                material.SetFloat("_Mode", 1); // Set to transparent mode for fullbright
            }

            return material;
        }

        /// <summary>
        /// Processes enqueued material binding requests on the main thread using sharedMaterials.
        /// Enforces frame budget limit in milliseconds to prevent stuttering during streaming spikes.
        /// </summary>
        public int ProcessMaterialQueue(float maxExecutionTimeMs = 2.0f)
        {
            int processedCount = 0;
            var stopwatch = Stopwatch.StartNew();

            while (MaterialBindingQueue.TryDequeue(out MaterialBindingRequest request))
            {
                if (request == null || request.Renderer == null)
                {
                    continue;
                }

                Material materialToApply = request.Material;
                if (materialToApply == null && request.TextureUuid != UUID.Zero)
                {
                    if (_materialContainers.TryGetValue(request.TextureUuid, out MaterialContainer container) && container.Material != null)
                    {
                        materialToApply = container.Material;
                    }
                }

                if (materialToApply == null)
                {
                    materialToApply = ZeroMaterial;
                }

                ApplyMaterialToRendererShared(request.Renderer, request.SubMeshIndex, materialToApply);

                // Update container.Renderers collection exclusively on main thread
                if (request.TextureUuid != UUID.Zero && _materialContainers.TryGetValue(request.TextureUuid, out MaterialContainer matContainer))
                {
                    if (!matContainer.Renderers.Contains(request.Renderer))
                    {
                        matContainer.Renderers.Add(request.Renderer);
                    }
                }

                processedCount++;

                if (stopwatch.Elapsed.TotalMilliseconds >= maxExecutionTimeMs)
                {
                    break;
                }
            }

            return processedCount;
        }

        public int ProcessQueue(float maxExecutionTimeMs = 2.0f) => ProcessMaterialQueue(maxExecutionTimeMs);

        private void ApplyMaterialToRendererShared(Renderer renderer, int subMeshIndex, Material material)
        {
            try
            {
                if (renderer == null || material == null) return;

                Material[] sharedMaterials = renderer.sharedMaterials;
                if (sharedMaterials == null || sharedMaterials.Length == 0)
                {
                    sharedMaterials = new Material[Mathf.Max(1, subMeshIndex + 1)];
                }
                else if (subMeshIndex >= sharedMaterials.Length)
                {
                    Array.Resize(ref sharedMaterials, subMeshIndex + 1);
                }

                if (subMeshIndex >= 0 && subMeshIndex < sharedMaterials.Length)
                {
                    sharedMaterials[subMeshIndex] = material;
                    renderer.sharedMaterials = sharedMaterials;
                }
                else
                {
                    _logger.LogWarning($"Invalid subMeshIndex {subMeshIndex} for renderer with {sharedMaterials.Length} shared materials");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to apply shared material to renderer at index {subMeshIndex}");
            }
        }

        public void UpdateMaterialTexture(UUID textureUuid, Texture2D newTexture)
        {
            try
            {
                _materialLock.EnterReadLock();

                if (_materialContainers.TryGetValue(textureUuid, out MaterialContainer container) && container.Material != null)
                {
                    container.Material.SetTexture(_clientManagerService.DiffuseName, newTexture);
                    _logger.LogDebug($"Updated material texture for {textureUuid}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to update material texture for {textureUuid}");
            }
            finally
            {
                _materialLock.ExitReadLock();
            }
        }

        public void RemoveRenderer(Renderer renderer)
        {
            try
            {
                _materialLock.EnterWriteLock();

                foreach (var container in _materialContainers.Values)
                {
                    container.Renderers.Remove(renderer);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove renderer from materials");
            }
            finally
            {
                _materialLock.ExitWriteLock();
            }
        }

        public void CleanupUnusedMaterials()
        {
            try
            {
                _materialLock.EnterWriteLock();
                
                var toRemove = new List<UUID>();

                foreach (var kvp in _materialContainers)
                {
                    var container = kvp.Value;
                    
                    // Remove null renderers
                    container.Renderers.RemoveAll(r => r == null);
                    
                    // If no renderers left, mark for removal
                    if (container.Renderers.Count == 0)
                    {
                        toRemove.Add(kvp.Key);
                    }
                }

                // Remove unused containers and destroy materials explicitly
                foreach (var uuid in toRemove)
                {
                    if (_materialContainers.TryRemove(uuid, out MaterialContainer container))
                    {
                        container.Dispose();
                    }
                }

                // Explicitly unload unused graphics assets from GPU memory
                Resources.UnloadUnusedAssets();

                if (toRemove.Count > 0)
                {
                    _logger.LogInformation($"Cleaned up {toRemove.Count} unused material containers and released native graphics assets");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup unused materials");
            }
            finally
            {
                _materialLock.ExitWriteLock();
            }
        }

        public void Dispose()
        {
            _logger.LogInformation("Disposing MaterialManager");

            try
            {
                _materialLock.EnterWriteLock();

                while (MaterialBindingQueue.TryDequeue(out _)) { }

                // Dispose all material containers
                foreach (var container in _materialContainers.Values)
                {
                    container.Dispose();
                }
                _materialContainers.Clear();

                // Dispose default materials
                if (ZeroMaterial != null)
                {
                    if (Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(ZeroMaterial);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(ZeroMaterial);
                    }
                    ZeroMaterial = null;
                }

                Resources.UnloadUnusedAssets();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during MaterialManager disposal");
            }
            finally
            {
                _materialLock.ExitWriteLock();
                _materialLock.Dispose();
            }

            _logger.LogInformation("MaterialManager disposed");
        }
    }
}