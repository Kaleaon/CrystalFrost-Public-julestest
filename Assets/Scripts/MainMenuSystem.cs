using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OpenMetaverse;
using System.Collections.Generic;

public class MainMenuSystem : MonoBehaviour
{
    [Header("Main Menu Bar")]
    public GameObject menuBar;
    public Button worldButton;
    public Button avatarButton;
    public Button buildButton;
    public Button communicateButton;
    public Button helpButton;
    
    [Header("World Menu")]
    public GameObject worldMenu;
    public Button homeButton;
    public Button teleportButton;
    public Button landmarkButton;
    public Button mapButton;
    public Button sunPositionButton;
    public Button environmentButton;
    
    [Header("Avatar Menu")]
    public GameObject avatarMenu;
    public Button appearanceButton;
    public Button attachmentsButton;
    public Button gesturesButton;
    public Button animationsButton;
    public Button statusButton;
    public Button preferencesButton;
    
    [Header("Build Menu")]
    public GameObject buildMenu;
    public Button createButton;
    public Button editButton;
    public Button toolsButton;
    public Button scriptsButton;
    public Button mediaButton;
    public Button landToolsButton;
    
    [Header("Communicate Menu")]
    public GameObject communicateMenu;
    public Button localChatButton;
    public Button instantMessageButton;
    public Button friendsButton;
    public Button groupsButton;
    public Button profileButton;
    public Button searchButton;
    
    [Header("Help Menu")]
    public GameObject helpMenu;
    public Button aboutButton;
    public Button keyboardShortcutsButton;
    public Button bugReportButton;
    public Button feedbackButton;
    public Button documentationButton;
    
    [Header("Context Menus")]
    public GameObject avatarContextMenu;
    public GameObject objectContextMenu;
    public GameObject landContextMenu;
    
    [Header("Quick Access Toolbar")]
    public GameObject quickToolbar;
    public Button flyToggleButton;
    public Button buildModeButton;
    public Button voiceChatButton;
    public Button streamMusicButton;
    public Button pauseAnimationsButton;
    
    [Header("Status Indicators")]
    public TMP_Text connectionStatus;
    public TMP_Text regionName;
    public TMP_Text avatarPosition;
    public TMP_Text frameRate;
    public Slider healthBar;
    public TMP_Text moneyBalance;
    
    [Header("Window References")]
    public InventoryBrowser inventoryBrowser;
    public PreferencesWindow preferencesWindow;
    public WorldMap worldMap;
    public SearchWindow searchWindow;
    public BuildTools buildTools;
    public GroupManager groupManager;
    public ProfileViewer profileViewer;
    public LSLScriptEditor scriptEditor;
    public MediaManager mediaManager;
    public WindlightManager windlightManager;
    public MarketplaceIntegration marketplace;
    public EnhancedChatSystem chatSystem;
    public UIManager uiManager;
    
    private GridClient client;
    private Dictionary<string, GameObject> openMenus = new();
    private bool isBuildModeEnabled = false;
    private bool isVoiceChatEnabled = false;
    private bool isMusicStreamEnabled = false;
    private bool areAnimationsPaused = false;

    void Awake()
    {
        SetupMenuSystem();
        HideAllMenus();
    }

