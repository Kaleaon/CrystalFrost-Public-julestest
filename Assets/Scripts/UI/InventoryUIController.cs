using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OpenMetaverse;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace CrystalFrost.UI
{
    /// <summary>
    /// Handles creation and management of modular inventory UI windows,
    /// including tabbed views, tree view, worn items panel, draggable and resizable handles.
    /// </summary>
    public class InventoryUIController : MonoBehaviour
    {
        private ILogger<InventoryUIController> _logger;

        private void Awake()
        {
            _logger = Services.GetService<ILogger<InventoryUIController>>();
        }

        public void CreateInventoryWindow()
        {
            _logger?.LogInformation("Creating modular inventory window");

            // 1. Create Canvas
            GameObject canvasGO = new GameObject("InventoryCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            // 2. Create Window Panel
            GameObject windowPanel = CreateInventoryPanel(canvasGO.transform);

            // 3. Create Tab Bar
            (Button invTabBtn, Button wornTabBtn) = CreateTabBar(windowPanel.transform);

            // 4. Create Content Views
            GameObject invTabContent = CreateTreeView(windowPanel.transform);
            GameObject wornTabContent = CreateWornItemsView(windowPanel.transform);

            // 5. Create Context Menu
            ContextMenuUI contextMenu = CreateContextMenu(windowPanel.transform);

            // 6. Setup Inventory Window Component
            InventoryWindowUI inventoryWindow = windowPanel.AddComponent<InventoryWindowUI>();
            inventoryWindow.treeNodePrefab = CreateTreeNodePrefab();
            inventoryWindow.contentRoot = invTabContent.transform.Find("Viewport/Content");
            inventoryWindow.contextMenu = contextMenu;
            inventoryWindow.inventoryTabContent = invTabContent;
            inventoryWindow.wornItemsTabContent = wornTabContent;
            inventoryWindow.inventoryTabButton = invTabBtn;
            inventoryWindow.wornItemsTabButton = wornTabBtn;
            inventoryWindow.wornItemsPanel = wornTabContent.GetComponent<WornItemsPanelUI>();

            // 7. Make window draggable and resizable
            windowPanel.AddComponent<DraggableWindow>();
            
            GameObject resizeHandle = CreateResizeHandle(windowPanel.transform);
            ResizableWindow resizable = resizeHandle.AddComponent<ResizableWindow>();
            resizable.targetRectTransform = windowPanel.GetComponent<RectTransform>();

            _logger?.LogInformation("Modular inventory window created successfully");
        }

        private GameObject CreateInventoryPanel(Transform parent)
        {
            GameObject panel = CreateUIPrefab("InventoryPanel", parent);

            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.15f, 0.15f, 0.18f, 0.95f);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(420, 620);
            panelRect.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 6;

            CreateInventoryHeader(panel.transform);

            return panel;
        }

        private void CreateInventoryHeader(Transform parent)
        {
            GameObject header = CreateUIPrefab("Header", parent);
            header.AddComponent<LayoutElement>().minHeight = 28;

            TMP_Text headerText = header.AddComponent<TMP_Text>();
            headerText.text = "Inventory & Worn Items";
            headerText.fontSize = 16;
            headerText.color = Color.white;
            headerText.alignment = TextAlignmentOptions.Center;
        }

        private (Button invBtn, Button wornBtn) CreateTabBar(Transform parent)
        {
            GameObject tabBar = CreateUIPrefab("TabBar", parent);
            tabBar.AddComponent<LayoutElement>().minHeight = 30;

            HorizontalLayoutGroup layout = tabBar.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.spacing = 4;

            Button invBtn = CreateTabButton(tabBar.transform, "Inventory Tree");
            Button wornBtn = CreateTabButton(tabBar.transform, "Worn Items");

            return (invBtn, wornBtn);
        }

        private Button CreateTabButton(Transform parent, string label)
        {
            GameObject btnGO = CreateUIPrefab($"Tab_{label}", parent);
            Image img = btnGO.AddComponent<Image>();
            img.color = new Color(0.25f, 0.25f, 0.32f, 0.9f);

            Button btn = btnGO.AddComponent<Button>();

            GameObject textGO = CreateUIPrefab("Text", btnGO.transform);
            TMP_Text txt = textGO.AddComponent<TMP_Text>();
            txt.text = label;
            txt.fontSize = 13;
            txt.color = Color.white;
            txt.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        private GameObject CreateTreeView(Transform parent)
        {
            GameObject treeView = CreateUIPrefab("TreeView", parent);
            treeView.AddComponent<LayoutElement>().flexibleHeight = 1;

            ScrollRect scrollRect = treeView.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            GameObject viewport = CreateUIPrefab("Viewport", treeView.transform);
            viewport.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.12f, 0.85f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;

            GameObject content = CreateUIPrefab("Content", viewport.transform);
            VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = false;
            contentLayout.childForceExpandHeight = false;

            ContentSizeFitter contentSizeFitter = content.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRect;
            scrollRect.content = content.GetComponent<RectTransform>();

            return treeView;
        }

        private GameObject CreateWornItemsView(Transform parent)
        {
            GameObject panel = CreateUIPrefab("WornItemsPanel", parent);
            panel.AddComponent<LayoutElement>().flexibleHeight = 1;

            ScrollRect scrollRect = panel.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            GameObject viewport = CreateUIPrefab("Viewport", panel.transform);
            viewport.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.12f, 0.85f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;

            GameObject content = CreateUIPrefab("Content", viewport.transform);
            VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = false;
            contentLayout.childForceExpandHeight = false;
            contentLayout.spacing = 4;

            ContentSizeFitter contentSizeFitter = content.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRect;
            scrollRect.content = content.GetComponent<RectTransform>();

            WornItemsPanelUI wornPanel = panel.AddComponent<WornItemsPanelUI>();
            wornPanel.contentRoot = content.transform;

            return panel;
        }

        private GameObject CreateTreeNodePrefab()
        {
            GameObject node = CreateUIPrefab("TreeNode", null);
            HorizontalLayoutGroup layout = node.AddComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = 4;

            GameObject indent = CreateUIPrefab("Indent", node.transform);
            LayoutElement indentLayout = indent.AddComponent<LayoutElement>();
            indentLayout.minWidth = 0;

            GameObject expandButton = CreateUIPrefab("ExpandButton", node.transform);
            expandButton.AddComponent<Image>().color = new Color(0.4f, 0.4f, 0.4f, 0.8f);
            expandButton.AddComponent<Button>();
            expandButton.GetComponent<RectTransform>().sizeDelta = new Vector2(16, 16);

            GameObject icon = CreateUIPrefab("Icon", node.transform);
            icon.AddComponent<Image>().color = Color.white;
            icon.GetComponent<RectTransform>().sizeDelta = new Vector2(16, 16);

            GameObject text = CreateUIPrefab("Text", node.transform);
            TMP_Text tmpText = text.AddComponent<TMP_Text>();
            tmpText.text = "Item Name";
            tmpText.fontSize = 13;
            tmpText.color = Color.white;
            text.AddComponent<LayoutElement>().flexibleWidth = 1;

            TreeNodeUI treeNodeUI = node.AddComponent<TreeNodeUI>();
            treeNodeUI.indentElement = indentLayout;
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
            img.color = new Color(0.12f, 0.12f, 0.15f, 0.95f);
            menuGO.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 200);

            GameObject buttonParent = CreateUIPrefab("ButtonParent", menuGO.transform);
            VerticalLayoutGroup layout = buttonParent.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 2;

            ContextMenuUI contextMenuUI = menuGO.AddComponent<ContextMenuUI>();
            contextMenuUI.buttonParent = buttonParent.transform;
            return contextMenuUI;
        }

        private GameObject CreateResizeHandle(Transform parent)
        {
            GameObject handle = CreateUIPrefab("ResizeHandle", parent);
            Image img = handle.AddComponent<Image>();
            img.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);

            RectTransform rect = handle.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(1, 0);
            rect.sizeDelta = new Vector2(16, 16);

            return handle;
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
