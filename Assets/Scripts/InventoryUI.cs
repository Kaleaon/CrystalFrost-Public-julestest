using UnityEngine;
using UnityEngine.UI;
using OpenMetaverse;
using System.Collections.Generic;
using CrystalFrost.UI;
using Microsoft.Extensions.Logging;
using CrystalFrost;

public class InventoryUI : MonoBehaviour
{
    public GameObject InventoryPanel;
    public RectTransform Content;
    public GameObject InventoryEntryPrefab;

    private bool isPopulated = false;
    private HashSet<UUID> expandedFolders = new HashSet<UUID>();
    private Dictionary<UUID, GameObject> inventoryEntries = new Dictionary<UUID, GameObject>();
    
    private ILogger<InventoryUI> _logger;
    private IUIStateTracker _uiStateTracker;
    
    private void Awake()
    {
        _logger = Services.GetService<ILogger<InventoryUI>>();
        _uiStateTracker = FindObjectOfType<UIStateTracker>();
    }

    public void TogglePanel()
    {
        if (InventoryPanel != null)
        {
            bool wasActive = InventoryPanel.activeSelf;
            bool newState = !wasActive;
            InventoryPanel.SetActive(newState);
            
            // Track visibility change
            _uiStateTracker?.TrackVisibilityChanged(InventoryPanel, "InventoryPanel", newState);
            
            if (newState && !isPopulated)
            {
                PopulateInventory();
            }
        }
    }

    public void PopulateInventory()
    {
        // Track population start
        _uiStateTracker?.TrackInteraction(gameObject, "InventoryUI", "PopulateStarted", null);
        
        // Clear existing entries
        foreach (Transform child in Content)
        {
            Destroy(child.gameObject);
        }

        InventoryFolder rootFolder = ClientManager.client.Inventory.Store.RootFolder;
        CreateInventoryEntries(rootFolder, 0);
        isPopulated = true;
        
        // Track population completion
        _uiStateTracker?.TrackContentChanged(gameObject, "InventoryUI", new { action = "PopulateCompleted", entryCount = inventoryEntries.Count });
    }

    void CreateInventoryEntries(InventoryFolder parentFolder, int depth)
    {
        List<InventoryBase> contents = ClientManager.client.Inventory.Store.GetContents(parentFolder);

        foreach (var item in contents)
        {
            CreateInventoryEntry(item, depth);
        }
    }

    void CreateInventoryEntry(InventoryBase item, int depth)
    {
        GameObject entryGO = Instantiate(InventoryEntryPrefab, Content);
        inventoryEntries[item.UUID] = entryGO;
        entryGO.SetActive(true);

        TMPro.TMP_Text nameText = entryGO.GetComponentInChildren<TMPro.TMP_Text>();
        nameText.text = new string(' ', depth * 4) + item.Name;

        // TODO: Set icon based on item type

        if (item is InventoryFolder)
        {
            Button button = entryGO.GetComponent<Button>();
            button.onClick.AddListener(() => OnFolderClicked((InventoryFolder)item));
        }
    }

    void OnFolderClicked(InventoryFolder folder)
    {
        if (expandedFolders.Contains(folder.UUID))
        {
            // Collapse
            expandedFolders.Remove(folder.UUID);
            CollapseFolder(folder);
            
            // Track folder collapse
            _uiStateTracker?.TrackInteraction(inventoryEntries[folder.UUID], "InventoryEntry", "FolderCollapsed", new { folderId = folder.UUID.ToString(), folderName = folder.Name });
        }
        else
        {
            // Expand
            expandedFolders.Add(folder.UUID);
            CreateInventoryEntries(folder, GetDepth(folder) + 1);
            
            // Track folder expansion
            _uiStateTracker?.TrackInteraction(inventoryEntries[folder.UUID], "InventoryEntry", "FolderExpanded", new { folderId = folder.UUID.ToString(), folderName = folder.Name });
        }
    }

    void CollapseFolder(InventoryFolder folder)
    {
        List<InventoryBase> contents = ClientManager.client.Inventory.Store.GetContents(folder);
        foreach (var item in contents)
        {
            if (inventoryEntries.TryGetValue(item.UUID, out GameObject entryGO))
            {
                Destroy(entryGO);
                inventoryEntries.Remove(item.UUID);
            }
            if (item is InventoryFolder)
            {
                CollapseFolder((InventoryFolder)item);
            }
        }
    }

    int GetDepth(InventoryBase item)
    {
        int depth = 0;
        UUID parentID = item.ParentUUID;
        while (parentID != UUID.Zero && parentID != ClientManager.client.Inventory.Store.RootFolder.UUID)
        {
            depth++;
            if (ClientManager.client.Inventory.Store.Items.TryGetValue(parentID, out InventoryItem parentItem))
            {
                parentID = parentItem.ParentUUID;
            }
            else
            {
                break;
            }
        }
        return depth;
    }
}
