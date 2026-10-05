# Crystal Frost Builder Setup Guide

## Quick Start for New Developers

This guide will get you up and running with Crystal Frost development quickly, covering the current architecture and development workflows.

## Prerequisites

### Unity Setup
- **Unity Version**: 2021.3.25f1 LTS (exact version required)
- **Required Modules**:
  - Windows Build Support (IL2CPP)
  - Mac Build Support (Mono)
  - Linux Build Support (IL2CPP)
  - Universal Render Pipeline (URP)

### Development Environment
- **IDE**: Visual Studio 2022 or JetBrains Rider recommended
- **Git**: Latest version with LFS support
- **.NET**: Framework 4.8 or later
- **NuGet**: For package management

## Project Structure Overview

```
CrystalFrost-Public-julestest/
├── Assets/
│   ├── CFEngine/                    # Core engine layer
│   │   ├── Assets/                  # Asset management system
│   │   ├── Client/                  # LibreMetaverse integration
│   │   ├── Services.cs             # Dependency injection container
│   │   └── ...
│   ├── Scripts/                     # Game logic and UI
│   │   ├── Assets/                  # Specialized asset managers
│   │   ├── UI/                     # User interface components
│   │   ├── Performance/            # Performance monitoring
│   │   ├── Controllers/            # MVC pattern controllers
│   │   └── ...
│   └── Tests/                      # Automated test suite
├── Docs/
│   ├── DEVELOPER_GUIDE.md          # This document
│   ├── IMPLEMENTATION_STATUS_UPDATE.md
│   ├── PROJECT_ANALYSIS_SUMMARY.md
│   └── PR_STRATEGY.md
└── README.md
```

## Development Architecture

### Dependency Injection Pattern
All components use the Services.cs container for dependency management:

```csharp
// Getting services
var logger = Services.GetService<ILogger<YourClass>>();
var assetManager = Services.GetService<IAssetManager>();

// Registering new services (in Services.cs)
_serviceCollection.AddSingleton<IYourInterface, YourImplementation>();
```

### Logging Pattern
Consistent logging throughout the application:

```csharp
public class YourClass
{
    private readonly ILogger<YourClass> _logger;
    
    public YourClass()
    {
        _logger = Services.GetService<ILogger<YourClass>>();
    }
    
    public void DoSomething()
    {
        try
        {
            // Your code here
            _logger.LogInformation("Operation completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Operation failed with context");
            throw; // or handle gracefully
        }
    }
}
```

### Error Handling Pattern
Comprehensive error handling with graceful degradation:

```csharp
public Texture2D RequestTexture(UUID uuid)
{
    try
    {
        // Primary operation
        return ProcessTexture(uuid);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Failed to process texture {uuid}");
        return GetFallbackTexture(); // Graceful degradation
    }
}
```

## Key Components Guide

### Asset Management System

The asset system is built on specialized managers:

#### TextureManager
```csharp
// Request a texture (returns immediately, loads asynchronously)
Texture2D texture = assetManager.RequestTexture(textureUUID);

// Process alpha channel for transparency
assetManager.ProcessAlphaTexture(textureUUID);
```

#### MaterialManager
```csharp
// Request a material with texture
Material material = assetManager.RequestMaterial(textureUUID);

// Update material on renderers
assetManager.UpdateMaterialOnRenderers(uuid, rendererList);
```

#### MeshManager
```csharp
// Request mesh for primitive object
assetManager.RequestMesh2(gameObject, primitive, meshUUID, meshHolder);

// Request sculpt processing
assetManager.RequestSculpt(gameObject, primitive);
```

### UI System

#### Inventory UI
```csharp
// Show context menu for inventory item
inventoryUI.ShowContextMenu(inventoryItem, mousePosition);

// Handle attachment selection
attachmentSelector.Show(inventoryItem, (attachmentPoint) => {
    // Handle attachment to selected point
});
```

#### Tree Node UI
```csharp
// Set up a tree node with data
treeNode.SetData(inventoryItem, depth, parentWindow);

// Handle selection
treeNode.SetSelected(true);
```

### SimManager Integration

