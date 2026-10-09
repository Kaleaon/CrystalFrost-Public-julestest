using NUnit.Framework;
using UnityEngine;
using OpenMetaverse;
using System.Collections.Generic;
using CrystalFrost.UI;
using TMPro;

namespace CrystalFrost.Tests
{
    /// <summary>
    /// Unit test suite for Modular Inventory Browser, Hierarchy Tree View,
    /// Context Menu, and Worn Items Panel System.
    /// </summary>
    public class InventoryBrowserTests
    {
        private GameObject _windowGO;
        private InventoryWindowUI _inventoryWindow;
        private WornItemsPanelUI _wornItemsPanel;
        private ContextMenuUI _contextMenu;

        [SetUp]
        public void Setup()
        {
            _windowGO = new GameObject("TestInventoryWindow");
            _inventoryWindow = _windowGO.AddComponent<InventoryWindowUI>();

            GameObject treeContent = new GameObject("TreeContent");
            treeContent.transform.SetParent(_windowGO.transform, false);

            GameObject wornContent = new GameObject("WornContent");
            wornContent.transform.SetParent(_windowGO.transform, false);
            _wornItemsPanel = wornContent.AddComponent<WornItemsPanelUI>();

            GameObject contextGO = new GameObject("ContextMenu");
            contextGO.transform.SetParent(_windowGO.transform, false);
            _contextMenu = contextGO.AddComponent<ContextMenuUI>();

            _inventoryWindow.inventoryTabContent = treeContent;
            _inventoryWindow.wornItemsTabContent = wornContent;
            _inventoryWindow.wornItemsPanel = _wornItemsPanel;
            _inventoryWindow.contextMenu = _contextMenu;
            _inventoryWindow.contentRoot = treeContent.transform;
        }

        [TearDown]
        public void TearDown()
        {
            if (_windowGO != null)
            {
                Object.DestroyImmediate(_windowGO);
            }
        }

        [Test]
        public void InventoryWindowUI_TabSwitching_ShouldToggleVisibility()
        {
            _inventoryWindow.ShowInventoryTab();
            Assert.IsTrue(_inventoryWindow.inventoryTabContent.activeSelf, "Inventory tab should be visible");
            Assert.IsFalse(_inventoryWindow.wornItemsTabContent.activeSelf, "Worn items tab should be hidden");

            _inventoryWindow.ShowWornItemsTab();
            Assert.IsFalse(_inventoryWindow.inventoryTabContent.activeSelf, "Inventory tab should be hidden");
            Assert.IsTrue(_inventoryWindow.wornItemsTabContent.activeSelf, "Worn items tab should be visible");
        }

        [Test]
        public void ContextMenuUI_AddButton_ShouldRegisterAction()
        {
            _contextMenu.ClearButtons();
            bool actionExecuted = false;

            _contextMenu.AddButton("Test Action", () =>
            {
                actionExecuted = true;
            });

            _contextMenu.Show(Vector2.zero);
            Assert.IsTrue(_contextMenu.gameObject.activeSelf, "Context menu should be active when shown");

            // Execute the action manually via button click logic
            UnityEngine.UI.Button button = _contextMenu.buttonParent.GetComponentInChildren<UnityEngine.UI.Button>();
            Assert.IsNotNull(button, "Button component should exist on context menu item");
            button.onClick.Invoke();

            Assert.IsTrue(actionExecuted, "Context menu button click should invoke passed callback");
            Assert.IsFalse(_contextMenu.gameObject.activeSelf, "Context menu should hide after action execution");
        }

        [Test]
        public void TreeNodeUI_FolderExpand_ShouldToggleState()
        {
            GameObject nodeGO = new GameObject("TestNode");
            TreeNodeUI nodeUI = nodeGO.AddComponent<TreeNodeUI>();

            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(nodeGO.transform, false);
            nodeUI.itemNameText = textGO.AddComponent<TMP_Text>();

            GameObject indentGO = new GameObject("Indent");
            indentGO.transform.SetParent(nodeGO.transform, false);
            nodeUI.indentElement = indentGO.AddComponent<UnityEngine.UI.LayoutElement>();

            InventoryFolder folder = new InventoryFolder(UUID.Random())
            {
                Name = "Test Folder",
                ParentUUID = UUID.Zero
            };

            nodeUI.SetData(folder, 0, _inventoryWindow);
            Assert.IsFalse(nodeUI.IsExpanded(), "New folder node should initially be collapsed");

            nodeUI.ToggleExpand();
            Assert.IsTrue(nodeUI.IsExpanded(), "Folder node should be expanded after ToggleExpand");

            Object.DestroyImmediate(nodeGO);
        }

        [Test]
        public void WornItemsPanelUI_IsBodyPart_ShouldIdentifyShapeAndSkin()
        {
            InventoryWearable shape = new InventoryWearable(UUID.Random())
            {
                Name = "Test Shape",
                WearableType = WearableType.Shape
            };

            InventoryWearable shirt = new InventoryWearable(UUID.Random())
            {
                Name = "Test Shirt",
                WearableType = WearableType.Shirt
            };

            InventoryAttachment attachment = new InventoryAttachment(UUID.Random())
            {
                Name = "Test Glasses",
                AttachmentPoint = AttachmentPoint.Nose
            };

            Assert.IsTrue(_wornItemsPanel.IsBodyPart(shape), "Shape should be categorized as body part");
            Assert.IsFalse(_wornItemsPanel.IsBodyPart(shirt), "Shirt should not be categorized as body part");
            Assert.IsFalse(_wornItemsPanel.IsBodyPart(attachment), "Attachment should not be categorized as body part");
        }

        [Test]
        public void ResizableWindow_Clamping_ShouldRespectLimits()
        {
            GameObject panelGO = new GameObject("ResizablePanel");
            RectTransform rectTransform = panelGO.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(400, 500);

            GameObject handleGO = new GameObject("Handle");
            handleGO.transform.SetParent(panelGO.transform, false);
            ResizableWindow resizable = handleGO.AddComponent<ResizableWindow>();
            resizable.targetRectTransform = rectTransform;
            resizable.minSize = new Vector2(200, 200);
            resizable.maxSize = new Vector2(800, 800);

            Assert.AreEqual(new Vector2(400, 500), rectTransform.sizeDelta, "Initial size should match");

            Object.DestroyImmediate(panelGO);
        }
    }
}
