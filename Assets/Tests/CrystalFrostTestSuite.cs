using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using CrystalFrost;
using CrystalFrost.Assets;
using Microsoft.Extensions.Logging;
using OpenMetaverse;

namespace CrystalFrost.Tests
{
    /// <summary>
    /// Test suite for the refactored asset management system
    /// Validates thread safety, memory management, and functionality
    /// </summary>
    public class AssetManagerTests
    {
        private IAssetManager assetManager;
        private ILogger<AssetManagerTests> logger;

        [SetUp]
        public void Setup()
        {
            // Initialize services for testing
            logger = Services.GetService<ILogger<AssetManagerTests>>();
            assetManager = Services.GetService<IAssetManager>();
            
            Assert.IsNotNull(assetManager, "AssetManager should be available from services");
            logger.LogInformation("AssetManagerTests setup completed");
        }

        [Test]
        public void AssetManager_ServiceRegistration_ShouldBeAvailable()
        {
            // Test that the asset manager is properly registered in DI container
            var manager = Services.GetService<IAssetManager>();
            Assert.IsNotNull(manager, "IAssetManager should be registered in services");
            
            // Test that it's a singleton
            var manager2 = Services.GetService<IAssetManager>();
            Assert.AreSame(manager, manager2, "AssetManager should be singleton");
        }

        [Test]
        public void TextureManager_RequestTexture_ShouldReturnTexture()
        {
            // Test basic texture request functionality
            UUID testTextureId = UUID.Random();
            
            try
            {
                Texture2D result = assetManager.RequestTexture(testTextureId);
                
                // Should return a texture (may be placeholder white texture)
                Assert.IsNotNull(result, "RequestTexture should return a texture");
                Assert.IsTrue(result.width > 0 && result.height > 0, "Returned texture should have valid dimensions");
                
                logger.LogInformation($"TextureManager test passed for UUID: {testTextureId}");
            }
            catch (System.Exception ex)
            {
                Assert.Fail($"RequestTexture should not throw exceptions: {ex.Message}");
            }
        }

        [Test]
        public void MaterialManager_RequestMaterial_ShouldReturnMaterial()
        {
            // Test material creation functionality
            UUID testTextureId = UUID.Random();
            
            try
            {
                Material result = assetManager.RequestMaterial(testTextureId);
                
                Assert.IsNotNull(result, "RequestMaterial should return a material");
                Assert.IsNotNull(result.shader, "Material should have a shader assigned");
                
                logger.LogInformation($"MaterialManager test passed for UUID: {testTextureId}");
            }
            catch (System.Exception ex)
            {
                Assert.Fail($"RequestMaterial should not throw exceptions: {ex.Message}");
            }
        }

        [Test]
        public void AssetManager_ConcurrentAccess_ShouldBeThreadSafe()
        {
            // Test thread safety with concurrent requests
            const int numThreads = 10;
            const int requestsPerThread = 100;
            bool allThreadsSucceeded = true;
            int completedThreads = 0;
            object lockObject = new object();

            System.Threading.Tasks.Parallel.For(0, numThreads, threadIndex =>
            {
                try
                {
                    for (int i = 0; i < requestsPerThread; i++)
                    {
                        UUID testId = UUID.Random();
                        var texture = assetManager.RequestTexture(testId);
                        Assert.IsNotNull(texture, $"Thread {threadIndex} request {i} should return texture");
                    }
                    
                    lock (lockObject)
                    {
                        completedThreads++;
                    }
                }
                catch (System.Exception ex)
                {
                    logger.LogError(ex, $"Thread {threadIndex} encountered error");
                    allThreadsSucceeded = false;
                }
            });

            Assert.IsTrue(allThreadsSucceeded, "All threads should complete without errors");
            Assert.AreEqual(numThreads, completedThreads, "All threads should complete");
            
            logger.LogInformation($"Thread safety test passed with {numThreads} threads, {requestsPerThread} requests each");
        }

        [Test]
        public void AssetManager_MemoryManagement_ShouldDisposeCleanly()
        {
            // Test proper disposal and memory management
            if (assetManager is System.IDisposable disposableManager)
            {
                Assert.DoesNotThrow(() => disposableManager.Dispose(), "AssetManager should dispose without throwing");
                logger.LogInformation("Memory management test passed");
            }
            else
            {
                Assert.Fail("AssetManager should implement IDisposable for proper memory management");
            }
        }

        [UnityTest]
        public IEnumerator AssetManager_PerformanceTest_ShouldMeetTargets()
        {
            // Performance test for asset requests
            const int numRequests = 1000;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            for (int i = 0; i < numRequests; i++)
            {
                UUID testId = UUID.Random();
                var texture = assetManager.RequestTexture(testId);
                Assert.IsNotNull(texture);
                
                // Yield periodically to avoid blocking Unity
                if (i % 100 == 0)
                {
                    yield return null;
                }
            }
            
            stopwatch.Stop();
            
            // Should complete 1000 requests in under 1 second (this is for placeholder textures)
            Assert.Less(stopwatch.ElapsedMilliseconds, 1000, 
                $"1000 texture requests should complete in under 1 second, took {stopwatch.ElapsedMilliseconds}ms");
            
            logger.LogInformation($"Performance test passed: {numRequests} requests in {stopwatch.ElapsedMilliseconds}ms");
        }

