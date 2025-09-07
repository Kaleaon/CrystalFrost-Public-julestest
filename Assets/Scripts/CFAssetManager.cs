using System;
using UnityEngine;
using OpenMetaverse;
using CrystalFrost;
using Microsoft.Extensions.Logging;
using CrystalFrost.Services;
using CrystalFrost.Assets;

namespace CrystalFrost
{
    /// <summary>
    /// Refactored CFAssetManager that coordinates between specialized asset managers
    /// Following Single Responsibility Principle and Composition over Inheritance
    /// 
    /// BEFORE: 1020-line monolithic class handling textures, materials, meshes, sculpts, caching
    /// AFTER: Lightweight coordinator delegating to specialized managers
    /// </summary>
    public class CFAssetManager : IDisposable
    {
        private readonly ILogger<CFAssetManager> _logger;
        private readonly IClientManagerService _clientManagerService;

        // Specialized managers
        private readonly TextureManager _textureManager;
        private readonly MaterialManager _materialManager;
        private readonly MeshManager _meshManager;

        // Backward compatibility properties
        public SimManager simManager;
        public Material zeroMaterial => _materialManager.ZeroMaterial;

        public CFAssetManager()
        {
            _logger = Services.GetService<ILogger<CFAssetManager>>();
            _clientManagerService = ClientManager.GetService();

            try
            {
                // Initialize specialized managers
                _textureManager = new TextureManager(
                    Services.GetService<ILogger<TextureManager>>(),
                    _clientManagerService
                );

                _materialManager = new MaterialManager(
                    Services.GetService<ILogger<MaterialManager>>(),
                    _clientManagerService,
                    _textureManager
                );

                _meshManager = new MeshManager(
                    Services.GetService<ILogger<MeshManager>>(),
                    _clientManagerService
                );

                _logger.LogInformation("CFAssetManager initialized with specialized managers");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize CFAssetManager");
                throw;
            }
        }

        #region Texture Management (delegates to TextureManager)

        /// <summary>
        /// Requests a texture by UUID, returning a placeholder white texture while loading
        /// </summary>
        public Texture2D RequestTexture(UUID uuid)
        {
            try
            {
                return _textureManager.RequestTexture(uuid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to request texture {uuid}");
                return _textureManager.GetWhiteTexture();
            }
        }

        /// <summary>
        /// Processes alpha channel for texture transparency
        /// </summary>
        public void ProcessAlphaTexture(UUID uuid)
        {
            try
            {
                _textureManager.ProcessAlphaTexture(uuid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process alpha for texture {uuid}");
            }
        }

        #endregion

        #region Material Management (delegates to MaterialManager)

        /// <summary>
        /// Requests a material for a specific texture and renderer configuration
        /// </summary>
        public Material RequestTexture(UUID uuid, Renderer renderer, int subMeshIndex, Color color, float glow, bool fullbright)
        {
            try
            {
                return _materialManager.RequestMaterial(uuid, renderer, subMeshIndex, color, glow, fullbright);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to request material for texture {uuid}");
                return _materialManager.ZeroMaterial;
            }
        }

        /// <summary>
        /// Updates a material's texture (for dynamic texture updates)
        /// </summary>
        public void UpdateMaterialTexture(UUID textureUuid, Texture2D newTexture)
        {
            try
            {
                _materialManager.UpdateMaterialTexture(textureUuid, newTexture);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to update material texture {textureUuid}");
            }
        }

        /// <summary>
        /// Removes a renderer from all materials (cleanup when object destroyed)
        /// </summary>
        public void RemoveRenderer(Renderer renderer)
        {
            try
            {
                _materialManager.RemoveRenderer(renderer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove renderer from materials");
            }
        }

        #endregion

        #region Mesh Management (delegates to MeshManager)

        /// <summary>
        /// Requests a mesh for a primitive object
        /// </summary>
        public void RequestMesh2(GameObject gameObject, Primitive primitive, UUID uuid, GameObject meshHolder)
        {
            try
            {
                _meshManager.RequestMesh(gameObject, primitive, uuid, meshHolder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to request mesh {uuid}");
            }
        }

        /// <summary>
        /// Requests sculpt processing for a primitive
        /// </summary>
        public void RequestSculpt(GameObject gameObject, Primitive prim)
        {
            try
            {
                _meshManager.RequestSculpt(gameObject, prim);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to request sculpt");
            }
        }

        #endregion

        #region Maintenance and Cleanup

        /// <summary>
        /// Performs periodic cleanup of unused resources
        /// </summary>
        public void PerformMaintenance()
        {
            try
            {
                _logger.LogDebug("Performing asset manager maintenance");
                
                _materialManager.CleanupUnusedMaterials();
                
                // Force garbage collection if needed
                if (System.GC.GetTotalMemory(false) > 100 * 1024 * 1024) // 100MB threshold
                {
                    System.GC.Collect();
                    _logger.LogDebug("Performed garbage collection");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during maintenance");
            }
        }

        /// <summary>
        /// Clears all caches and resets managers
        /// </summary>
        public void ClearAllCaches()
        {
            try
            {
                _logger.LogInformation("Clearing all asset caches");
                
                _textureManager.ClearCache();
                _meshManager.ClearCache();
                _materialManager.CleanupUnusedMaterials();
                
                _logger.LogInformation("All asset caches cleared");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing caches");
            }
        }

        #endregion

        #region Legacy Support Methods

        /// <summary>
        /// Legacy method signature for backward compatibility
        /// This was the main texture reinitialize callback in the original CFAssetManager
        /// </summary>
        public void MainThreadTextureReinitialize(byte[] bytes, UUID uuid, int width, int height, int components)
        {
            try
            {
                // This method is preserved for backward compatibility
                // In the refactored version, texture processing is handled internally by TextureManager
                _logger.LogDebug($"Legacy texture reinitialize called for {uuid} - delegating to TextureManager");
                
                // The actual texture processing is now handled by TextureManager's callback system
                // This method can be deprecated once all callers are updated
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in legacy texture reinitialize for {uuid}");
            }
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            _logger.LogInformation("Disposing CFAssetManager");

            try
            {
                // Dispose specialized managers in reverse order of creation
                _meshManager?.Dispose();
                _materialManager?.Dispose();
                _textureManager?.Dispose();

                _logger.LogInformation("CFAssetManager disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during CFAssetManager disposal");
            }
        }

        #endregion
    }
}