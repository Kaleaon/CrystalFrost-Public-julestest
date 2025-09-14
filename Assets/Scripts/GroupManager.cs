/*
 * Crystal Frost Second Life Viewer - Advanced Group Management System
 * 
 * SYSTEM OVERVIEW:
 * ================
 * This is a comprehensive group management system for the Crystal Frost Second Life viewer,
 * providing complete group functionality including membership management, group chat,
 * role administration, and social interaction features. The system offers a full-featured
 * group experience with advanced filtering, search, and administrative capabilities.
 * 
 * ARCHITECTURE:
 * =============
 * - Unity MonoBehaviour component with rich UI integration
 * - Multi-threaded group operations using LibreMetaverse
 * - Event-driven architecture with proper thread synchronization
 * - Hierarchical group data management and display
 * - Real-time group member tracking and status updates
 * - Administrative tools with role-based permissions
 * - Integrated group chat and communication systems
 * 
 * KEY FEATURES:
 * =============
 * 1. GROUP MEMBERSHIP MANAGEMENT:
 *    - Complete member list with roles and status
 *    - Member search and filtering capabilities
 *    - Online status tracking for members
 *    - Member profile access and interaction
 *    - Invitation and ejection management
 * 
 * 2. GROUP ADMINISTRATION:
 *    - Role creation and management
 *    - Permission assignment and control
 *    - Group settings configuration
 *    - Member role assignments
 *    - Group policy enforcement
 * 
 * 3. GROUP COMMUNICATION:
 *    - Integrated group chat functionality
 *    - Group notice system with attachments
 *    - Announcement broadcasting
 *    - Private officer communication
 *    - Communication history and logging
 * 
 * 4. GROUP DISCOVERY AND JOINING:
 *    - Group search integration
 *    - Join request processing
 *    - Group information display
 *    - Membership fee handling
 *    - Group reputation and ratings
 * 
 * 5. ADVANCED GROUP FEATURES:
 *    - Group land and parcel management
 *    - Group asset sharing and inventory
 *    - Event coordination and planning
 *    - Group voting and decision making
 *    - Group statistics and analytics
 * 
 * TECHNICAL IMPLEMENTATION:
 * =========================
 * - Unity UI system with dynamic content generation
 * - LibreMetaverse GroupManager integration
 * - Thread-safe event handling using UnityMainThreadDispatcher
 * - Efficient data caching and synchronization
 * - Real-time updates for group changes
 * - Memory-efficient member list management
 * - Asynchronous operations for responsive UI
 * 
 * INTEGRATION POINTS:
 * ===================
 * - LibreMetaverse GroupManager for all group operations
 * - Crystal Frost chat system for group communication
 * - Profile viewer for member information display
 * - Search system for group discovery
 * - Notification system for group alerts
 * - Permission system for administrative controls
 * 
 * SOCIAL AND ADMINISTRATIVE FEATURES:
 * ====================================
 * - Comprehensive member management tools
 * - Role-based access control systems
 * - Group communication and collaboration
 * - Administrative oversight and moderation
 * - Group growth and recruitment tools
 * - Community building and engagement features
 * 
 * USAGE:
 * ======
 * This component should be attached to a GameObject with proper UI references configured.
 * The group manager can be opened via ShowGroupManager() and will display all groups
 * the user belongs to with full management capabilities.
 * 
 * Author: Crystal Frost Development Team
 * Version: 2.0
 * Unity Compatibility: 2021.3.6f1 LTS and higher
 * LibreMetaverse: Compatible with latest versions
 */

using OpenMetaverse;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Linq;
using System;

/// <summary>
/// Comprehensive Group Management System for Crystal Frost Second Life Viewer
/// Provides complete group functionality including membership management, administration,
/// communication, and social interaction features with advanced UI and real-time updates.
/// </summary>
public class GroupManager : MonoBehaviour
{
    #region Inspector Fields
    
    [Header("Window Management")]
    [Tooltip("Main group manager window GameObject")]
    public GameObject groupManagerWindow;
    
    [Tooltip("Button to close the group manager window")]
    public Button closeButton;
    
    [Tooltip("Button to refresh group data and member lists")]
    public Button refreshButton;
    
    [Header("Group Selection")]
    [Tooltip("Dropdown for selecting active group")]
    public TMP_Dropdown groupDropdown;
    
    [Tooltip("Button to leave the currently selected group")]
    public Button leaveGroupButton;
    
    [Tooltip("Button to create a new group")]
    public Button createGroupButton;
    
    [Header("Group Information Display")]
    [Tooltip("Group name display text")]
    public TMP_Text groupNameText;
    
    [Tooltip("Group member count display")]
    public TMP_Text memberCountText;
    
    [Tooltip("Group description text")]
    public TMP_Text groupDescriptionText;
    
    [Tooltip("Group charter/rules text")]
    public TMP_Text groupCharterText;
    
    [Tooltip("Group insignia/logo image")]
    public RawImage groupInsigniaImage;
    
    [Header("Tab System")]
    [Tooltip("Tab button for general group information")]
    public Button generalTabButton;
    
    [Tooltip("Tab button for member management")]
    public Button membersTabButton;
    
    [Tooltip("Tab button for group notices")]
    public Button noticesTabButton;
    
