using OpenMetaverse;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.IO;
using System;

public class EnhancedChatSystem : MonoBehaviour
{
    [Header("Chat Window")]
    public GameObject chatWindow;
    public Button minimizeButton;
    public Button maximizeButton;
    public Button settingsButton;
    
    [Header("Chat Tabs")]
    public Transform tabButtonsRoot;
    public GameObject tabButtonPrefab;
    public Transform chatPanelsRoot;
    public GameObject chatPanelPrefab;
    
    [Header("Chat Input")]
    public TMP_InputField chatInput;
    public Button sendButton;
    public TMP_Dropdown chatChannelDropdown;
    public Button emotesButton;
    public Button attachmentButton;
    
    [Header("Chat Display")]
    public ScrollRect chatScrollRect;
    public Transform chatContentRoot;
    public GameObject chatMessagePrefab;
    public GameObject systemMessagePrefab;
    
    [Header("Users List")]
    public GameObject usersPanel;
    public Transform usersListRoot;
    public GameObject userItemPrefab;
    public TMP_Text usersCountText;
    
    [Header("Chat Settings")]
    public GameObject chatSettingsPanel;
    public Slider fontSizeSlider;
    public Toggle timestampsToggle;
    public Toggle soundsToggle;
    public Toggle logChatToggle;
    public ColorPicker chatColorPicker;
    public Toggle filterProfanityToggle;
    
    [Header("Emotes Panel")]
    public GameObject emotesPanel;
    public Transform emotesGridRoot;
    public GameObject emoteButtonPrefab;
    
    private GridClient client;
    private Dictionary<string, ChatTab> chatTabs = new();
    private ChatTab activeTab;
    private List<ChatUser> nearbyUsers = new();
    private Dictionary<UUID, string> userNames = new();
    private List<string> chatHistory = new();
    private int chatHistoryIndex = 0;
    private bool isMinimized = false;
    
    // Chat channels
    private const int PUBLIC_CHANNEL = 0;
    private const int DEBUG_CHANNEL = 2147483647;
    
    public class ChatTab
    {
        public string name;
        public ChatType type;
        public UUID targetID; // For IM tabs
        public List<ChatMessage> messages = new();
        public GameObject tabButton;
        public GameObject chatPanel;
        public Transform contentRoot;
        public bool hasUnreadMessages = false;
        public int unreadCount = 0;
    }
    
    public enum ChatType
    {
        Local,
        IM,
        Group,
        Conference,
        System
    }
    
    public class ChatMessage
    {
        public string senderName;
        public UUID senderID;
        public string message;
        public ChatType type;
        public int channel;
        public DateTime timestamp;
        public Color color;
        public bool isSystem;
    }
    
    public class ChatUser
    {
        public UUID id;
        public string name;
        public Vector3 position;
        public float distance;
        public bool isTyping;
        public bool isMuted;
    }

    void Awake()
    {
        SetupUI();
        InitializeChatTabs();
    }

    void SetupUI()
    {
        if (minimizeButton) minimizeButton.onClick.AddListener(ToggleMinimize);
        if (maximizeButton) maximizeButton.onClick.AddListener(ToggleMinimize);
        if (settingsButton) settingsButton.onClick.AddListener(ShowChatSettings);
        
        if (sendButton) sendButton.onClick.AddListener(SendMessage);
        if (emotesButton) emotesButton.onClick.AddListener(ShowEmotes);
        if (attachmentButton) attachmentButton.onClick.AddListener(SendAttachment);
        
        if (chatInput)
        {
            chatInput.onEndEdit.AddListener(OnChatInputSubmit);
            chatInput.onValueChanged.AddListener(OnChatInputChanged);
        }
        
        if (chatChannelDropdown) chatChannelDropdown.onValueChanged.AddListener(OnChannelChanged);
        
        // Setup chat settings
        if (fontSizeSlider) fontSizeSlider.onValueChanged.AddListener(OnFontSizeChanged);
        if (timestampsToggle) timestampsToggle.onValueChanged.AddListener(OnTimestampsChanged);
        if (soundsToggle) soundsToggle.onValueChanged.AddListener(OnSoundsChanged);
        if (logChatToggle) logChatToggle.onValueChanged.AddListener(OnLogChatChanged);
        if (filterProfanityToggle) filterProfanityToggle.onValueChanged.AddListener(OnFilterProfanityChanged);
        
        SetupChannelDropdown();
        LoadChatSettings();
    }

