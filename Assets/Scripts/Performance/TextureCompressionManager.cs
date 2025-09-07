using System;
using UnityEngine;
using Microsoft.Extensions.Logging;

namespace CrystalFrost.Performance
{
    /// <summary>
    /// Quality settings for texture optimization
    /// </summary>
    public enum TextureQuality
    {
        Low = 0,    // Aggressive compression, lower resolution
        Medium = 1, // Balanced compression and quality
        High = 2,   // Minimal compression, higher quality
        Ultra = 3   // No compression, maximum quality
    }

    /// <summary>
    /// Texture compression and optimization manager
    /// Automatically selects appropriate compression formats and settings based on content and platform
    /// </summary>
    public class TextureCompressionManager
    {
        private readonly ILogger<TextureCompressionManager> _logger;
        
        // Platform-specific compression format preferences
        private TextureFormat[] _preferredFormats;
        private TextureQuality _currentQuality = TextureQuality.Medium;
        
        // Performance thresholds
        private const int MAX_TEXTURE_SIZE_LOW = 256;
        private const int MAX_TEXTURE_SIZE_MEDIUM = 512;
        private const int MAX_TEXTURE_SIZE_HIGH = 1024;
        private const int MAX_TEXTURE_SIZE_ULTRA = 2048;

        public TextureCompressionManager(ILogger<TextureCompressionManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            InitializePlatformFormats();
        }

        private void InitializePlatformFormats()
        {
            // Select compression formats based on platform capabilities
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    _preferredFormats = new[] { TextureFormat.DXT5, TextureFormat.DXT1, TextureFormat.BC7 };
                    break;
                    
                case RuntimePlatform.Android:
                    _preferredFormats = new[] { TextureFormat.ETC2_RGBA8, TextureFormat.ETC_RGB4, TextureFormat.ASTC_6x6 };
                    break;
                    
                case RuntimePlatform.IPhonePlayer:
                    _preferredFormats = new[] { TextureFormat.ASTC_6x6, TextureFormat.PVRTC_RGBA4, TextureFormat.ETC2_RGBA8 };
                    break;
                    
                default:
                    _preferredFormats = new[] { TextureFormat.RGBA32 }; // Fallback to uncompressed
                    break;
            }
            
            _logger.LogInformation($"Initialized texture compression for {Application.platform} with {_preferredFormats.Length} preferred formats");
        }

        public void SetQuality(TextureQuality quality)
        {
            _currentQuality = quality;
            _logger.LogInformation($"Texture quality set to {quality}");
        }

        public Texture2D OptimizeTexture(Texture2D originalTexture, bool hasAlpha = true)
        {
            if (originalTexture == null)
                return originalTexture;

            try
            {
                // Determine target resolution based on quality
                int targetSize = GetTargetTextureSize(originalTexture.width, originalTexture.height);
                
                // Create optimized texture
                Texture2D optimizedTexture = CreateOptimizedTexture(originalTexture, targetSize, hasAlpha);
                
                _logger.LogDebug($"Optimized texture from {originalTexture.width}x{originalTexture.height} to {optimizedTexture.width}x{optimizedTexture.height}");
                
                return optimizedTexture;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to optimize texture");
                return originalTexture;
            }
        }

        private int GetTargetTextureSize(int width, int height)
        {
            int maxDimension = Mathf.Max(width, height);
            
            int maxSize = _currentQuality switch
            {
                TextureQuality.Low => MAX_TEXTURE_SIZE_LOW,
                TextureQuality.Medium => MAX_TEXTURE_SIZE_MEDIUM,
                TextureQuality.High => MAX_TEXTURE_SIZE_HIGH,
                TextureQuality.Ultra => MAX_TEXTURE_SIZE_ULTRA,
                _ => MAX_TEXTURE_SIZE_MEDIUM
            };

            // Don't upscale textures
            return Mathf.Min(maxDimension, maxSize);
        }

        private Texture2D CreateOptimizedTexture(Texture2D original, int targetSize, bool hasAlpha)
        {
            // Scale texture if needed
            Texture2D scaledTexture = ScaleTexture(original, targetSize);
            
            // Apply compression if quality allows
            if (_currentQuality != TextureQuality.Ultra)
            {
                TextureFormat targetFormat = SelectCompressionFormat(hasAlpha);
                
                if (targetFormat != TextureFormat.RGBA32)
                {
                    return CompressTexture(scaledTexture, targetFormat);
                }
            }

            return scaledTexture;
        }

