using System;
using UnityEngine;
using OpenMetaverse;
using CrystalFrost;
using Microsoft.Extensions.Logging;
using CrystalFrost.Services;
using CrystalFrost.Assets.Mesh;

namespace CrystalFrost.Assets
{
    /// <summary>
    /// Specialized manager for mesh processing and sculpt handling
    /// Delegates mesh requests to IAssetManager.Meshes and eliminates duplicate caches
    /// </summary>
    public class MeshManager : IDisposable
    {
        private readonly ILogger<MeshManager> _logger;
        private readonly IClientManagerService _clientManagerService;
        private readonly IMeshManager _engineMeshManager;
        private readonly IAssetManager _assetManager;

        public MeshManager(ILogger<MeshManager> logger, IClientManagerService clientManagerService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clientManagerService = clientManagerService ?? throw new ArgumentNullException(nameof(clientManagerService));
            
            _engineMeshManager = Services.GetService<IMeshManager>();
            _assetManager = Services.GetService<IAssetManager>();
        }

        public void RequestMesh(GameObject gameObject, Primitive primitive, UUID meshUuid, GameObject meshHolder)
        {
            if (gameObject == null || primitive == null)
            {
                _logger.LogWarning("Invalid parameters for mesh request");
                return;
            }

            // Forward request to IAssetManager.Meshes
            try
            {
                if (_assetManager?.Meshes != null)
                {
                    _assetManager.Meshes.RequestMesh(gameObject, primitive, meshUuid, meshHolder);
                }
                else
                {
                    _engineMeshManager?.RequestMesh(gameObject, primitive, meshUuid, meshHolder);
                }
                
                _logger.LogDebug($"Delegated mesh request for {meshUuid} to IAssetManager.Meshes");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to delegate mesh request for {meshUuid}");
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
                
                // Forward sculpt texture request to IAssetManager.Textures
                if (_assetManager?.Textures != null)
                {
                    _assetManager.Textures.RequestImage(sculptTexture);
                }
                
                _logger.LogDebug($"Delegated sculpt request for texture {sculptTexture} to IAssetManager.Textures");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to request sculpt texture");
            }
        }

        public void ClearCache()
        {
            _logger.LogInformation("Clearing mesh manager cache state");
        }

        public void Dispose()
        {
            _logger.LogInformation("Disposing MeshManager");
            ClearCache();
            _logger.LogInformation("MeshManager disposed");
        }
    }
}
