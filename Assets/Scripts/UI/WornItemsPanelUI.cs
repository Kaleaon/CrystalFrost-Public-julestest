using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OpenMetaverse;
using Microsoft.Extensions.Logging;

namespace CrystalFrost.UI
{
    /// <summary>
    /// UI panel that displays currently worn clothing, body attachments, and HUDs,
    /// providing visibility and detachment controls with real-time event synchronization.
    /// </summary>
    public class WornItemsPanelUI : MonoBehaviour
    {
        [Header("UI References")]
        public Transform contentRoot;
        public GameObject wornItemPrefab;
        public Button detachAllButton;
        public TMP_Text statusText;
        public TMP_InputField filterInputField;

        private ILogger<WornItemsPanelUI> _logger;
        private GridClient _client;
        private readonly List<WornItemEntry> _activeEntries = new();
        private bool _isSubscribed = false;

        public class WornItemEntry
        {
            public InventoryItem Item;
            public InventoryItem LinkItem;
            public string Category;
            public string DetailText;
            public GameObject UIObject;
        }

        private void Awake()
        {
            _logger = Services.GetService<ILogger<WornItemsPanelUI>>();
            
            if (detachAllButton != null)
            {
                detachAllButton.onClick.AddListener(DetachAllItems);
            }

            if (filterInputField != null)
            {
                filterInputField.onValueChanged.AddListener((_) => RefreshWornItems());
            }
        }

        private void Start()
        {
            InitializeClientAndEvents();
            RefreshWornItems();
        }

        private void OnEnable()
        {
            InitializeClientAndEvents();
            RefreshWornItems();
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
                if (_client != null && _client.Network != null)
                {
                    _client.Inventory.FolderUpdated += OnInventoryFolderUpdated;
                    _client.Inventory.ItemReceived += OnInventoryItemReceived;
                    _client.Appearance.AppearanceSet += OnAppearanceSet;
                    _client.Objects.KillObject += OnObjectKilled;
                    _isSubscribed = true;
                    _logger?.LogInformation("WornItemsPanelUI subscribed to LibreMetaverse appearance and folder events.");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to initialize client events for WornItemsPanelUI.");
            }
        }

        private void UnsubscribeEvents()
        {
            if (!_isSubscribed || _client == null) return;

            try
            {
                _client.Inventory.FolderUpdated -= OnInventoryFolderUpdated;
                _client.Inventory.ItemReceived -= OnInventoryItemReceived;
                _client.Appearance.AppearanceSet -= OnAppearanceSet;
                _client.Objects.KillObject -= OnObjectKilled;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error unsubscribing WornItemsPanelUI events.");
            }
            finally
            {
                _isSubscribed = false;
            }
        }

