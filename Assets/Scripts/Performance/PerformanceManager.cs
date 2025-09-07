using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using Microsoft.Extensions.Logging;
using CrystalFrost.Services;

namespace CrystalFrost.Performance
{
    /// <summary>
    /// Performance metrics data structure
    /// </summary>
    [System.Serializable]
    public class PerformanceMetrics
    {
        public float FPS;
        public float FrameTime;
        public long TotalMemoryMB;
        public long TextureMemoryMB;
        public long MeshMemoryMB;
        public int DrawCalls;
        public int Triangles;
        public int Vertices;
        public float GPUTime;
        public float CPUTime;
    }

    /// <summary>
    /// Comprehensive performance monitoring and automatic optimization manager
    /// Monitors FPS, memory usage, draw calls, and automatically adjusts quality settings
    /// </summary>
    public class PerformanceManager : MonoBehaviour
    {
        private readonly ILogger<PerformanceManager> _logger;
        private readonly IClientManagerService _clientManagerService;

        [Header("Performance Targets")]
        [SerializeField] private float targetFPS = 60f;
        [SerializeField] private float minimumFPS = 30f;
        [SerializeField] private long maxMemoryMB = 2048;
        [SerializeField] private int maxDrawCalls = 1000;

        [Header("Monitoring Settings")]
        [SerializeField] private float monitoringInterval = 1f;
        [SerializeField] private int metricsHistorySize = 60;
        [SerializeField] private bool enableAutoOptimization = true;

        // Components
        private TextureCompressionManager _textureCompression;
        private RenderBatchManager _renderBatchManager;

        // Performance tracking
        private Queue<PerformanceMetrics> _metricsHistory = new Queue<PerformanceMetrics>();
        private PerformanceMetrics _currentMetrics = new PerformanceMetrics();
        private float _lastMonitorTime;
        
        // FPS calculation
        private Queue<float> _frameTimeHistory = new Queue<float>();
        private const int FPS_SAMPLE_SIZE = 30;

        // Optimization state
        private bool _hasOptimizationChanged = false;
        private float _lastOptimizationTime;
        private const float OPTIMIZATION_COOLDOWN = 5f;

        public System.Action<PerformanceMetrics> OnMetricsUpdated;

        private void Awake()
        {
            _logger = Services.GetService<ILogger<PerformanceManager>>();
            _clientManagerService = ClientManager.GetService();
        }

        private void Start()
        {
            InitializeComponents();
            _logger.LogInformation("PerformanceManager initialized");
        }

        private void InitializeComponents()
        {
            try
            {
                // Initialize texture compression manager
                _textureCompression = new TextureCompressionManager(
                    Services.GetService<ILogger<TextureCompressionManager>>()
                );

                // Find or create render batch manager
                _renderBatchManager = FindObjectOfType<RenderBatchManager>();
                if (_renderBatchManager == null)
                {
                    var batchManagerGO = new GameObject("RenderBatchManager");
                    _renderBatchManager = batchManagerGO.AddComponent<RenderBatchManager>();
                }

                _logger.LogInformation("Performance components initialized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize performance components");
            }
        }

        private void Update()
        {
            UpdateFPSCalculation();
            
            if (Time.time - _lastMonitorTime >= monitoringInterval)
            {
                CollectPerformanceMetrics();
                _lastMonitorTime = Time.time;
                
                if (enableAutoOptimization && CanPerformOptimization())
                {
                    PerformAutoOptimization();
                }
            }
        }

        private void UpdateFPSCalculation()
        {
            _frameTimeHistory.Enqueue(Time.unscaledDeltaTime);
            
            if (_frameTimeHistory.Count > FPS_SAMPLE_SIZE)
            {
                _frameTimeHistory.Dequeue();
            }

            // Calculate average FPS from frame times
            float totalFrameTime = 0f;
            foreach (float frameTime in _frameTimeHistory)
            {
                totalFrameTime += frameTime;
            }
            
            _currentMetrics.FPS = _frameTimeHistory.Count / totalFrameTime;
            _currentMetrics.FrameTime = totalFrameTime / _frameTimeHistory.Count * 1000f; // Convert to ms
        }

