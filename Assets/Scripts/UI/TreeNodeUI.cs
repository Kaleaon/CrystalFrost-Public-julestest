using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OpenMetaverse;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using CrystalFrost;

/// <summary>
/// Enhanced UI node for inventory tree with comprehensive icon support and visual feedback
/// </summary>
public class TreeNodeUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    public TMP_Text itemNameText;
    public Image itemIcon;
    public Button expandButton;
    public LayoutElement indentElement;
    
    [Header("Visual Feedback")]
    public Image backgroundImage;
    public Color normalColor = Color.clear;
    public Color hoverColor = new Color(1f, 1f, 1f, 0.1f);
    public Color selectedColor = new Color(0.2f, 0.6f, 1f, 0.3f);
    
    [Header("Icons")]
    public Sprite folderClosedIcon;
    public Sprite folderOpenIcon;
    public Sprite textureIcon;
    public Sprite soundIcon;
    public Sprite animationIcon;
    public Sprite landmarkIcon;
    public Sprite noteIcon;
    public Sprite scriptIcon;
    public Sprite wearableIcon;
    public Sprite attachmentIcon;
    public Sprite unknownIcon;

    private InventoryBase itemData;
    private InventoryWindowUI inventoryWindow;
    private int depth;
    private bool isExpanded = false;
    private bool isSelected = false;
    private ILogger<TreeNodeUI> _logger;

    // Icon mapping for different inventory types
    private readonly Dictionary<AssetType, Sprite> _assetTypeIcons = new();

    private void Awake()
    {
        _logger = Services.GetService<ILogger<TreeNodeUI>>();
        InitializeIconMapping();
        SetupEventHandlers();
    }

    private void InitializeIconMapping()
    {
        // Map asset types to appropriate icons
        if (textureIcon != null) _assetTypeIcons[AssetType.Texture] = textureIcon;
        if (soundIcon != null) _assetTypeIcons[AssetType.Sound] = soundIcon;
        if (animationIcon != null) _assetTypeIcons[AssetType.Animation] = animationIcon;
        if (landmarkIcon != null) _assetTypeIcons[AssetType.Landmark] = landmarkIcon;
        if (noteIcon != null) _assetTypeIcons[AssetType.Notecard] = noteIcon;
        if (scriptIcon != null) _assetTypeIcons[AssetType.LSLText] = scriptIcon;
    }

    private void SetupEventHandlers()
    {
        // Setup hover effects
        EventTrigger trigger = gameObject.GetComponent<EventTrigger>() ?? gameObject.AddComponent<EventTrigger>();
        
        // Mouse enter
        EventTrigger.Entry pointerEnter = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerEnter
        };
        pointerEnter.callback.AddListener((data) => OnPointerEnter());
        trigger.triggers.Add(pointerEnter);
        
        // Mouse exit
        EventTrigger.Entry pointerExit = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerExit
        };
        pointerExit.callback.AddListener((data) => OnPointerExit());
        trigger.triggers.Add(pointerExit);
    }

    public void SetData(InventoryBase data, int nodeDepth, InventoryWindowUI window)
    {
        itemData = data;
        depth = nodeDepth;
        inventoryWindow = window;

        itemNameText.text = data.Name;
        indentElement.flexibleWidth = depth * 20;

        if (data is InventoryFolder folder)
        {
            SetupFolderNode(folder);
        }
        else if (data is InventoryItem item)
        {
            SetupItemNode(item);
        }
        
        UpdateBackgroundColor();
        _logger.LogDebug($"TreeNodeUI configured for {data.Name} (Type: {data.GetType().Name})");
    }

    private void SetupFolderNode(InventoryFolder folder)
    {
        expandButton.gameObject.SetActive(true);
        expandButton.onClick.RemoveAllListeners();
        expandButton.onClick.AddListener(ToggleExpand);
        
        // Set folder icon based on expansion state
        UpdateFolderIcon();
        
        // Special folder type handling
        if (folder.PreferredType != FolderType.None)
        {
            SetSpecialFolderIcon(folder.PreferredType);
        }
    }

    private void SetupItemNode(InventoryItem item)
    {
        expandButton.gameObject.SetActive(false);
        
        // Set icon based on item type
        SetItemIcon(item);
    }

    private void UpdateFolderIcon()
    {
        if (itemIcon != null)
        {
            itemIcon.sprite = isExpanded ? folderOpenIcon : folderClosedIcon;
            itemIcon.enabled = itemIcon.sprite != null;
        }
    }

    private void SetSpecialFolderIcon(FolderType folderType)
    {
        // Could implement special icons for system folders like Trash, Library, etc.
        // For now, use standard folder icons
        UpdateFolderIcon();
    }

    private void SetItemIcon(InventoryItem item)
    {
        if (itemIcon == null) return;

        Sprite iconToUse = unknownIcon;

        // Handle different inventory item types
        switch (item)
        {
            case InventoryWearable wearable:
                iconToUse = wearableIcon;
                break;
            case InventoryAttachment attachment:
                iconToUse = attachmentIcon;
                break;
            case InventoryTexture texture:
                iconToUse = textureIcon;
                break;
            case InventorySound sound:
                iconToUse = soundIcon;
                break;
            case InventoryAnimation animation:
                iconToUse = animationIcon;
                break;
            case InventoryLandmark landmark:
                iconToUse = landmarkIcon;
                break;
            case InventoryNotecard notecard:
                iconToUse = noteIcon;
                break;
            case InventoryLSL script:
                iconToUse = scriptIcon;
                break;
            default:
                // Try to match by asset type if specific type doesn't match
                if (_assetTypeIcons.TryGetValue(item.AssetType, out Sprite assetIcon))
                {
                    iconToUse = assetIcon;
                }
                break;
        }

        itemIcon.sprite = iconToUse;
        itemIcon.enabled = iconToUse != null;
    }

    private void ToggleExpand()
    {
        isExpanded = !isExpanded;
        
        // Update visual cue for expansion (rotate arrow)
        expandButton.transform.localRotation = isExpanded ? Quaternion.Euler(0, 0, 90) : Quaternion.identity;
        
        // Update folder icon
        UpdateFolderIcon();

        inventoryWindow.ToggleFolder(itemData as InventoryFolder, this, depth);
        
        _logger.LogDebug($"Folder {itemData.Name} {(isExpanded ? "expanded" : "collapsed")}");
    }

    private void OnPointerEnter()
    {
        if (!isSelected && backgroundImage != null)
        {
            backgroundImage.color = hoverColor;
        }
    }

    private void OnPointerExit()
    {
        if (!isSelected)
        {
            UpdateBackgroundColor();
        }
    }

    private void UpdateBackgroundColor()
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = isSelected ? selectedColor : normalColor;
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateBackgroundColor();
    }

    public bool IsExpanded() => isExpanded;
    public UUID GetItemUUID() => itemData.UUID;
    public InventoryBase GetItemData() => itemData;
    public bool IsSelected() => isSelected;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Right-click: Show context menu
            inventoryWindow.ShowContextMenu(itemData, eventData.position);
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            // Left-click: Select item
            inventoryWindow.SelectItem(this);
            
            // Double-click handling for items (could open properties, wear, etc.)
            if (eventData.clickCount == 2 && itemData is InventoryItem item)
            {
                HandleDoubleClick(item);
            }
        }
    }

    private void HandleDoubleClick(InventoryItem item)
    {
        try
        {
            // Default double-click behavior based on item type
            switch (item)
            {
                case InventoryWearable wearable:
                case InventoryAttachment attachment:
                    // Wear/attach the item
                    ClientManager.client.Appearance.AddToOutfit(new System.Collections.Generic.List<InventoryItem> { item }, true);
                    _logger.LogInformation($"Double-clicked to wear/attach: {item.Name}");
                    break;
                case InventoryTexture texture:
                    // Could open texture preview
                    _logger.LogInformation($"Double-clicked texture: {item.Name}");
                    break;
                case InventoryNotecard notecard:
                    // Could open notecard editor
                    _logger.LogInformation($"Double-clicked notecard: {item.Name}");
                    break;
                default:
                    _logger.LogDebug($"Double-clicked item: {item.Name} (Type: {item.GetType().Name})");
                    break;
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, $"Error handling double-click for item: {item.Name}");
        }
    }

    private void OnDestroy()
    {
        expandButton?.onClick.RemoveAllListeners();
    }
}
