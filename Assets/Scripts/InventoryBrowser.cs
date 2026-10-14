using OpenMetaverse;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using CrystalFrost.Inventory;
using CrystalFrost.UI;

public class InventoryBrowser : MonoBehaviour
{
    [Header("UI References")]
    public GameObject inventoryWindow;
    public Transform folderTreeRoot;
    public Transform itemListRoot;
    public GameObject folderButtonPrefab;
    public GameObject itemButtonPrefab;
    public TMP_Text selectedItemName;
    public TMP_Text selectedItemDescription;
    public Image selectedItemIcon;
    public Button wearButton;
    public Button detachButton;
    public Button deleteButton;
    public Button renameButton;
    
    [Header("Search")]
    public TMP_InputField searchField;
    public Button searchButton;

    [Header("Virtualization & Pre-Fetch")]
    public InventoryFetchManager fetchManager;
    public VirtualScrollViewController virtualScrollView;
    
    private GridClient client;
    private Dictionary<UUID, InventoryFolder> folderCache = new();
    private Dictionary<UUID, GameObject> folderButtons = new();
    private InventoryFolder currentFolder;
    private InventoryItem selectedItem;
    private bool isInventoryLoaded = false;
    private List<InventoryItem> currentDisplayedItems = new();
    private bool isSearching = false;
    private string currentSearchQuery = string.Empty;

    public class InventoryFolderButton : MonoBehaviour
    {
        public UUID folderID;
        public InventoryBrowser browser;
        public TMP_Text folderNameText;
        public Button expandButton;
        public Transform childContainer;
        public bool isExpanded = false;
        
        public void OnFolderClick()
        {
            browser.SelectFolder(folderID);
        }
        
        public void OnExpandClick()
        {
            ToggleExpanded();
        }
        
        public void ToggleExpanded()
        {
            isExpanded = !isExpanded;
            childContainer.gameObject.SetActive(isExpanded);
            expandButton.GetComponentInChildren<TMP_Text>().text = isExpanded ? "-" : "+";
        }
    }
    
    public class InventoryItemButton : MonoBehaviour
    {
        public UUID itemID;
        public InventoryBrowser browser;
        public TMP_Text itemNameText;
        public Image itemIcon;
        
        public void OnItemClick()
        {
            browser.SelectItem(itemID);
        }
    }

    void Awake()
    {
        client = ClientManager.client;
        if (inventoryWindow) inventoryWindow.SetActive(false);

        if (fetchManager == null)
        {
            fetchManager = GetComponent<InventoryFetchManager>();
            if (fetchManager == null)
            {
                fetchManager = gameObject.AddComponent<InventoryFetchManager>();
            }
        }

        if (virtualScrollView == null && itemListRoot != null)
        {
            virtualScrollView = itemListRoot.GetComponentInParent<VirtualScrollViewController>();
            if (virtualScrollView == null)
            {
                var scrollRect = itemListRoot.GetComponentInParent<ScrollRect>();
                if (scrollRect != null)
                {
                    virtualScrollView = scrollRect.gameObject.AddComponent<VirtualScrollViewController>();
                    virtualScrollView.scrollRect = scrollRect;
                    virtualScrollView.viewport = scrollRect.viewport;
                    virtualScrollView.content = itemListRoot as RectTransform;
                    virtualScrollView.itemPrefab = itemButtonPrefab;
                }
            }
        }
        
        // Setup search
        if (searchButton) searchButton.onClick.AddListener(SearchInventory);
        if (searchField) searchField.onEndEdit.AddListener((text) => { if (Input.GetKeyDown(KeyCode.Return)) SearchInventory(); });
        
        // Setup item action buttons
        if (wearButton) wearButton.onClick.AddListener(WearSelectedItem);
        if (detachButton) detachButton.onClick.AddListener(DetachSelectedItem);
        if (deleteButton) deleteButton.onClick.AddListener(DeleteSelectedItem);
        if (renameButton) renameButton.onClick.AddListener(RenameSelectedItem);
    }

