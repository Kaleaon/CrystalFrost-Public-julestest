using OpenMetaverse;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class GroupManager : MonoBehaviour
{
    [Header("Window Management")]
    public GameObject groupWindow;
    public Button closeButton;
    
    [Header("Group List")]
    public Transform groupListRoot;
    public GameObject groupItemPrefab;
    public Button refreshGroupsButton;
    
    [Header("Group Details")]
    public GameObject groupDetailsPanel;
    public TMP_Text groupNameText;
    public TMP_Text groupCharterText;
    public TMP_Text memberCountText;
    public RawImage groupInsigniaImage;
    public Toggle showInProfileToggle;
    public Toggle receiveNoticesToggle;
    public Toggle listInProfileToggle;
    
    [Header("Group Actions")]
    public Button activateGroupButton;
    public Button leaveGroupButton;
    public Button groupInfoButton;
    public Button groupChatButton;
    public Button inviteMemberButton;
    public Button ejectMemberButton;
    
    [Header("Group Chat")]
    public GameObject groupChatPanel;
    public Transform chatRoot;
    public TMP_InputField chatInput;
    public Button sendChatButton;
    public ScrollRect chatScrollRect;
    
    [Header("Members List")]
    public GameObject membersPanel;
    public Transform membersRoot;
    public GameObject memberItemPrefab;
    public TMP_Dropdown roleFilterDropdown;
    
    [Header("Roles & Titles")]
    public GameObject rolesPanel;
    public Transform rolesRoot;
    public GameObject roleItemPrefab;
    
    [Header("Group Notices")]
    public GameObject noticesPanel;
    public Transform noticesRoot;
    public GameObject noticeItemPrefab;
    public Button createNoticeButton;
    
    private GridClient client;
    private Dictionary<UUID, Group> myGroups = new();
    private Dictionary<UUID, List<GroupMember>> groupMembers = new();
    private Dictionary<UUID, List<GroupRole>> groupRoles = new();
    private Dictionary<UUID, List<GroupNoticesListEntry>> groupNotices = new();
    private Group selectedGroup;
    private UUID activeGroupID = UUID.Zero;
    
    public class GroupChatMessage
    {
        public string senderName;
        public string message;
        public System.DateTime timestamp;
        public UUID senderID;
    }
    
    private Dictionary<UUID, List<GroupChatMessage>> groupChatHistory = new();

    void Awake()
    {
        groupWindow.SetActive(false);
        SetupUI();
    }

    void SetupUI()
    {
        if (closeButton) closeButton.onClick.AddListener(() => groupWindow.SetActive(false));
        if (refreshGroupsButton) refreshGroupsButton.onClick.AddListener(RefreshGroups);
        
        // Group action buttons
        if (activateGroupButton) activateGroupButton.onClick.AddListener(ActivateGroup);
        if (leaveGroupButton) leaveGroupButton.onClick.AddListener(LeaveGroup);
        if (groupInfoButton) groupInfoButton.onClick.AddListener(ShowGroupInfo);
        if (groupChatButton) groupChatButton.onClick.AddListener(ToggleGroupChat);
        if (inviteMemberButton) inviteMemberButton.onClick.AddListener(InviteMember);
        if (ejectMemberButton) ejectMemberButton.onClick.AddListener(EjectMember);
        
        // Chat
        if (sendChatButton) sendChatButton.onClick.AddListener(SendGroupChat);
        if (chatInput) chatInput.onEndEdit.AddListener((text) => { if (Input.GetKeyDown(KeyCode.Return)) SendGroupChat(); });
        
        // Toggles
        if (showInProfileToggle) showInProfileToggle.onValueChanged.AddListener(OnShowInProfileChanged);
        if (receiveNoticesToggle) receiveNoticesToggle.onValueChanged.AddListener(OnReceiveNoticesChanged);
        if (listInProfileToggle) listInProfileToggle.onValueChanged.AddListener(OnListInProfileChanged);
        
        // Create notice
        if (createNoticeButton) createNoticeButton.onClick.AddListener(CreateNotice);
        
        // Role filter
        if (roleFilterDropdown) roleFilterDropdown.onValueChanged.AddListener(OnRoleFilterChanged);
    }

    void Start()
    {
        client = ClientManager.client;
        
        if (client != null)
        {
            client.Groups.GroupMembersReply += OnGroupMembersReply;
            client.Groups.GroupRolesDataReply += OnGroupRolesDataReply;
            client.Groups.GroupProfileReply += OnGroupProfileReply;
            client.Groups.GroupNoticesListReply += OnGroupNoticesListReply;
            client.Groups.GroupJoinedReply += OnGroupJoinedReply;
            client.Groups.GroupLeaveReply += OnGroupLeaveReply;
            client.Self.GroupChatJoined += OnGroupChatJoined;
            client.Self.GroupChatLeft += OnGroupChatLeft;
            client.Avatars.UUIDNameReply += OnUUIDNameReply;
        }
        
        RefreshGroups();
    }

    void OnDestroy()
    {
        if (client != null)
        {
            client.Groups.GroupMembersReply -= OnGroupMembersReply;
            client.Groups.GroupRolesDataReply -= OnGroupRolesDataReply;
            client.Groups.GroupProfileReply -= OnGroupProfileReply;
            client.Groups.GroupNoticesListReply -= OnGroupNoticesListReply;
            client.Groups.GroupJoinedReply -= OnGroupJoinedReply;
            client.Groups.GroupLeaveReply -= OnGroupLeaveReply;
            client.Self.GroupChatJoined -= OnGroupChatJoined;
            client.Self.GroupChatLeft -= OnGroupChatLeft;
            client.Avatars.UUIDNameReply -= OnUUIDNameReply;
        }
    }

    public void ShowGroupManager()
    {
        groupWindow.SetActive(true);
        RefreshGroups();
    }

    void RefreshGroups()
    {
        if (client == null) return;
        
        // Get current groups from client
        myGroups.Clear();
        
        // Clear UI
        foreach (Transform child in groupListRoot)
        {
            Destroy(child.gameObject);
        }
        
        // Populate from client groups
        var clientGroups = client.Groups.GroupsCache;
        if (clientGroups != null)
        {
            foreach (var group in clientGroups.Values)
            {
                myGroups[group.ID] = group;
                CreateGroupListItem(group);
            }
        }
        
        // Request fresh group data
        client.Groups.RequestCurrentGroups();
    }

    void CreateGroupListItem(Group group)
    {
        if (groupItemPrefab == null || groupListRoot == null) return;
        
        var groupObj = Instantiate(groupItemPrefab, groupListRoot);
        var nameText = groupObj.GetComponentInChildren<TMP_Text>();
        var button = groupObj.GetComponent<Button>();
        
        if (nameText) nameText.text = group.Name;
        
        if (button)
        {
            button.onClick.AddListener(() => SelectGroup(group));
        }
        
        // Highlight active group
        if (group.ID == activeGroupID)
        {
            var colors = button.colors;
            colors.normalColor = Color.yellow;
            button.colors = colors;
        }
    }

    void SelectGroup(Group group)
    {
        selectedGroup = group;
        ShowGroupDetails(group);
        
        // Request additional group data
        RequestGroupData(group.ID);
    }

    void ShowGroupDetails(Group group)
    {
        if (groupDetailsPanel) groupDetailsPanel.SetActive(true);
        
        if (groupNameText) groupNameText.text = group.Name;
        if (groupCharterText) groupCharterText.text = "Loading charter...";
        if (memberCountText) memberCountText.text = "Loading member count...";
        
        // Update toggles based on group membership info
        var membership = client.Groups.GroupsCache?.ContainsKey(group.ID) == true 
            ? client.Groups.GroupsCache[group.ID] : null;
            
        if (membership != null)
        {
            if (showInProfileToggle) showInProfileToggle.isOn = membership.ListInProfile;
            if (receiveNoticesToggle) receiveNoticesToggle.isOn = membership.AcceptNotices;
        }
        
        UpdateGroupActionButtons();
    }

    void RequestGroupData(UUID groupID)
    {
        if (client == null) return;
        
        // Request group profile
        client.Groups.RequestGroupProfile(groupID);
        
        // Request group members
        client.Groups.RequestGroupMembers(groupID);
        
        // Request group roles
        client.Groups.RequestGroupRoles(groupID);
        
        // Request group notices
        client.Groups.RequestGroupNoticesList(groupID);
    }

    void UpdateGroupActionButtons()
    {
        if (selectedGroup == null) return;
        
        bool isActivated = selectedGroup.ID == activeGroupID;
        bool isMember = myGroups.ContainsKey(selectedGroup.ID);
        
        if (activateGroupButton)
        {
            activateGroupButton.gameObject.SetActive(isMember && !isActivated);
        }
        
        if (leaveGroupButton)
        {
            leaveGroupButton.gameObject.SetActive(isMember);
        }
        
        // Update button texts and states based on permissions
        // This would need more detailed permission checking
    }

    #region Event Handlers

    void OnGroupMembersReply(object sender, GroupMembersReplyEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            groupMembers[e.GroupID] = e.Members;
            
            if (selectedGroup != null && selectedGroup.ID == e.GroupID)
            {
                DisplayGroupMembers(e.Members);
                if (memberCountText) memberCountText.text = $"{e.Members.Count} members";
            }
        });
    }

    void OnGroupRolesDataReply(object sender, GroupRolesDataReplyEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            groupRoles[e.GroupID] = e.Roles;
            
            if (selectedGroup != null && selectedGroup.ID == e.GroupID)
            {
                DisplayGroupRoles(e.Roles);
                UpdateRoleFilter(e.Roles);
            }
        });
    }

    void OnGroupProfileReply(object sender, GroupProfileReplyEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            if (selectedGroup != null && selectedGroup.ID == e.Group.ID)
            {
                if (groupCharterText) groupCharterText.text = e.Group.Charter;
                
                // Request group insignia if available
                if (e.Group.InsigniaID != UUID.Zero)
                {
                    client.Assets.RequestImage(e.Group.InsigniaID, ImageType.Normal);
                }
            }
        });
    }

    void OnGroupNoticesListReply(object sender, GroupNoticesListReplyEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            groupNotices[e.GroupID] = e.Notices;
            
            if (selectedGroup != null && selectedGroup.ID == e.GroupID)
            {
                DisplayGroupNotices(e.Notices);
            }
        });
    }

    void OnGroupJoinedReply(object sender, GroupOperationEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            if (e.Success)
            {
                Debug.Log($"Successfully joined group {e.GroupID}");
                RefreshGroups();
            }
            else
            {
                Debug.Log($"Failed to join group: {e.GroupID}");
            }
        });
    }

    void OnGroupLeaveReply(object sender, GroupOperationEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            if (e.Success)
            {
                Debug.Log($"Successfully left group {e.GroupID}");
                if (selectedGroup != null && selectedGroup.ID == e.GroupID)
                {
                    selectedGroup = null;
                    groupDetailsPanel.SetActive(false);
                }
                RefreshGroups();
            }
            else
            {
                Debug.Log($"Failed to leave group: {e.GroupID}");
            }
        });
    }

    void OnGroupChatJoined(object sender, GroupChatJoinedEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            Debug.Log($"Joined group chat: {e.SessionName}");
            
            if (!groupChatHistory.ContainsKey(e.SessionID))
            {
                groupChatHistory[e.SessionID] = new List<GroupChatMessage>();
            }
        });
    }

    void OnGroupChatLeft(object sender, GroupChatLeftEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            Debug.Log($"Left group chat: {e.SessionID}");
        });
    }

    void OnUUIDNameReply(object sender, UUIDNameReplyEventArgs e)
    {
        // Update member names in the display
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            // Refresh member display if showing members
            if (membersPanel.activeSelf && selectedGroup != null)
            {
                if (groupMembers.ContainsKey(selectedGroup.ID))
                {
                    DisplayGroupMembers(groupMembers[selectedGroup.ID]);
                }
            }
        });
    }

    #endregion

    void DisplayGroupMembers(List<GroupMember> members)
    {
        // Clear existing members
        foreach (Transform child in membersRoot)
        {
            Destroy(child.gameObject);
        }
        
        // Create member items
        foreach (var member in members)
        {
            CreateMemberItem(member);
        }
        
        // Request names for members we don't have
        var unknownMembers = new List<UUID>();
        foreach (var member in members)
        {
            if (!ClientManager.chat.avatarNames.ContainsKey(member.ID))
            {
                unknownMembers.Add(member.ID);
            }
        }
        
        if (unknownMembers.Count > 0)
        {
            client.Avatars.RequestAvatarNames(unknownMembers);
        }
    }

    void CreateMemberItem(GroupMember member)
    {
        if (memberItemPrefab == null || membersRoot == null) return;
        
        var memberObj = Instantiate(memberItemPrefab, membersRoot);
        var nameText = memberObj.transform.Find("NameText")?.GetComponent<TMP_Text>();
        var titleText = memberObj.transform.Find("TitleText")?.GetComponent<TMP_Text>();
        var onlineText = memberObj.transform.Find("OnlineText")?.GetComponent<TMP_Text>();
        var button = memberObj.GetComponent<Button>();
        
        string memberName = ClientManager.chat.avatarNames.ContainsKey(member.ID) 
            ? ClientManager.chat.avatarNames[member.ID] : "Loading...";
            
        if (nameText) nameText.text = memberName;
        if (titleText) titleText.text = member.Title;
        if (onlineText) onlineText.text = member.IsOnline ? "Online" : "Offline";
        
        if (button)
        {
            button.onClick.AddListener(() => ShowMemberProfile(member.ID));
        }
    }

    void DisplayGroupRoles(List<GroupRole> roles)
    {
        // Clear existing roles
        foreach (Transform child in rolesRoot)
        {
            Destroy(child.gameObject);
        }
        
        // Create role items
        foreach (var role in roles)
        {
            CreateRoleItem(role);
        }
    }

    void CreateRoleItem(GroupRole role)
    {
        if (roleItemPrefab == null || rolesRoot == null) return;
        
        var roleObj = Instantiate(roleItemPrefab, rolesRoot);
        var nameText = roleObj.GetComponentInChildren<TMP_Text>();
        
        if (nameText) nameText.text = $"{role.Name} - {role.Description}";
    }

    void DisplayGroupNotices(List<GroupNoticesListEntry> notices)
    {
        // Clear existing notices
        foreach (Transform child in noticesRoot)
        {
            Destroy(child.gameObject);
        }
        
        // Create notice items
        foreach (var notice in notices.Take(20)) // Limit to recent notices
        {
            CreateNoticeItem(notice);
        }
    }

    void CreateNoticeItem(GroupNoticesListEntry notice)
    {
        if (noticeItemPrefab == null || noticesRoot == null) return;
        
        var noticeObj = Instantiate(noticeItemPrefab, noticesRoot);
        var titleText = noticeObj.transform.Find("TitleText")?.GetComponent<TMP_Text>();
        var dateText = noticeObj.transform.Find("DateText")?.GetComponent<TMP_Text>();
        var authorText = noticeObj.transform.Find("AuthorText")?.GetComponent<TMP_Text>();
        var button = noticeObj.GetComponent<Button>();
        
        if (titleText) titleText.text = notice.Subject;
        if (dateText) dateText.text = notice.Timestamp.ToString("MMM dd, yyyy");
        if (authorText) authorText.text = notice.FromName;
        
        if (button)
        {
            button.onClick.AddListener(() => ShowNoticeDetails(notice));
        }
    }

    void UpdateRoleFilter(List<GroupRole> roles)
    {
        if (roleFilterDropdown == null) return;
        
        roleFilterDropdown.options.Clear();
        roleFilterDropdown.options.Add(new TMP_Dropdown.OptionData("All Roles"));
        
        foreach (var role in roles)
        {
            roleFilterDropdown.options.Add(new TMP_Dropdown.OptionData(role.Name));
        }
        
        roleFilterDropdown.RefreshShownValue();
    }

    #region Action Handlers

    void ActivateGroup()
    {
        if (selectedGroup == null) return;
        
        client.Groups.ActivateGroup(selectedGroup.ID);
        activeGroupID = selectedGroup.ID;
        
        RefreshGroups(); // Refresh to update UI
    }

    void LeaveGroup()
    {
        if (selectedGroup == null) return;
        
        // Confirm leaving group
        if (UnityEngine.Application.isEditor || 
            UnityEngine.Windows.Input.ShowMessageBox("Leave Group", 
            $"Are you sure you want to leave '{selectedGroup.Name}'?", "Yes", "No") == 0)
        {
            client.Groups.LeaveGroup(selectedGroup.ID);
        }
    }

    void ShowGroupInfo()
    {
        if (selectedGroup == null) return;
        
        // Switch to appropriate tab/panel
        // Implementation depends on your UI layout
    }

    void ToggleGroupChat()
    {
        if (selectedGroup == null) return;
        
        bool isActive = groupChatPanel.activeSelf;
        groupChatPanel.SetActive(!isActive);
        
        if (!isActive)
        {
            // Join group chat session
            client.Self.RequestJoinGroupChat(selectedGroup.ID);
        }
    }

    void SendGroupChat()
    {
        if (chatInput == null || selectedGroup == null) return;
        
        string message = chatInput.text.Trim();
        if (string.IsNullOrEmpty(message)) return;
        
        client.Self.GroupChatMessage(selectedGroup.ID, message);
        chatInput.text = "";
    }

    void InviteMember()
    {
        // This would open an invite dialog
        Debug.Log("Invite member to group");
    }

    void EjectMember()
    {
        // This would show member selection for ejection
        Debug.Log("Eject member from group");
    }

    void CreateNotice()
    {
        // This would open a create notice dialog
        Debug.Log("Create group notice");
    }

    void ShowMemberProfile(UUID memberID)
    {
        var profileViewer = FindObjectOfType<ProfileViewer>();
        if (profileViewer != null)
        {
            profileViewer.ShowProfile(memberID);
        }
    }

    void ShowNoticeDetails(GroupNoticesListEntry notice)
    {
        // This would show detailed notice information
        Debug.Log($"Show notice: {notice.Subject}");
    }

    #endregion

    #region Toggle Handlers

    void OnShowInProfileChanged(bool value)
    {
        if (selectedGroup == null) return;
        
        // Update group preferences
        client.Groups.SetGroupAcceptNotices(selectedGroup.ID, receiveNoticesToggle.isOn, value);
    }

    void OnReceiveNoticesChanged(bool value)
    {
        if (selectedGroup == null) return;
        
        // Update group preferences
        client.Groups.SetGroupAcceptNotices(selectedGroup.ID, value, showInProfileToggle.isOn);
    }

    void OnListInProfileChanged(bool value)
    {
        if (selectedGroup == null) return;
        
        // This would update list in profile setting
        // Implementation depends on the LibreMetaverse API
    }

    void OnRoleFilterChanged(int value)
    {
        if (selectedGroup == null || !groupMembers.ContainsKey(selectedGroup.ID)) return;
        
        var members = groupMembers[selectedGroup.ID];
        
        if (value == 0) // All roles
        {
            DisplayGroupMembers(members);
        }
        else
        {
            // Filter by specific role
            // This would need role-member mapping data
            DisplayGroupMembers(members);
        }
    }

    #endregion
}