using OpenMetaverse;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ProfileViewer : MonoBehaviour
{
    [Header("Window Management")]
    public GameObject profileWindow;
    public Button closeButton;
    
    [Header("Profile Tabs")]
    public Button profileTabButton;
    public Button picksTabButton;
    public Button classifiedsTabButton;
    public Button notesTabButton;
    public GameObject profileTab;
    public GameObject picksTab;
    public GameObject classifiedsTab;
    public GameObject notesTab;
    
    [Header("Profile Info")]
    public RawImage profileImage;
    public TMP_Text avatarNameText;
    public TMP_Text bornDateText;
    public TMP_Text accountTypeText;
    public TMP_Text aboutText;
    public TMP_Text firstLifeText;
    public TMP_Text partnerNameText;
    public Button partnerButton;
    
    [Header("Profile Actions")]
    public Button imButton;
    public Button addFriendButton;
    public Button payButton;
    public Button teleportButton;
    public Button muteButton;
    public Button reportButton;
    
    [Header("Groups Display")]
    public Transform groupsRoot;
    public GameObject groupItemPrefab;
    
    [Header("Picks Display")]
    public Transform picksRoot;
    public GameObject pickItemPrefab;
    
    [Header("Notes")]
    public TMP_InputField notesField;
    public Button saveNotesButton;
    
    private GridClient client;
    private UUID currentProfileID;
    private Avatar.AvatarProperties currentProfile;
    private Dictionary<UUID, string> avatarNames = new();
    private Dictionary<UUID, Texture2D> profileImages = new();
    private string personalNotes = "";
    
    public class GroupInfo
    {
        public UUID groupID;
        public string groupName;
        public string title;
        public bool acceptNotices;
        public bool listInProfile;
    }
    
    public class PickInfo
    {
        public UUID pickID;
        public string name;
        public string description;
        public Vector3 position;
        public UUID snapshotID;
        public bool enabled;
    }

    void Awake()
    {
        profileWindow.SetActive(false);
        SetupUI();
    }

    void SetupUI()
    {
        if (closeButton) closeButton.onClick.AddListener(() => profileWindow.SetActive(false));
        
        // Setup tab buttons
        if (profileTabButton) profileTabButton.onClick.AddListener(() => SwitchToTab(0));
        if (picksTabButton) picksTabButton.onClick.AddListener(() => SwitchToTab(1));
        if (classifiedsTabButton) classifiedsTabButton.onClick.AddListener(() => SwitchToTab(2));
        if (notesTabButton) notesTabButton.onClick.AddListener(() => SwitchToTab(3));
        
        // Setup action buttons
        if (imButton) imButton.onClick.AddListener(StartIM);
        if (addFriendButton) addFriendButton.onClick.AddListener(AddFriend);
        if (payButton) payButton.onClick.AddListener(PayMoney);
        if (teleportButton) teleportButton.onClick.AddListener(OfferTeleport);
        if (muteButton) muteButton.onClick.AddListener(ToggleMute);
        if (reportButton) reportButton.onClick.AddListener(ReportUser);
        if (partnerButton) partnerButton.onClick.AddListener(ShowPartnerProfile);
        if (saveNotesButton) saveNotesButton.onClick.AddListener(SaveNotes);
        
        SwitchToTab(0);
    }

    void Start()
    {
        client = ClientManager.client;
        
        if (client != null)
        {
            client.Avatars.AvatarPropertiesReply += OnAvatarPropertiesReply;
            client.Avatars.AvatarGroupsReply += OnAvatarGroupsReply;
            client.Avatars.AvatarPicksReply += OnAvatarPicksReply;
            client.Avatars.PickInfoReply += OnPickInfoReply;
            client.Avatars.UUIDNameReply += OnUUIDNameReply;
            client.Assets.ImageReceived += OnImageReceived;
        }
    }

    void OnDestroy()
    {
        if (client != null)
        {
            client.Avatars.AvatarPropertiesReply -= OnAvatarPropertiesReply;
            client.Avatars.AvatarGroupsReply -= OnAvatarGroupsReply;
            client.Avatars.AvatarPicksReply -= OnAvatarPicksReply;
            client.Avatars.PickInfoReply -= OnPickInfoReply;
            client.Avatars.UUIDNameReply -= OnUUIDNameReply;
            client.Assets.ImageReceived -= OnImageReceived;
        }
    }

    public void ShowProfile(UUID avatarID)
    {
        if (avatarID == UUID.Zero) return;
        
        currentProfileID = avatarID;
        profileWindow.SetActive(true);
        
        // Clear current data
        ClearProfileData();
        
        // Request profile information
        RequestProfileData(avatarID);
        
        // Load personal notes
        LoadPersonalNotes(avatarID);
    }

    void ClearProfileData()
    {
        if (avatarNameText) avatarNameText.text = "Loading...";
        if (bornDateText) bornDateText.text = "";
        if (accountTypeText) accountTypeText.text = "";
        if (aboutText) aboutText.text = "";
        if (firstLifeText) firstLifeText.text = "";
        if (partnerNameText) partnerNameText.text = "";
        if (profileImage) profileImage.texture = null;
        
        // Clear groups
        foreach (Transform child in groupsRoot)
        {
            Destroy(child.gameObject);
        }
        
        // Clear picks
        foreach (Transform child in picksRoot)
        {
            Destroy(child.gameObject);
        }
    }

    void RequestProfileData(UUID avatarID)
    {
        if (client == null) return;
        
        // Request basic profile properties
        client.Avatars.RequestAvatarProperties(avatarID);
        
        // Request groups
        client.Avatars.RequestAvatarGroups(avatarID);
        
        // Request picks
        client.Avatars.RequestAvatarPicks(avatarID);
        
        // Request name if we don't have it
        if (!avatarNames.ContainsKey(avatarID))
        {
            client.Avatars.RequestAvatarName(avatarID);
        }
        else
        {
            if (avatarNameText) avatarNameText.text = avatarNames[avatarID];
        }
    }

    void SwitchToTab(int tabIndex)
    {
        // Hide all tabs
        if (profileTab) profileTab.SetActive(tabIndex == 0);
        if (picksTab) picksTab.SetActive(tabIndex == 1);
        if (classifiedsTab) classifiedsTab.SetActive(tabIndex == 2);
        if (notesTab) notesTab.SetActive(tabIndex == 3);
        
        // Update button states (visual feedback would be added here)
    }

    #region Event Handlers

    void OnAvatarPropertiesReply(object sender, AvatarPropertiesReplyEventArgs e)
    {
        if (e.AvatarID != currentProfileID) return;
        
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            currentProfile = e.Properties;
            UpdateProfileDisplay();
        });
    }

    void OnAvatarGroupsReply(object sender, AvatarGroupsReplyEventArgs e)
    {
        if (e.AvatarID != currentProfileID) return;
        
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            DisplayGroups(e.Groups);
        });
    }

    void OnAvatarPicksReply(object sender, AvatarPicksReplyEventArgs e)
    {
        if (e.AvatarID != currentProfileID) return;
        
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            foreach (var pick in e.Picks)
            {
                // Request detailed pick info
                client.Avatars.RequestPickInfo(e.AvatarID, pick.Key);
            }
        });
    }

    void OnPickInfoReply(object sender, PickInfoReplyEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            CreatePickItem(e.Pick);
        });
    }

    void OnUUIDNameReply(object sender, UUIDNameReplyEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            foreach (var nameKVP in e.Names)
            {
                avatarNames[nameKVP.Key] = nameKVP.Value;
                
                if (nameKVP.Key == currentProfileID && avatarNameText)
                {
                    avatarNameText.text = nameKVP.Value;
                }
                
                // Update partner name if this is the partner
                if (currentProfile != null && nameKVP.Key == currentProfile.Partner && partnerNameText)
                {
                    partnerNameText.text = nameKVP.Value;
                }
            }
        });
    }

    void OnImageReceived(object sender, ImageReceivedEventArgs e)
    {
        if (currentProfile != null && e.ImageID == currentProfile.ProfileImage)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                var texture = new Texture2D(e.Image.Width, e.Image.Height);
                texture.LoadImage(e.Image.ExportJPEG());
                profileImages[e.ImageID] = texture;
                
                if (profileImage) profileImage.texture = texture;
            });
        }
    }

    #endregion

    void UpdateProfileDisplay()
    {
        if (currentProfile == null) return;
        
        // Update basic info
        if (bornDateText) bornDateText.text = "Born: " + currentProfile.BornOn;
        if (accountTypeText) accountTypeText.text = GetAccountTypeString(currentProfile.CharterMember);
        if (aboutText) aboutText.text = currentProfile.AboutText;
        if (firstLifeText) firstLifeText.text = currentProfile.FirstLifeText;
        
        // Update partner info
        if (currentProfile.Partner != UUID.Zero)
        {
            if (avatarNames.ContainsKey(currentProfile.Partner))
            {
                if (partnerNameText) partnerNameText.text = avatarNames[currentProfile.Partner];
            }
            else
            {
                client.Avatars.RequestAvatarName(currentProfile.Partner);
                if (partnerNameText) partnerNameText.text = "Loading...";
            }
            
            if (partnerButton) partnerButton.gameObject.SetActive(true);
        }
        else
        {
            if (partnerNameText) partnerNameText.text = "None";
            if (partnerButton) partnerButton.gameObject.SetActive(false);
        }
        
        // Request profile image
        if (currentProfile.ProfileImage != UUID.Zero)
        {
            if (profileImages.ContainsKey(currentProfile.ProfileImage))
            {
                if (profileImage) profileImage.texture = profileImages[currentProfile.ProfileImage];
            }
            else
            {
                client.Assets.RequestImage(currentProfile.ProfileImage, ImageType.Normal);
            }
        }
        
        // Update action button states
        UpdateActionButtons();
    }

    string GetAccountTypeString(bool charterMember)
    {
        return charterMember ? "Charter Member" : "Resident";
    }

    void UpdateActionButtons()
    {
        bool isSelf = currentProfileID == client.Self.AgentID;
        
        // Disable certain actions for self
        if (imButton) imButton.gameObject.SetActive(!isSelf);
        if (addFriendButton) addFriendButton.gameObject.SetActive(!isSelf);
        if (payButton) payButton.gameObject.SetActive(!isSelf);
        if (teleportButton) teleportButton.gameObject.SetActive(!isSelf);
        if (muteButton) muteButton.gameObject.SetActive(!isSelf);
        if (reportButton) reportButton.gameObject.SetActive(!isSelf);
        
        // Update friend button text
        if (addFriendButton && !isSelf)
        {
            bool isFriend = IsFriend(currentProfileID);
            var buttonText = addFriendButton.GetComponentInChildren<TMP_Text>();
            if (buttonText) buttonText.text = isFriend ? "Remove Friend" : "Add Friend";
        }
        
        // Update mute button text
        if (muteButton && !isSelf)
        {
            bool isMuted = IsMuted(currentProfileID);
            var buttonText = muteButton.GetComponentInChildren<TMP_Text>();
            if (buttonText) buttonText.text = isMuted ? "Unmute" : "Mute";
        }
    }

    void DisplayGroups(Dictionary<UUID, string> groups)
    {
        // Clear existing groups
        foreach (Transform child in groupsRoot)
        {
            Destroy(child.gameObject);
        }
        
        // Create group items
        foreach (var group in groups)
        {
            CreateGroupItem(group.Key, group.Value);
        }
    }

    void CreateGroupItem(UUID groupID, string groupName)
    {
        if (groupItemPrefab == null || groupsRoot == null) return;
        
        var groupObj = Instantiate(groupItemPrefab, groupsRoot);
        var nameText = groupObj.GetComponentInChildren<TMP_Text>();
        var button = groupObj.GetComponent<Button>();
        
        if (nameText) nameText.text = groupName;
        
        if (button)
        {
            button.onClick.AddListener(() => ShowGroupInfo(groupID));
        }
    }

    void CreatePickItem(Avatar.AvatarPickerReply pick)
    {
        if (pickItemPrefab == null || picksRoot == null) return;
        
        var pickObj = Instantiate(pickItemPrefab, picksRoot);
        var nameText = pickObj.transform.Find("NameText")?.GetComponent<TMP_Text>();
        var descText = pickObj.transform.Find("DescriptionText")?.GetComponent<TMP_Text>();
        var button = pickObj.GetComponent<Button>();
        
        if (nameText) nameText.text = pick.Name;
        if (descText) descText.text = pick.Desc;
        
        if (button)
        {
            button.onClick.AddListener(() => TeleportToPick(pick));
        }
    }

    #region Action Handlers

    void StartIM()
    {
        if (ClientManager.chat != null)
        {
            ClientManager.chat.StartIM(currentProfileID);
            ClientManager.chatWindow?.SwitchToIM(currentProfileID);
        }
    }

    void AddFriend()
    {
        if (client == null) return;
        
        bool isFriend = IsFriend(currentProfileID);
        
        if (isFriend)
        {
            // Remove friend
            client.Friends.TerminateFriendship(currentProfileID);
        }
        else
        {
            // Add friend
            string name = avatarNames.ContainsKey(currentProfileID) ? avatarNames[currentProfileID] : "Unknown";
            client.Friends.OfferFriendship(currentProfileID, $"Friend request from {client.Self.Name}");
        }
        
        UpdateActionButtons();
    }

    void PayMoney()
    {
        // This would open a pay dialog
        Debug.Log($"Pay money to {currentProfileID}");
    }

    void OfferTeleport()
    {
        if (client == null) return;
        
        string message = $"Teleport offer from {client.Self.Name}";
        client.Self.SendTeleportLure(currentProfileID, message);
    }

    void ToggleMute()
    {
        if (client == null) return;
        
        bool isMuted = IsMuted(currentProfileID);
        
        if (isMuted)
        {
            client.Self.UnmuteUser(currentProfileID);
        }
        else
        {
            client.Self.MuteUser(currentProfileID, "User muted");
        }
        
        UpdateActionButtons();
    }

    void ReportUser()
    {
        // This would open a report dialog
        Debug.Log($"Report user {currentProfileID}");
    }

    void ShowPartnerProfile()
    {
        if (currentProfile != null && currentProfile.Partner != UUID.Zero)
        {
            ShowProfile(currentProfile.Partner);
        }
    }

    void ShowGroupInfo(UUID groupID)
    {
        // This would show detailed group information
        Debug.Log($"Show group info for {groupID}");
    }

    void TeleportToPick(Avatar.AvatarPickerReply pick)
    {
        if (client == null) return;
        
        // This would need to extract region and position from the pick
        Debug.Log($"Teleport to pick: {pick.Name}");
    }

    #endregion

    #region Utility Methods

    bool IsFriend(UUID avatarID)
    {
        if (client == null) return false;
        
        return client.Friends.FriendList.Find(f => f.UUID == avatarID) != null;
    }

    bool IsMuted(UUID avatarID)
    {
        if (client == null) return false;
        
        return client.Self.MuteList.Find(m => m.ID == avatarID) != null;
    }

    void LoadPersonalNotes(UUID avatarID)
    {
        string key = $"PersonalNotes_{avatarID}";
        personalNotes = PlayerPrefs.GetString(key, "");
        
        if (notesField) notesField.text = personalNotes;
    }

    void SaveNotes()
    {
        if (notesField == null) return;
        
        personalNotes = notesField.text;
        string key = $"PersonalNotes_{currentProfileID}";
        PlayerPrefs.SetString(key, personalNotes);
        PlayerPrefs.Save();
        
        Debug.Log("Personal notes saved");
    }

    #endregion

    // Public method to show profile by name
    public void ShowProfileByName(string avatarName)
    {
        // Find avatar ID by name
        foreach (var kvp in avatarNames)
        {
            if (kvp.Value.Equals(avatarName, System.StringComparison.OrdinalIgnoreCase))
            {
                ShowProfile(kvp.Key);
                return;
            }
        }
        
        // If not found in cache, this would need to do a directory search
        Debug.Log($"Avatar '{avatarName}' not found in cache");
    }
}