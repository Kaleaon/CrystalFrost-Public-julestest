using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OpenMetaverse;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using CrystalFrost;
using CrystalFrost.UI;

/// <summary>
/// Enhanced UI node for inventory tree with icon support, expand/collapse,
/// selection state, and right-click context menu triggers.
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
    public Sprite objectIcon;
    public Sprite gestureIcon;
    public Sprite bodyPartIcon;
    public Sprite unknownIcon;

    private InventoryBase itemData;
    private InventoryWindowUI inventoryWindow;
    private int depth;
    private bool isExpanded = false;
    private bool isSelected = false;
    private ILogger<TreeNodeUI> _logger;

    private readonly Dictionary<AssetType, Sprite> _assetTypeIcons = new();

    private void Awake()
    {
        _logger = Services.GetService<ILogger<TreeNodeUI>>();
        InitializeIconMapping();
        SetupEventHandlers();
    }

    private void InitializeIconMapping()
    {
        if (textureIcon != null) _assetTypeIcons[AssetType.Texture] = textureIcon;
        if (soundIcon != null) _assetTypeIcons[AssetType.Sound] = soundIcon;
        if (animationIcon != null) _assetTypeIcons[AssetType.Animation] = animationIcon;
        if (landmarkIcon != null) _assetTypeIcons[AssetType.Landmark] = landmarkIcon;
        if (noteIcon != null) _assetTypeIcons[AssetType.Notecard] = noteIcon;
        if (scriptIcon != null) _assetTypeIcons[AssetType.LSLText] = scriptIcon;
        if (objectIcon != null) _assetTypeIcons[AssetType.Object] = objectIcon;
        if (gestureIcon != null) _assetTypeIcons[AssetType.Gesture] = gestureIcon;
        if (wearableIcon != null) _assetTypeIcons[AssetType.Clothing] = wearableIcon;
        if (bodyPartIcon != null) _assetTypeIcons[AssetType.Bodypart] = bodyPartIcon;
    }

    private void SetupEventHandlers()
    {
        EventTrigger trigger = gameObject.GetComponent<EventTrigger>() ?? gameObject.AddComponent<EventTrigger>();
        
        EventTrigger.Entry pointerEnter = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerEnter
        };
        pointerEnter.callback.AddListener((_) => OnPointerEnter());
        trigger.triggers.Add(pointerEnter);
        
        EventTrigger.Entry pointerExit = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerExit
        };
        pointerExit.callback.AddListener((_) => OnPointerExit());
        trigger.triggers.Add(pointerExit);
    }

    public void SetData(InventoryBase data, int nodeDepth, InventoryWindowUI window)
    {
        itemData = data;
        depth = nodeDepth;
        inventoryWindow = window;

        if (itemNameText != null)
        {
            itemNameText.text = data != null ? data.Name : "Unknown Item";
        }

        if (indentElement != null)
        {
            indentElement.minWidth = depth * 18;
        }

        if (data is InventoryFolder folder)
        {
            SetupFolderNode(folder);
        }
        else if (data is InventoryItem item)
        {
            SetupItemNode(item);
        }
        
        UpdateBackgroundColor();
        _logger?.LogDebug($"TreeNodeUI configured for {data?.Name} (Type: {data?.GetType().Name})");
    }

    private void SetupFolderNode(InventoryFolder folder)
    {
        if (expandButton != null)
        {
            expandButton.gameObject.SetActive(true);
            expandButton.onClick.RemoveAllListeners();
            expandButton.onClick.AddListener(ToggleExpand);
        }
        
        UpdateFolderIcon();
    }

    private void SetupItemNode(InventoryItem item)
    {
        if (expandButton != null)
        {
            expandButton.gameObject.SetActive(false);
        }
        
        SetItemIcon(item);
    }

    private void UpdateFolderIcon()
    {
        if (itemIcon != null)
        {
            Sprite icon = isExpanded ? folderOpenIcon : folderClosedIcon;
            if (icon != null)
            {
                itemIcon.sprite = icon;
                itemIcon.enabled = true;
            }
        }
    }

    private void SetItemIcon(InventoryItem item)
    {
        if (itemIcon == null) return;

        Sprite iconToUse = unknownIcon;

        if (item is InventoryWearable wearable)
        {
            iconToUse = IsBodyPart(wearable) ? (bodyPartIcon ?? wearableIcon) : wearableIcon;
        }
        else if (item is InventoryAttachment)
        {
            iconToUse = attachmentIcon ?? objectIcon;
        }
        else if (item is InventoryTexture)
        {
            iconToUse = textureIcon;
        }
        else if (item is InventorySound)
        {
            iconToUse = soundIcon;
        }
        else if (item is InventoryAnimation)
        {
            iconToUse = animationIcon;
        }
        else if (item is InventoryLandmark)
        {
            iconToUse = landmarkIcon;
        }
        else if (item is InventoryNotecard)
        {
            iconToUse = noteIcon;
        }
        else if (item is InventoryLSL)
        {
            iconToUse = scriptIcon;
        }
        else if (item is InventoryObject)
        {
            iconToUse = objectIcon;
        }
        else if (item is InventoryGesture)
        {
            iconToUse = gestureIcon;
        }
        else if (_assetTypeIcons.TryGetValue(item.AssetType, out Sprite assetIcon))
        {
            iconToUse = assetIcon;
        }

        if (iconToUse != null)
        {
            itemIcon.sprite = iconToUse;
            itemIcon.enabled = true;
        }
    }

    private bool IsBodyPart(InventoryWearable wearable)
    {
        WearableType type = wearable.WearableType;
        return type == WearableType.Shape ||
               type == WearableType.Skin ||
               type == WearableType.Eyes ||
               type == WearableType.Hair;
    }

    public void ToggleExpand()
    {
        isExpanded = !isExpanded;
        
        if (expandButton != null)
        {
            expandButton.transform.localRotation = isExpanded ? Quaternion.Euler(0, 0, 90) : Quaternion.identity;
        }
        
        UpdateFolderIcon();

        if (inventoryWindow != null && itemData is InventoryFolder folder)
        {
            inventoryWindow.ToggleFolder(folder, this, depth);
        }
        
        _logger?.LogDebug($"Folder {itemData?.Name} {(isExpanded ? "expanded" : "collapsed")}");
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
    public UUID GetItemUUID() => itemData != null ? itemData.UUID : UUID.Zero;
    public InventoryBase GetItemData() => itemData;
    public bool IsSelected() => isSelected;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (inventoryWindow != null)
            {
                inventoryWindow.ShowContextMenu(itemData, eventData.position);
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (inventoryWindow != null)
            {
                inventoryWindow.SelectItem(this);
            }
            
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
            if (item is InventoryWearable || item is InventoryAttachment || item is InventoryObject)
            {
                if (ClientManager.client != null && ClientManager.client.Appearance != null)
                {
                    ClientManager.client.Appearance.AddToOutfit(new List<InventoryItem> { item }, true);
                }
                _logger?.LogInformation($"Double-clicked to wear/attach: {item.Name}");
            }
        }
        catch (System.Exception ex)
        {
            _logger?.LogError(ex, $"Error handling double-click for item: {item.Name}");
        }
    }

    private void OnDestroy()
    {
        if (expandButton != null)
        {
            expandButton.onClick.RemoveAllListeners();
        }
    }
}