    void SetupMenuSystem()
    {
        // Main menu buttons
        if (worldButton) worldButton.onClick.AddListener(() => ToggleMenu("World"));
        if (avatarButton) avatarButton.onClick.AddListener(() => ToggleMenu("Avatar"));
        if (buildButton) buildButton.onClick.AddListener(() => ToggleMenu("Build"));
        if (communicateButton) communicateButton.onClick.AddListener(() => ToggleMenu("Communicate"));
        if (helpButton) helpButton.onClick.AddListener(() => ToggleMenu("Help"));
        
        // World menu
        if (homeButton) homeButton.onClick.AddListener(GoHome);
        if (teleportButton) teleportButton.onClick.AddListener(ShowTeleportOptions);
        if (landmarkButton) landmarkButton.onClick.AddListener(ShowLandmarks);
        if (mapButton) mapButton.onClick.AddListener(ShowWorldMap);
        if (sunPositionButton) sunPositionButton.onClick.AddListener(ShowSunPosition);
        if (environmentButton) environmentButton.onClick.AddListener(ShowEnvironmentSettings);
        
        // Avatar menu
        if (appearanceButton) appearanceButton.onClick.AddListener(ShowAppearance);
        if (attachmentsButton) attachmentsButton.onClick.AddListener(ShowAttachments);
        if (gesturesButton) gesturesButton.onClick.AddListener(ShowGestures);
        if (animationsButton) animationsButton.onClick.AddListener(ShowAnimations);
        if (statusButton) statusButton.onClick.AddListener(ShowStatus);
        if (preferencesButton) preferencesButton.onClick.AddListener(ShowPreferences);
        
        // Build menu
        if (createButton) createButton.onClick.AddListener(ShowCreateTools);
        if (editButton) editButton.onClick.AddListener(ShowEditTools);
        if (toolsButton) toolsButton.onClick.AddListener(ShowBuildTools);
        if (scriptsButton) scriptsButton.onClick.AddListener(ShowScriptEditor);
        if (mediaButton) mediaButton.onClick.AddListener(ShowMediaTools);
        if (landToolsButton) landToolsButton.onClick.AddListener(ShowLandTools);
        
        // Communicate menu
        if (localChatButton) localChatButton.onClick.AddListener(FocusLocalChat);
        if (instantMessageButton) instantMessageButton.onClick.AddListener(ShowIMOptions);
        if (friendsButton) friendsButton.onClick.AddListener(ShowFriends);
        if (groupsButton) groupsButton.onClick.AddListener(ShowGroups);
        if (profileButton) profileButton.onClick.AddListener(ShowMyProfile);
        if (searchButton) searchButton.onClick.AddListener(ShowSearch);
        
        // Help menu
        if (aboutButton) aboutButton.onClick.AddListener(ShowAbout);
        if (keyboardShortcutsButton) keyboardShortcutsButton.onClick.AddListener(ShowKeyboardShortcuts);
        if (bugReportButton) bugReportButton.onClick.AddListener(ShowBugReport);
        if (feedbackButton) feedbackButton.onClick.AddListener(ShowFeedback);
        if (documentationButton) documentationButton.onClick.AddListener(OpenDocumentation);
        
        // Quick toolbar
        if (flyToggleButton) flyToggleButton.onClick.AddListener(ToggleFly);
        if (buildModeButton) buildModeButton.onClick.AddListener(ToggleBuildMode);
        if (voiceChatButton) voiceChatButton.onClick.AddListener(ToggleVoiceChat);
        if (streamMusicButton) streamMusicButton.onClick.AddListener(ToggleMusicStream);
        if (pauseAnimationsButton) pauseAnimationsButton.onClick.AddListener(ToggleAnimations);
        
        // Register menus
        openMenus["World"] = worldMenu;
        openMenus["Avatar"] = avatarMenu;
        openMenus["Build"] = buildMenu;
        openMenus["Communicate"] = communicateMenu;
        openMenus["Help"] = helpMenu;
    }

    void Start()
    {
        client = ClientManager.client;
        
        if (client != null)
        {
            client.Network.LoginCompleted += OnLoginCompleted;
            client.Network.SimChanged += OnSimChanged;
            client.Self.MoneyBalanceReply += OnMoneyBalanceReply;
        }
        
        StartCoroutine(UpdateStatusDisplay());
    }

    void OnDestroy()
    {
        if (client != null)
        {
            client.Network.LoginCompleted -= OnLoginCompleted;
            client.Network.SimChanged -= OnSimChanged;
            client.Self.MoneyBalanceReply -= OnMoneyBalanceReply;
        }
    }

