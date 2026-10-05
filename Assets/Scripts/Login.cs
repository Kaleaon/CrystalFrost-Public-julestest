using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using LibreMetaverse;
using OpenMetaverse;
using OpenMetaverse.Packets;

using CrystalFrost;
using CrystalFrost.Client.Credentials;
using CrystalFrost.Config;
using CrystalFrost.Extensions;
using CrystalFrost.Scripts;
using CrystalFrost.Timing;
using CrystalFrost.Controllers;
using CrystalFrost.UI;
using Bunny;

using Microsoft.Extensions.Logging;
using Temp;
using TMPro;

#if USE_KWS
using KWS;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Main login coordinator that orchestrates the login process using specialized controllers
/// Refactored from original 673-line monolithic class to follow Single Responsibility Principle
/// </summary>
public class Login : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject loggedInUI;
    [SerializeField] private GameObject loginUI;
    [SerializeField] private GameObject consoleUI;
    [SerializeField] private TMP_InputField firstName;
    [SerializeField] private TMP_InputField lastName;
    [SerializeField] private TMP_InputField password;
    [SerializeField] private TMP_InputField gridURL;
    [SerializeField] private TMP_Text console;

    [Header("World References")]
    public Terrain terrainPrefab;
    public GameObject chatUI;
    public CameraControls cameraControls;
    public GameObject contactsUI;
    public Transform capsule;

    // Controllers
    private LoginUIController _loginUIController;
    private AuthenticationController _authController;
    private GridEventController _gridEventController;
    private TerrainController _terrainController;
    private InventoryUIController _inventoryUIController;

    // Legacy fields maintained for backward compatibility
    [System.Obsolete("Use LoginUIController instead")]
    public class LoginDetails
    {
        public string FirstName;
        public string LastName;
        public string Password;
        public string StartLocation;
        public bool GroupCommands;
        public string MasterName;
        public UUID MasterKey;
        public string URI;
    }

    // Group and appearance data
    public UUID GroupID = UUID.Zero;
    public Dictionary<UUID, GroupMember> GroupMembers;
    public Dictionary<UUID, AvatarAppearancePacket> Appearances = new();
    public bool Running = true;
    public bool GroupCommands = false;
    public string MasterName = string.Empty;
    public UUID MasterKey = UUID.Zero;
    public bool AllowObjectMaster = false;
    public InventoryFolder CurrentDirectory = null;

    private readonly System.Timers.Timer updateTimer;
    private UUID GroupMembersRequestID;
    public Dictionary<UUID, Group> GroupsCache = null;

    const float DEG_TO_RAD = 0.017453292519943295769236907684886f;

    private ILogger<Login> _log;
    private EventSystem system;

    // Legacy region handle methods - moved to TerrainController but kept for compatibility
    public ulong GetNorth(ulong handle) => _terrainController.GetNorth(handle);
    public ulong GetSouth(ulong handle) => _terrainController.GetSouth(handle);
    public ulong GetEast(ulong handle) => _terrainController.GetEast(handle);
    public ulong GetWest(ulong handle) => _terrainController.GetWest(handle);

    void Awake()
    {
        Application.targetFrameRate = 1000;
        QualitySettings.vSyncCount = 0;

        _log = Services.GetService<ILogger<Login>>();

        // Initialize main thread ID
        ClientManager.mainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;

        // Set up console output
        Bunny.Console.textOutput = console;

        // Initialize grid client
        ClientManager.client = Services.GetService<GridClient>();
        ClientManager.client.Self.Movement.Camera.Far = 32;
        ClientManager.client.Self.Movement.SetFOVVerticalAngle(Camera.main.fieldOfView * DEG_TO_RAD);

        InitializeControllers();
        SetupControllerEvents();

        // Initialize UI state
        loggedInUI.SetActive(false);
    }

    private void InitializeControllers()
    {
        // Create controller components
        _loginUIController = gameObject.AddComponent<LoginUIController>();
        _authController = gameObject.AddComponent<AuthenticationController>();
        _gridEventController = gameObject.AddComponent<GridEventController>();
        _terrainController = gameObject.AddComponent<TerrainController>();
        _inventoryUIController = gameObject.AddComponent<InventoryUIController>();

        // Initialize controllers
        _authController.Initialize();
        
        // Set up LoginUIController with proper UI references
        SetupLoginUIController();

        // Set terrain prefab
        _terrainController.terrainPrefab = terrainPrefab;
    }

    private void SetupLoginUIController()
    {
        // Initialize LoginUIController with UI references
        _loginUIController.Initialize(
            _authController.GetCurrentCredential(),
            loginUI, loggedInUI, consoleUI,
            firstName, lastName, password, gridURL, console
        );
    }

    private void SetupControllerEvents()
    {
        // Login UI events
        _loginUIController.OnLoginRequested += HandleLoginRequest;

        // Authentication events
        _authController.OnLoginSuccess += HandleLoginSuccess;
        _authController.OnLogoutComplete += HandleLogoutComplete;
        _authController.OnStatusUpdate += HandleStatusUpdate;

        // Grid events
        _gridEventController.OnSimConnected += HandleSimConnected;
        _gridEventController.OnRegionCrossed += HandleRegionCrossed;
    }

    private void Start()
    {
        system = EventSystem.current;
        cameraControls.enabled = true;
    }

    private void Update()
    {
        if (!ClientManager.active)
        {
            // Update logic when not connected
        }
    }

    private void OnDestroy()
    {
        _gridEventController?.UnregisterEventHandlers();
    }

    // Public API methods maintained for compatibility
    public void TryLogin()
    {
        _loginUIController.OnLoginButtonClicked();
    }

    public void LogOut()
    {
        _authController.Logout();
    }

    // Event handlers
    private void HandleLoginRequest(string firstName, string lastName, string password, string gridURL)
    {
        _authController.TryLogin(firstName, lastName, password, gridURL);
    }

    private void HandleLoginSuccess()
    {
        _loginUIController.ShowLoggedInUI();
        _gridEventController.RegisterEventHandlers();
        
        // Set up simulator and avatar state from original logic
        Simulator sim = ClientManager.client.Network.CurrentSim;
        ClientManager.simManager._thissim = sim;
        ClientManager.simManager.water.position = new Vector3(127f, sim.WaterHeight, 127f);

        // Set up avatar
        Avatar av = gameObject.GetComponent<Avatar>();
        av.id = ClientManager.client.Self.LocalID;
        capsule.SetPositionAndRotation(
            ClientManager.client.Self.SimPosition.ToVector3(),
            ClientManager.client.Self.SimRotation.ToUnity());
        Camera.main.transform.SetPositionAndRotation(
            ClientManager.client.Self.SimPosition.ToVector3(),
            ClientManager.client.Self.SimRotation.ToUnity());
        
        // Enable additional UI elements
        if (chatUI != null) chatUI.SetActive(true);
        if (contactsUI != null) contactsUI.SetActive(true);
        
        cameraControls.enabled = true;

        // Save credentials and setup contacts
        _authController.SaveCredentials();
        gameObject.GetComponent<ChatWindowUI>().PopulateContacts();
        CreateInventoryWindow();
    }

    private void HandleLogoutComplete()
    {
        _loginUIController.ShowLoginUI();
        
        // Disable additional UI elements
        if (chatUI != null) chatUI.SetActive(false);
        if (contactsUI != null) contactsUI.SetActive(false);
    }

    private void HandleStatusUpdate(string message)
    {
        _loginUIController.ShowConsoleMessage(message);
    }

    private void HandleSimConnected(SimConnectedEventArgs e)
    {
        _log.LogInformation($"Connected to sim: {e.Simulator.Name}");
        // Handle terrain creation if needed
    }

    private void HandleRegionCrossed(RegionCrossedEventArgs e)
    {
        _log.LogInformation($"Region crossed from {e.OldSimulator.Name} to {e.NewSimulator.Name}");
    }

    // Public methods for terrain management (delegates to TerrainController)
    public void CreateSimulatorTerrainTiles(string name, uint handle, uint sizeX, uint sizeY)
    {
        _terrainController.CreateSimulatorTerrainTiles(name, handle, sizeX, sizeY);
    }

    // Public method for inventory window creation (delegates to InventoryUIController)
    public void CreateInventoryWindow()
    {
        _inventoryUIController.CreateInventoryWindow();
    }
}