using OpenMetaverse;
using UnityEngine;

namespace CrystalFrost.Services
{
    /// <summary>
    /// Interface for client management services
    /// </summary>
    public interface IClientManagerService
    {
        // Core client state
        bool IsOpenSim { get; set; }
        GridClient Client { get; set; }
        TexturePipeline TexturePipeline { get; set; }
        bool Active { get; set; }
        int MainThreadId { get; set; }
        float ViewDistance { get; set; }

        // Managers
        CFAssetManager AssetManager { get; set; }
        SimManager SimManager { get; set; }
        SoundManager SoundManager { get; set; }

        // UI Components
        Chat Chat { get; set; }
        ChatWindowUI ChatWindow { get; set; }
        Avatar Avatar { get; set; }
        CurrentOutfitFolder CurrentOutfitFolder { get; set; }

        // Material configuration
        string DiffuseName { get; }
        string ColorName { get; }
        string EmissiveMapName { get; }
        string EmissiveColorName { get; }
        string MaterialNameModifier { get; }

        // Utility methods
        bool IsMainThread { get; }
        void Initialize();
        void Cleanup();
    }
}