    void SetupChannelDropdown()
    {
        if (chatChannelDropdown == null) return;
        
        chatChannelDropdown.options.Clear();
        chatChannelDropdown.options.Add(new TMP_Dropdown.OptionData("Say (0)"));
        chatChannelDropdown.options.Add(new TMP_Dropdown.OptionData("Whisper (1)"));
        chatChannelDropdown.options.Add(new TMP_Dropdown.OptionData("Shout (2)"));
        chatChannelDropdown.options.Add(new TMP_Dropdown.OptionData("Debug"));
        chatChannelDropdown.RefreshShownValue();
    }

    void InitializeChatTabs()
    {
        // Create default tabs
        CreateChatTab("Local", ChatType.Local, UUID.Zero);
        CreateChatTab("System", ChatType.System, UUID.Zero);
        
        // Set Local as active tab
        SwitchToTab("Local");
    }

    void Start()
    {
        client = ClientManager.client;
        
        if (client != null)
        {
            client.Self.ChatFromSimulator += OnChatFromSimulator;
            client.Self.InstantMessage += OnInstantMessage;
            client.Avatars.UUIDNameReply += OnUUIDNameReply;
            client.Network.LoginCompleted += OnLoginCompleted;
            client.Objects.AvatarUpdate += OnAvatarUpdate;
        }
        
        // Add system welcome message
        AddSystemMessage("Welcome to Crystal Frost! Type your message and press Enter to chat.");
    }

    void OnDestroy()
    {
        if (client != null)
        {
            client.Self.ChatFromSimulator -= OnChatFromSimulator;
            client.Self.InstantMessage -= OnInstantMessage;
            client.Avatars.UUIDNameReply -= OnUUIDNameReply;
            client.Network.LoginCompleted -= OnLoginCompleted;
            client.Objects.AvatarUpdate -= OnAvatarUpdate;
        }
    }

    ChatTab CreateChatTab(string name, ChatType type, UUID targetID)
    {
        if (chatTabs.ContainsKey(name))
        {
            return chatTabs[name];
        }
        
        var tab = new ChatTab
        {
            name = name,
            type = type,
            targetID = targetID
        };
        
        // Create tab button
        if (tabButtonPrefab && tabButtonsRoot)
        {
            tab.tabButton = Instantiate(tabButtonPrefab, tabButtonsRoot);
            var buttonText = tab.tabButton.GetComponentInChildren<TMP_Text>();
            if (buttonText) buttonText.text = name;
            
            var button = tab.tabButton.GetComponent<Button>();
            if (button)
            {
                button.onClick.AddListener(() => SwitchToTab(name));
            }
        }
        
        // Create chat panel
        if (chatPanelPrefab && chatPanelsRoot)
        {
            tab.chatPanel = Instantiate(chatPanelPrefab, chatPanelsRoot);
            tab.contentRoot = tab.chatPanel.transform.Find("Content");
            tab.chatPanel.SetActive(false);
        }
        
        chatTabs[name] = tab;
        return tab;
    }

    void SwitchToTab(string tabName)
    {
        if (!chatTabs.ContainsKey(tabName)) return;
        
        // Hide current tab
        if (activeTab != null && activeTab.chatPanel)
        {
            activeTab.chatPanel.SetActive(false);
            
            // Update tab button appearance
            var currentButton = activeTab.tabButton?.GetComponent<Button>();
            if (currentButton)
            {
                var colors = currentButton.colors;
                colors.normalColor = Color.white;
                currentButton.colors = colors;
            }
        }
        
        // Show new tab
        activeTab = chatTabs[tabName];
        if (activeTab.chatPanel)
        {
            activeTab.chatPanel.SetActive(true);
        }
        
        // Update tab button appearance
        var newButton = activeTab.tabButton?.GetComponent<Button>();
        if (newButton)
        {
            var colors = newButton.colors;
            colors.normalColor = Color.yellow;
            newButton.colors = colors;
        }
        
        // Clear unread messages
        activeTab.hasUnreadMessages = false;
        activeTab.unreadCount = 0;
        UpdateTabNotification(activeTab);
        
        // Scroll to bottom
        StartCoroutine(ScrollToBottom());
    }

