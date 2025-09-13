using OpenMetaverse;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

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
    
    private GridClient client;
    private Dictionary<UUID, InventoryFolder> folderCache = new();
    private Dictionary<UUID, GameObject> folderButtons = new();
    private InventoryFolder currentFolder;
    private InventoryItem selectedItem;
    private bool isInventoryLoaded = false;

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
        inventoryWindow.SetActive(false);
        
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
        // Register inventory events
        client.Inventory.FolderUpdated += OnFolderUpdated;
        client.Inventory.ItemReceived += OnItemReceived;
        client.Network.LoginCompleted += OnLoginCompleted;
        
        if (ClientManager.active && !isInventoryLoaded)
        {
            LoadInventory();
        }
    }

    void OnDestroy()
    {
        if (client != null)
        {
            client.Inventory.FolderUpdated -= OnFolderUpdated;
            client.Inventory.ItemReceived -= OnItemReceived;
            client.Network.LoginCompleted -= OnLoginCompleted;
        }
    }

    void OnLoginCompleted(object sender, LoginCompleteEventArgs e)
    {
        if (e.Success)
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
        if (!client.Inventory.Store.Contains(folderID)) return;
        
        var folder = (InventoryFolder)client.Inventory.Store[folderID];
        currentFolder = folder;
        
        // Clear current items
        foreach (Transform child in itemListRoot)
        {
            Destroy(child.gameObject);
        }
        
        // Load folder contents
        var contents = client.Inventory.Store.GetContents(folderID);
        foreach (var item in contents)
        {
            if (item is InventoryItem invItem)
            {
                CreateItemButton(invItem);
            }
            else if (item is InventoryFolder subFolder)
            {
                // Ensure subfolder is in cache
                folderCache[subFolder.UUID] = subFolder;
            }
        }
        
        // Request folder contents if empty
        if (contents.Count == 0)
        {
            client.Inventory.RequestFolderContents(folderID, client.Self.AgentID, true, true, InventorySortOrder.ByName);
        }
    }

    void CreateItemButton(InventoryItem item)
    {
        if (itemButtonPrefab == null) return;
        
        var buttonObj = Instantiate(itemButtonPrefab, itemListRoot);
        var itemButton = buttonObj.GetComponent<InventoryItemButton>();
        
        if (itemButton == null)
        {
            itemButton = buttonObj.AddComponent<InventoryItemButton>();
        }
        
        itemButton.itemID = item.UUID;
        itemButton.browser = this;
        
        if (itemButton.itemNameText)
        {
            itemButton.itemNameText.text = item.Name;
        }
        
        // Set item icon based on type
        if (itemButton.itemIcon)
        {
            itemButton.itemIcon.sprite = GetItemTypeIcon(item);
        }
        
        // Setup click handler
        var button = buttonObj.GetComponent<Button>();
        if (button) button.onClick.AddListener(itemButton.OnItemClick);
    }

    public void SelectItem(UUID itemID)
    {
        if (!client.Inventory.Store.Contains(itemID)) return;
        
        selectedItem = (InventoryItem)client.Inventory.Store[itemID];
        
        // Update item details UI
        if (selectedItemName) selectedItemName.text = selectedItem.Name;
        if (selectedItemDescription) selectedItemDescription.text = selectedItem.Description;
        if (selectedItemIcon) selectedItemIcon.sprite = GetItemTypeIcon(selectedItem);
        
        // Update button states
        UpdateActionButtons();
    }

    void UpdateActionButtons()
    {
        if (selectedItem == null) return;
        
        bool canWear = selectedItem is InventoryWearable || selectedItem is InventoryAttachment;
        bool isWorn = IsItemWorn(selectedItem);
        
        if (wearButton) wearButton.gameObject.SetActive(canWear && !isWorn);
        if (detachButton) detachButton.gameObject.SetActive(canWear && isWorn);
        if (deleteButton) deleteButton.gameObject.SetActive(true);
        if (renameButton) renameButton.gameObject.SetActive(true);
    }

    bool IsItemWorn(InventoryItem item)
    {
        if (ClientManager.currentOutfitFolder == null) return false;
        
        var links = ClientManager.currentOutfitFolder.ContentLinks();
        return links.Any(link => link.AssetUUID == item.UUID);
    }

    void WearSelectedItem()
    {
        if (selectedItem == null || ClientManager.currentOutfitFolder == null) return;
        
        ClientManager.currentOutfitFolder.AddToOutfit(selectedItem, true);
        UpdateActionButtons();
    }

    void DetachSelectedItem()
    {
        if (selectedItem == null || ClientManager.currentOutfitFolder == null) return;
        
        ClientManager.currentOutfitFolder.RemoveFromOutfit(selectedItem);
        UpdateActionButtons();
    }

    void DeleteSelectedItem()
    {
        if (selectedItem == null) return;
        
        // Confirm deletion
        if (UnityEngine.Application.isEditor || 
            UnityEngine.Windows.Input.ShowMessageBox("Delete Item", 
            $"Are you sure you want to delete '{selectedItem.Name}'?", "Yes", "No") == 0)
        {
            client.Inventory.RemoveItem(selectedItem.UUID);
        }
    }

    void RenameSelectedItem()
    {
        if (selectedItem == null) return;
        
        // This would need a dialog box implementation
        Debug.Log($"Rename item: {selectedItem.Name}");
    }

    void SearchInventory()
    {
        if (searchField == null || string.IsNullOrEmpty(searchField.text)) return;
        
        string searchTerm = searchField.text.ToLower();
        
        // Clear current display
        foreach (Transform child in itemListRoot)
        {
            Destroy(child.gameObject);
        }
        
        // Search through all inventory items
        var allItems = client.Inventory.Store.GetContents(client.Inventory.Store.RootFolder.UUID);
        SearchFolder(client.Inventory.Store.RootFolder.UUID, searchTerm);
    }

    void SearchFolder(UUID folderID, string searchTerm)
    {
        var contents = client.Inventory.Store.GetContents(folderID);
        
        foreach (var item in contents)
        {
            if (item is InventoryItem invItem)
            {
                if (invItem.Name.ToLower().Contains(searchTerm) || 
                    invItem.Description.ToLower().Contains(searchTerm))
                {
                    CreateItemButton(invItem);
                }
            }
            else if (item is InventoryFolder folder)
            {
                SearchFolder(folder.UUID, searchTerm);
            }
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