    System.Collections.IEnumerator UpdateStatusDisplay()
    {
        while (true)
        {
            UpdateStatus();
            yield return new WaitForSeconds(1.0f);
        }
    }

    void UpdateStatus()
    {
        if (client == null) return;
        
        // Connection status
        if (connectionStatus)
        {
            connectionStatus.text = ClientManager.active ? "Connected" : "Disconnected";
            connectionStatus.color = ClientManager.active ? Color.green : Color.red;
        }
        
        // Region name
        if (regionName && client.Network.CurrentSim != null)
        {
            regionName.text = client.Network.CurrentSim.Name;
        }
        
        // Avatar position
        if (avatarPosition)
        {
            var pos = client.Self.SimPosition;
            avatarPosition.text = $"<{pos.X:F0}, {pos.Y:F0}, {pos.Z:F0}>";
        }
        
        // Frame rate
        if (frameRate)
        {
            float fps = 1.0f / Time.unscaledDeltaTime;
            frameRate.text = $"FPS: {fps:F0}";
        }
        
        // Health bar (SL doesn't have health, so always full)
        if (healthBar)
        {
            healthBar.value = 1.0f;
        }
    }

    void ToggleMenu(string menuName)
    {
        if (!openMenus.ContainsKey(menuName)) return;
        
        GameObject menu = openMenus[menuName];
        bool wasActive = menu.activeSelf;
        
        // Hide all other menus
        HideAllMenus();
        
        // Toggle this menu
        menu.SetActive(!wasActive);
    }

    void HideAllMenus()
    {
        foreach (var menu in openMenus.Values)
        {
            if (menu) menu.SetActive(false);
        }
        
        // Also hide context menus
        if (avatarContextMenu) avatarContextMenu.SetActive(false);
        if (objectContextMenu) objectContextMenu.SetActive(false);
        if (landContextMenu) landContextMenu.SetActive(false);
    }

    void Update()
    {
        // Hide menus when clicking elsewhere
        if (Input.GetMouseButtonDown(0))
        {
            bool clickedOnMenu = false;
            
            foreach (var menu in openMenus.Values)
            {
                if (menu && menu.activeSelf && RectTransformUtility.RectangleContainsScreenPoint(
                    menu.GetComponent<RectTransform>(), Input.mousePosition))
                {
                    clickedOnMenu = true;
                    break;
                }
            }
            
            if (!clickedOnMenu)
            {
                HideAllMenus();
            }
        }
        
        // Handle keyboard shortcuts
        HandleKeyboardShortcuts();
    }