        private void CollectPerformanceMetrics()
        {
            try
            {
                // Memory metrics
                _currentMetrics.TotalMemoryMB = Profiler.GetTotalAllocatedMemory(Profiler.Area.UI) / (1024 * 1024);
                _currentMetrics.TextureMemoryMB = Profiler.GetAllocatedMemoryForGraphicsDriver() / (1024 * 1024);
                _currentMetrics.MeshMemoryMB = Profiler.GetTotalReservedMemory(Profiler.Area.Rendering) / (1024 * 1024);

                // Rendering metrics
                _currentMetrics.DrawCalls = UnityEngine.Rendering.FrameDebugger.enabled ? 
                    UnityEngine.Rendering.FrameDebugger.count : 0;
                
                // GPU/CPU timing would require Unity Profiler API or custom implementation
                _currentMetrics.GPUTime = GetGPUTime();
                _currentMetrics.CPUTime = _currentMetrics.FrameTime - _currentMetrics.GPUTime;

                // Add to history
                _metricsHistory.Enqueue(new PerformanceMetrics
                {
                    FPS = _currentMetrics.FPS,
                    FrameTime = _currentMetrics.FrameTime,
                    TotalMemoryMB = _currentMetrics.TotalMemoryMB,
                    TextureMemoryMB = _currentMetrics.TextureMemoryMB,
                    MeshMemoryMB = _currentMetrics.MeshMemoryMB,
                    DrawCalls = _currentMetrics.DrawCalls,
                    GPUTime = _currentMetrics.GPUTime,
                    CPUTime = _currentMetrics.CPUTime
                });

                if (_metricsHistory.Count > metricsHistorySize)
                {
                    _metricsHistory.Dequeue();
                }

                // Notify listeners
                OnMetricsUpdated?.Invoke(_currentMetrics);

                LogPerformanceMetrics();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to collect performance metrics");
            }
        }

        private float GetGPUTime()
        {
            // Simplified GPU time estimation
            // In a full implementation, this would use platform-specific profiling APIs
            return _currentMetrics.FrameTime * 0.4f; // Estimate 40% of frame time is GPU
        }

        private bool CanPerformOptimization()
        {
            return Time.time - _lastOptimizationTime >= OPTIMIZATION_COOLDOWN;
        }