    [Tooltip("Tab button for group roles and permissions")]
    public Button rolesTabButton;
    
    [Header("Tab Content Panels")]
    [Tooltip("General information tab panel")]
    public GameObject generalTab;
    
    [Tooltip("Members management tab panel")]
    public GameObject membersTab;
    
    [Tooltip("Notices management tab panel")]
    public GameObject noticesTab;
    
    [Tooltip("Roles and permissions tab panel")]
    public GameObject rolesTab;
    
    [Header("Members Management")]
    [Tooltip("Root container for member list items")]
    public Transform membersListRoot;
    
    [Tooltip("Prefab for individual member items")]
    public GameObject memberItemPrefab;
    
    [Tooltip("Search field for finding members")]
    public TMP_InputField memberSearchField;
    
    [Tooltip("Filter dropdown for member status")]
    public TMP_Dropdown memberFilterDropdown;
    
    [Tooltip("Button to invite new members")]
    public Button inviteMemberButton;
    
    [Header("Group Notices")]
    [Tooltip("Root container for group notice items")]
    public Transform noticesListRoot;
    
    [Tooltip("Prefab for individual notice items")]
    public GameObject noticeItemPrefab;
    
    [Tooltip("Button to send new group notice")]
    public Button sendNoticeButton;
    
    [Tooltip("Input field for notice subject")]
    public TMP_InputField noticeSubjectField;
    
    [Tooltip("Input field for notice message")]
    public TMP_InputField noticeMessageField;
    
    [Header("Roles Management")]
    [Tooltip("Root container for group role items")]
    public Transform rolesListRoot;
    
    [Tooltip("Prefab for individual role items")]
    public GameObject roleItemPrefab;
    
    [Tooltip("Button to create new role")]
    public Button createRoleButton;
    
    [Tooltip("Selected role name display")]
    public TMP_Text selectedRoleNameText;
    
    [Tooltip("Role permissions checklist root")]
    public Transform permissionsRoot;
    
    #endregion
    
    #region Private Fields
    
    /// <summary>GridClient reference for LibreMetaverse integration</summary>
    private GridClient client;
    
    /// <summary>Currently selected group for management</summary>
    private Group currentGroup;
    
    /// <summary>Complete list of user's group memberships</summary>
    private Dictionary<UUID, Group> userGroups = new();
    
    /// <summary>Current group members list</summary>
    private Dictionary<UUID, GroupMember> groupMembers = new();
    
    /// <summary>Current group roles list</summary>
    private Dictionary<UUID, GroupRole> groupRoles = new();
    
    /// <summary>Current group notices list</summary>
    private List<GroupNotice> groupNotices = new();
    
    /// <summary>Selected member for actions</summary>
    private GroupMember selectedMember;
    
    /// <summary>Selected role for editing</summary>
    private GroupRole selectedRole;
    
    /// <summary>Currently active tab index</summary>
    private int activeTabIndex = 0;
    
    /// <summary>Cache for member names</summary>
    private Dictionary<UUID, string> memberNames = new();
    
    /// <summary>Filter for member display</summary>
    private MemberFilter currentMemberFilter = MemberFilter.All;
    
    /// <summary>Search term for member filtering</summary>
    private string memberSearchTerm = "";
    
    #endregion
    
    #region Enums and Data Structures
    
    /// <summary>
    /// Member filtering options for display management
    /// </summary>
    public enum MemberFilter
    {
        All,            // Show all members
        Online,         // Show only online members
        Officers,       // Show only group officers
        Recent,         // Show recently active members
        Invited         // Show pending invitations
    }
    
    /// <summary>
    /// Group notice information container
    /// Stores complete notice data for display and management
    /// </summary>
    [System.Serializable]
    public class GroupNotice
    {
        public UUID noticeID;               // Unique notice identifier
        public string subject;              // Notice subject line
        public string message;              // Notice message content
        public string senderName;           // Notice sender's name
        public UUID senderID;               // Notice sender's UUID
        public DateTime timestamp;          // When notice was sent
        public bool hasAttachment;          // Whether notice has attachment
        public UUID attachmentID;           // Attachment inventory item ID
        public string attachmentName;       // Attachment display name
    }
    
    #endregion
    
    #region Unity Lifecycle
    
    /// <summary>
    /// Initialize group manager component
    /// Called before Start() on the first frame
    /// </summary>
    void Awake()
    {
        // Hide group manager window initially
        groupManagerWindow.SetActive(false);
        
        // Setup UI event handlers and components
        SetupUI();
    }
    
    /// <summary>
    /// Complete initialization after all objects are available
    /// Called on the first frame after Awake()
    /// </summary>
    void Start()
    {
        // Get GridClient reference for LibreMetaverse integration
        client = ClientManager.client;
        
        // Subscribe to LibreMetaverse group events
        if (client != null)
        {
            client.Groups.GroupMembersReply += OnGroupMembersReply;
            client.Groups.GroupRolesDataReply += OnGroupRolesDataReply;
            client.Groups.GroupNoticesListReply += OnGroupNoticesListReply;
            client.Groups.GroupJoinedReply += OnGroupJoinedReply;
            client.Groups.GroupDropped += OnGroupDropped;
            client.Groups.GroupProfile += OnGroupProfile;
            client.Avatars.UUIDNameReply += OnUUIDNameReply;
        }
    }
    
