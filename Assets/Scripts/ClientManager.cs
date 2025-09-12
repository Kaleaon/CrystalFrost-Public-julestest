using OpenMetaverse;
using CrystalFrost;
using CrystalFrost.Services;
using UnityEngine;

/// <summary>
/// Hybrid ClientManager that provides both static access (for backward compatibility) 
/// and service-based access (for new code following dependency injection pattern)
/// 
/// MIGRATION STRATEGY:
/// - Phase 1: Keep static interface, delegate to service internally
/// - Phase 2: Gradually migrate code to use IClientManagerService via DI
/// - Phase 3: Deprecate static interface
/// - Phase 4: Remove static interface entirely
/// </summary>
public static class ClientManager
{
    // Legacy static interface - delegates to service
    private static IClientManagerService Service => ClientManagerService.Instance;

    // Core client state
    public static bool isOpenSim 
    {
        get => Service.IsOpenSim;
        set => Service.IsOpenSim = value;
    }

    public static GridClient client
    {
        get => Service.Client;
        set => Service.Client = value;
    }

    public static TexturePipeline texturePipeline
    {
        get => Service.TexturePipeline;
        set => Service.TexturePipeline = value;
    }

    public static bool active
    {
        get => Service.Active;
        set => Service.Active = value;
    }

    public static CFAssetManager assetManager
    {
        get => Service.AssetManager;
        set => Service.AssetManager = value;
    }

    public static SimManager simManager
    {
        get => Service.SimManager;
        set => Service.SimManager = value;
    }

    public static SoundManager soundManager
    {
        get => Service.SoundManager;
        set => Service.SoundManager = value;
    }

    public static int mainThreadId
    {
        get => Service.MainThreadId;
        set => Service.MainThreadId = value;
    }

    public static float viewDistance
    {
        get => Service.ViewDistance;
        set => Service.ViewDistance = value;
    }

    public static Chat chat
    {
        get => Service.Chat;
        set => Service.Chat = value;
    }

    public static ChatWindowUI chatWindow
    {
        get => Service.ChatWindow;
        set => Service.ChatWindow = value;
    }

    public static Avatar avatar
    {
        get => Service.Avatar;
        set => Service.Avatar = value;
    }

    public static CurrentOutfitFolder currentOutfitFolder
    {
        get => Service.CurrentOutfitFolder;
        set => Service.CurrentOutfitFolder = value;
    }

    // Material configuration
    public static string DiffuseName => Service.DiffuseName;
    public static string ColorName => Service.ColorName;
    public static string EmissiveMapName => Service.EmissiveMapName;
    public static string EmissiveColorName => Service.EmissiveColorName;
    public static string MaterialNameModifier => Service.MaterialNameModifier;

    // Utility properties
    public static bool IsMainThread => Service.IsMainThread;

    // Service management methods (new)
    public static void InitializeService()
    {
        Service.Initialize();
    }

    public static void CleanupService()
    {
        Service.Cleanup();
    }

    // For dependency injection in new code
    public static IClientManagerService GetService()
    {
        return Service;
    }
}