    void HandleKeyboardShortcuts()
    {
        if (Input.GetKeyDown(KeyCode.F1)) ShowKeyboardShortcuts();
        if (Input.GetKeyDown(KeyCode.F2)) ToggleBuildMode();
        if (Input.GetKeyDown(KeyCode.F3)) ShowWorldMap();
        if (Input.GetKeyDown(KeyCode.F4)) ShowSearch();
        if (Input.GetKeyDown(KeyCode.F5)) ToggleFly();
        
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            if (Input.GetKeyDown(KeyCode.I)) ShowInventory();
            if (Input.GetKeyDown(KeyCode.P)) ShowMyProfile();
            if (Input.GetKeyDown(KeyCode.G)) ShowGroups();
            if (Input.GetKeyDown(KeyCode.F)) ShowFriends();
            if (Input.GetKeyDown(KeyCode.M)) ShowWorldMap();
            if (Input.GetKeyDown(KeyCode.E)) ShowPreferences();
            if (Input.GetKeyDown(KeyCode.H)) GoHome();
        }
    }

    #region World Menu Actions

    void GoHome()
    {
        if (client == null) return;
        
        client.Self.Teleport(UUID.Zero); // Teleport home
        HideAllMenus();
        
        if (uiManager)
        {
            uiManager.QueueNotification("Teleport", "Teleporting home...", 3.0f, UIManager.NotificationType.Info);
        }
    }

    void ShowTeleportOptions()
    {
        // This could show a teleport history or favorites
        Debug.Log("Show teleport options");
        HideAllMenus();
    }

    void ShowLandmarks()
    {
        // This could show saved landmarks
        Debug.Log("Show landmarks");
        HideAllMenus();
    }

    void ShowWorldMap()
    {
        if (worldMap) worldMap.ShowWorldMap();
        HideAllMenus();
    }

    void ShowSunPosition()
    {
        if (windlightManager) windlightManager.ShowWindlightEditor();
        HideAllMenus();
    }

    void ShowEnvironmentSettings()
    {
        if (windlightManager) windlightManager.ShowWindlightEditor();
        HideAllMenus();
    }

    #endregion

    #region Avatar Menu Actions

    void ShowAppearance()
    {
        if (inventoryBrowser) inventoryBrowser.ToggleInventoryWindow();
        HideAllMenus();
    }

    void ShowAttachments()
    {
        if (inventoryBrowser) inventoryBrowser.ToggleInventoryWindow();
        HideAllMenus();
    }

    void ShowGestures()
    {
        Debug.Log("Show gestures (not implemented)");
        HideAllMenus();
    }

    void ShowAnimations()
    {
        Debug.Log("Show animations (not implemented)");
        HideAllMenus();
    }

    void ShowStatus()
    {
        if (profileViewer && client != null)
        {
            profileViewer.ShowProfile(client.Self.AgentID);
        }
        HideAllMenus();
    }

    void ShowPreferences()
    {
        if (preferencesWindow) preferencesWindow.ShowPreferences();
        HideAllMenus();
    }

    #endregion

    #region Build Menu Actions

    void ShowCreateTools()
    {
        if (buildTools) buildTools.ShowBuildTools();
        HideAllMenus();
    }

    void ShowEditTools()
    {
        if (buildTools) buildTools.ShowBuildTools();
        HideAllMenus();
    }

    void ShowBuildTools()
    {
        if (buildTools) buildTools.ShowBuildTools();
        HideAllMenus();
    }

    void ShowScriptEditor()
    {
        if (scriptEditor) scriptEditor.ShowScriptEditor();
        HideAllMenus();
    }

    void ShowMediaTools()
    {
        if (mediaManager) mediaManager.ShowMediaPlayer();
        HideAllMenus();
    }

    void ShowLandTools()
    {
        Debug.Log("Show land tools (not implemented)");
        HideAllMenus();
    }

    #endregion

    #region Communicate Menu Actions

    void FocusLocalChat()
    {
        if (chatSystem && chatSystem.chatInput)
        {
            chatSystem.chatInput.Select();
            chatSystem.chatInput.ActivateInputField();
        }
        HideAllMenus();
    }

    void ShowIMOptions()
    {
        Debug.Log("Show IM options");
        HideAllMenus();
    }

    void ShowFriends()
    {
        Debug.Log("Show friends list");
        HideAllMenus();
    }

    void ShowGroups()
    {
        if (groupManager) groupManager.ShowGroupManager();
        HideAllMenus();
    }

    void ShowMyProfile()
    {
        if (profileViewer && client != null)
        {
            profileViewer.ShowProfile(client.Self.AgentID);
        }
        HideAllMenus();
    }

    void ShowSearch()
    {
        if (searchWindow) searchWindow.ShowSearchWindow();
        HideAllMenus();
    }

    #endregion

    #region Help Menu Actions

    void ShowAbout()
    {
        string aboutText = $"Crystal Frost Second Life Viewer\n" +
                          $"Version: {Application.version}\n" +
                          $"Unity: {Application.unityVersion}\n" +
                          $"Built with LibreMetaverse\n\n" +
                          $"An open-source Second Life viewer\n" +
                          $"designed for modern virtual worlds.";
        
        if (uiManager)
        {
            uiManager.QueueNotification("About Crystal Frost", aboutText, 8.0f, UIManager.NotificationType.Info);
        }
        
        HideAllMenus();
    }

    void ShowKeyboardShortcuts()
    {
        string shortcuts = "Keyboard Shortcuts:\n\n" +
                          "F1 - Help\n" +
                          "F2 - Toggle Build Mode\n" +
                          "F3 - World Map\n" +
                          "F4 - Search\n" +
                          "F5 - Toggle Fly\n\n" +
                          "Ctrl+I - Inventory\n" +
                          "Ctrl+P - Profile\n" +
                          "Ctrl+G - Groups\n" +
                          "Ctrl+F - Friends\n" +
                          "Ctrl+M - Map\n" +
                          "Ctrl+E - Preferences\n" +
                          "Ctrl+H - Go Home";
        
        if (uiManager)
        {
            uiManager.QueueNotification("Keyboard Shortcuts", shortcuts, 8.0f, UIManager.NotificationType.Info);
        }
        
        HideAllMenus();
    }

    void ShowBugReport()
    {
        Debug.Log("Show bug report form");
        HideAllMenus();
    }

    void ShowFeedback()
    {
        Debug.Log("Show feedback form");
        HideAllMenus();
    }

    void OpenDocumentation()
    {
        Application.OpenURL("https://github.com/crystal-frost/documentation");
        HideAllMenus();
    }

    #endregion

    #region Quick Toolbar Actions

    void ToggleFly()
    {
        if (client == null) return;
        
        bool newFlyState = !client.Self.Movement.Fly;
        client.Self.Movement.Fly = newFlyState;
        client.Self.Movement.SendUpdate();
        
        // Update button appearance
        if (flyToggleButton)
        {
            var colors = flyToggleButton.colors;
            colors.normalColor = newFlyState ? Color.yellow : Color.white;
            flyToggleButton.colors = colors;
        }
        
        if (uiManager)
        {
            uiManager.QueueNotification("Flight", newFlyState ? "Flight enabled" : "Flight disabled", 2.0f);
        }
    }

    void ToggleBuildMode()
    {
        isBuildModeEnabled = !isBuildModeEnabled;
        
        if (isBuildModeEnabled)
        {
            if (buildTools) buildTools.ShowBuildTools();
        }
        else
        {
            if (buildTools) buildTools.HideBuildTools();
        }
        
        // Update button appearance
        if (buildModeButton)
        {
            var colors = buildModeButton.colors;
            colors.normalColor = isBuildModeEnabled ? Color.yellow : Color.white;
            buildModeButton.colors = colors;
        }
        
        if (uiManager)
        {
            uiManager.QueueNotification("Build Mode", isBuildModeEnabled ? "Build mode enabled" : "Build mode disabled", 2.0f);
        }
    }

    void ToggleVoiceChat()
    {
        isVoiceChatEnabled = !isVoiceChatEnabled;
        
        // Update button appearance
        if (voiceChatButton)
        {
            var colors = voiceChatButton.colors;
            colors.normalColor = isVoiceChatEnabled ? Color.green : Color.white;
            voiceChatButton.colors = colors;
        }
        
        if (uiManager)
        {
            uiManager.QueueNotification("Voice Chat", isVoiceChatEnabled ? "Voice chat enabled" : "Voice chat disabled", 2.0f);
        }
    }

    void ToggleMusicStream()
    {
        isMusicStreamEnabled = !isMusicStreamEnabled;
        
        if (mediaManager)
        {
            if (isMusicStreamEnabled)
            {
                // mediaManager.StartMusicStream();
            }
            else
            {
                mediaManager.StopAllMedia();
            }
        }
        
        // Update button appearance
        if (streamMusicButton)
        {
            var colors = streamMusicButton.colors;
            colors.normalColor = isMusicStreamEnabled ? Color.cyan : Color.white;
            streamMusicButton.colors = colors;
        }
        
        if (uiManager)
        {
            uiManager.QueueNotification("Music Stream", isMusicStreamEnabled ? "Music enabled" : "Music disabled", 2.0f);
        }
    }

    void ToggleAnimations()
    {
        areAnimationsPaused = !areAnimationsPaused;
        
        // Update button appearance
        if (pauseAnimationsButton)
        {
            var colors = pauseAnimationsButton.colors;
            colors.normalColor = areAnimationsPaused ? Color.red : Color.white;
            pauseAnimationsButton.colors = colors;
        }
        
        if (uiManager)
        {
            uiManager.QueueNotification("Animations", areAnimationsPaused ? "Animations paused" : "Animations resumed", 2.0f);
        }
    }

    #endregion

    #region Event Handlers

    void OnLoginCompleted(object sender, LoginCompleteEventArgs e)
    {
        if (e.Success && client != null)
        {
            // Request money balance
            client.Self.RequestBalance();
            
            if (uiManager)
            {
                uiManager.QueueNotification("Login", $"Welcome, {client.Self.Name}!", 3.0f, UIManager.NotificationType.Success);
            }
        }
    }

    void OnSimChanged(object sender, SimChangedEventArgs e)
    {
        if (uiManager)
        {
            uiManager.QueueNotification("Region", $"Entered {e.PreviousSimulator.Name}", 3.0f);
        }
    }

    void OnMoneyBalanceReply(object sender, MoneyBalanceReplyEventArgs e)
    {
        if (moneyBalance)
        {
            moneyBalance.text = $"L${e.Balance}";
        }
    }

    #endregion

    #region Context Menu Methods

    public void ShowAvatarContextMenu(Vector3 position, UUID avatarID)
    {
        if (avatarContextMenu == null) return;
        
        HideAllMenus();
        avatarContextMenu.SetActive(true);
        avatarContextMenu.transform.position = position;
        
        // Setup context menu actions
        SetupAvatarContextMenu(avatarID);
    }

    public void ShowObjectContextMenu(Vector3 position, uint localID)
    {
        if (objectContextMenu == null) return;
        
        HideAllMenus();
        objectContextMenu.SetActive(true);
        objectContextMenu.transform.position = position;
        
        // Setup context menu actions
        SetupObjectContextMenu(localID);
    }

    public void ShowLandContextMenu(Vector3 position)
    {
        if (landContextMenu == null) return;
        
        HideAllMenus();
        landContextMenu.SetActive(true);
        landContextMenu.transform.position = position;
        
        // Setup context menu actions
        SetupLandContextMenu(position);
    }

    void SetupAvatarContextMenu(UUID avatarID)
    {
        // Add context menu items for avatar interactions
        Debug.Log($"Setup avatar context menu for {avatarID}");
    }

    void SetupObjectContextMenu(uint localID)
    {
        // Add context menu items for object interactions
        Debug.Log($"Setup object context menu for {localID}");
    }

    void SetupLandContextMenu(Vector3 position)
    {
        // Add context menu items for land interactions
        Debug.Log($"Setup land context menu at {position}");
    }

    #endregion

    #region Utility Methods

    void ShowInventory()
    {
        if (inventoryBrowser) inventoryBrowser.ToggleInventoryWindow();
    }

    public void ShowMarketplace()
    {
        if (marketplace) marketplace.ShowMarketplace();
    }

    public void OpenScriptEditor()
    {
        if (scriptEditor) scriptEditor.ShowScriptEditor();
    }

    public void OpenMediaPlayer()
    {
        if (mediaManager) mediaManager.ShowMediaPlayer();
    }

    public void ShowWindlightEditor()
    {
        if (windlightManager) windlightManager.ShowWindlightEditor();
    }

    public bool IsMenuOpen(string menuName)
    {
        return openMenus.ContainsKey(menuName) && openMenus[menuName].activeSelf;
    }

    public bool IsAnyMenuOpen()
    {
        foreach (var menu in openMenus.Values)
        {
            if (menu && menu.activeSelf) return true;
        }
        return false;
    }

    #endregion
}