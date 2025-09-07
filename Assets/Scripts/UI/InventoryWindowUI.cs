using UnityEngine;
using OpenMetaverse;
using System.Collections.Generic;
using CrystalFrost.UI;
using Microsoft.Extensions.Logging;
using CrystalFrost;

public class InventoryWindowUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject treeNodePrefab;
    public Transform contentRoot;
    public ContextMenuUI contextMenu;
    public AttachmentPointSelectorUI attachmentPointSelector;
    
    private ILogger<InventoryWindowUI> _logger;

    // A dictionary to keep track of the UI nodes to manage expand/collapse state
    private Dictionary<UUID, TreeNodeUI> uiNodes = new Dictionary<UUID, TreeNodeUI>();
    private Dictionary<UUID, List<GameObject>> childNodes = new Dictionary<UUID, List<GameObject>>();
    private TreeNodeUI selectedNode;

    private void Start()
    {
        // Initialize logger
        _logger = Services.GetService<ILogger<InventoryWindowUI>>();
        
        // Subscribe to the folder update event to know when the inventory is ready
        ClientManager.client.Inventory.FolderUpdated += Inventory_FolderUpdated;
        
        _logger.LogInformation("InventoryWindowUI initialized and subscribed to folder updates");
    }

    private void OnDestroy()
    {
        // Always unsubscribe from events
        ClientManager.client.Inventory.FolderUpdated -= Inventory_FolderUpdated;
    }

    private void Inventory_FolderUpdated(object sender, FolderUpdatedEventArgs e)
    {
        // Check if the root folder has been updated, which signals the inventory is loaded
        if (e.FolderID == ClientManager.client.Inventory.Store.RootFolder.UUID)
        {
            // Unsubscribe so we don't re-populate on every subsequent folder update
            ClientManager.client.Inventory.FolderUpdated -= Inventory_FolderUpdated;

            // Now it's safe to populate the tree
            PopulateTree(ClientManager.client.Inventory.Store.RootFolder, contentRoot, 0);
        }
    }

    public void PopulateTree(InventoryFolder parentFolder, Transform parentTransform, int depth)
    {
        List<InventoryBase> contents = ClientManager.client.Inventory.Store.GetContents(parentFolder.UUID);

        childNodes[parentFolder.UUID] = new List<GameObject>();

        foreach (var item in contents)
        {
            GameObject nodeGO = Instantiate(treeNodePrefab, parentTransform);
            TreeNodeUI nodeUI = nodeGO.GetComponent<TreeNodeUI>();

            nodeUI.SetData(item, depth, this);
            uiNodes[item.UUID] = nodeUI;
            childNodes[parentFolder.UUID].Add(nodeGO);

            if (item is InventoryFolder)
            {
                nodeGO.name = $"Folder: {item.Name}";
            }
            else
            {
                nodeGO.name = $"Item: {item.Name}";
            }
        }
    }

    public void ToggleFolder(InventoryFolder folder, TreeNodeUI nodeUI, int depth)
    {
        bool isExpanded = nodeUI.IsExpanded();

        if (isExpanded)
        {
            // If it's already expanded, we need to populate its children
            if (!childNodes.ContainsKey(folder.UUID))
            {
                PopulateTree(folder, nodeUI.transform, depth + 1);
            }
            // Show children
            if (childNodes.TryGetValue(folder.UUID, out var children))
            {
                foreach(var child in children)
                {
                    child.SetActive(true);
                }
            }
        }
        else
        {
            // If it's collapsed, hide all descendants
            HideChildren(folder.UUID);
        }
    }

    private void HideChildren(UUID folderId)
    {
        if (childNodes.TryGetValue(folderId, out var children))
        {
            foreach (var child in children)
            {
                child.SetActive(false);
                // If this child is a folder, recursively hide its children too
                InventoryBase itemData = uiNodes[child.GetComponent<TreeNodeUI>().GetItemUUID()].GetItemData();
                if (itemData is InventoryFolder)
                {
                    HideChildren(itemData.UUID);
                }
            }
        }
    }

    public void ShowContextMenu(InventoryBase item, Vector2 position)
    {
        contextMenu.ClearButtons();

        // Add actions based on item type
        if (item is InventoryWearable || item is InventoryAttachment)
        {
            contextMenu.AddButton("Wear", () => {
                ClientManager.client.Appearance.AddToOutfit(new List<InventoryItem> { (InventoryItem)item }, true);
            });
            contextMenu.AddButton("Take Off", () => {
                ClientManager.client.Appearance.RemoveFromOutfit(new List<InventoryItem> { (InventoryItem)item });
            });
        }

        if (item is InventoryAttachment)
        {
            // Comprehensive attachment functionality with point selection
            contextMenu.AddButton("Attach To...", () => {
                if (attachmentPointSelector != null)
                {
                    attachmentPointSelector.Show((InventoryItem)item, (attachmentPoint) => {
                        try
                        {
                            ClientManager.client.Objects.AttachObject(
                                ClientManager.client.Network.CurrentSim,
                                ((InventoryItem)item).UUID,
                                ((InventoryItem)item).ParentUUID,
                                attachmentPoint,
                                OpenMetaverse.Packets.AttachObjectPacket.DataBlock.ATTACHMENT_FLAG_OBJECT_INVENTORY
                            );
                            _logger.LogInformation($"Attached {item.Name} to {attachmentPoint}");
                        }
                        catch (System.Exception ex)
                        {
                            _logger.LogError(ex, $"Failed to attach {item.Name} to {attachmentPoint}");
                        }
                    });
                }
                else
                {
                    _logger.LogWarning("AttachmentPointSelector not assigned to InventoryWindowUI");
                }
            });
            
            contextMenu.AddButton("Detach", () => {
                try
                {
                    ClientManager.client.Appearance.Detach((InventoryItem)item);
                    _logger.LogInformation($"Detached {item.Name}");
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, $"Failed to detach {item.Name}");
                }
            });
        }

        // Add more general actions
        contextMenu.AddButton("Delete", () => {
            ClientManager.client.Inventory.Remove(item.UUID, null);
        });

        contextMenu.Show(position);
    }

    /// <summary>
    /// Handles item selection in the inventory tree
    /// </summary>
    /// <param name="node">The tree node that was selected</param>
    public void SelectItem(TreeNodeUI node)
    {
        try
        {
            // Deselect previous node
            if (selectedNode != null)
            {
                selectedNode.SetSelected(false);
            }
            
            // Select new node
            selectedNode = node;
            node.SetSelected(true);
            
            _logger.LogDebug($"Selected inventory item: {node.GetItemData().Name}");
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error selecting inventory item");
        }
    }
}
