using OpenMetaverse;
using UnityEngine;
using Microsoft.Extensions.Logging;

namespace CrystalFrost.Services
{
    /// <summary>
    /// Service-based implementation of client management functionality
    /// Replaces static ClientManager with proper dependency injection and lifecycle management
    /// </summary>
    public class ClientManagerService : MonoBehaviour, IClientManagerService
    {
        private ILogger<ClientManagerService> _logger;

        // Core client state
        public bool IsOpenSim { get; set; } = false;
        public GridClient Client { get; set; }
        public TexturePipeline TexturePipeline { get; set; }
        public bool Active { get; set; } = false;
        public int MainThreadId { get; set; }
        public float ViewDistance { get; set; } = 32f;

        // Managers
        public CFAssetManager AssetManager { get; set; }
        public SimManager SimManager { get; set; }
        public SoundManager SoundManager { get; set; }

        // UI Components
        public Chat Chat { get; set; }
        public ChatWindowUI ChatWindow { get; set; }
        public Avatar Avatar { get; set; }
        public CurrentOutfitFolder CurrentOutfitFolder { get; set; }

        // Material configuration - these are constants, not changed at runtime
        public string DiffuseName => "_MainTex";
        public string ColorName => "_Color";
        public string EmissiveMapName => "_EmissionMap";
        public string EmissiveColorName => "_EmissionColor";

#if MK_GLOW_PRESENT
        public string MaterialNameModifier => "MK Glow ";
#else
        public string MaterialNameModifier => "";
#endif

        // Utility properties
        public bool IsMainThread => System.Threading.Thread.CurrentThread.ManagedThreadId == MainThreadId;

        private void Awake()
        {
            _logger = Services.GetService<ILogger<ClientManagerService>>();
            MainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        }

        public void Initialize()
        {
            _logger.LogInformation("Initializing ClientManagerService");
            
            // Initialize core client if not already done
            if (Client == null)
            {
                Client = Services.GetService<GridClient>();
            }

            // Initialize asset manager if not already done  
            if (AssetManager == null)
            {
                AssetManager = new CFAssetManager();
            }

            // Set up default values
            if (ViewDistance <= 0) ViewDistance = 32f;

            _logger.LogInformation("ClientManagerService initialization complete");
        }

        public void Cleanup()
        {
            _logger.LogInformation("Cleaning up ClientManagerService");

            try
            {
                // Dispose of disposable resources
                AssetManager?.Dispose();
                (SimManager as SimManager)?.Dispose();
                CurrentOutfitFolder?.Dispose();

                // Clear references
                Client = null;
                TexturePipeline = null;
                AssetManager = null;
                SimManager = null;
                SoundManager = null;
                Chat = null;
                ChatWindow = null;
                Avatar = null;
                CurrentOutfitFolder = null;

                Active = false;
                IsOpenSim = false;

                _logger.LogInformation("ClientManagerService cleanup complete");
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error during ClientManagerService cleanup");
            }
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        // Static access methods for backward compatibility during transition
        // These will delegate to the service instance
        private static ClientManagerService _instance;
        
        public static ClientManagerService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ClientManagerService>();
                    if (_instance == null)
                    {
                        var go = new GameObject("ClientManagerService");
                        _instance = go.AddComponent<ClientManagerService>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        // Provide static compatibility layer
        public static bool GetIsOpenSim() => Instance.IsOpenSim;
        public static void SetIsOpenSim(bool value) => Instance.IsOpenSim = value;
        
        public static GridClient GetClient() => Instance.Client;
        public static void SetClient(GridClient client) => Instance.Client = client;
        
        public static bool GetActive() => Instance.Active;
        public static void SetActive(bool active) => Instance.Active = active;

        public static CFAssetManager GetAssetManager() => Instance.AssetManager;
        public static SimManager GetSimManager() => Instance.SimManager;
        public static SoundManager GetSoundManager() => Instance.SoundManager;

        public static bool GetIsMainThread() => Instance.IsMainThread;
    }
}