        #region Event Handlers (Real-time Sync without UI Locking)
        private void OnInventoryFolderUpdated(object sender, FolderUpdatedEventArgs e)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                RefreshWornItems();
            });
        }

        private void OnInventoryItemReceived(object sender, ItemReceivedEventArgs e)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                RefreshWornItems();
            });
        }

        private void OnAppearanceSet(object sender, AppearanceSetEventArgs e)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                RefreshWornItems();
            });
        }

        private void OnObjectKilled(object sender, KillObjectEventArgs e)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                RefreshWornItems();
            });
        }
        #endregion

        /// <summary>
        /// Scans COF (Current Outfit Folder) and active attachments, rendering worn items UI.
        /// </summary>
        public void RefreshWornItems()
        {
            if (contentRoot == null) return;

            // Clear old entries
            foreach (Transform child in contentRoot)
            {
                Destroy(child.gameObject);
            }
            _activeEntries.Clear();

            List<WornItemEntry> itemsToDisplay = GetCurrentlyWornItems();
            
            string filter = filterInputField != null ? filterInputField.text.Trim().ToLower() : string.Empty;
            if (!string.IsNullOrEmpty(filter))
            {
                itemsToDisplay = itemsToDisplay.Where(e => 
                    e.Item.Name.ToLower().Contains(filter) || 
                    e.Category.ToLower().Contains(filter) ||
                    e.DetailText.ToLower().Contains(filter)
                ).ToList();
            }

            foreach (var entry in itemsToDisplay)
            {
                GameObject rowGO = CreateWornItemRow(entry);
                entry.UIObject = rowGO;
                _activeEntries.Add(entry);
            }

            if (statusText != null)
            {
                statusText.text = $"Worn Items: {_activeEntries.Count}";
            }

            if (detachAllButton != null)
            {
                detachAllButton.interactable = _activeEntries.Any(e => !IsBodyPart(e.Item));
            }
        }

        public List<WornItemEntry> GetCurrentlyWornItems()
        {
            List<WornItemEntry> list = new();

            if (ClientManager.currentOutfitFolder == null && (_client == null || _client.Inventory == null))
            {
                return list;
            }

            try
            {
                if (ClientManager.currentOutfitFolder != null)
                {
                    var links = ClientManager.currentOutfitFolder.ContentLinks();
                    foreach (var link in links)
                    {
                        var realItem = ClientManager.currentOutfitFolder.RealInventoryItem(link);
                        if (realItem == null) continue;

                        string category = "Attachment";
                        string detail = "Attached";

                        if (realItem is InventoryWearable wearable)
                        {
                            category = IsBodyPart(realItem) ? "Body Part" : "Clothing";
                            detail = wearable.WearableType.ToString();
                        }
                        else if (realItem is InventoryAttachment attachment)
                        {
                            category = "Attachment";
                            detail = attachment.AttachmentPoint.ToString();
                        }
                        else if (realItem is InventoryObject obj)
                        {
                            category = "HUD / Object";
                            detail = "Object Attachment";
                        }

                        list.Add(new WornItemEntry
                        {
                            Item = realItem,
                            LinkItem = link,
                            Category = category,
                            DetailText = detail
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error gathering worn items from Current Outfit Folder.");
            }

            return list;
        }

        private GameObject CreateWornItemRow(WornItemEntry entry)
        {
            if (wornItemPrefab != null)
            {
                GameObject row = Instantiate(wornItemPrefab, contentRoot);
                PopulateRowComponents(row, entry);
                return row;
            }

            // Fallback runtime UI creation if prefab is omitted
            GameObject container = new GameObject($"WornItem_{entry.Item.Name}");
            container.transform.SetParent(contentRoot, false);

            HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = 8;
            layout.padding = new RectOffset(6, 6, 4, 4);

            Image bg = container.AddComponent<Image>();
            bg.color = new Color(0.18f, 0.18f, 0.22f, 0.85f);

            LayoutElement containerLE = container.AddComponent<LayoutElement>();
            containerLE.minHeight = 32;
            containerLE.flexibleWidth = 1;

            // Label text (Name & detail)
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(container.transform, false);
            TMP_Text nameText = textGO.AddComponent<TMP_Text>();
            nameText.text = $"<b>{entry.Item.Name}</b> <color=#A9A9A9>({entry.DetailText})</color>";
            nameText.fontSize = 13;
            nameText.color = Color.white;
            nameText.alignment = TextAlignmentOptions.MidlineLeft;
            
            LayoutElement textLE = textGO.AddComponent<LayoutElement>();
            textLE.flexibleWidth = 1;

            // Action button (Detach / Take Off)
            GameObject buttonGO = new GameObject("DetachButton");
            buttonGO.transform.SetParent(container.transform, false);
            Image btnImg = buttonGO.AddComponent<Image>();
            btnImg.color = new Color(0.7f, 0.2f, 0.2f, 0.9f);
            Button btn = buttonGO.AddComponent<Button>();

            GameObject btnTextGO = new GameObject("BtnText");
            btnTextGO.transform.SetParent(buttonGO.transform, false);
            TMP_Text btnText = btnTextGO.AddComponent<TMP_Text>();
            btnText.text = IsBodyPart(entry.Item) ? "Cannot Detach" : "Detach";
            btnText.fontSize = 12;
            btnText.color = Color.white;
            btnText.alignment = TextAlignmentOptions.Center;

            RectTransform btnTextRect = btnText.GetComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.sizeDelta = Vector2.zero;

            LayoutElement btnLE = buttonGO.AddComponent<LayoutElement>();
            btnLE.minWidth = 80;
            btnLE.minHeight = 24;

            if (IsBodyPart(entry.Item))
            {
                btn.interactable = false;
                btnImg.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            }
            else
            {
                btn.onClick.AddListener(() => DetachItem(entry));
            }

            return container;
        }

        private void PopulateRowComponents(GameObject row, WornItemEntry entry)
        {
            TMP_Text text = row.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                text.text = $"{entry.Item.Name} ({entry.DetailText})";
            }

            Button btn = row.GetComponentInChildren<Button>();
            if (btn != null)
            {
                if (IsBodyPart(entry.Item))
                {
                    btn.interactable = false;
                }
                else
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => DetachItem(entry));
                }
            }
        }

        public void DetachItem(WornItemEntry entry)
        {
            if (entry == null || entry.Item == null) return;

            try
            {
                _logger?.LogInformation($"Detaching worn item: {entry.Item.Name}");

                if (ClientManager.currentOutfitFolder != null)
                {
                    ClientManager.currentOutfitFolder.RemoveFromOutfit(entry.Item);
                }
                else if (_client != null && _client.Appearance != null)
                {
                    _client.Appearance.RemoveFromOutfit(new List<InventoryItem> { entry.Item });
                }

                RefreshWornItems();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"Failed to detach item {entry.Item.Name}");
            }
        }

        public void DetachAllItems()
        {
            try
            {
                var removableItems = _activeEntries
                    .Where(e => !IsBodyPart(e.Item))
                    .Select(e => e.Item)
                    .ToList();

                if (removableItems.Count == 0) return;

                _logger?.LogInformation($"Detaching all non-body-part worn items ({removableItems.Count} items)");

                if (ClientManager.currentOutfitFolder != null)
                {
                    ClientManager.currentOutfitFolder.RemoveFromOutfit(removableItems);
                }
                else if (_client != null && _client.Appearance != null)
                {
                    _client.Appearance.RemoveFromOutfit(removableItems);
                }

                RefreshWornItems();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in DetachAllItems");
            }
        }

        public bool IsBodyPart(InventoryItem item)
        {
            if (item is InventoryWearable wearable)
            {
                WearableType type = wearable.WearableType;
                return type == WearableType.Shape ||
                       type == WearableType.Skin ||
                       type == WearableType.Eyes ||
                       type == WearableType.Hair;
            }
            return false;
        }
    }
}