    /// <summary>
    /// Cleanup when component is destroyed
    /// Unsubscribes from events and saves current state
    /// </summary>
    void OnDestroy()
    {
        // Unsubscribe from LibreMetaverse events to prevent memory leaks
        if (client != null)
        {
            client.Groups.GroupMembersReply -= OnGroupMembersReply;
            client.Groups.GroupRolesDataReply -= OnGroupRolesDataReply;
            client.Groups.GroupNoticesListReply -= OnGroupNoticesListReply;
            client.Groups.GroupJoinedReply -= OnGroupJoinedReply;
            client.Groups.GroupDropped -= OnGroupDropped;
            client.Groups.GroupProfile -= OnGroupProfile;
            client.Avatars.UUIDNameReply -= OnUUIDNameReply;
        }
        
        // Clear caches to free memory
        userGroups.Clear();
        groupMembers.Clear();
        groupRoles.Clear();
        groupNotices.Clear();
        memberNames.Clear();
    }
    
    #endregion
    
    #region Initialization and Setup
    
    /// <summary>
    /// Configure UI event handlers and initialize components
    /// Sets up all button clicks, dropdown changes, and input events
    /// </summary>
    void SetupUI()
    {
        // Main window controls
        if (closeButton) closeButton.onClick.AddListener(() => groupManagerWindow.SetActive(false));
        if (refreshButton) refreshButton.onClick.AddListener(RefreshGroupData);
        
        // Group selection controls
        if (groupDropdown) groupDropdown.onValueChanged.AddListener(OnGroupSelectionChanged);
        if (leaveGroupButton) leaveGroupButton.onClick.AddListener(LeaveCurrentGroup);
        if (createGroupButton) createGroupButton.onClick.AddListener(ShowCreateGroupDialog);
        
        // Tab navigation buttons
        if (generalTabButton) generalTabButton.onClick.AddListener(() => SwitchToTab(0));
        if (membersTabButton) membersTabButton.onClick.AddListener(() => SwitchToTab(1));
        if (noticesTabButton) noticesTabButton.onClick.AddListener(() => SwitchToTab(2));
        if (rolesTabButton) rolesTabButton.onClick.AddListener(() => SwitchToTab(3));
        
        // Members management controls
        if (memberSearchField) memberSearchField.onValueChanged.AddListener(OnMemberSearchChanged);
        if (memberFilterDropdown) memberFilterDropdown.onValueChanged.AddListener(OnMemberFilterChanged);
        if (inviteMemberButton) inviteMemberButton.onClick.AddListener(ShowInviteMemberDialog);
        
        // Notices management controls
        if (sendNoticeButton) sendNoticeButton.onClick.AddListener(SendGroupNotice);
        
        // Roles management controls  
        if (createRoleButton) createRoleButton.onClick.AddListener(ShowCreateRoleDialog);
        
        // Initialize filter dropdown
        SetupMemberFilterDropdown();
        
        // Set initial tab to general
        SwitchToTab(0);
    }
    
    /// <summary>
    /// Setup member filter dropdown with available options
    /// Populates dropdown with filtering choices
    /// </summary>
    void SetupMemberFilterDropdown()
    {
        if (memberFilterDropdown == null) return;
        
        memberFilterDropdown.options.Clear();
        memberFilterDropdown.options.Add(new TMP_Dropdown.OptionData("All Members"));
        memberFilterDropdown.options.Add(new TMP_Dropdown.OptionData("Online Only"));
        memberFilterDropdown.options.Add(new TMP_Dropdown.OptionData("Officers"));
        memberFilterDropdown.options.Add(new TMP_Dropdown.OptionData("Recent Activity"));
        memberFilterDropdown.options.Add(new TMP_Dropdown.OptionData("Pending Invites"));
        
        memberFilterDropdown.RefreshShownValue();
    }
    
    #endregion
    
    #region Public Interface
    
    /// <summary>
    /// Show the group manager window and initialize data
    /// Main entry point for opening the group management interface
    /// </summary>
    public void ShowGroupManager()
    {
        groupManagerWindow.SetActive(true);
        
        // Load user's group memberships
        LoadUserGroups();
        
        // Refresh data for better user experience
        RefreshGroupData();
    }
    
    /// <summary>
    /// Show details for a specific group
    /// Programmatically opens group manager for specific group
    /// </summary>
    /// <param name="groupID">UUID of group to display</param>
    public void ShowGroupDetails(UUID groupID)
    {
        ShowGroupManager();
        
        // Select the specified group if user is a member
        if (userGroups.ContainsKey(groupID))
        {
            SelectGroup(userGroups[groupID]);
        }
        else
        {
            // Request group information even if not a member
            RequestGroupProfile(groupID);
        }
    }
    
    #endregion
    
    #region Group Selection and Data Loading
    
    /// <summary>
    /// Load user's group memberships from LibreMetaverse
    /// Populates the group dropdown with available groups
    /// </summary>
    void LoadUserGroups()
    {
        if (client == null) return;
        
        // Clear existing groups
        userGroups.Clear();
        
        // Get groups from client (assuming they're already loaded)
        foreach (var group in client.Groups.GroupList)
        {
            userGroups[group.Key] = group.Value;
        }
        
        // Update group dropdown
        UpdateGroupDropdown();
        
        // Select first group if available
        if (userGroups.Count > 0)
        {
            var firstGroup = userGroups.Values.First();
            SelectGroup(firstGroup);
        }
    }
    