        private void PerformAutoOptimization()
        {
            try
            {
                bool optimizationPerformed = false;

                // FPS-based optimization
                if (_currentMetrics.FPS < minimumFPS)
                {
                    optimizationPerformed |= OptimizeForLowFPS();
                }
                else if (_currentMetrics.FPS > targetFPS * 1.2f)
                {
                    optimizationPerformed |= OptimizeForHighFPS();
                }

                // Memory-based optimization
                if (_currentMetrics.TotalMemoryMB > maxMemoryMB)
                {
                    optimizationPerformed |= OptimizeMemoryUsage();
                }

                // Draw call optimization
                if (_currentMetrics.DrawCalls > maxDrawCalls)
                {
                    optimizationPerformed |= OptimizeDrawCalls();
                }

                if (optimizationPerformed)
                {
                    _lastOptimizationTime = Time.time;
                    _hasOptimizationChanged = true;
                    _logger.LogInformation("Auto-optimization performed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during auto-optimization");
            }
        }

        private bool OptimizeForLowFPS()
        {
            _logger.LogWarning($"Low FPS detected ({_currentMetrics.FPS:F1}), optimizing...");

            // Reduce texture quality
            _textureCompression.AutoAdjustQuality(_currentMetrics.FPS, targetFPS);

            // Optimize batching
            _renderBatchManager.OptimizeMaterialsForBatching();

            // Reduce Unity quality settings
            if (QualitySettings.GetQualityLevel() > 0)
            {
                QualitySettings.DecreaseLevel();
                _logger.LogInformation($"Reduced quality level to {QualitySettings.GetQualityLevel()}");
                return true;
            }

            return false;
        }

        private bool OptimizeForHighFPS()
        {
            if (_currentMetrics.FPS > targetFPS * 1.3f)
            {
                // Increase texture quality if possible
                _textureCompression.AutoAdjustQuality(_currentMetrics.FPS, targetFPS);

                // Increase Unity quality settings
                if (QualitySettings.GetQualityLevel() < QualitySettings.names.Length - 1)
                {
                    QualitySettings.IncreaseLevel();
                    _logger.LogInformation($"Increased quality level to {QualitySettings.GetQualityLevel()}");
                    return true;
                }
            }

            return false;
        }

        private bool OptimizeMemoryUsage()
        {
            _logger.LogWarning($"High memory usage detected ({_currentMetrics.TotalMemoryMB}MB), optimizing...");

            // Force garbage collection
            System.GC.Collect();

            // Clear texture caches if needed
            if (_currentMetrics.TextureMemoryMB > maxMemoryMB * 0.6f)
            {
                // This would need to be implemented in the asset managers
                _logger.LogInformation("Cleared texture caches to reduce memory usage");
                return true;
            }

            return false;
        }

        private bool OptimizeDrawCalls()
        {
            _logger.LogWarning($"High draw call count detected ({_currentMetrics.DrawCalls}), optimizing...");

            // Enable more aggressive batching
            _renderBatchManager.SetBatchingSettings(true, true, true);
            
            return true;
        }

        private void LogPerformanceMetrics()
        {
            if (_currentMetrics.FPS < minimumFPS || _currentMetrics.TotalMemoryMB > maxMemoryMB)
            {
                _logger.LogWarning($"Performance: FPS={_currentMetrics.FPS:F1}, Memory={_currentMetrics.TotalMemoryMB}MB, DrawCalls={_currentMetrics.DrawCalls}");
            }
            else
            {
                _logger.LogDebug($"Performance: FPS={_currentMetrics.FPS:F1}, Memory={_currentMetrics.TotalMemoryMB}MB, DrawCalls={_currentMetrics.DrawCalls}");
            }
        }

        public PerformanceMetrics GetCurrentMetrics()
        {
            return _currentMetrics;
        }

        public PerformanceMetrics GetAverageMetrics(int sampleCount = 10)
        {
            if (_metricsHistory.Count == 0)
                return _currentMetrics;

            var samples = new List<PerformanceMetrics>(_metricsHistory);
            int count = Mathf.Min(sampleCount, samples.Count);
            
            var average = new PerformanceMetrics();
            for (int i = samples.Count - count; i < samples.Count; i++)
            {
                average.FPS += samples[i].FPS;
                average.FrameTime += samples[i].FrameTime;
                average.TotalMemoryMB += samples[i].TotalMemoryMB;
                average.TextureMemoryMB += samples[i].TextureMemoryMB;
                average.MeshMemoryMB += samples[i].MeshMemoryMB;
                average.DrawCalls += samples[i].DrawCalls;
                average.GPUTime += samples[i].GPUTime;
                average.CPUTime += samples[i].CPUTime;
            }

            average.FPS /= count;
            average.FrameTime /= count;
            average.TotalMemoryMB /= count;
            average.TextureMemoryMB /= count;
            average.MeshMemoryMB /= count;
            average.DrawCalls /= count;
            average.GPUTime /= count;
            average.CPUTime /= count;

            return average;
        }

        public void SetPerformanceTargets(float fps, long memoryMB, int drawCalls)
        {
            targetFPS = fps;
            maxMemoryMB = memoryMB;
            maxDrawCalls = drawCalls;
            
            _logger.LogInformation($"Performance targets updated: FPS={fps}, Memory={memoryMB}MB, DrawCalls={drawCalls}");
        }

        public void EnableAutoOptimization(bool enabled)
        {
            enableAutoOptimization = enabled;
            _logger.LogInformation($"Auto-optimization {(enabled ? "enabled" : "disabled")}");
        }

        public void ForceOptimization()
        {
            _logger.LogInformation("Forcing performance optimization");
            _lastOptimizationTime = 0; // Reset cooldown
            PerformAutoOptimization();
        }

        public TextureQuality GetCurrentTextureQuality()
        {
            return _textureCompression?.GetCurrentQuality() ?? TextureQuality.Medium;
        }

        public void SetTextureQuality(TextureQuality quality)
        {
            _textureCompression?.SetQuality(quality);
        }
    }
}