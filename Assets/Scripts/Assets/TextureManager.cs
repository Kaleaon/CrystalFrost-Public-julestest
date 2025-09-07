using System;
using System.Collections.Concurrent;
using UnityEngine;
using OpenMetaverse;
using OpenMetaverse.Assets;
using Microsoft.Extensions.Logging;
using CrystalFrost.Services;

namespace CrystalFrost.Assets
{
    /// <summary>
    /// Specialized manager for texture loading, caching, processing and pooling
    /// Extracted from CFAssetManager to follow Single Responsibility Principle
    /// </summary>
    public class TextureManager : IDisposable
    {
        private readonly ILogger<TextureManager> _logger;
        private readonly IClientManagerService _clientManagerService;
        
        // Texture pooling for performance optimization
        private readonly ConcurrentQueue<Texture2D> _whiteTexturePool = new();
        private const int INITIAL_POOL_SIZE = 50;
        private const int MAX_POOL_SIZE = 200;

        // Texture caching
        private readonly ConcurrentDictionary<UUID, Texture2D> _textureCache = new();
        private readonly ConcurrentDictionary<UUID, bool> _processingTextures = new();

        public TextureManager(ILogger<TextureManager> logger, IClientManagerService clientManagerService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clientManagerService = clientManagerService ?? throw new ArgumentNullException(nameof(clientManagerService));
            
            InitializeTexturePool();
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

            // Check cache first
            if (_textureCache.TryGetValue(textureUuid, out Texture2D cachedTexture))
            {
                return cachedTexture;
            }

            // Check if already processing
            if (_processingTextures.ContainsKey(textureUuid))
            {
                return GetWhiteTexture(); // Return placeholder while processing
            }

            // Mark as processing and request from server
            _processingTextures[textureUuid] = true;
            RequestTextureFromServer(textureUuid);
            
            return GetWhiteTexture(); // Return placeholder
        }

        private void RequestTextureFromServer(UUID textureUuid)
        {
            try
            {
                _clientManagerService.Client.Assets.RequestImage(textureUuid, (state, assetTexture) =>
                {
                    ProcessTextureCallback(state, assetTexture);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to request texture {textureUuid}");
                _processingTextures.TryRemove(textureUuid, out _);
            }
        }

        private void ProcessTextureCallback(TextureRequestState state, AssetTexture assetTexture)
        {
            if (state != TextureRequestState.Finished || assetTexture?.AssetData == null)
            {
                _processingTextures.TryRemove(assetTexture?.AssetID ?? UUID.Zero, out _);
                return;
            }

            try
            {
                // Process on main thread
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    ProcessTextureOnMainThread(assetTexture);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process texture {assetTexture.AssetID}");
                _processingTextures.TryRemove(assetTexture.AssetID, out _);
            }
        }

        private void ProcessTextureOnMainThread(AssetTexture assetTexture)
        {
            try
            {
                byte[] imageData = assetTexture.AssetData;
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                
                if (texture.LoadImage(imageData))
                {
                    texture.name = assetTexture.AssetID.ToString();
                    
                    // Apply texture settings
                    texture.wrapMode = TextureWrapMode.Repeat;
                    texture.filterMode = FilterMode.Bilinear;
                    
                    // Cache the texture
                    _textureCache[assetTexture.AssetID] = texture;
                    
                    _logger.LogDebug($"Successfully processed texture {assetTexture.AssetID}");
                }
                else
                {
                    _logger.LogWarning($"Failed to load image data for texture {assetTexture.AssetID}");
                    UnityEngine.Object.Destroy(texture);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing texture {assetTexture.AssetID} on main thread");
            }
            finally
            {
                _processingTextures.TryRemove(assetTexture.AssetID, out _);
            }
        }

        public void ProcessAlphaTexture(UUID textureUuid)
        {
            if (!_textureCache.TryGetValue(textureUuid, out Texture2D texture))
            {
                _logger.LogWarning($"Cannot process alpha for texture {textureUuid} - not found in cache");
                return;
            }

            try
            {
                // Process alpha channel for transparency
                Color[] pixels = texture.GetPixels();
                bool hasAlpha = false;

                for (int i = 0; i < pixels.Length; i++)
                {
                    if (pixels[i].a < 1.0f)
                    {
                        hasAlpha = true;
                        break;
                    }
                }

                if (hasAlpha)
                {
                    _logger.LogDebug($"Texture {textureUuid} has alpha channel");
                    // Additional alpha processing can be added here
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing alpha for texture {textureUuid}");
            }
        }

        public void ClearCache()
        {
            _logger.LogInformation("Clearing texture cache");
            
            foreach (var texture in _textureCache.Values)
            {
                if (texture != null)
                {
                    UnityEngine.Object.Destroy(texture);
                }
            }
            
            _textureCache.Clear();
            _processingTextures.Clear();
        }

        public void Dispose()
        {
            _logger.LogInformation("Disposing TextureManager");
            
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