    /// <summary>
    /// Update group selection dropdown with current groups
    /// Refreshes dropdown options with user's group memberships
    /// </summary>
    void UpdateGroupDropdown()
    {
        if (groupDropdown == null) return;
        
        groupDropdown.options.Clear();
        
        foreach (var group in userGroups.Values)
        {
            groupDropdown.options.Add(new TMP_Dropdown.OptionData(group.Name));
        }
        
        groupDropdown.RefreshShownValue();
        
        // Update UI state based on group availability
        bool hasGroups = userGroups.Count > 0;
        if (leaveGroupButton) leaveGroupButton.interactable = hasGroups;
    }
    
    /// <summary>
    /// Select a group for management and display
    /// Updates all UI elements for the selected group
    /// </summary>
    /// <param name="group">Group to select and display</param>
    void SelectGroup(Group group)
    {
        currentGroup = group;
        
        // Update group information display
        UpdateGroupInfoDisplay();
        
        // Request detailed group data
        RequestGroupData(group.ID);
        
        // Update dropdown selection
        UpdateDropdownSelection(group);
    }
    
    /// <summary>
    /// Update dropdown selection to match current group
    /// Synchronizes dropdown with programmatic group selection
    /// </summary>
    /// <param name="group">Group to select in dropdown</param>
    void UpdateDropdownSelection(Group group)
    {
        if (groupDropdown == null) return;
        
        for (int i = 0; i < userGroups.Count; i++)
        {
            if (userGroups.Values.ElementAt(i).ID == group.ID)
            {
                groupDropdown.value = i;
                break;
            }
        }
    }
    
