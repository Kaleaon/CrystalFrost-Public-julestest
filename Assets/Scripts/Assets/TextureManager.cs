using System;
using System.Collections.Concurrent;
using UnityEngine;
using OpenMetaverse;
using CrystalFrost;
using Microsoft.Extensions.Logging;
using CrystalFrost.Services;
using CrystalFrost.Performance;

namespace CrystalFrost.Assets
{
    /// <summary>
    /// Specialized manager for texture loading, processing and pooling
    /// Delegates raw download and decode tasks to IAssetManager.Textures
    /// </summary>
    public class TextureManager : IDisposable
    {
        private readonly ILogger<TextureManager> _logger;
        private readonly IClientManagerService _clientManagerService;
        private readonly TextureCompressionManager _compressionManager;
        private readonly ITextureManager _engineTextureManager;
        
        // Texture pooling for performance optimization
        private readonly ConcurrentQueue<Texture2D> _whiteTexturePool = new();
        private const int INITIAL_POOL_SIZE = 50;
        private const int MAX_POOL_SIZE = 200;

        public TextureManager(ILogger<TextureManager> logger, IClientManagerService clientManagerService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clientManagerService = clientManagerService ?? throw new ArgumentNullException(nameof(clientManagerService));
            
            _engineTextureManager = Services.GetService<ITextureManager>();

            // Subscribe to CFEngine ready textures queue
            if (_engineTextureManager?.ReadyTextures != null)
            {
                _engineTextureManager.ReadyTextures.ItemEnqueued += OnReadyTextureEnqueued;
            }

            // Initialize texture compression manager
            _compressionManager = new TextureCompressionManager(
                Services.GetService<ILogger<TextureCompressionManager>>()
            );
            
            InitializeTexturePool();
        }

        private void OnReadyTextureEnqueued(Textures.DecodedTexture decoded)
        {
            if (decoded == null) return;

            try
            {
                UnityMainThreadDispatcher.Instance()?.Enqueue(() =>
                {
                    ProcessDecodedTextureOnMainThread(decoded);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to enqueue decoded texture {decoded.UUID} for main thread processing");
            }
        }

        private void ProcessDecodedTextureOnMainThread(Textures.DecodedTexture decoded)
        {
            try
            {
                if (decoded?.Data == null || decoded.Width <= 0 || decoded.Height <= 0)
                {
                    return;
                }

                TextureFormat format = decoded.Components == 4 ? TextureFormat.RGBA32 : TextureFormat.RGB24;
                Texture2D texture = new Texture2D(decoded.Width, decoded.Height, format, false);
                
                try
                {
                    texture.LoadRawTextureData(decoded.Data);
                    texture.Apply();
                }
                catch
                {
                    // Fallback in case raw texture data was formatted differently
                    texture.LoadImage(decoded.Data);
                }

                texture.name = decoded.UUID.ToString();
                
                // Apply compression optimization
                texture = _compressionManager.OptimizeTexture(texture, HasAlphaChannel(texture));
                _compressionManager.OptimizeTextureSettings(texture, false, true);

                // Update materials with decoded texture
                _clientManagerService.AssetManager?.UpdateMaterialTexture(decoded.UUID, texture);

                _logger.LogDebug($"Successfully processed engine texture {decoded.UUID}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing decoded texture {decoded?.UUID} on main thread");
            }
        }

        private void InitializeTexturePool()
        {
            _logger.LogInformation("Initializing texture pool");
            
            for (int i = 0; i < INITIAL_POOL_SIZE; i++)
            {
                var whiteTexture = CreateWhiteTexture();
                _whiteTexturePool.Enqueue(whiteTexture);
            }
            
            _logger.LogInformation($"Texture pool initialized with {INITIAL_POOL_SIZE} white textures");
        }

        private Texture2D CreateWhiteTexture()
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return texture;
        }

        public Texture2D GetWhiteTexture()
        {
            if (_whiteTexturePool.TryDequeue(out Texture2D texture))
            {
                return texture;
            }
            
            // Pool exhausted, create new texture
            return CreateWhiteTexture();
        }

        public void ReturnWhiteTextureToPool(Texture2D texture)
        {
            if (texture == null) return;
            
            // Only return to pool if we haven't exceeded max size
            if (_whiteTexturePool.Count < MAX_POOL_SIZE)
            {
                _whiteTexturePool.Enqueue(texture);
            }
            else
            {
                // Pool is full, destroy the texture
                UnityEngine.Object.Destroy(texture);
            }
        }

        public Texture2D RequestTexture(UUID textureUuid)
        {
            if (textureUuid == UUID.Zero)
            {
                return GetWhiteTexture();
            }

            // Forward download and decode task to IAssetManager.Textures
            _engineTextureManager?.RequestImage(textureUuid);
            
            return GetWhiteTexture(); // Return placeholder white texture while background worker runs
        }

        private bool HasAlphaChannel(Texture2D texture)
        {
            try
            {
                Color[] samplePixels = texture.GetPixels(0, 0, Mathf.Min(32, texture.width), Mathf.Min(32, texture.height));
                
                for (int i = 0; i < samplePixels.Length; i += 4)
                {
                    if (samplePixels[i].a < 0.99f)
                    {
                        return true;
                    }
                }
                
                return false;
            }
            catch
            {
                return true;
            }
        }

        public void ProcessAlphaTexture(UUID textureUuid)
        {
            _logger.LogDebug($"ProcessAlphaTexture requested for {textureUuid}");
        }

        public void ClearCache()
        {
            _logger.LogInformation("Clearing texture manager cache state");
        }

        public void Dispose()
        {
            _logger.LogInformation("Disposing TextureManager");

            if (_engineTextureManager?.ReadyTextures != null)
            {
                _engineTextureManager.ReadyTextures.ItemEnqueued -= OnReadyTextureEnqueued;
            }
            
            ClearCache();
            
            // Clean up texture pool
            while (_whiteTexturePool.TryDequeue(out Texture2D texture))
            {
                if (texture != null)
                {
                    UnityEngine.Object.Destroy(texture);
                }
            }
            
            _logger.LogInformation("TextureManager disposed");
        }
    }
}