#### Object Processing
```csharp
// The SimManager automatically handles these based on primitive type:
// - RequestSculptForObject(sceneObject)
// - RequestMeshForObject(sceneObject) 
// - RequestGeneratedMeshForObject(sceneObject)
// - SetupLights(sceneObject)
// - SetupParticles(sceneObject)
```

## Development Workflows

### Adding New Features

1. **Plan the Architecture**
   - Identify which layer the feature belongs to (CFEngine vs Scripts)
   - Determine if it needs a new service or extends existing ones
   - Plan the interfaces and dependencies

2. **Implement the Service**
   ```csharp
   // Create interface
   public interface INewFeatureService
   {
       void DoSomething();
   }
   
   // Implement service
   public class NewFeatureService : INewFeatureService, IDisposable
   {
       private readonly ILogger<NewFeatureService> _logger;
       
       public NewFeatureService()
       {
           _logger = Services.GetService<ILogger<NewFeatureService>>();
       }
       
       public void DoSomething()
       {
           // Implementation with error handling and logging
       }
       
       public void Dispose()
       {
           // Cleanup resources
       }
   }
   ```

3. **Register the Service**
   ```csharp
   // In Services.cs RegisterComponents()
   _serviceCollection.AddSingleton<INewFeatureService, NewFeatureService>();
   ```

4. **Add Tests**
   ```csharp
   [Test]
   public void NewFeature_BasicOperation_ShouldWork()
   {
       var service = Services.GetService<INewFeatureService>();
       Assert.DoesNotThrow(() => service.DoSomething());
   }
   ```

### Modifying Existing Code

1. **Maintain Backward Compatibility**
   - Don't change existing public interfaces
   - Add new overloads instead of changing signatures
   - Use deprecation warnings for obsolete methods

2. **Follow Established Patterns**
   - Use the same logging, error handling, and DI patterns
   - Match the existing code style and documentation format
   - Add appropriate tests for new functionality

3. **Update Documentation**
   - Update XML documentation for changed methods
   - Add comments for complex logic
   - Update this guide if architecture changes

### Testing Strategy

#### Unit Tests
```csharp
[Test]
public void Component_Operation_ShouldProduceExpectedResult()
{
    // Arrange
    var component = Services.GetService<IComponent>();
    
    // Act
    var result = component.DoOperation();
    
    // Assert
    Assert.IsNotNull(result);
    Assert.AreEqual(expectedValue, result.Value);
}
```

#### Integration Tests
```csharp
[UnityTest]
public IEnumerator Component_ComplexOperation_ShouldWorkEndToEnd()
{
    // Setup complex scenario
    var component = Services.GetService<IComponent>();
    
    // Perform operation that takes time
    component.StartAsyncOperation();
    
    // Wait for completion
    yield return new WaitUntil(() => component.IsComplete);
    
    // Verify results
    Assert.IsTrue(component.WasSuccessful);
}
```

#### Performance Tests
```csharp
[Test]
public void Component_Performance_ShouldMeetTargets()
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    
    // Perform operation
    component.DoPerformanceCriticalOperation();
    
    stopwatch.Stop();
    Assert.Less(stopwatch.ElapsedMilliseconds, 100, "Should complete in under 100ms");
}
```

## Performance Guidelines

### Memory Management
- **Always implement IDisposable** for managers that hold Unity resources
- **Use object pooling** for frequently created/destroyed objects
- **Clear collections** in Dispose methods
- **Monitor memory usage** with Unity Profiler

### Threading
- **Use ConcurrentDictionary** for shared data structures
- **Use ReaderWriterLockSlim** for read-heavy scenarios
- **Avoid Unity API calls** from background threads
- **Use UnityMainThreadDispatcher** for UI updates from background threads

### Asset Loading
- **Request assets early** to allow time for loading
- **Use fallback textures/materials** while loading
- **Cache loaded assets** appropriately
- **Implement progressive loading** for large assets

## Debugging Guide

### Common Issues

#### Service Not Found
```
ApplicationException: The requested type IYourService could not be provided.
```
**Solution**: Register the service in Services.cs:
```csharp
_serviceCollection.AddSingleton<IYourService, YourService>();
```

#### Memory Leaks
**Symptoms**: Increasing memory usage over time
**Solution**: Implement IDisposable and call Dispose() appropriately:
```csharp
public void Dispose()
{
    // Dispose Unity objects
    if (material != null) Destroy(material);
    
    // Clear collections
    cache.Clear();
    
    // Dispose managed resources
    disposableField?.Dispose();
}
```

