using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OpenMetaverse;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [Header("Main Menu Bar")]
    public Button inventoryButton;
    public Button preferencesButton;
    public Button worldMapButton;
    public Button searchButton;
    public Button buildButton;
    public Button groupsButton;
    public Button friendsButton;
    public Button profileButton;
    
    [Header("Quick Actions")]
    public Button flyButton;
    public Button runButton;
    public Button sitButton;
    public Button standButton;
    public Button homeButton;
    
    [Header("Status Bar")]
    public TMP_Text regionNameText;
    public TMP_Text coordinatesText;
    public TMP_Text fpsText;
    public TMP_Text avatarNameText;
    public TMP_Text balanceText;
    public Slider healthBar;
    
    [Header("Minimap")]
    public RawImage minimapImage;
    public Transform minimapAvatarDot;
    public Button minimapButton;
    
    [Header("Chat Bar")]
    public Button chatButton;
    public TMP_Text chatPreview;
    
    [Header("Windows")]
    public InventoryBrowser inventoryBrowser;
    public PreferencesWindow preferencesWindow;
    public WorldMap worldMap;
    public SearchWindow searchWindow;
    public BuildTools buildTools;
    public GroupManager groupManager;
    public ProfileViewer profileViewer;
    
    [Header("Context Menus")]
    public GameObject avatarContextMenu;
    public GameObject objectContextMenu;
    public GameObject groundContextMenu;
    
    [Header("Notifications")]
    public GameObject notificationPanel;
    public Transform notificationRoot;
    public GameObject notificationPrefab;
    
    private GridClient client;
    private bool flyModeEnabled = false;
    private bool runModeEnabled = false;
    private Dictionary<string, GameObject> openWindows = new();
    private Queue<NotificationData> notificationQueue = new();
    
    public class NotificationData
    {
        public string title;
        public string message;
        public float duration;
        public NotificationType type;
    }
    
    public enum NotificationType
    {
        Info,
        Warning,
        Error,
        Success,
        FriendRequest,
        GroupInvite,
        TeleportOffer
    }

    void Awake()
    {
        SetupUI();
    }

    void SetupUI()
    {
        // Main menu buttons
        if (inventoryButton) inventoryButton.onClick.AddListener(() => ToggleWindow("Inventory"));
        if (preferencesButton) preferencesButton.onClick.AddListener(() => ToggleWindow("Preferences"));
        if (worldMapButton) worldMapButton.onClick.AddListener(() => ToggleWindow("WorldMap"));
        if (searchButton) searchButton.onClick.AddListener(() => ToggleWindow("Search"));
        if (buildButton) buildButton.onClick.AddListener(() => ToggleWindow("Build"));
        if (groupsButton) groupsButton.onClick.AddListener(() => ToggleWindow("Groups"));
        if (friendsButton) friendsButton.onClick.AddListener(() => ShowFriendsList());
        if (profileButton) profileButton.onClick.AddListener(() => ShowMyProfile());
        
        // Quick action buttons
        if (flyButton) flyButton.onClick.AddListener(ToggleFly);
        if (runButton) runButton.onClick.AddListener(ToggleRun);
        if (sitButton) sitButton.onClick.AddListener(SitDown);
        if (standButton) standButton.onClick.AddListener(StandUp);
        if (homeButton) homeButton.onClick.AddListener(GoHome);
        
        // Minimap
        if (minimapButton) minimapButton.onClick.AddListener(() => ToggleWindow("WorldMap"));
        
        // Chat
        if (chatButton) chatButton.onClick.AddListener(OpenChat);
        
        // Initialize windows dictionary
        if (inventoryBrowser) openWindows["Inventory"] = inventoryBrowser.gameObject;
        if (preferencesWindow) openWindows["Preferences"] = preferencesWindow.gameObject;
        if (worldMap) openWindows["WorldMap"] = worldMap.gameObject;
        if (searchWindow) openWindows["Search"] = searchWindow.gameObject;
        if (buildTools) openWindows["Build"] = buildTools.gameObject;
        if (groupManager) openWindows["Groups"] = groupManager.gameObject;
        if (profileViewer) openWindows["Profile"] = profileViewer.gameObject;
    }

    void Start()
    {
        client = ClientManager.client;
        
        if (client != null)
        {
            client.Network.LoginCompleted += OnLoginCompleted;
            client.Network.SimChanged += OnSimChanged;
            client.Friends.FriendshipOffered += OnFriendshipOffered;
            client.Groups.GroupInvitation += OnGroupInvitation;
            client.Self.TeleportProgress += OnTeleportProgress;
            client.Self.MoneyBalanceReply += OnMoneyBalanceReply;
        }
        
        StartCoroutine(UpdateUI());
    }

    void OnDestroy()
    {
        if (client != null)
        {
            client.Network.LoginCompleted -= OnLoginCompleted;
            client.Network.SimChanged -= OnSimChanged;
            client.Friends.FriendshipOffered -= OnFriendshipOffered;
            client.Groups.GroupInvitation -= OnGroupInvitation;
            client.Self.TeleportProgress -= OnTeleportProgress;
            client.Self.MoneyBalanceReply -= OnMoneyBalanceReply;
        }
    }

    System.Collections.IEnumerator UpdateUI()
    {
        while (true)
        {
            if (ClientManager.active && client != null)
            {
                UpdateStatusBar();
                UpdateMinimap();
                UpdateFPS();
                ProcessNotifications();
            }
            
            yield return new WaitForSeconds(0.5f);
        }
    }

    void UpdateStatusBar()
    {
        if (client.Network.CurrentSim != null)
        {
            if (regionNameText) regionNameText.text = client.Network.CurrentSim.Name;
            
            var pos = client.Self.SimPosition;
            if (coordinatesText) coordinatesText.text = $"<{pos.X:F0}, {pos.Y:F0}, {pos.Z:F0}>";
        }
        
        if (avatarNameText) avatarNameText.text = client.Self.Name;
        
        // Update health bar (if applicable)
        if (healthBar) healthBar.value = 1.0f; // SL doesn't have health, so always full
    }

    void UpdateMinimap()
    {
        if (minimapImage == null || minimapAvatarDot == null) return;
        
        // Update avatar dot position on minimap
        var pos = client.Self.SimPosition;
        var simSize = client.Network.CurrentSim != null ? client.Network.CurrentSim.SizeX : 256;
        
        float normalizedX = pos.X / simSize;
        float normalizedY = pos.Y / simSize;
        
        var rectTransform = minimapAvatarDot.GetComponent<RectTransform>();
        if (rectTransform)
        {
            var minimapRect = minimapImage.rectTransform.rect;
            rectTransform.anchoredPosition = new Vector2(
                normalizedX * minimapRect.width - minimapRect.width * 0.5f,
                normalizedY * minimapRect.height - minimapRect.height * 0.5f
            );
        }
        
        // Rotate avatar dot based on avatar rotation
        var rotation = client.Self.SimRotation;
        minimapAvatarDot.rotation = UnityEngine.Quaternion.Euler(0, 0, -rotation.GetEulerAngles().Z * Mathf.Rad2Deg);
    }

    void UpdateFPS()
    {
        if (fpsText)
        {
            float fps = 1.0f / Time.unscaledDeltaTime;
            fpsText.text = $"FPS: {fps:F0}";
        }
    }

    void ProcessNotifications()
    {
        while (notificationQueue.Count > 0)
        {
            var notification = notificationQueue.Dequeue();
            ShowNotification(notification);
        }
    }

    #region Window Management

    void ToggleWindow(string windowName)
    {
        if (!openWindows.ContainsKey(windowName)) return;
        
        var window = openWindows[windowName];
        bool isActive = window.activeSelf;
        
        if (!isActive)
        {
            // Show window
            switch (windowName)
            {
                case "Inventory":
                    inventoryBrowser?.ToggleInventoryWindow();
                    break;
                case "Preferences":
                    preferencesWindow?.ShowPreferences();
                    break;
                case "WorldMap":
                    worldMap?.ShowWorldMap();
                    break;
                case "Search":
                    searchWindow?.ShowSearchWindow();
                    break;
                case "Build":
                    buildTools?.ShowBuildTools();
                    break;
                case "Groups":
                    groupManager?.ShowGroupManager();
                    break;
                default:
                    window.SetActive(true);
                    break;
            }
        }
        else
        {
            // Hide window
            switch (windowName)
            {
                case "Build":
                    buildTools?.HideBuildTools();
                    break;
                default:
                    window.SetActive(false);
                    break;
            }
        }
    }

    #endregion

    #region Quick Actions

    void ToggleFly()
    {
        if (client == null) return;
        
        flyModeEnabled = !flyModeEnabled;
        client.Self.Movement.Fly = flyModeEnabled;
        client.Self.Movement.SendUpdate();
        
        // Update button appearance
        if (flyButton)
        {
            var colors = flyButton.colors;
            colors.normalColor = flyModeEnabled ? Color.yellow : Color.white;
            flyButton.colors = colors;
        }
        
        ShowNotification(new NotificationData
        {
            title = "Flight",
            message = flyModeEnabled ? "Flight enabled" : "Flight disabled",
            duration = 2.0f,
            type = NotificationType.Info
        });
    }

    void ToggleRun()
    {
        if (client == null) return;
        
        runModeEnabled = !runModeEnabled;
        client.Self.Movement.AlwaysRun = runModeEnabled;
        client.Self.Movement.SendUpdate();
        
        // Update button appearance
        if (runButton)
        {
            var colors = runButton.colors;
            colors.normalColor = runModeEnabled ? Color.yellow : Color.white;
            runButton.colors = colors;
        }
    }

    void SitDown()
    {
        if (client == null) return;
        
        client.Self.Movement.SitOnGround = true;
        client.Self.Movement.SendUpdate();
    }

    void StandUp()
    {
        if (client == null) return;
        
        client.Self.Movement.StandUp = true;
        client.Self.Movement.SendUpdate();
    }

    void GoHome()
    {
        if (client == null) return;
        
        client.Self.Teleport(UUID.Zero); // Teleport home
        
        ShowNotification(new NotificationData
        {
            title = "Teleport",
            message = "Teleporting home...",
            duration = 3.0f,
            type = NotificationType.Info
        });
    }

    #endregion

    #region Profile and Friends

    void ShowMyProfile()
    {
        if (profileViewer && client != null)
        {
            profileViewer.ShowProfile(client.Self.AgentID);
        }
    }

    void ShowFriendsList()
    {
        // This could show a friends window or integrate with contacts
        if (ClientManager.chatWindow)
        {
            ClientManager.chatWindow.ContactsButton();
        }
    }

    #endregion

    #region Chat

    void OpenChat()
    {
        if (ClientManager.chatWindow)
        {
            // Focus on chat input
            var chatInput = FindObjectOfType<Chat>()?.input;
            if (chatInput)
            {
                chatInput.Select();
                chatInput.ActivateInputField();
            }
        }
    }

    public void UpdateChatPreview(string message)
    {
        if (chatPreview && !string.IsNullOrEmpty(message))
        {
            chatPreview.text = message.Length > 50 ? message.Substring(0, 50) + "..." : message;
        }
    }

    #endregion

    #region Context Menus

    public void ShowAvatarContextMenu(Vector3 position, UUID avatarID)
    {
        if (avatarContextMenu == null) return;
        
        avatarContextMenu.SetActive(true);
        avatarContextMenu.transform.position = position;
        
        // Setup context menu actions for avatar
        SetupAvatarContextMenu(avatarID);
    }

    public void ShowObjectContextMenu(Vector3 position, uint localID)
    {
        if (objectContextMenu == null) return;
        
        objectContextMenu.SetActive(true);
        objectContextMenu.transform.position = position;
        
        // Setup context menu actions for object
        SetupObjectContextMenu(localID);
    }

    public void ShowGroundContextMenu(Vector3 position)
    {
        if (groundContextMenu == null) return;
        
        groundContextMenu.SetActive(true);
        groundContextMenu.transform.position = position;
        
        // Setup context menu actions for ground
        SetupGroundContextMenu(position);
    }

    void SetupAvatarContextMenu(UUID avatarID)
    {
        // Add buttons for: Profile, IM, Add Friend, Teleport Offer, etc.
    }

    void SetupObjectContextMenu(uint localID)
    {
        // Add buttons for: Touch, Sit, Buy, Inspect, etc.
    }

    void SetupGroundContextMenu(Vector3 position)
    {
        // Add buttons for: Teleport Here, Create Object, etc.
    }

    #endregion

    #region Notifications

    public void ShowNotification(NotificationData notification)
    {
        if (notificationPrefab == null || notificationRoot == null) return;
        
        var notificationObj = Instantiate(notificationPrefab, notificationRoot);
        var titleText = notificationObj.transform.Find("TitleText")?.GetComponent<TMP_Text>();
        var messageText = notificationObj.transform.Find("MessageText")?.GetComponent<TMP_Text>();
        var icon = notificationObj.transform.Find("Icon")?.GetComponent<Image>();
        
        if (titleText) titleText.text = notification.title;
        if (messageText) messageText.text = notification.message;
        
        if (icon)
        {
            icon.color = GetNotificationColor(notification.type);
        }
        
        // Auto-remove notification after duration
        Destroy(notificationObj, notification.duration);
    }

    Color GetNotificationColor(NotificationType type)
    {
        switch (type)
        {
            case NotificationType.Info: return Color.blue;
            case NotificationType.Warning: return Color.yellow;
            case NotificationType.Error: return Color.red;
            case NotificationType.Success: return Color.green;
            case NotificationType.FriendRequest: return Color.cyan;
            case NotificationType.GroupInvite: return Color.magenta;
            case NotificationType.TeleportOffer: return Color.white;
            default: return Color.gray;
        }
    }

    public void QueueNotification(string title, string message, float duration = 3.0f, NotificationType type = NotificationType.Info)
    {
        notificationQueue.Enqueue(new NotificationData
        {
            title = title,
            message = message,
            duration = duration,
            type = type
        });
    }

    #endregion

    #region Event Handlers

    void OnLoginCompleted(object sender, LoginCompleteEventArgs e)
    {
        if (e.Success)
        {
            ShowNotification(new NotificationData
            {
                title = "Login",
                message = "Successfully logged in!",
                duration = 3.0f,
                type = NotificationType.Success
            });
            
            // Request money balance
            client.Self.RequestBalance();
        }
        else
        {
            ShowNotification(new NotificationData
            {
                title = "Login Failed",
                message = e.Message,
                duration = 5.0f,
                type = NotificationType.Error
            });
        }
    }

    void OnSimChanged(object sender, SimChangedEventArgs e)
    {
        ShowNotification(new NotificationData
        {
            title = "Region Change",
            message = $"Entered {e.PreviousSimulator.Name}",
            duration = 3.0f,
            type = NotificationType.Info
        });
    }

    void OnFriendshipOffered(object sender, FriendshipOfferedEventArgs e)
    {
        ShowNotification(new NotificationData
        {
            title = "Friend Request",
            message = $"{e.AgentName} wants to be your friend",
            duration = 10.0f,
            type = NotificationType.FriendRequest
        });
    }

    void OnGroupInvitation(object sender, GroupInvitationEventArgs e)
    {
        ShowNotification(new NotificationData
        {
            title = "Group Invite",
            message = $"Invited to join {e.GroupName}",
            duration = 10.0f,
            type = NotificationType.GroupInvite
        });
    }

    void OnTeleportProgress(object sender, TeleportEventArgs e)
    {
        ShowNotification(new NotificationData
        {
            title = "Teleport",
            message = e.Message,
            duration = 3.0f,
            type = NotificationType.Info
        });
    }

    void OnMoneyBalanceReply(object sender, MoneyBalanceReplyEventArgs e)
    {
        if (balanceText)
        {
            balanceText.text = $"L${e.Balance}";
        }
    }

    #endregion
}