        private Texture2D ScaleTexture(Texture2D original, int targetSize)
        {
            if (Mathf.Max(original.width, original.height) <= targetSize)
                return original;

            // Calculate new dimensions maintaining aspect ratio
            float aspectRatio = (float)original.width / original.height;
            int newWidth, newHeight;
            
            if (original.width > original.height)
            {
                newWidth = targetSize;
                newHeight = Mathf.RoundToInt(targetSize / aspectRatio);
            }
            else
            {
                newHeight = targetSize;
                newWidth = Mathf.RoundToInt(targetSize * aspectRatio);
            }

            // Create render texture for scaling
            RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(original, rt);

            // Create new texture
            Texture2D scaledTexture = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
            RenderTexture.active = rt;
            scaledTexture.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
            scaledTexture.Apply();
            
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            return scaledTexture;
        }

        private TextureFormat SelectCompressionFormat(bool hasAlpha)
        {
            foreach (var format in _preferredFormats)
            {
                // Check if format supports alpha if needed
                if (hasAlpha && !FormatSupportsAlpha(format))
                    continue;
                    
                // Check if format is supported on current platform
                if (SystemInfo.SupportsTextureFormat(format))
                {
                    return format;
                }
            }

            // Fallback to uncompressed
            return TextureFormat.RGBA32;
        }

        private bool FormatSupportsAlpha(TextureFormat format)
        {
            return format switch
            {
                TextureFormat.DXT1 => false,
                TextureFormat.DXT5 => true,
                TextureFormat.BC7 => true,
                TextureFormat.ETC_RGB4 => false,
                TextureFormat.ETC2_RGBA8 => true,
                TextureFormat.ASTC_6x6 => true,
                TextureFormat.PVRTC_RGBA4 => true,
                TextureFormat.RGBA32 => true,
                _ => false
            };
        }

        private Texture2D CompressTexture(Texture2D texture, TextureFormat format)
        {
            try
            {
                // Create compressed texture
                Texture2D compressedTexture = new Texture2D(texture.width, texture.height, format, false);
                
                // Copy pixel data and compress
                Color[] pixels = texture.GetPixels();
                compressedTexture.SetPixels(pixels);
                compressedTexture.Compress(true);
                compressedTexture.Apply();

                // Apply optimized settings
                compressedTexture.wrapMode = TextureWrapMode.Repeat;
                compressedTexture.filterMode = FilterMode.Bilinear;
                compressedTexture.anisoLevel = GetAnisoLevel();

                return compressedTexture;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to compress texture to {format}, using original");
                return texture;
            }
        }

        private int GetAnisoLevel()
        {
            return _currentQuality switch
            {
                TextureQuality.Low => 1,
                TextureQuality.Medium => 4,
                TextureQuality.High => 8,
                TextureQuality.Ultra => 16,
                _ => 4
            };
        }

        public void OptimizeTextureSettings(Texture2D texture, bool isUITexture = false, bool isMipMapped = true)
        {
            if (texture == null) return;

            try
            {
                // Set appropriate filter mode
                texture.filterMode = isUITexture ? FilterMode.Point : FilterMode.Bilinear;
                
                // Set wrap mode
                texture.wrapMode = isUITexture ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
                
                // Set anisotropic filtering
                texture.anisoLevel = isUITexture ? 0 : GetAnisoLevel();
                
                _logger.LogDebug($"Applied optimized settings to texture {texture.name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to optimize texture settings");
            }
        }

        public TextureQuality GetCurrentQuality()
        {
            return _currentQuality;
        }

        public void AutoAdjustQuality(float currentFPS, float targetFPS = 60f)
        {
            if (currentFPS < targetFPS * 0.8f) // If FPS drops below 80% of target
            {
                if (_currentQuality > TextureQuality.Low)
                {
                    SetQuality(_currentQuality - 1);
                    _logger.LogInformation($"Auto-reduced texture quality to {_currentQuality} due to low FPS ({currentFPS:F1})");
                }
            }
            else if (currentFPS > targetFPS * 1.1f) // If FPS is above 110% of target
            {
                if (_currentQuality < TextureQuality.Ultra)
                {
                    SetQuality(_currentQuality + 1);
                    _logger.LogInformation($"Auto-increased texture quality to {_currentQuality} due to high FPS ({currentFPS:F1})");
                }
            }
        }
    }
}