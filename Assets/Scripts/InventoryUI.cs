using UnityEngine;
using OpenMetaverse;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    public GameObject InventoryPanel;
    public RectTransform Content;
    public GameObject InventoryEntryPrefab;

    private bool isPopulated = false;
    private HashSet<UUID> expandedFolders = new HashSet<UUID>();
    private Dictionary<UUID, GameObject> inventoryEntries = new Dictionary<UUID, GameObject>();

    public void TogglePanel()
    {
        if (InventoryPanel != null)
        {
            InventoryPanel.SetActive(!InventoryPanel.activeSelf);
            if (InventoryPanel.activeSelf && !isPopulated)
            {
                PopulateInventory();
            }
        }
    }

    public void PopulateInventory()
    {
        // Clear existing entries
        foreach (Transform child in Content)
        {
            Destroy(child.gameObject);
        }

        InventoryFolder rootFolder = ClientManager.client.Inventory.Store.RootFolder;
        CreateInventoryEntries(rootFolder, 0);
        isPopulated = true;
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
        }
        else
        {
            // Expand
            expandedFolders.Add(folder.UUID);
            CreateInventoryEntries(folder, GetDepth(folder) + 1);
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
