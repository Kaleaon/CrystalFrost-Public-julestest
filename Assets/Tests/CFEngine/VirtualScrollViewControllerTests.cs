using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using CrystalFrost.UI;

namespace CrystalFrost.Tests
{
    [TestFixture]
    public class VirtualScrollViewControllerTests
    {
        private GameObject _scrollGO;
        private VirtualScrollViewController _controller;
        private RectTransform _viewportRect;
        private RectTransform _contentRect;

        [SetUp]
        public void Setup()
        {
            _scrollGO = new GameObject("TestVirtualScrollView");
            ScrollRect scrollRect = _scrollGO.AddComponent<ScrollRect>();

            GameObject viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(_scrollGO.transform, false);
            _viewportRect = viewportGO.AddComponent<RectTransform>();
            _viewportRect.sizeDelta = new Vector2(400f, 600f);

            GameObject contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);
            _contentRect = contentGO.AddComponent<RectTransform>();
            _contentRect.sizeDelta = new Vector2(400f, 0f);

            scrollRect.viewport = _viewportRect;
            scrollRect.content = _contentRect;

            GameObject itemPrefab = new GameObject("ItemPrefab");
            itemPrefab.AddComponent<RectTransform>();

            _controller = _scrollGO.AddComponent<VirtualScrollViewController>();
            _controller.scrollRect = scrollRect;
            _controller.viewport = _viewportRect;
            _controller.content = _contentRect;
            _controller.itemPrefab = itemPrefab;
            _controller.defaultItemHeight = 40f;
            _controller.bufferItemCount = 3;
        }

        [TearDown]
        public void TearDown()
        {
            if (_scrollGO != null)
            {
                UnityEngine.Object.DestroyImmediate(_scrollGO);
            }
        }

        [Test]
        public void ContainerPool_MaintainsConstantObjectCount_DuringRapidScrolling()
        {
            const int totalItems = 1000;
            _controller.SetData(totalItems, (container, index) => { });

            int initialPoolSize = _controller.ActiveContainerCount;
            Assert.Greater(initialPoolSize, 0, "Container pool should instantiate visible items");

            // Simulate rapid scrolling through 1,000 items
            float[] scrollOffsets = new float[] { 0f, 500f, 2500f, 10000f, 25000f, 38000f, 100f };

            foreach (float offset in scrollOffsets)
            {
                _contentRect.anchoredPosition = new Vector2(0f, offset);
                _controller.ForceUpdateLayout();

                Assert.AreEqual(initialPoolSize, _controller.ActiveContainerCount,
                    $"Container count must remain constant during scrolling at offset {offset}");
            }
        }

        [Test]
        public void VariableItemHeights_CalculatesCumulativeOffsetsAccurately()
        {
            // Height provider: alternating 30px and 60px
            Func<int, float> heightProvider = (idx) => (idx % 2 == 0) ? 30f : 60f;

            _controller.SetData(10, (go, idx) => { }, heightProvider);

            Assert.AreEqual(30f, _controller.GetItemHeight(0));
            Assert.AreEqual(60f, _controller.GetItemHeight(1));
            Assert.AreEqual(30f, _controller.GetItemHeight(2));

            Assert.AreEqual(0f, _controller.GetItemTopY(0));
            Assert.AreEqual(30f, _controller.GetItemTopY(1));
            Assert.AreEqual(90f, _controller.GetItemTopY(2));
            Assert.AreEqual(120f, _controller.GetItemTopY(3));
        }

        [Test]
        public void RebindsItemData_WhenScrolledIntoView()
        {
            HashSet<int> boundIndices = new HashSet<int>();

            _controller.SetData(500, (container, index) =>
            {
                boundIndices.Add(index);
            });

            Assert.IsTrue(boundIndices.Contains(0), "Index 0 should be bound at scroll offset 0");

            boundIndices.Clear();
            _contentRect.anchoredPosition = new Vector2(0f, 4000f); // Scroll down
            _controller.ForceUpdateLayout();

            Assert.IsFalse(boundIndices.Contains(0), "Index 0 should no longer be bound when scrolled far past");
            Assert.IsTrue(boundIndices.Contains(100), "Index 100 should be bound when scrolled to 4000px offset");
        }
    }
}
