using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OpenMetaverse;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace CrystalFrost.UI
{
    /// <summary>
    /// Handles creation and management of inventory UI windows
    /// </summary>
    public class InventoryUIController : MonoBehaviour
    {
        private ILogger<InventoryUIController> _logger;
        private IUIStateTracker _uiStateTracker;

        private void Awake()
        {
            _logger = Services.GetService<ILogger<InventoryUIController>>();
            _uiStateTracker = FindObjectOfType<UIStateTracker>();
        }

        public void CreateInventoryWindow()
        {
            _logger.LogInformation("Creating inventory window");
            
            // Track the creation process start
            _uiStateTracker?.TrackInteraction(gameObject, "InventoryUIController", "CreateWindowStarted", null);

            // 1. Create Canvas
            GameObject canvasGO = new GameObject("InventoryCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            // Track canvas creation
            _uiStateTracker?.TrackComponentCreated(canvasGO, "InventoryCanvas");

            // 2. Create Window Panel
            GameObject windowPanel = CreateInventoryPanel(canvasGO.transform);

            // 3. Create Tree View
            GameObject treeView = CreateTreeView(windowPanel.transform);

            // 4. Create Context Menu
            ContextMenuUI contextMenu = CreateContextMenu(windowPanel.transform);

            // 5. Setup Inventory Window Component
            InventoryWindowUI inventoryWindow = windowPanel.AddComponent<InventoryWindowUI>();
            inventoryWindow.treeNodePrefab = CreateTreeNodePrefab();
            inventoryWindow.contentRoot = treeView.transform.Find("Content");
            inventoryWindow.contextMenu = contextMenu;

            // 6. Make window draggable
            windowPanel.AddComponent<DraggableWindow>();

            // Track successful window creation
            _uiStateTracker?.TrackInteraction(gameObject, "InventoryUIController", "CreateWindowCompleted", new { windowId = windowPanel.GetInstanceID() });

            _logger.LogInformation("Inventory window created successfully");
        }

        private GameObject CreateInventoryPanel(Transform parent)
        {
            GameObject panel = CreateUIPrefab("InventoryPanel", parent);
            panel.transform.SetParent(parent, false);

            // Set up panel background
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

            // Set panel size and position
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(400, 600);
            panelRect.anchoredPosition = Vector2.zero;

            // Add layout
            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 5;

            // Add header
            CreateInventoryHeader(panel.transform);

            // Track panel creation
            _uiStateTracker?.TrackComponentCreated(panel, "InventoryPanel");

            return panel;
        }

        private void CreateInventoryHeader(Transform parent)
        {
            GameObject header = CreateUIPrefab("Header", parent);
            header.AddComponent<LayoutElement>().minHeight = 30;

            TMP_Text headerText = header.AddComponent<TMP_Text>();
            headerText.text = "Inventory";
            headerText.fontSize = 18;
            headerText.color = Color.white;
            headerText.alignment = TextAlignmentOptions.Center;
        }

        private GameObject CreateTreeView(Transform parent)
        {
            GameObject treeView = CreateUIPrefab("TreeView", parent);
            treeView.AddComponent<LayoutElement>().flexibleHeight = 1;

            // Create scroll view
            ScrollRect scrollRect = treeView.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            // Create viewport
            GameObject viewport = CreateUIPrefab("Viewport", treeView.transform);
            viewport.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.anchoredPosition = Vector2.zero;

            // Create content
            GameObject content = CreateUIPrefab("Content", viewport.transform);
            VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = false;
            contentLayout.childForceExpandHeight = false;

            ContentSizeFitter contentSizeFitter = content.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Setup scroll rect
            scrollRect.viewport = viewportRect;
            scrollRect.content = content.GetComponent<RectTransform>();

            return treeView;
        }

        private GameObject CreateTreeNodePrefab()
        {
            GameObject node = CreateUIPrefab("TreeNode", null);
            HorizontalLayoutGroup layout = node.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = 5;

            // Create indent spacer
            GameObject indent = CreateUIPrefab("Indent", node.transform);
            LayoutElement indentLayout = indent.AddComponent<LayoutElement>();
            indentLayout.minWidth = 0;
            indentLayout.flexibleWidth = 0;

            // Create expand/collapse button
            GameObject expandButton = CreateUIPrefab("ExpandButton", node.transform);
            expandButton.AddComponent<Image>().color = Color.gray;
            expandButton.AddComponent<Button>();
            expandButton.GetComponent<RectTransform>().sizeDelta = new Vector2(16, 16);

            // Create icon
            GameObject icon = CreateUIPrefab("Icon", node.transform);
            icon.AddComponent<Image>().color = Color.cyan;
            icon.GetComponent<RectTransform>().sizeDelta = new Vector2(16, 16);

            // Create text
            GameObject text = CreateUIPrefab("Text", node.transform);
            TMP_Text tmpText = text.AddComponent<TMP_Text>();
            tmpText.text = "Item Name";
            tmpText.fontSize = 14;
            tmpText.color = Color.white;
            text.AddComponent<LayoutElement>().flexibleWidth = 1;

            // Add TreeNodeUI component
            TreeNodeUI treeNodeUI = node.AddComponent<TreeNodeUI>();
            treeNodeUI.indentElement = indent.GetComponent<LayoutElement>();
            treeNodeUI.expandButton = expandButton.GetComponent<Button>();
            treeNodeUI.itemIcon = icon.GetComponent<Image>();
            treeNodeUI.itemNameText = tmpText;

            return node;
        }

        private ContextMenuUI CreateContextMenu(Transform parent)
        {
            GameObject menuGO = CreateUIPrefab("ContextMenu", parent);
            menuGO.transform.SetParent(parent, false);
            Image img = menuGO.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            menuGO.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 200);

            GameObject buttonParent = CreateUIPrefab("ButtonParent", menuGO.transform);
            VerticalLayoutGroup layout = buttonParent.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;

            ContextMenuUI contextMenuUI = menuGO.AddComponent<ContextMenuUI>();
            contextMenuUI.buttonParent = buttonParent.transform;
            return contextMenuUI;
        }

        private GameObject CreateContextMenuButtonPrefab()
        {
            GameObject buttonGO = CreateUIPrefab("ContextMenuButtonPrefab", null);
            buttonGO.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 1f);
            buttonGO.AddComponent<Button>();
            buttonGO.AddComponent<LayoutElement>().minHeight = 22;

            GameObject textGO = CreateUIPrefab("Text", buttonGO.transform);
            TMP_Text text = textGO.AddComponent<TMP_Text>();
            text.text = "Action";
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            return buttonGO;
        }

        private GameObject CreateUIPrefab(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            return go;
        }
    }
}