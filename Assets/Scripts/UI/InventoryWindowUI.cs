using UnityEngine;
using UnityEngine.UI;
using OpenMetaverse;
using System.Collections.Generic;
using CrystalFrost.UI;
using Microsoft.Extensions.Logging;
using CrystalFrost;
using TMPro;

/// <summary>
/// Main modular window manager for the Inventory Browser and Worn Items panel.
/// Supports hierarchical tree views, lazy folder loading, context menu actions,
/// and tabbed navigation.
/// </summary>
public class InventoryWindowUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject treeNodePrefab;
    public Transform contentRoot;
    public ContextMenuUI contextMenu;
    public AttachmentPointSelectorUI attachmentPointSelector;
    public WornItemsPanelUI wornItemsPanel;
    
    [Header("Tabs")]
    public GameObject inventoryTabContent;
    public GameObject wornItemsTabContent;
    public Button inventoryTabButton;
    public Button wornItemsTabButton;

    private ILogger<InventoryWindowUI> _logger;
    private readonly Dictionary<UUID, TreeNodeUI> uiNodes = new();
    private readonly Dictionary<UUID, List<GameObject>> childNodes = new();
    private readonly HashSet<UUID> loadedFolders = new();
    private TreeNodeUI selectedNode;
    private GridClient _client;
    private bool _isSubscribed = false;

    private void Awake()
    {
        _logger = Services.GetService<ILogger<InventoryWindowUI>>();
        
        if (inventoryTabButton != null)
        {
            inventoryTabButton.onClick.AddListener(ShowInventoryTab);
        }

        if (wornItemsTabButton != null)
        {
            wornItemsTabButton.onClick.AddListener(ShowWornItemsTab);
        }
    }

    private void Start()
    {
        InitializeClientAndEvents();
        ShowInventoryTab();
    }

    private void OnEnable()
    {
        InitializeClientAndEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    public void InitializeClientAndEvents()
    {
        if (_isSubscribed) return;

        try
        {
            _client = ClientManager.client;
            if (_client != null && _client.Inventory != null)
            {
                _client.Inventory.FolderUpdated += Inventory_FolderUpdated;
                _client.Inventory.ItemReceived += Inventory_ItemReceived;
                _isSubscribed = true;

                if (_client.Inventory.Store != null && _client.Inventory.Store.RootFolder != null)
                {
                    PopulateRootFolder();
                }
            }
        }
        catch (System.Exception ex)
        {
            _logger?.LogError(ex, "Failed to subscribe InventoryWindowUI events.");
        }
    }

    private void UnsubscribeEvents()
    {
        if (!_isSubscribed || _client == null) return;

        try
        {
            _client.Inventory.FolderUpdated -= Inventory_FolderUpdated;
            _client.Inventory.ItemReceived -= Inventory_ItemReceived;
        }
        catch (System.Exception ex)
        {
            _logger?.LogError(ex, "Error unsubscribing InventoryWindowUI events.");
        }
        finally
        {
            _isSubscribed = false;
        }
    }

    #region Tab Management
    public void ShowInventoryTab()
    {
        if (inventoryTabContent != null) inventoryTabContent.SetActive(true);
        if (wornItemsTabContent != null) wornItemsTabContent.SetActive(false);
    }

    public void ShowWornItemsTab()
    {
        if (inventoryTabContent != null) inventoryTabContent.SetActive(false);
        if (wornItemsTabContent != null) wornItemsTabContent.SetActive(true);

        if (wornItemsPanel != null)
        {
            wornItemsPanel.RefreshWornItems();
        }
    }
    #endregion

    #region Event Handlers
    private void Inventory_FolderUpdated(object sender, FolderUpdatedEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            if (_client?.Inventory?.Store?.RootFolder != null && e.FolderID == _client.Inventory.Store.RootFolder.UUID)
            {
                PopulateRootFolder();
            }
            else if (uiNodes.TryGetValue(e.FolderID, out TreeNodeUI nodeUI))
            {
                if (nodeUI.IsExpanded() && _client?.Inventory?.Store?.Contains(e.FolderID) == true)
                {
                    InventoryFolder folder = (InventoryFolder)_client.Inventory.Store[e.FolderID];
                    int depth = GetNodeDepth(e.FolderID);
                    PopulateTree(folder, nodeUI.transform, depth + 1);
                }
            }
        });
    }

    private void Inventory_ItemReceived(object sender, ItemReceivedEventArgs e)
    {
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            if (e.Item != null && uiNodes.ContainsKey(e.Item.ParentUUID))
            {
                RefreshFolderContents(e.Item.ParentUUID);
            }
        });
    }
    #endregion

    public void PopulateRootFolder()
    {
        if (_client?.Inventory?.Store?.RootFolder == null || contentRoot == null) return;

        // Clear existing UI
        foreach (Transform child in contentRoot)
        {
            Destroy(child.gameObject);
        }
        uiNodes.Clear();
        childNodes.Clear();
        loadedFolders.Clear();

        PopulateTree(_client.Inventory.Store.RootFolder, contentRoot, 0);
    }

    public void PopulateTree(InventoryFolder parentFolder, Transform parentTransform, int depth)
    {
        if (parentFolder == null || parentTransform == null) return;

        if (childNodes.TryGetValue(parentFolder.UUID, out var oldChildren))
        {
            foreach (var child in oldChildren)
            {
                if (child != null) Destroy(child);
            }
        }

        childNodes[parentFolder.UUID] = new List<GameObject>();
        loadedFolders.Add(parentFolder.UUID);

        List<InventoryBase> contents = _client.Inventory.Store.GetContents(parentFolder.UUID);

        if (contents.Count == 0 && _client != null)
        {
            // Request folder contents asynchronously from grid without thread locking
            _client.Inventory.RequestFolderContents(parentFolder.UUID, _client.Self.AgentID, true, true, InventorySortOrder.ByName);
        }

        foreach (var item in contents)
        {
            if (item == null || treeNodePrefab == null) continue;

            GameObject nodeGO = Instantiate(treeNodePrefab, parentTransform);
            TreeNodeUI nodeUI = nodeGO.GetComponent<TreeNodeUI>();
            if (nodeUI == null) nodeUI = nodeGO.AddComponent<TreeNodeUI>();

            nodeUI.SetData(item, depth, this);
            uiNodes[item.UUID] = nodeUI;
            childNodes[parentFolder.UUID].Add(nodeGO);

            nodeGO.name = (item is InventoryFolder) ? $"Folder_{item.Name}" : $"Item_{item.Name}";
        }
    }

    public void RefreshFolderContents(UUID folderId)
    {
        if (_client?.Inventory?.Store?.Contains(folderId) == true && _client.Inventory.Store[folderId] is InventoryFolder folder)
        {
            if (uiNodes.TryGetValue(folderId, out TreeNodeUI parentNode))
            {
                int depth = GetNodeDepth(folderId);
                PopulateTree(folder, parentNode.transform, depth + 1);
            }
        }
    }

    private int GetNodeDepth(UUID folderId)
    {
        int depth = 0;
        UUID current = folderId;

        while (_client?.Inventory?.Store?.Contains(current) == true)
        {
            InventoryBase item = _client.Inventory.Store[current];
            if (item.ParentUUID == UUID.Zero || item.ParentUUID == current) break;
            current = item.ParentUUID;
            depth++;
        }

        return depth;
    }

    public void ToggleFolder(InventoryFolder folder, TreeNodeUI nodeUI, int depth)
    {
        if (folder == null || nodeUI == null) return;

        bool isExpanded = nodeUI.IsExpanded();

        if (isExpanded)
        {
            if (!loadedFolders.Contains(folder.UUID))
            {
                PopulateTree(folder, nodeUI.transform, depth + 1);
            }

            if (childNodes.TryGetValue(folder.UUID, out var children))
            {
                foreach (var child in children)
                {
                    if (child != null) child.SetActive(true);
                }
            }
        }
        else
        {
            HideChildren(folder.UUID);
        }
    }

    private void HideChildren(UUID folderId)
    {
        if (childNodes.TryGetValue(folderId, out var children))
        {
            foreach (var child in children)
            {
                if (child == null) continue;
                child.SetActive(false);

                TreeNodeUI childUI = child.GetComponent<TreeNodeUI>();
                if (childUI != null && childUI.GetItemData() is InventoryFolder subFolder)
                {
                    HideChildren(subFolder.UUID);
                }
            }
        }
    }

    #region Context Menu & Action Handlers
    public void ShowContextMenu(InventoryBase item, Vector2 position)
    {
        if (item == null || contextMenu == null) return;

        contextMenu.ClearButtons();

        if (item is InventoryItem invItem)
        {
            ShowItemContextMenu(invItem);
        }
        else if (item is InventoryFolder folder)
        {
            ShowFolderContextMenu(folder);
        }

        contextMenu.Show(position);
    }

    private void ShowItemContextMenu(InventoryItem item)
    {
        bool canWear = item is InventoryWearable || item is InventoryAttachment || item is InventoryObject;
        bool isWorn = IsItemWorn(item);

        if (canWear)
        {
            if (!isWorn)
            {
                contextMenu.AddButton("Wear / Attach", () => WearItem(item));
            }
            else
            {
                contextMenu.AddButton("Take Off / Detach", () => DetachItem(item));
            }
        }

        if (item is InventoryAttachment || item is InventoryObject)
        {
            contextMenu.AddButton("Attach To...", () => OpenAttachmentPointSelector(item));
        }

        contextMenu.AddButton("Offer / Give", () => OfferItem(item));
        contextMenu.AddButton("Rename", () => RenameItemOrFolder(item));
        contextMenu.AddButton("Move To...", () => MoveItemOrFolder(item));
        contextMenu.AddButton("Delete", () => DeleteItemOrFolder(item));
    }

    private void ShowFolderContextMenu(InventoryFolder folder)
    {
        contextMenu.AddButton("Create Subfolder", () => CreateSubfolder(folder));
        contextMenu.AddButton("Rename Folder", () => RenameItemOrFolder(folder));
        contextMenu.AddButton("Delete Folder", () => DeleteItemOrFolder(folder));
    }

    private bool IsItemWorn(InventoryItem item)
    {
        if (ClientManager.currentOutfitFolder == null) return false;
        var links = ClientManager.currentOutfitFolder.ContentLinks();
        return links.Exists(l => l.AssetUUID == item.UUID);
    }

    private void WearItem(InventoryItem item)
    {
        _logger?.LogInformation($"Wearing item: {item.Name}");
        if (ClientManager.currentOutfitFolder != null)
        {
            ClientManager.currentOutfitFolder.AddToOutfit(item, true);
        }
        else if (_client?.Appearance != null)
        {
            _client.Appearance.AddToOutfit(new List<InventoryItem> { item }, true);
        }
    }

    private void DetachItem(InventoryItem item)
    {
        _logger?.LogInformation($"Detaching item: {item.Name}");
        if (ClientManager.currentOutfitFolder != null)
        {
            ClientManager.currentOutfitFolder.RemoveFromOutfit(item);
        }
        else if (_client?.Appearance != null)
        {
            _client.Appearance.RemoveFromOutfit(new List<InventoryItem> { item });
        }
    }

    private void OpenAttachmentPointSelector(InventoryItem item)
    {
        if (attachmentPointSelector != null)
        {
            attachmentPointSelector.Show(item, (point) =>
            {
                if (_client?.Objects != null)
                {
                    _client.Objects.AttachObject(_client.Network.CurrentSim, item.UUID, item.ParentUUID, point, OpenMetaverse.Packets.AttachObjectPacket.DataBlock.ATTACHMENT_FLAG_OBJECT_INVENTORY);
                    _logger?.LogInformation($"Attached {item.Name} to {point}");
                }
            });
        }
        else if (_client?.Appearance != null)
        {
            _client.Appearance.Attach(item, AttachmentPoint.Default, true);
        }
    }

    private void OfferItem(InventoryItem item)
    {
        _logger?.LogInformation($"Offering asset item: {item.Name}");
        // Trigger inventory offer dialog or call LibreMetaverse offer item logic
    }

    private void RenameItemOrFolder(InventoryBase item)
    {
        if (_client?.Inventory == null || item == null) return;
        _logger?.LogInformation($"Renaming item: {item.Name}");

        // In a real dialog, prompt user. For instant renaming support:
        if (item is InventoryItem invItem)
        {
            _client.Inventory.RequestFetchInventory(invItem.UUID, invItem.OwnerID);
        }
    }

    private void MoveItemOrFolder(InventoryBase item)
    {
        if (_client?.Inventory == null || item == null) return;
        _logger?.LogInformation($"Moving item: {item.Name}");
    }

    private void DeleteItemOrFolder(InventoryBase item)
    {
        if (_client?.Inventory == null || item == null) return;

        _logger?.LogInformation($"Deleting item: {item.Name}");
        if (item is InventoryItem invItem)
        {
            _client.Inventory.RemoveItem(invItem.UUID);
        }
        else if (item is InventoryFolder folder)
        {
            _client.Inventory.RemoveFolder(folder.UUID);
        }
    }

    private void CreateSubfolder(InventoryFolder folder)
    {
        if (_client?.Inventory == null || folder == null) return;

        _logger?.LogInformation($"Creating subfolder in: {folder.Name}");
        _client.Inventory.CreateFolder(folder.UUID, "New Folder", FolderType.None);
    }
    #endregion

    public void SelectItem(TreeNodeUI node)
    {
        if (selectedNode != null)
        {
            selectedNode.SetSelected(false);
        }

        selectedNode = node;
        if (selectedNode != null)
        {
            selectedNode.SetSelected(true);
            _logger?.LogDebug($"Selected inventory item: {selectedNode.GetItemData()?.Name}");
        }
    }
}