    void Start()
    {
        if (fetchManager != null)
        {
            fetchManager.Initialize(client);
            fetchManager.OnSearchResultsUpdated += OnSearchResultsUpdated;
        }

        // Register inventory events
        if (client != null && client.Inventory != null)
        {
            client.Inventory.FolderUpdated += OnFolderUpdated;
            client.Inventory.ItemReceived += OnItemReceived;
        }
        if (client != null && client.Network != null)
        {
            client.Network.LoginProgress += OnLoginProgress;
        }
        
        if (ClientManager.active && !isInventoryLoaded)
        {
            LoadInventory();
        }
    }

    void OnDestroy()
    {
        if (fetchManager != null)
        {
            fetchManager.OnSearchResultsUpdated -= OnSearchResultsUpdated;
        }
        if (client != null && client.Inventory != null)
        {
            client.Inventory.FolderUpdated -= OnFolderUpdated;
            client.Inventory.ItemReceived -= OnItemReceived;
        }
        if (client != null && client.Network != null)
        {
            client.Network.LoginProgress -= OnLoginProgress;
        }
    }

    void OnSearchResultsUpdated()
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            if (isSearching)
            {
                PerformSearch(currentSearchQuery);
            }
            else if (currentFolder != null)
            {
                SelectFolder(currentFolder.UUID);
            }
        });
    }

    void OnLoginProgress(object sender, LoginProgressEventArgs e)
    {
        if (e.Status == LoginStatus.Success)
        {
            LoadInventory();
        }
    }

    void OnFolderUpdated(object sender, FolderUpdatedEventArgs e)
    {
        if (e.Success)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() => RefreshFolderDisplay(e.FolderID));
        }
    }

    void OnItemReceived(object sender, ItemReceivedEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() => RefreshItemDisplay(e.Item));
    }

    public void ToggleInventoryWindow()
    {
        bool newState = !inventoryWindow.activeSelf;
        inventoryWindow.SetActive(newState);
        
        if (newState && !isInventoryLoaded)
        {
            LoadInventory();
        }
    }

    void LoadInventory()
    {
        if (client.Inventory.Store == null || client.Inventory.Store.RootFolder == null)
        {
            Debug.LogWarning("Inventory not ready");
            return;
        }

        isInventoryLoaded = true;
        
        // Clear existing UI
        foreach (Transform child in folderTreeRoot)
        {
            Destroy(child.gameObject);
        }
        folderButtons.Clear();
        folderCache.Clear();

        // Load root folder
        var rootFolder = client.Inventory.Store.RootFolder;
        CreateFolderButton(rootFolder, folderTreeRoot, 0);
        
        // Load main folders
        var contents = client.Inventory.Store.GetContents(rootFolder.UUID);
        foreach (var item in contents)
        {
            if (item is InventoryFolder folder)
            {
                folderCache[folder.UUID] = folder;
                CreateFolderButton(folder, folderTreeRoot, 1);
            }
        }
        
        // Select root folder by default
        SelectFolder(rootFolder.UUID);
    }

    GameObject CreateFolderButton(InventoryFolder folder, Transform parent, int depth)
    {
        if (folderButtonPrefab == null)
        {
            Debug.LogError("Folder button prefab not assigned");
            return null;
        }

        var buttonObj = Instantiate(folderButtonPrefab, parent);
        var folderButton = buttonObj.GetComponent<InventoryFolderButton>();
        
        if (folderButton == null)
        {
            folderButton = buttonObj.AddComponent<InventoryFolderButton>();
        }
        
        folderButton.folderID = folder.UUID;
        folderButton.browser = this;
        
        // Set folder name
        if (folderButton.folderNameText)
        {
            folderButton.folderNameText.text = folder.Name;
        }
        
        // Add padding for depth
        var layoutGroup = buttonObj.GetComponent<HorizontalLayoutGroup>();
        if (layoutGroup) layoutGroup.padding.left = depth * 20;
        
        // Setup expand button
        if (folderButton.expandButton)
        {
            folderButton.expandButton.onClick.AddListener(folderButton.OnExpandClick);
        }
        
        // Setup folder click
        var button = buttonObj.GetComponent<Button>();
        if (button) button.onClick.AddListener(folderButton.OnFolderClick);
        
        folderButtons[folder.UUID] = buttonObj;
        return buttonObj;
    }

    public void SelectFolder(UUID folderID)
    {
        if (client == null || client.Inventory == null || client.Inventory.Store == null) return;
        if (!client.Inventory.Store.Contains(folderID)) return;
        
        isSearching = false;
        currentSearchQuery = string.Empty;

        var folder = (InventoryFolder)client.Inventory.Store[folderID];
        currentFolder = folder;
        
        if (fetchManager != null)
        {
            fetchManager.QueueFolderFetch(folderID, FetchPriority.High);
        }

        // Load folder contents
        var contents = client.Inventory.Store.GetContents(folderID);
        currentDisplayedItems = contents.OfType<InventoryItem>().ToList();

        foreach (var item in contents)
        {
            if (item is InventoryFolder subFolder)
            {
                folderCache[subFolder.UUID] = subFolder;
                if (fetchManager != null)
                {
                    fetchManager.QueueFolderFetch(subFolder.UUID, FetchPriority.Normal);
                }
            }
        }

        UpdateVirtualItemList();
        
        // Request folder contents if empty or unfetched
        if (contents.Count == 0 && (fetchManager == null || fetchManager.GetFolderStatus(folderID) == InventoryFetchStatus.Unfetched))
        {
            client.Inventory.RequestFolderContents(folderID, client.Self.AgentID, true, true, InventorySortOrder.ByName);
        }
    }

    public void SelectItem(UUID itemID)
    {
        if (client == null || client.Inventory == null || client.Inventory.Store == null) return;
        if (!client.Inventory.Store.Contains(itemID)) return;

        var item = client.Inventory.Store[itemID] as InventoryItem;
        if (item == null) return;

        selectedItem = item;
        if (selectedItemName != null) selectedItemName.text = item.Name;
        if (selectedItemDescription != null) selectedItemDescription.text = item.Description;
        if (selectedItemIcon != null) selectedItemIcon.sprite = GetItemTypeIcon(item);
    }

    public void WearSelectedItem()
    {
        if (selectedItem == null || client == null) return;
    }

    public void DetachSelectedItem()
    {
        if (selectedItem == null || client == null) return;
    }

    public void DeleteSelectedItem()
    {
        if (selectedItem == null || client == null || client.Inventory == null) return;
        client.Inventory.RemoveItem(selectedItem.UUID);
        selectedItem = null;
        if (selectedItemName != null) selectedItemName.text = string.Empty;
        if (selectedItemDescription != null) selectedItemDescription.text = string.Empty;
        if (currentFolder != null) SelectFolder(currentFolder.UUID);
    }

    public void RenameSelectedItem()
    {
        if (selectedItem == null || client == null || client.Inventory == null) return;
    }


    void SearchInventory()
    {
        if (searchField == null || string.IsNullOrWhiteSpace(searchField.text)) return;
        PerformSearch(searchField.text);
    }

    public void PerformSearch(string searchTerm)
    {
        isSearching = true;
        currentSearchQuery = searchTerm;

        if (fetchManager != null)
        {
            currentDisplayedItems = fetchManager.SearchInventory(searchTerm, client);
        }
        else
        {
            currentDisplayedItems.Clear();
            if (client != null && client.Inventory != null && client.Inventory.Store != null && client.Inventory.Store.RootFolder != null)
            {
                SearchFolderFallback(client.Inventory.Store.RootFolder.UUID, searchTerm.ToLowerInvariant());
            }
        }

        UpdateVirtualItemList();
    }

    void SearchFolderFallback(UUID folderID, string searchTerm)
    {
        var contents = client.Inventory.Store.GetContents(folderID);
        
        foreach (var item in contents)
        {
            if (item is InventoryItem invItem)
            {
                if ((invItem.Name != null && invItem.Name.ToLowerInvariant().Contains(searchTerm)) || 
                    (invItem.Description != null && invItem.Description.ToLowerInvariant().Contains(searchTerm)))
                {
                    currentDisplayedItems.Add(invItem);
                }
            }
            else if (item is InventoryFolder folder)
            {
                SearchFolderFallback(folder.UUID, searchTerm);
            }
        }
    }

    void UpdateVirtualItemList()
    {
        if (virtualScrollView != null)
        {
            if (virtualScrollView.itemPrefab == null && itemButtonPrefab != null)
            {
                virtualScrollView.itemPrefab = itemButtonPrefab;
            }

            virtualScrollView.SetData(currentDisplayedItems.Count, BindItemButton);
        }
        else
        {
            // Fallback non-virtualized display if virtual scroll view is missing
            if (itemListRoot != null)
            {
                foreach (Transform child in itemListRoot)
                {
                    Destroy(child.gameObject);
                }
                foreach (var item in currentDisplayedItems)
                {
                    CreateItemButton(item);
                }
            }
        }
    }

    void BindItemButton(GameObject container, int index)
    {
        if (index < 0 || index >= currentDisplayedItems.Count) return;
        var item = currentDisplayedItems[index];

        var itemButton = container.GetComponent<InventoryItemButton>();
        if (itemButton == null)
        {
            itemButton = container.AddComponent<InventoryItemButton>();
        }

        itemButton.itemID = item.UUID;
        itemButton.browser = this;

        if (itemButton.itemNameText == null)
        {
            itemButton.itemNameText = container.GetComponentInChildren<TMP_Text>();
        }
        if (itemButton.itemNameText != null)
        {
            itemButton.itemNameText.text = item.Name;
        }

        if (itemButton.itemIcon == null)
        {
            itemButton.itemIcon = container.GetComponentInChildren<Image>();
        }
        if (itemButton.itemIcon != null)
        {
            itemButton.itemIcon.sprite = GetItemTypeIcon(item);
        }

        var button = container.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(itemButton.OnItemClick);
        }
    }

    void CreateItemButton(InventoryItem item)
    {
        if (itemButtonPrefab == null || itemListRoot == null) return;
        
        var buttonObj = Instantiate(itemButtonPrefab, itemListRoot);
        var itemButton = buttonObj.GetComponent<InventoryItemButton>();
        
        if (itemButton == null)
        {
            itemButton = buttonObj.AddComponent<InventoryItemButton>();
        }
        
        itemButton.itemID = item.UUID;
        itemButton.browser = this;
        
        if (itemButton.itemNameText == null)
        {
            itemButton.itemNameText = buttonObj.GetComponentInChildren<TMP_Text>();
        }
        if (itemButton.itemNameText != null)
        {
            itemButton.itemNameText.text = item.Name;
        }
        
        if (itemButton.itemIcon == null)
        {
            itemButton.itemIcon = buttonObj.GetComponentInChildren<Image>();
        }
        if (itemButton.itemIcon != null)
        {
            itemButton.itemIcon.sprite = GetItemTypeIcon(item);
        }
        
        var button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(itemButton.OnItemClick);
        }
    }

    void RefreshFolderDisplay(UUID folderID)
    {
        if (currentFolder != null && currentFolder.UUID == folderID)
        {
            SelectFolder(folderID);
        }
    }

    void RefreshItemDisplay(InventoryItem item)
    {
        if (currentFolder != null && item.ParentUUID == currentFolder.UUID)
        {
            CreateItemButton(item);
        }
    }

    Sprite GetItemTypeIcon(InventoryItem item)
    {
        // This would return appropriate icons based on item type
        // For now, return default sprite
        return null;
    }
}