    System.Collections.IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        
        if (chatScrollRect)
        {
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    void UpdateTabNotification(ChatTab tab)
    {
        if (tab.tabButton == null) return;
        
        var buttonText = tab.tabButton.GetComponentInChildren<TMP_Text>();
        if (buttonText)
        {
            string displayName = tab.name;
            if (tab.hasUnreadMessages && tab.unreadCount > 0)
            {
                displayName += $" ({tab.unreadCount})";
            }
            buttonText.text = displayName;
        }
        
        // Change button color for unread messages
        var button = tab.tabButton.GetComponent<Button>();
        if (button)
        {
            var colors = button.colors;
            if (tab.hasUnreadMessages && tab != activeTab)
            {
                colors.normalColor = Color.cyan;
            }
            else if (tab == activeTab)
            {
                colors.normalColor = Color.yellow;
            }
            else
            {
                colors.normalColor = Color.white;
            }
            button.colors = colors;
        }
    }

    #region Event Handlers

    void OnChatFromSimulator(object sender, ChatEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            var message = new ChatMessage
            {
                senderName = e.FromName,
                senderID = e.SourceID,
                message = e.Message,
                type = ChatType.Local,
                channel = e.Channel,
                timestamp = DateTime.Now,
                color = GetChatColor(e.Type),
                isSystem = e.Type == ChatType.System
            };
            
            // Add to appropriate tab
            string tabName = "Local";
            if (e.Type == ChatType.System)
            {
                tabName = "System";
            }
            
            AddMessageToTab(tabName, message);
            
            // Update nearby users
            UpdateNearbyUser(e.SourceID, e.FromName, e.Position);
        });
    }

    void OnInstantMessage(object sender, InstantMessageEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            // Create IM tab if it doesn't exist
            string tabName = e.IM.FromAgentName;
            if (!chatTabs.ContainsKey(tabName))
            {
                CreateChatTab(tabName, ChatType.IM, e.IM.FromAgentID);
            }
            
            var message = new ChatMessage
            {
                senderName = e.IM.FromAgentName,
                senderID = e.IM.FromAgentID,
                message = e.IM.Message,
                type = ChatType.IM,
                timestamp = DateTime.Now,
                color = Color.blue
            };
            
            AddMessageToTab(tabName, message);
            
            // Play IM sound
            if (soundsToggle && soundsToggle.isOn)
            {
                PlayIMSound();
            }
        });
    }

    void OnUUIDNameReply(object sender, UUIDNameReplyEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            foreach (var nameKVP in e.Names)
            {
                userNames[nameKVP.Key] = nameKVP.Value;
                
                // Update user in nearby list
                var user = nearbyUsers.Find(u => u.id == nameKVP.Key);
                if (user != null)
                {
                    user.name = nameKVP.Value;
                    UpdateUsersDisplay();
                }
            }
        });
    }

    void OnLoginCompleted(object sender, LoginCompleteEventArgs e)
    {
        if (e.Success)
        {
            AddSystemMessage($"Logged in as {client.Self.Name}");
        }
    }

    void OnAvatarUpdate(object sender, AvatarUpdateEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            // Update nearby users based on avatar updates
            UpdateNearbyUser(e.Avatar.ID, "", e.Avatar.Position.ToVector3());
        });
    }

    #endregion

    void AddMessageToTab(string tabName, ChatMessage message)
    {
        if (!chatTabs.ContainsKey(tabName)) return;
        
        var tab = chatTabs[tabName];
        tab.messages.Add(message);
        
        // Limit message history
        if (tab.messages.Count > 1000)
        {
            tab.messages.RemoveAt(0);
        }
        
        // Create message display
        CreateMessageDisplay(tab, message);
        
        // Update unread count if not active tab
        if (tab != activeTab)
        {
            tab.hasUnreadMessages = true;
            tab.unreadCount++;
            UpdateTabNotification(tab);
        }
        
        // Log message if enabled
        if (logChatToggle && logChatToggle.isOn)
        {
            LogMessage(message);
        }
        
        // Auto-scroll if at bottom
        if (tab == activeTab)
        {
            StartCoroutine(ScrollToBottom());
        }
    }

    void CreateMessageDisplay(ChatTab tab, ChatMessage message)
    {
        if (tab.contentRoot == null) return;
        
        GameObject messagePrefab = message.isSystem ? systemMessagePrefab : chatMessagePrefab;
        if (messagePrefab == null) return;
        
        var messageObj = Instantiate(messagePrefab, tab.contentRoot);
        
        var timestampText = messageObj.transform.Find("Timestamp")?.GetComponent<TMP_Text>();
        var nameText = messageObj.transform.Find("SenderName")?.GetComponent<TMP_Text>();
        var messageText = messageObj.transform.Find("MessageText")?.GetComponent<TMP_Text>();
        
        // Set timestamp
        if (timestampText && timestampsToggle && timestampsToggle.isOn)
        {
            timestampText.text = message.timestamp.ToString("HH:mm");
            timestampText.gameObject.SetActive(true);
        }
        else if (timestampText)
        {
            timestampText.gameObject.SetActive(false);
        }
        
        // Set sender name
        if (nameText && !message.isSystem)
        {
            nameText.text = message.senderName + ":";
            nameText.color = message.color;
        }
        else if (nameText)
        {
            nameText.gameObject.SetActive(false);
        }
        
        // Set message text
        if (messageText)
        {
            string displayMessage = message.message;
            
            // Filter profanity if enabled
            if (filterProfanityToggle && filterProfanityToggle.isOn)
            {
                displayMessage = FilterProfanity(displayMessage);
            }
            
            messageText.text = displayMessage;
            messageText.color = message.isSystem ? Color.gray : Color.white;
            
            // Apply font size
            if (fontSizeSlider)
            {
                messageText.fontSize = fontSizeSlider.value;
            }
        }
    }

    void SendMessage()
    {
        if (chatInput == null || string.IsNullOrEmpty(chatInput.text)) return;
        
        string message = chatInput.text.Trim();
        chatInput.text = "";
        
        // Add to chat history
        chatHistory.Add(message);
        if (chatHistory.Count > 50)
        {
            chatHistory.RemoveAt(0);
        }
        chatHistoryIndex = chatHistory.Count;
        
        // Check for commands
        if (message.StartsWith("/"))
        {
            ProcessCommand(message);
            return;
        }
        
        // Send message based on active tab
        if (activeTab.type == ChatType.Local)
        {
            SendLocalMessage(message);
        }
        else if (activeTab.type == ChatType.IM)
        {
            SendInstantMessage(message, activeTab.targetID);
        }
    }

    void SendLocalMessage(string message)
    {
        if (client == null) return;
        
        ChatType chatType = ChatType.Normal;
        int channel = PUBLIC_CHANNEL;
        
        // Determine chat type based on channel dropdown
        if (chatChannelDropdown)
        {
            switch (chatChannelDropdown.value)
            {
                case 0: chatType = ChatType.Normal; break;
                case 1: chatType = ChatType.Whisper; break;
                case 2: chatType = ChatType.Shout; break;
                case 3: channel = DEBUG_CHANNEL; break;
            }
        }
        
        client.Self.Chat(message, channel, chatType);
        
        // Add our own message to chat
        var ourMessage = new ChatMessage
        {
            senderName = client.Self.Name,
            senderID = client.Self.AgentID,
            message = message,
            type = ChatType.Local,
            channel = channel,
            timestamp = DateTime.Now,
            color = GetChatColor(chatType)
        };
        
        AddMessageToTab("Local", ourMessage);
    }

    void SendInstantMessage(string message, UUID targetID)
    {
        if (client == null || targetID == UUID.Zero) return;
        
        client.Self.SendInstantMessage(message, targetID);
        
        // Add our message to IM tab
        string targetName = userNames.ContainsKey(targetID) ? userNames[targetID] : "Unknown";
        
        var ourMessage = new ChatMessage
        {
            senderName = client.Self.Name,
            senderID = client.Self.AgentID,
            message = message,
            type = ChatType.IM,
            timestamp = DateTime.Now,
            color = Color.green
        };
        
        AddMessageToTab(targetName, ourMessage);
    }

    void ProcessCommand(string command)
    {
        string[] parts = command.Split(' ');
        string cmd = parts[0].ToLower();
        
        switch (cmd)
        {
            case "/help":
                ShowCommands();
                break;
            case "/clear":
                ClearActiveTab();
                break;
            case "/time":
                AddSystemMessage($"Current time: {DateTime.Now}");
                break;
            case "/pos":
                if (client != null)
                {
                    var pos = client.Self.SimPosition;
                    AddSystemMessage($"Position: <{pos.X:F1}, {pos.Y:F1}, {pos.Z:F1}>");
                }
                break;
            case "/fly":
                if (client != null)
                {
                    client.Self.Movement.Fly = !client.Self.Movement.Fly;
                    client.Self.Movement.SendUpdate();
                    AddSystemMessage(client.Self.Movement.Fly ? "Flying enabled" : "Flying disabled");
                }
                break;
            case "/sit":
                if (client != null)
                {
                    client.Self.Movement.SitOnGround = true;
                    client.Self.Movement.SendUpdate();
                    AddSystemMessage("Sitting down");
                }
                break;
            case "/stand":
                if (client != null)
                {
                    client.Self.Movement.StandUp = true;
                    client.Self.Movement.SendUpdate();
                    AddSystemMessage("Standing up");
                }
                break;
            default:
                AddSystemMessage($"Unknown command: {cmd}. Type /help for available commands.");
                break;
        }
    }

    void ShowCommands()
    {
        AddSystemMessage("Available commands:");
        AddSystemMessage("/help - Show this help");
        AddSystemMessage("/clear - Clear current chat tab");
        AddSystemMessage("/time - Show current time");
        AddSystemMessage("/pos - Show current position");
        AddSystemMessage("/fly - Toggle flight");
        AddSystemMessage("/sit - Sit down");
        AddSystemMessage("/stand - Stand up");
    }

    void ClearActiveTab()
    {
        if (activeTab?.contentRoot == null) return;
        
        // Clear messages
        activeTab.messages.Clear();
        
        // Clear UI
        foreach (Transform child in activeTab.contentRoot)
        {
            Destroy(child.gameObject);
        }
        
        AddSystemMessage("Chat cleared");
    }

    void AddSystemMessage(string message)
    {
        var systemMessage = new ChatMessage
        {
            senderName = "System",
            message = message,
            type = ChatType.System,
            timestamp = DateTime.Now,
            color = Color.gray,
            isSystem = true
        };
        
        AddMessageToTab("System", systemMessage);
    }

    Color GetChatColor(ChatType chatType)
    {
        switch (chatType)
        {
            case ChatType.Normal: return Color.white;
            case ChatType.Whisper: return Color.gray;
            case ChatType.Shout: return Color.red;
            case ChatType.System: return Color.yellow;
            default: return Color.white;
        }
    }

    void UpdateNearbyUser(UUID userID, string name, Vector3 position)
    {
        if (userID == client?.Self.AgentID) return; // Don't add ourselves
        
        var existingUser = nearbyUsers.Find(u => u.id == userID);
        if (existingUser != null)
        {
            existingUser.position = position;
            if (!string.IsNullOrEmpty(name))
            {
                existingUser.name = name;
            }
            
            // Calculate distance
            if (client != null)
            {
                existingUser.distance = Vector3.Distance(position, client.Self.SimPosition.ToVector3());
            }
        }
        else
        {
            var newUser = new ChatUser
            {
                id = userID,
                name = string.IsNullOrEmpty(name) ? "Loading..." : name,
                position = position,
                distance = client != null ? Vector3.Distance(position, client.Self.SimPosition.ToVector3()) : 0f
            };
            
            nearbyUsers.Add(newUser);
            
            // Request name if we don't have it
            if (string.IsNullOrEmpty(name) && !userNames.ContainsKey(userID))
            {
                client?.Avatars.RequestAvatarName(userID);
            }
        }
        
        UpdateUsersDisplay();
    }

    void UpdateUsersDisplay()
    {
        if (usersListRoot == null) return;
        
        // Clear existing user items
        foreach (Transform child in usersListRoot)
        {
            Destroy(child.gameObject);
        }
        
        // Sort users by distance
        var sortedUsers = nearbyUsers.OrderBy(u => u.distance).ToList();
        
        // Create user items
        foreach (var user in sortedUsers.Take(20)) // Limit display
        {
            CreateUserItem(user);
        }
        
        // Update count
        if (usersCountText)
        {
            usersCountText.text = $"Nearby: {nearbyUsers.Count}";
        }
    }

    void CreateUserItem(ChatUser user)
    {
        if (userItemPrefab == null) return;
        
        var userObj = Instantiate(userItemPrefab, usersListRoot);
        var nameText = userObj.GetComponentInChildren<TMP_Text>();
        var button = userObj.GetComponent<Button>();
        
        if (nameText)
        {
            nameText.text = $"{user.name} ({user.distance:F0}m)";
            if (user.isMuted) nameText.color = Color.red;
            if (user.isTyping) nameText.text += " ✎";
        }
        
        if (button)
        {
            button.onClick.AddListener(() => StartIMWithUser(user));
        }
    }

    void StartIMWithUser(ChatUser user)
    {
        // Create or switch to IM tab
        string tabName = user.name;
        if (!chatTabs.ContainsKey(tabName))
        {
            CreateChatTab(tabName, ChatType.IM, user.id);
        }
        
        SwitchToTab(tabName);
    }

    #region UI Event Handlers

    void OnChatInputSubmit(string text)
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SendMessage();
        }
    }

    void OnChatInputChanged(string text)
    {
        // Handle chat history navigation
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            NavigateChatHistory(-1);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            NavigateChatHistory(1);
        }
    }

    void NavigateChatHistory(int direction)
    {
        if (chatHistory.Count == 0) return;
        
        chatHistoryIndex = Mathf.Clamp(chatHistoryIndex + direction, 0, chatHistory.Count);
        
        if (chatHistoryIndex < chatHistory.Count)
        {
            chatInput.text = chatHistory[chatHistoryIndex];
            chatInput.caretPosition = chatInput.text.Length;
        }
        else
        {
            chatInput.text = "";
        }
    }

    void OnChannelChanged(int value)
    {
        // Channel changed - update UI if needed
    }

    void ToggleMinimize()
    {
        isMinimized = !isMinimized;
        
        // Show/hide chat panels
        if (chatPanelsRoot) chatPanelsRoot.gameObject.SetActive(!isMinimized);
        if (usersPanel) usersPanel.SetActive(!isMinimized);
        
        // Update button visibility
        if (minimizeButton) minimizeButton.gameObject.SetActive(!isMinimized);
        if (maximizeButton) maximizeButton.gameObject.SetActive(isMinimized);
    }

    void ShowChatSettings()
    {
        if (chatSettingsPanel)
        {
            chatSettingsPanel.SetActive(!chatSettingsPanel.activeSelf);
        }
    }

    void ShowEmotes()
    {
        if (emotesPanel)
        {
            emotesPanel.SetActive(!emotesPanel.activeSelf);
            
            if (emotesPanel.activeSelf)
            {
                CreateEmoteButtons();
            }
        }
    }

    void CreateEmoteButtons()
    {
        if (emotesGridRoot == null || emoteButtonPrefab == null) return;
        
        // Clear existing emotes
        foreach (Transform child in emotesGridRoot)
        {
            Destroy(child.gameObject);
        }
        
        // Common emotes
        string[] emotes = { "smile", "laugh", "frown", "wink", "nod", "shake", "wave", "bow", "dance", "clap" };
        
        foreach (string emote in emotes)
        {
            var emoteObj = Instantiate(emoteButtonPrefab, emotesGridRoot);
            var emoteText = emoteObj.GetComponentInChildren<TMP_Text>();
            var button = emoteObj.GetComponent<Button>();
            
            if (emoteText) emoteText.text = emote;
            
            if (button)
            {
                string emoteAction = emote;
                button.onClick.AddListener(() => PerformEmote(emoteAction));
            }
        }
    }

    void PerformEmote(string emote)
    {
        if (client == null) return;
        
        // Send emote message
        string emoteMessage = $"/me {emote}s";
        client.Self.Chat(emoteMessage, 0, ChatType.Normal);
        
        // Add to local chat
        var emoteMsg = new ChatMessage
        {
            senderName = client.Self.Name,
            senderID = client.Self.AgentID,
            message = emoteMessage,
            type = ChatType.Local,
            timestamp = DateTime.Now,
            color = Color.magenta
        };
        
        AddMessageToTab("Local", emoteMsg);
        
        // Hide emotes panel
        if (emotesPanel) emotesPanel.SetActive(false);
    }

    void SendAttachment()
    {
        // This would open a file dialog to send attachments
        Debug.Log("Send attachment (not implemented)");
    }

    #endregion

    #region Settings Event Handlers

    void OnFontSizeChanged(float size)
    {
        // Update font size for all messages
        var allMessages = FindObjectsOfType<TMP_Text>().Where(t => t.transform.IsChildOf(chatContentRoot));
        foreach (var message in allMessages)
        {
            message.fontSize = size;
        }
    }

    void OnTimestampsChanged(bool enabled)
    {
        // Refresh message display
        RefreshActiveTab();
    }

    void OnSoundsChanged(bool enabled)
    {
        // Enable/disable chat sounds
    }

    void OnLogChatChanged(bool enabled)
    {
        // Enable/disable chat logging
    }

    void OnFilterProfanityChanged(bool enabled)
    {
        // Refresh message display
        RefreshActiveTab();
    }

    #endregion

    void RefreshActiveTab()
    {
        if (activeTab?.contentRoot == null) return;
        
        // Clear and recreate all messages
        foreach (Transform child in activeTab.contentRoot)
        {
            Destroy(child.gameObject);
        }
        
        foreach (var message in activeTab.messages)
        {
            CreateMessageDisplay(activeTab, message);
        }
        
        StartCoroutine(ScrollToBottom());
    }

    string FilterProfanity(string message)
    {
        // Basic profanity filter - replace with asterisks
        string[] badWords = { "damn", "hell", "crap" }; // Add more as needed
        
        foreach (string word in badWords)
        {
            string replacement = new string('*', word.Length);
            message = System.Text.RegularExpressions.Regex.Replace(
                message, word, replacement, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        
        return message;
    }

    void LogMessage(ChatMessage message)
    {
        string logPath = Path.Combine(Application.persistentDataPath, "ChatLogs");
        Directory.CreateDirectory(logPath);
        
        string fileName = $"chat_{DateTime.Now:yyyy-MM-dd}.log";
        string filePath = Path.Combine(logPath, fileName);
        
        string logEntry = $"[{message.timestamp:HH:mm:ss}] {message.senderName}: {message.message}\n";
        
        try
        {
            File.AppendAllText(filePath, logEntry);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to log chat message: {ex.Message}");
        }
    }

    void LoadChatSettings()
    {
        // Load chat settings from PlayerPrefs
        if (fontSizeSlider) fontSizeSlider.value = PlayerPrefs.GetFloat("ChatFontSize", 14f);
        if (timestampsToggle) timestampsToggle.isOn = PlayerPrefs.GetInt("ChatTimestamps", 1) == 1;
        if (soundsToggle) soundsToggle.isOn = PlayerPrefs.GetInt("ChatSounds", 1) == 1;
        if (logChatToggle) logChatToggle.isOn = PlayerPrefs.GetInt("ChatLogging", 0) == 1;
        if (filterProfanityToggle) filterProfanityToggle.isOn = PlayerPrefs.GetInt("ChatFilter", 0) == 1;
    }

    void SaveChatSettings()
    {
        if (fontSizeSlider) PlayerPrefs.SetFloat("ChatFontSize", fontSizeSlider.value);
        if (timestampsToggle) PlayerPrefs.SetInt("ChatTimestamps", timestampsToggle.isOn ? 1 : 0);
        if (soundsToggle) PlayerPrefs.SetInt("ChatSounds", soundsToggle.isOn ? 1 : 0);
        if (logChatToggle) PlayerPrefs.SetInt("ChatLogging", logChatToggle.isOn ? 1 : 0);
        if (filterProfanityToggle) PlayerPrefs.SetInt("ChatFilter", filterProfanityToggle.isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    void PlayIMSound()
    {
        // Play IM notification sound
        var audioSource = GetComponent<AudioSource>();
        if (audioSource && audioSource.clip)
        {
            audioSource.Play();
        }
    }

    // Public methods for external control
    public void StartIM(UUID targetID)
    {
        if (!userNames.ContainsKey(targetID))
        {
            client?.Avatars.RequestAvatarName(targetID);
        }
        
        string targetName = userNames.ContainsKey(targetID) ? userNames[targetID] : targetID.ToString();
        
        if (!chatTabs.ContainsKey(targetName))
        {
            CreateChatTab(targetName, ChatType.IM, targetID);
        }
        
        SwitchToTab(targetName);
    }

    public void SwitchToIM(UUID targetID)
    {
        string targetName = userNames.ContainsKey(targetID) ? userNames[targetID] : targetID.ToString();
        
        if (chatTabs.ContainsKey(targetName))
        {
            SwitchToTab(targetName);
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus)
        {
            SaveChatSettings();
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            SaveChatSettings();
        }
    }
}