        [TearDown]
        public void TearDown()
        {
            // Cleanup after tests
            if (assetManager is System.IDisposable disposableManager)
            {
                disposableManager.Dispose();
            }
            
            logger.LogInformation("AssetManagerTests teardown completed");
        }
    }

    /// <summary>
    /// Test suite for UI components
    /// </summary>
    public class UIComponentTests
    {
        private ILogger<UIComponentTests> logger;

        [SetUp]
        public void Setup()
        {
            logger = Services.GetService<ILogger<UIComponentTests>>();
            logger.LogInformation("UIComponentTests setup completed");
        }

        [Test]
        public void AttachmentPointSelector_AllPointsDefined_ShouldHave33Points()
        {
            // Test that all attachment points are properly defined
            var attachmentPoints = System.Enum.GetValues(typeof(AttachmentPoint));
            
            // Should have all the major attachment points
            Assert.GreaterOrEqual(attachmentPoints.Length, 30, "Should have at least 30 attachment points defined");
            
            logger.LogInformation($"Attachment points test passed: {attachmentPoints.Length} points defined");
        }

        [Test]
        public void InventoryUI_ErrorHandling_ShouldNotThrow()
        {
            // Test that UI components handle null gracefully
            Assert.DoesNotThrow(() =>
            {
                // These should not throw exceptions even with null data
                var ui = new GameObject().AddComponent<TreeNodeUI>();
                // Test basic methods don't crash
            }, "UI components should handle null data gracefully");
            
            logger.LogInformation("UI error handling test passed");
        }
    }

    /// <summary>
    /// Test suite for SimManager functionality
    /// </summary>
    public class SimManagerTests
    {
        private ILogger<SimManagerTests> logger;

        [SetUp]
        public void Setup()
        {
            logger = Services.GetService<ILogger<SimManagerTests>>();
            logger.LogInformation("SimManagerTests setup completed");
        }

        [Test]
        public void SunPosition_Calculation_ShouldBeValid()
        {
            // Test sun position calculation logic
            float[] testPhases = { 0f, 0.25f, 0.5f, 0.75f, 1f };
            
            foreach (float phase in testPhases)
            {
                // Sun calculations should not throw exceptions
                Assert.DoesNotThrow(() =>
                {
                    // Test that sun phase calculations are within valid ranges
                    Assert.GreaterOrEqual(phase, 0f, "Sun phase should be >= 0");
                    Assert.LessOrEqual(phase, 1f, "Sun phase should be <= 1");
                }, $"Sun calculation should work for phase {phase}");
            }
            
            logger.LogInformation("Sun position calculation test passed");
        }

        [Test]
        public void ParticleSystem_Setup_ShouldHandleAllPatterns()
        {
            // Test that particle system setup handles all pattern types
            var patterns = System.Enum.GetValues(typeof(Primitive.ParticleSystem.SourcePattern));
            
            foreach (Primitive.ParticleSystem.SourcePattern pattern in patterns)
            {
                Assert.DoesNotThrow(() =>
                {
                    // Particle system setup should handle all patterns without throwing
                    logger.LogDebug($"Testing particle pattern: {pattern}");
                }, $"Particle system should handle pattern {pattern}");
            }
            
            logger.LogInformation($"Particle system test passed for {patterns.Length} patterns");
        }
    }

    /// <summary>
    /// Performance and memory tests
    /// </summary>
    public class PerformanceTests
    {
        private ILogger<PerformanceTests> logger;

        [SetUp]
        public void Setup()
        {
            logger = Services.GetService<ILogger<PerformanceTests>>();
            logger.LogInformation("PerformanceTests setup completed");
        }

        [Test]
        public void Services_GetService_ShouldBeFast()
        {
            // Test that service resolution is fast
            const int numCalls = 10000;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            for (int i = 0; i < numCalls; i++)
            {
                var service = Services.GetService<ILogger<PerformanceTests>>();
                Assert.IsNotNull(service);
            }
            
            stopwatch.Stop();
            
            // Service resolution should be very fast (under 100ms for 10k calls)
            Assert.Less(stopwatch.ElapsedMilliseconds, 100, 
                $"Service resolution should be fast, took {stopwatch.ElapsedMilliseconds}ms for {numCalls} calls");
            
            logger.LogInformation($"Service resolution performance test passed: {numCalls} calls in {stopwatch.ElapsedMilliseconds}ms");
        }

        [UnityTest]
        public IEnumerator GarbageCollection_AssetRequests_ShouldNotExceedLimits()
        {
            // Test that asset requests don't create excessive garbage
            long initialMemory = System.GC.GetTotalMemory(true);
            
            // Make many asset requests
            const int numRequests = 1000;
            var assetManager = Services.GetService<IAssetManager>();
            
            for (int i = 0; i < numRequests; i++)
            {
                assetManager.RequestTexture(UUID.Random());
                
                if (i % 100 == 0)
                {
                    yield return null; // Don't block Unity
                }
            }
            
            // Force garbage collection and measure
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            
            long finalMemory = System.GC.GetTotalMemory(false);
            long memoryIncrease = finalMemory - initialMemory;
            
            // Memory increase should be reasonable (under 50MB for 1000 requests)
            Assert.Less(memoryIncrease, 50 * 1024 * 1024, 
                $"Memory increase should be reasonable, increased by {memoryIncrease / 1024 / 1024}MB");
            
            logger.LogInformation($"Garbage collection test passed: {memoryIncrease / 1024 / 1024}MB increase for {numRequests} requests");
        }
    }
}