#### Thread Safety Issues
**Symptoms**: Random crashes, corrupted data
**Solution**: Use concurrent collections:
```csharp
// Replace
Dictionary<UUID, Data> cache = new();

// With
ConcurrentDictionary<UUID, Data> cache = new();
```

### Debugging Tools

#### Unity Profiler
- **Memory Profiler**: Monitor memory allocation and leaks
- **CPU Profiler**: Identify performance bottlenecks
- **Rendering Profiler**: Optimize draw calls and batching

#### Console Logging
```csharp
// Enable detailed logging in Unity Console
_logger.LogDebug("Detailed information for debugging");
_logger.LogInformation("General information");
_logger.LogWarning("Something unexpected happened");
_logger.LogError("An error occurred");
```

#### Performance Monitoring
The built-in PerformanceManager provides real-time metrics:
- FPS monitoring
- Memory usage tracking
- Draw call counting
- Automatic quality adjustment

## Contribution Guidelines

### Code Style
- **Follow C# conventions**: PascalCase for public members, camelCase for private
- **Use meaningful names**: Descriptive variable and method names
- **Add XML documentation**: All public APIs must be documented
- **Keep methods focused**: Single responsibility principle

### Pull Request Process
1. **Create feature branch**: `feature/your-feature-name`
2. **Implement with tests**: Include unit tests for new functionality
3. **Update documentation**: Add or update relevant documentation
4. **Test thoroughly**: Run all tests and manual testing
5. **Submit PR**: Include description of changes and testing performed

### Documentation Requirements
- **XML Documentation**: All public APIs
- **Inline Comments**: Complex algorithms and business logic
- **Architecture Notes**: Any architectural decisions or patterns
- **Performance Notes**: Critical performance considerations

## Advanced Topics

### Custom Asset Managers
```csharp
public class CustomAssetManager : ICustomAssetManager, IDisposable
{
    private readonly ILogger<CustomAssetManager> _logger;
    private readonly ConcurrentDictionary<UUID, CustomAsset> _cache = new();
    
    public CustomAssetManager()
    {
        _logger = Services.GetService<ILogger<CustomAssetManager>>();
    }
    
    public CustomAsset RequestAsset(UUID uuid)
    {
        return _cache.GetOrAdd(uuid, CreateAsset);
    }
    
    private CustomAsset CreateAsset(UUID uuid)
    {
        // Create and return asset
        return new CustomAsset(uuid);
    }
    
    public void Dispose()
    {
        foreach (var asset in _cache.Values)
        {
            asset.Dispose();
        }
        _cache.Clear();
    }
}
```

### Performance Optimization
```csharp
// Use object pooling for frequently created objects
public class ObjectPool<T> where T : new()
{
    private readonly ConcurrentQueue<T> _objects = new();
    
    public T Get()
    {
        if (_objects.TryDequeue(out T item))
            return item;
        return new T();
    }
    
    public void Return(T item)
    {
        // Reset object state
        _objects.Enqueue(item);
    }
}
```

### VR Integration Preparation
The architecture is ready for VR integration:
```csharp
// VR-specific optimizations are already in place:
// - Performance monitoring
// - Memory management
// - Efficient rendering pipeline
// - Thread-safe operations
```

## Support and Resources

### Documentation
- **DEVELOPER_GUIDE.md**: This comprehensive guide
- **IMPLEMENTATION_STATUS_UPDATE.md**: Current implementation status
- **PROJECT_ANALYSIS_SUMMARY.md**: Overall project analysis
- **PR_STRATEGY.md**: Development strategy and roadmap

### Community
- **GitHub Issues**: Report bugs and request features
- **Discord**: Real-time developer discussion
- **Wiki**: Community-maintained documentation

### Learning Resources
- **Unity Documentation**: https://docs.unity3d.com/
- **LibreMetaverse**: https://github.com/LibreMetaverse/LibreMetaverse
- **C# Best Practices**: Microsoft's C# coding conventions
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection documentation

This setup guide provides everything needed to start contributing to Crystal Frost development. The architecture is stable, well-documented, and ready for the next phase of development.