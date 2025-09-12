using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
                    UnityEngine.Object.Destroy(Material);
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
            try
            {
                _materialLock.EnterReadLock();

                // Check if material container already exists
                if (_materialContainers.TryGetValue(textureUuid, out MaterialContainer container))
                {
                    if (container.Material != null)
                    {
                        AddRendererToMaterial(textureUuid, renderer);
                        ApplyMaterialToRenderer(renderer, subMeshIndex, container.Material);
                        return container.Material;
                    }
                }
            }
            finally
            {
                _materialLock.ExitReadLock();
            }

            // Material doesn't exist, create it
            return CreateNewMaterial(textureUuid, renderer, subMeshIndex, color, glow, fullbright);
        }

        private Material CreateNewMaterial(UUID textureUuid, Renderer renderer, int subMeshIndex, Color color, float glow, bool fullbright)
        {
            try
            {
                _materialLock.EnterWriteLock();

                // Double-check pattern - another thread might have created it
                if (_materialContainers.TryGetValue(textureUuid, out MaterialContainer existingContainer) && existingContainer.Material != null)
                {
                    AddRendererToMaterial(textureUuid, renderer);
                    ApplyMaterialToRenderer(renderer, subMeshIndex, existingContainer.Material);
                    return existingContainer.Material;
                }

                // Create new material
                Material material = CreateMaterial(textureUuid, color, glow, fullbright);
                
                // Create or update material container
                MaterialContainer container = _materialContainers.GetOrAdd(textureUuid, _ => new MaterialContainer());
                container.Material = material;
                container.Renderers.Add(renderer);

                ApplyMaterialToRenderer(renderer, subMeshIndex, material);

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

        private void AddRendererToMaterial(UUID textureUuid, Renderer renderer)
        {
            if (_materialContainers.TryGetValue(textureUuid, out MaterialContainer container))
            {
                if (!container.Renderers.Contains(renderer))
                {
                    container.Renderers.Add(renderer);
                }
            }
        }

        private void ApplyMaterialToRenderer(Renderer renderer, int subMeshIndex, Material material)
        {
            try
            {
                if (renderer == null || material == null) return;

                Material[] materials = renderer.materials;
                
                if (subMeshIndex >= 0 && subMeshIndex < materials.Length)
                {
                    materials[subMeshIndex] = material;
                    renderer.materials = materials;
                }
                else
                {
                    _logger.LogWarning($"Invalid subMeshIndex {subMeshIndex} for renderer with {materials.Length} materials");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to apply material to renderer at index {subMeshIndex}");
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

                // Remove unused containers
                foreach (var uuid in toRemove)
                {
                    if (_materialContainers.TryRemove(uuid, out MaterialContainer container))
                    {
                        container.Dispose();
                    }
                }

                if (toRemove.Count > 0)
                {
                    _logger.LogInformation($"Cleaned up {toRemove.Count} unused material containers");
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

                // Dispose all material containers
                foreach (var container in _materialContainers.Values)
                {
                    container.Dispose();
                }
                _materialContainers.Clear();

                // Dispose default materials
                if (ZeroMaterial != null)
                {
                    UnityEngine.Object.Destroy(ZeroMaterial);
                    ZeroMaterial = null;
                }
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