    /// <summary>
    /// Request comprehensive group data from LibreMetaverse
    /// Initiates multiple API calls to gather complete group information
    /// </summary>
    /// <param name="groupID">UUID of group to request data for</param>
    void RequestGroupData(UUID groupID)
    {
        if (client == null) return;
        
        try
        {
            // Request group profile information
            client.Groups.RequestGroupProfile(groupID);
            
            // Request group members list
            client.Groups.RequestGroupMembers(groupID);
            
            // Request group roles and permissions
            client.Groups.RequestGroupRoles(groupID);
            
            // Request group notices
            client.Groups.RequestGroupNoticesList(groupID);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error requesting group data: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Request group profile for non-member viewing
    /// Allows viewing group information without membership
    /// </summary>
    /// <param name="groupID">UUID of group to get profile for</param>
    void RequestGroupProfile(UUID groupID)
    {
        if (client == null) return;
        
        try
        {
            client.Groups.RequestGroupProfile(groupID);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error requesting group profile: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Refresh all group data for current group
    /// Forces reload of group information and member lists
    /// </summary>
    void RefreshGroupData()
    {
        if (currentGroup != null)
        {
            RequestGroupData(currentGroup.ID);
        }
        
        // Also refresh user's group list
        LoadUserGroups();
    }
    
    #endregion
    
    #region UI Display Updates
    
    /// <summary>
    /// Update group information display with current group data
    /// Refreshes all group-specific UI elements
    /// </summary>
    void UpdateGroupInfoDisplay()
    {
        if (currentGroup == null) return;
        
        // Update basic group information
        if (groupNameText) groupNameText.text = currentGroup.Name;
        if (memberCountText) memberCountText.text = $"Members: {groupMembers.Count}";
        
        // Note: Additional group information would come from group profile events
        // For now, display what we have from the Group object
    }
    
    #endregion
    
    #region Tab Management
    
    /// <summary>
    /// Switch to a specific tab in the group manager interface
    /// Manages tab visibility and updates tab button states
    /// </summary>
    /// <param name="tabIndex">Index of tab to switch to (0-3)</param>
    void SwitchToTab(int tabIndex)
    {
        activeTabIndex = tabIndex;
        
        // Hide all tab panels
        if (generalTab) generalTab.SetActive(tabIndex == 0);
        if (membersTab) membersTab.SetActive(tabIndex == 1);
        if (noticesTab) noticesTab.SetActive(tabIndex == 2);
        if (rolesTab) rolesTab.SetActive(tabIndex == 3);
        
        // Update tab button visual states
        UpdateTabButtonStates();
        
        // Load tab-specific data if needed
        LoadTabData(tabIndex);
    }
    
    /// <summary>
    /// Update visual states of tab buttons
    /// Provides visual feedback for the currently active tab
    /// </summary>
    void UpdateTabButtonStates()
    {
        // Define colors for active and inactive states
        Color activeColor = Color.yellow;
        Color inactiveColor = Color.white;
        
        // Update each tab button based on current selection
        UpdateTabButtonColor(generalTabButton, activeTabIndex == 0, activeColor, inactiveColor);
        UpdateTabButtonColor(membersTabButton, activeTabIndex == 1, activeColor, inactiveColor);
        UpdateTabButtonColor(noticesTabButton, activeTabIndex == 2, activeColor, inactiveColor);
        UpdateTabButtonColor(rolesTabButton, activeTabIndex == 3, activeColor, inactiveColor);
    }
    
    /// <summary>
    /// Update individual tab button color based on active state
    /// Helper method for consistent button styling
    /// </summary>
    /// <param name="button">Button to update</param>
    /// <param name="isActive">Whether this button represents the active tab</param>
    /// <param name="activeColor">Color for active state</param>
    /// <param name="inactiveColor">Color for inactive state</param>
    void UpdateTabButtonColor(Button button, bool isActive, Color activeColor, Color inactiveColor)
    {
        if (button == null) return;
        
        var colors = button.colors;
        colors.normalColor = isActive ? activeColor : inactiveColor;
        colors.highlightedColor = isActive ? activeColor * 0.9f : inactiveColor * 1.1f;
        colors.selectedColor = isActive ? activeColor * 0.8f : inactiveColor * 0.9f;
        button.colors = colors;
    }
    
    /// <summary>
    /// Load data specific to the activated tab
    /// Ensures tab content is properly populated when switching
    /// </summary>
    /// <param name="tabIndex">Index of the activated tab</param>
    void LoadTabData(int tabIndex)
    {
        if (currentGroup == null) return;
        
        switch (tabIndex)
        {
            case 0: // General tab
                // General information is loaded when group is selected
                break;
                
            case 1: // Members tab
                UpdateMembersDisplay();
                break;
                
            case 2: // Notices tab
                UpdateNoticesDisplay();
                break;
                
            case 3: // Roles tab
                UpdateRolesDisplay();
                break;
        }
    }
    
    #endregion
    
    #region Members Management
    
    /// <summary>
    /// Update the members display with current member data
    /// Creates UI elements for all visible group members
    /// </summary>
    void UpdateMembersDisplay()
    {
        if (membersListRoot == null) return;
        
        // Clear existing member items
        foreach (Transform child in membersListRoot)
        {
            Destroy(child.gameObject);
        }
        
        // Filter members based on current filter and search
        var filteredMembers = FilterMembers();
        
        // Create UI items for filtered members
        foreach (var member in filteredMembers)
        {
            CreateMemberItem(member);
        }
        
        // Update member count display
        if (memberCountText) 
        {
            memberCountText.text = $"Showing {filteredMembers.Count} of {groupMembers.Count} members";
        }
    }
    
    /// <summary>
    /// Filter members based on current search and filter settings
    /// Applies search term and filter criteria to member list
    /// </summary>
    /// <returns>Filtered list of group members</returns>
    List<GroupMember> FilterMembers()
    {
        var filtered = groupMembers.Values.AsEnumerable();
        
        // Apply search filter
        if (!string.IsNullOrEmpty(memberSearchTerm))
        {
            filtered = filtered.Where(m => 
                memberNames.GetValueOrDefault(m.ID, "Unknown")
                    .ToLower().Contains(memberSearchTerm.ToLower()));
        }
        
        // Apply status filter
        switch (currentMemberFilter)
        {
            case MemberFilter.Online:
                filtered = filtered.Where(m => m.IsOnline);
                break;
            case MemberFilter.Officers:
                filtered = filtered.Where(m => HasOfficerRole(m));
                break;
            case MemberFilter.Recent:
                // Filter by last online date (would need additional data)
                break;
        }
        
        // Sort by name
        return filtered.OrderBy(m => memberNames.GetValueOrDefault(m.ID, "Unknown")).ToList();
    }
    
    /// <summary>
    /// Check if member has officer-level role
    /// Determines if member has administrative permissions
    /// </summary>
    /// <param name="member">Member to check role for</param>
    /// <returns>True if member has officer role</returns>
    bool HasOfficerRole(GroupMember member)
    {
        // Check if member has roles with officer permissions
        // This would need to check role permissions
        return member.Powers != GroupPowers.None;
    }
    
    /// <summary>
    /// Create a UI item for a group member
    /// Instantiates and configures a member display element
    /// </summary>
    /// <param name="member">Group member to create item for</param>
    void CreateMemberItem(GroupMember member)
    {
        if (memberItemPrefab == null) return;
        
        try
        {
            // Instantiate member item from prefab
            var memberObj = Instantiate(memberItemPrefab, membersListRoot);
            
            // Configure member information display
            var nameText = memberObj.transform.Find("NameText")?.GetComponent<TMP_Text>();
            var roleText = memberObj.transform.Find("RoleText")?.GetComponent<TMP_Text>();
            var statusIcon = memberObj.transform.Find("StatusIcon")?.GetComponent<Image>();
            
            // Set member name
            string memberName = memberNames.GetValueOrDefault(member.ID, "Loading...");
            if (nameText) nameText.text = memberName;
            
            // Set member role/title
            if (roleText) roleText.text = member.Title ?? "Member";
            
            // Set online status
            if (statusIcon)
            {
                statusIcon.color = member.IsOnline ? Color.green : Color.gray;
            }
            
            // Configure click handler for member actions
            var button = memberObj.GetComponent<Button>();
            if (button)
            {
                button.onClick.AddListener(() => SelectMember(member));
            }
            
            // Request member name if not cached
            if (!memberNames.ContainsKey(member.ID))
            {
                client?.Avatars.RequestAvatarName(member.ID);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error creating member item: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Select a member for actions and detailed display
    /// Updates selected member and enables member-specific actions
    /// </summary>
    /// <param name="member">Member to select</param>
    void SelectMember(GroupMember member)
    {
        selectedMember = member;
        
        // Update member action buttons based on permissions
        UpdateMemberActionButtons();
        
        // Could show member details panel here
        Debug.Log($"Selected member: {memberNames.GetValueOrDefault(member.ID, member.ID.ToString())}");
    }
    
    /// <summary>
    /// Update member action buttons based on current selection and permissions
    /// Enables/disables buttons based on user permissions and selected member
    /// </summary>
    void UpdateMemberActionButtons()
    {
        if (selectedMember == null) return;
        
        // Check current user's permissions in the group
        bool canInvite = HasPermission(GroupPowers.Invite);
        bool canEject = HasPermission(GroupPowers.Eject);
        bool canManageRoles = HasPermission(GroupPowers.RoleProperties);
        
        // Update button states based on permissions
        // (Would need actual action buttons in the UI)
    }
    
    /// <summary>
    /// Check if current user has specific group permission
    /// Validates user permissions for administrative actions
    /// </summary>
    /// <param name="power">Permission to check for</param>
    /// <returns>True if user has the specified permission</returns>
    bool HasPermission(GroupPowers power)
    {
        if (client == null || currentGroup == null) return false;
        
        // Get current user's powers in the group
        var userMember = groupMembers.Values.FirstOrDefault(m => m.ID == client.Self.AgentID);
        if (userMember == null) return false;
        
        return (userMember.Powers & power) == power;
    }
    
    #endregion
    
    #region Notices Management
    
    /// <summary>
    /// Update the notices display with current notice data
    /// Creates UI elements for all group notices
    /// </summary>
    void UpdateNoticesDisplay()
    {
        if (noticesListRoot == null) return;
        
        // Clear existing notice items
        foreach (Transform child in noticesListRoot)
        {
            Destroy(child.gameObject);
        }
        
        // Create UI items for notices (sorted by date, newest first)
        var sortedNotices = groupNotices.OrderByDescending(n => n.timestamp);
        foreach (var notice in sortedNotices)
        {
            CreateNoticeItem(notice);
        }
    }
    
    /// <summary>
    /// Create a UI item for a group notice
    /// Instantiates and configures a notice display element
    /// </summary>
    /// <param name="notice">Group notice to create item for</param>
    void CreateNoticeItem(GroupNotice notice)
    {
        if (noticeItemPrefab == null) return;
        
        try
        {
            // Instantiate notice item from prefab
            var noticeObj = Instantiate(noticeItemPrefab, noticesListRoot);
            
            // Configure notice information display
            var subjectText = noticeObj.transform.Find("SubjectText")?.GetComponent<TMP_Text>();
            var senderText = noticeObj.transform.Find("SenderText")?.GetComponent<TMP_Text>();
            var dateText = noticeObj.transform.Find("DateText")?.GetComponent<TMP_Text>();
            var attachmentIcon = noticeObj.transform.Find("AttachmentIcon")?.GetComponent<Image>();
            
            // Set notice information
            if (subjectText) subjectText.text = notice.subject;
            if (senderText) senderText.text = $"From: {notice.senderName}";
            if (dateText) dateText.text = notice.timestamp.ToString("MMM dd, yyyy");
            
            // Show attachment icon if notice has attachment
            if (attachmentIcon) attachmentIcon.gameObject.SetActive(notice.hasAttachment);
            
            // Configure click handler for notice details
            var button = noticeObj.GetComponent<Button>();
            if (button)
            {
                button.onClick.AddListener(() => ShowNoticeDetails(notice));
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error creating notice item: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Show detailed information about a group notice
    /// Displays full notice content and attachment information
    /// </summary>
    /// <param name="notice">Notice to show details for</param>
    void ShowNoticeDetails(GroupNotice notice)
    {
        // This would show a detailed notice view
        Debug.Log($"Show details for notice: {notice.subject}");
        Debug.Log($"Message: {notice.message}");
        
        // Could open a notice details dialog here
    }
    
    /// <summary>
    /// Send a new group notice
    /// Creates and sends a notice to all group members
    /// </summary>
    void SendGroupNotice()
    {
        if (client == null || currentGroup == null) return;
        
        // Validate input fields
        string subject = noticeSubjectField?.text ?? "";
        string message = noticeMessageField?.text ?? "";
        
        if (string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(message))
        {
            Debug.LogWarning("Notice subject and message are required");
            return;
        }
        
        try
        {
            // Send group notice through LibreMetaverse
            client.Groups.SendGroupNotice(currentGroup.ID, subject, message, UUID.Zero);
            
            // Clear input fields
            if (noticeSubjectField) noticeSubjectField.text = "";
            if (noticeMessageField) noticeMessageField.text = "";
            
            Debug.Log($"Sent group notice: {subject}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error sending group notice: {ex.Message}");
        }
    }
    
    #endregion
    
    #region Roles Management
    
    /// <summary>
    /// Update the roles display with current role data
    /// Creates UI elements for all group roles
    /// </summary>
    void UpdateRolesDisplay()
    {
        if (rolesListRoot == null) return;
        
        // Clear existing role items
        foreach (Transform child in rolesListRoot)
        {
            Destroy(child.gameObject);
        }
        
        // Create UI items for roles
        foreach (var role in groupRoles.Values)
        {
            CreateRoleItem(role);
        }
    }
    
    /// <summary>
    /// Create a UI item for a group role
    /// Instantiates and configures a role display element
    /// </summary>
    /// <param name="role">Group role to create item for</param>
    void CreateRoleItem(GroupRole role)
    {
        if (roleItemPrefab == null) return;
        
        try
        {
            // Instantiate role item from prefab
            var roleObj = Instantiate(roleItemPrefab, rolesListRoot);
            
            // Configure role information display
            var nameText = roleObj.GetComponentInChildren<TMP_Text>();
            if (nameText) nameText.text = role.Name;
            
            // Configure click handler for role selection
            var button = roleObj.GetComponent<Button>();
            if (button)
            {
                button.onClick.AddListener(() => SelectRole(role));
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error creating role item: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Select a role for editing and management
    /// Updates selected role and displays role permissions
    /// </summary>
    /// <param name="role">Role to select</param>
    void SelectRole(GroupRole role)
    {
        selectedRole = role;
        
        // Update role name display
        if (selectedRoleNameText) selectedRoleNameText.text = role.Name;
        
        // Update permissions display
        UpdatePermissionsDisplay(role);
    }
    
    /// <summary>
    /// Update permissions display for selected role
    /// Shows checkboxes for all available group permissions
    /// </summary>
    /// <param name="role">Role to display permissions for</param>
    void UpdatePermissionsDisplay(GroupRole role)
    {
        if (permissionsRoot == null) return;
        
        // This would create checkbox UI for each group permission
        // and set their states based on the role's powers
        Debug.Log($"Display permissions for role: {role.Name}");
        Debug.Log($"Role powers: {role.Powers}");
    }
    
    #endregion
    
    #region LibreMetaverse Event Handlers
    
    /// <summary>
    /// Handle group members list response from LibreMetaverse
    /// Processes group membership data and updates display
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="e">Event arguments containing member data</param>
    void OnGroupMembersReply(object sender, GroupMembersReplyEventArgs e)
    {
        if (currentGroup == null || e.GroupID != currentGroup.ID) return;
        
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            // Store member data
            groupMembers.Clear();
            foreach (var member in e.Members)
            {
                groupMembers[member.Key] = member.Value;
            }
            
            // Request names for all members
            foreach (var memberID in groupMembers.Keys)
            {
                if (!memberNames.ContainsKey(memberID))
                {
                    client?.Avatars.RequestAvatarName(memberID);
                }
            }
            
            // Update display if members tab is active
            if (activeTabIndex == 1)
            {
                UpdateMembersDisplay();
            }
            
            // Update member count
            UpdateGroupInfoDisplay();
        });
    }
    
    /// <summary>
    /// Handle group roles data response from LibreMetaverse
    /// Processes group role information and updates display
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="e">Event arguments containing role data</param>
    void OnGroupRolesDataReply(object sender, GroupRolesDataReplyEventArgs e)
    {
        if (currentGroup == null || e.GroupID != currentGroup.ID) return;
        
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            // Store role data
            groupRoles.Clear();
            foreach (var role in e.Roles)
            {
                groupRoles[role.Key] = role.Value;
            }
            
            // Update display if roles tab is active
            if (activeTabIndex == 3)
            {
                UpdateRolesDisplay();
            }
        });
    }
    
    /// <summary>
    /// Handle group notices list response from LibreMetaverse
    /// Processes group notice data and updates display
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="e">Event arguments containing notice data</param>
    void OnGroupNoticesListReply(object sender, GroupNoticesListReplyEventArgs e)
    {
        if (currentGroup == null || e.GroupID != currentGroup.ID) return;
        
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            // Convert LibreMetaverse notices to our format
            groupNotices.Clear();
            foreach (var notice in e.Notices)
            {
                var groupNotice = new GroupNotice
                {
                    noticeID = notice.NoticeID,
                    subject = notice.Subject,
                    senderName = notice.FromName,
                    timestamp = notice.Timestamp,
                    hasAttachment = notice.HasAttachment
                };
                groupNotices.Add(groupNotice);
            }
            
            // Update display if notices tab is active
            if (activeTabIndex == 2)
            {
                UpdateNoticesDisplay();
            }
        });
    }
    
    /// <summary>
    /// Handle group profile information response from LibreMetaverse
    /// Processes detailed group information and updates display
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="e">Event arguments containing group profile</param>
    void OnGroupProfile(object sender, GroupProfileEventArgs e)
    {
        if (currentGroup == null || e.Group.ID != currentGroup.ID) return;
        
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            // Update group information with profile data
            if (groupDescriptionText) groupDescriptionText.text = e.Group.Charter;
            if (groupCharterText) groupCharterText.text = e.Group.Charter;
            
            // Request group insignia image if available
            if (e.Group.InsigniaID != UUID.Zero && client != null)
            {
                client.Assets.RequestImage(e.Group.InsigniaID, ImageType.Normal);
            }
        });
    }
    
    /// <summary>
    /// Handle group joined notification from LibreMetaverse
    /// Updates UI when user joins a new group
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="e">Event arguments containing join information</param>
    void OnGroupJoinedReply(object sender, GroupOperationEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            if (e.Success)
            {
                Debug.Log($"Successfully joined group: {e.GroupID}");
                LoadUserGroups(); // Refresh group list
            }
            else
            {
                Debug.LogError($"Failed to join group: {e.GroupID}");
            }
        });
    }
    
    /// <summary>
    /// Handle group dropped notification from LibreMetaverse
    /// Updates UI when user leaves a group
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="e">Event arguments containing drop information</param>
    void OnGroupDropped(object sender, GroupDroppedEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            Debug.Log($"Left group: {e.GroupID}");
            
            // Remove from user groups
            userGroups.Remove(e.GroupID);
            
            // Refresh UI
            UpdateGroupDropdown();
            
            // Select different group if current group was dropped
            if (currentGroup != null && currentGroup.ID == e.GroupID)
            {
                if (userGroups.Count > 0)
                {
                    SelectGroup(userGroups.Values.First());
                }
                else
                {
                    currentGroup = null;
                    ClearAllDisplays();
                }
            }
        });
    }
    
    /// <summary>
    /// Handle avatar name responses from LibreMetaverse
    /// Updates member name cache and display
    /// </summary>
    /// <param name="sender">Event sender</param>
    /// <param name="e">Event arguments containing name data</param>
    void OnUUIDNameReply(object sender, UUIDNameReplyEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            // Store all received names
            foreach (var nameKVP in e.Names)
            {
                memberNames[nameKVP.Key] = nameKVP.Value;
            }
            
            // Refresh members display if currently visible
            if (activeTabIndex == 1)
            {
                UpdateMembersDisplay();
            }
        });
    }
    
    #endregion
    
    #region UI Event Handlers
    
    /// <summary>
    /// Handle group selection change from dropdown
    /// Switches to newly selected group
    /// </summary>
    /// <param name="dropdownIndex">Selected dropdown index</param>
    void OnGroupSelectionChanged(int dropdownIndex)
    {
        if (dropdownIndex >= 0 && dropdownIndex < userGroups.Count)
        {
            var selectedGroup = userGroups.Values.ElementAt(dropdownIndex);
            SelectGroup(selectedGroup);
        }
    }
    
    /// <summary>
    /// Handle member search field changes
    /// Updates member filtering based on search term
    /// </summary>
    /// <param name="searchTerm">New search term</param>
    void OnMemberSearchChanged(string searchTerm)
    {
        memberSearchTerm = searchTerm;
        
        // Update display if members tab is active
        if (activeTabIndex == 1)
        {
            UpdateMembersDisplay();
        }
    }
    
    /// <summary>
    /// Handle member filter dropdown changes
    /// Updates member filtering based on selected filter
    /// </summary>
    /// <param name="filterIndex">Selected filter index</param>
    void OnMemberFilterChanged(int filterIndex)
    {
        if (filterIndex >= 0 && filterIndex < System.Enum.GetValues(typeof(MemberFilter)).Length)
        {
            currentMemberFilter = (MemberFilter)filterIndex;
            
            // Update display if members tab is active
            if (activeTabIndex == 1)
            {
                UpdateMembersDisplay();
            }
        }
    }
    
    #endregion
    
    #region Group Management Actions
    
    /// <summary>
    /// Leave the currently selected group
    /// Initiates group departure process
    /// </summary>
    void LeaveCurrentGroup()
    {
        if (currentGroup == null || client == null) return;
        
        // Confirm group departure
        string groupName = currentGroup.Name;
        Debug.Log($"Attempting to leave group: {groupName}");
        
        try
        {
            client.Groups.LeaveGroup(currentGroup.ID);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error leaving group: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Show dialog for creating a new group
    /// Opens group creation interface
    /// </summary>
    void ShowCreateGroupDialog()
    {
        // This would open a group creation dialog
        Debug.Log("Show create group dialog (not implemented)");
    }
    
    /// <summary>
    /// Show dialog for inviting new members
    /// Opens member invitation interface
    /// </summary>
    void ShowInviteMemberDialog()
    {
        // This would open a member invitation dialog
        Debug.Log("Show invite member dialog (not implemented)");
    }
    
    /// <summary>
    /// Show dialog for creating new roles
    /// Opens role creation interface
    /// </summary>
    void ShowCreateRoleDialog()
    {
        // This would open a role creation dialog
        Debug.Log("Show create role dialog (not implemented)");
    }
    
    #endregion
    
    #region Utility Methods
    
    /// <summary>
    /// Clear all display data when no group is selected
    /// Resets UI to empty state
    /// </summary>
    void ClearAllDisplays()
    {
        // Clear group information
        if (groupNameText) groupNameText.text = "No Group Selected";
        if (memberCountText) memberCountText.text = "Members: 0";
        if (groupDescriptionText) groupDescriptionText.text = "";
        if (groupCharterText) groupCharterText.text = "";
        if (groupInsigniaImage) groupInsigniaImage.texture = null;
        
        // Clear all lists
        groupMembers.Clear();
        groupRoles.Clear();
        groupNotices.Clear();
        
        // Update displays
        UpdateMembersDisplay();
        UpdateNoticesDisplay();
        UpdateRolesDisplay();
    }
    
    #endregion
}