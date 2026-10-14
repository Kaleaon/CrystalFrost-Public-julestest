using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CrystalFrost.UI
{
    /// <summary>
    /// UI ScrollRect virtualization controller that maintains a fixed pool of container GameObjects,
    /// positions visible items based on scroll offset, supports variable item heights, and dynamically
    /// rebinds item data without creating or destroying GameObjects during scrolling.
    /// </summary>
    public class VirtualScrollViewController : MonoBehaviour
    {
        [Header("UI References")]
        public ScrollRect scrollRect;
        public RectTransform viewport;
        public RectTransform content;
        public GameObject itemPrefab;

        [Header("Layout Configuration")]
        public float defaultItemHeight = 40f;
        public int bufferItemCount = 3;

        private readonly List<GameObject> _containerPool = new();
        private readonly List<float> _itemHeights = new();
        private readonly List<float> _cumulativeHeights = new();

        private int _totalItemCount = 0;
        private Action<GameObject, int> _onBindItemCallback;
        private Func<int, float> _itemHeightProvider;

        private int _lastFirstVisibleIndex = -1;
        private int _lastLastVisibleIndex = -1;
        private float _lastScrollPositionY = -1f;

        public int ActiveContainerCount => _containerPool.Count;
        public int TotalItemCount => _totalItemCount;

        private void Awake()
        {
            if (scrollRect == null) scrollRect = GetComponent<ScrollRect>();
            if (scrollRect != null)
            {
                if (viewport == null) viewport = scrollRect.viewport;
                if (content == null) content = scrollRect.content;
            }
        }

        private void Start()
        {
            if (scrollRect != null)
            {
                scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
            }
        }

        private void OnDestroy()
        {
            if (scrollRect != null)
            {
                scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
            }
        }

        /// <summary>
        /// Sets up the virtual scroll view with total item count and item binding callback.
        /// </summary>
        public void SetData(int itemCount, Action<GameObject, int> onBindItem, Func<int, float> itemHeightProvider = null)
        {
            _totalItemCount = Mathf.Max(0, itemCount);
            _onBindItemCallback = onBindItem;
            _itemHeightProvider = itemHeightProvider;

            RebuildHeightCache();
            UpdatePoolSize();
            ResetScrollPositionAndForceUpdate();
        }

        /// <summary>
        /// Updates item count without resetting scroll position if count changed.
        /// </summary>
        public void UpdateItemCount(int itemCount)
        {
            _totalItemCount = Mathf.Max(0, itemCount);
            RebuildHeightCache();
            UpdatePoolSize();
            ForceUpdateLayout();
        }

        public void RebuildHeightCache()
        {
            _itemHeights.Clear();
            _cumulativeHeights.Clear();

            float totalCumulative = 0f;
            for (int i = 0; i < _totalItemCount; i++)
            {
                float height = (_itemHeightProvider != null) ? _itemHeightProvider(i) : defaultItemHeight;
                if (height <= 0f) height = defaultItemHeight;

                _itemHeights.Add(height);
                totalCumulative += height;
                _cumulativeHeights.Add(totalCumulative);
            }

            if (content != null)
            {
                content.sizeDelta = new Vector2(content.sizeDelta.x, totalCumulative);
            }
        }

        private void UpdatePoolSize()
        {
            if (viewport == null || itemPrefab == null) return;

            float viewportHeight = viewport.rect.height;
            if (viewportHeight <= 0) viewportHeight = 600f; // Default fallback

            int maxVisibleContainers = Mathf.CeilToInt(viewportHeight / Mathf.Max(10f, defaultItemHeight)) + (bufferItemCount * 2) + 2;

            // Grow pool up to maxVisibleContainers
            while (_containerPool.Count < maxVisibleContainers)
            {
                GameObject container = Instantiate(itemPrefab, content);
                container.name = $"VirtualContainer_{_containerPool.Count}";
                
                RectTransform rt = container.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(0f, 1f);
                }

                container.SetActive(false);
                _containerPool.Add(container);
            }
        }

        public void ResetScrollPositionAndForceUpdate()
        {
            if (content != null)
            {
                content.anchoredPosition = new Vector2(content.anchoredPosition.x, 0f);
            }
            _lastFirstVisibleIndex = -1;
            _lastLastVisibleIndex = -1;
            ForceUpdateLayout();
        }

        public void ForceUpdateLayout()
        {
            OnScrollValueChanged(Vector2.zero);
        }

        private void OnScrollValueChanged(Vector2 delta)
        {
            if (content == null || viewport == null || _totalItemCount == 0 || _containerPool.Count == 0)
            {
                // Deactivate all containers when empty
                foreach (var container in _containerPool)
                {
                    if (container != null && container.activeSelf)
                    {
                        container.SetActive(false);
                    }
                }
                return;
            }

            float scrollY = Mathf.Max(0f, content.anchoredPosition.y);
            float viewportHeight = viewport.rect.height;

            GetVisibleRange(scrollY, viewportHeight, out int firstIndex, out int lastIndex);

            // Rebind and reposition visible items
            int visibleRange = (lastIndex >= firstIndex) ? (lastIndex - firstIndex + 1) : 0;

            for (int poolIdx = 0; poolIdx < _containerPool.Count; poolIdx++)
            {
                GameObject container = _containerPool[poolIdx];
                if (container == null) continue;

                if (poolIdx < visibleRange)
                {
                    int itemIndex = firstIndex + poolIdx;
                    if (itemIndex >= 0 && itemIndex < _totalItemCount)
                    {
                        float itemTopY = GetItemTopY(itemIndex);
                        float itemHeight = GetItemHeight(itemIndex);

                        RectTransform rt = container.GetComponent<RectTransform>();
                        if (rt != null)
                        {
                            rt.anchoredPosition = new Vector2(0f, -itemTopY);
                            rt.sizeDelta = new Vector2(content.rect.width, itemHeight);
                        }

                        if (!container.activeSelf) container.SetActive(true);

                        _onBindItemCallback?.Invoke(container, itemIndex);
                    }
                    else
                    {
                        if (container.activeSelf) container.SetActive(false);
                    }
                }
                else
                {
                    if (container.activeSelf) container.SetActive(false);
                }
            }

            _lastFirstVisibleIndex = firstIndex;
            _lastLastVisibleIndex = lastIndex;
            _lastScrollPositionY = scrollY;
        }

        public void GetVisibleRange(float scrollY, float viewportHeight, out int firstIndex, out int lastIndex)
        {
            if (_totalItemCount == 0 || _cumulativeHeights.Count == 0)
            {
                firstIndex = 0;
                lastIndex = -1;
                return;
            }

            float topY = scrollY;
            float bottomY = scrollY + viewportHeight;

            // Find first item where cumulative height >= topY
            int rawFirst = BinarySearchCumulativeHeight(topY);
            int rawLast = BinarySearchCumulativeHeight(bottomY);

            firstIndex = Mathf.Max(0, rawFirst - bufferItemCount);
            lastIndex = Mathf.Min(_totalItemCount - 1, rawLast + bufferItemCount);
        }

        private int BinarySearchCumulativeHeight(float targetY)
        {
            int low = 0;
            int high = _cumulativeHeights.Count - 1;

            while (low <= high)
            {
                int mid = (low + high) / 2;
                if (_cumulativeHeights[mid] < targetY)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return Mathf.Clamp(low, 0, _cumulativeHeights.Count - 1);
        }

        public float GetItemTopY(int index)
        {
            if (index <= 0) return 0f;
            if (index - 1 < _cumulativeHeights.Count)
            {
                return _cumulativeHeights[index - 1];
            }
            return (index) * defaultItemHeight;
        }

        public float GetItemHeight(int index)
        {
            if (index >= 0 && index < _itemHeights.Count)
            {
                return _itemHeights[index];
            }
            return defaultItemHeight;
        }
    }
}
