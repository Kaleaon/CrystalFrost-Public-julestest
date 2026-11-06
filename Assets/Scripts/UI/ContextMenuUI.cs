using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using TMPro;

namespace CrystalFrost.UI
{
    /// <summary>
    /// Canvas-clamped context menu for item and folder actions in the inventory browser.
    /// </summary>
    public class ContextMenuUI : MonoBehaviour
    {
        public GameObject buttonPrefab;
        public Transform buttonParent;

        private readonly List<GameObject> _currentButtons = new();
        private Canvas _parentCanvas;
        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _parentCanvas = GetComponentInParent<Canvas>();

            if (buttonParent == null)
            {
                buttonParent = transform;
            }

            Hide();
        }

        private void Update()
        {
            if (gameObject.activeSelf)
            {
                // Hide on click outside or escape
                if (Input.GetMouseButtonDown(0) && !RectTransformUtility.RectangleContainsScreenPoint(_rectTransform, Input.mousePosition))
                {
                    Hide();
                }
                else if (Input.GetKeyDown(KeyCode.Escape))
                {
                    Hide();
                }
            }
        }

        public void Show(Vector2 position)
        {
            gameObject.SetActive(true);
            transform.position = position;
            ClampToCanvas();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void ClearButtons()
        {
            foreach (var button in _currentButtons)
            {
                if (button != null)
                {
                    Destroy(button);
                }
            }
            _currentButtons.Clear();
        }

        public void AddButton(string label, Action onClickAction)
        {
            GameObject buttonGO = CreateButtonObject(label);
            Button buttonComp = buttonGO.GetComponent<Button>();
            
            buttonComp.onClick.RemoveAllListeners();
            buttonComp.onClick.AddListener(() =>
            {
                onClickAction?.Invoke();
                Hide();
            });

            _currentButtons.Add(buttonGO);
        }

        private GameObject CreateButtonObject(string label)
        {
            if (buttonPrefab != null)
            {
                GameObject instantiated = Instantiate(buttonPrefab, buttonParent);
                TMP_Text tmpText = instantiated.GetComponentInChildren<TMP_Text>();
                if (tmpText != null) tmpText.text = label;
                return instantiated;
            }

            // Fallback UI button creation
            GameObject btnGO = new GameObject($"ContextBtn_{label}");
            btnGO.transform.SetParent(buttonParent, false);

            Image img = btnGO.AddComponent<Image>();
            img.color = new Color(0.25f, 0.25f, 0.3f, 0.95f);

            Button btn = btnGO.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(0.4f, 0.4f, 0.5f, 1f);
            cb.pressedColor = new Color(0.2f, 0.4f, 0.8f, 1f);
            btn.colors = cb;

            LayoutElement le = btnGO.AddComponent<LayoutElement>();
            le.minHeight = 24;
            le.flexibleWidth = 1;

            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(btnGO.transform, false);
            TMP_Text text = textGO.AddComponent<TMP_Text>();
            text.text = label;
            text.fontSize = 13;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Left;

            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8, 0);
            textRect.offsetMax = new Vector2(-8, 0);

            return btnGO;
        }

        private void ClampToCanvas()
        {
            if (_parentCanvas == null || _rectTransform == null) return;

            RectTransform canvasRect = _parentCanvas.GetComponent<RectTransform>();
            Vector3[] canvasCorners = new Vector3[4];
            canvasRect.GetWorldCorners(canvasCorners);

            Vector3[] menuCorners = new Vector3[4];
            _rectTransform.GetWorldCorners(menuCorners);

            Vector3 pos = _rectTransform.position;

            float width = menuCorners[2].x - menuCorners[0].x;
            float height = menuCorners[1].y - menuCorners[0].y;

            if (pos.x + width > canvasCorners[2].x)
            {
                pos.x = canvasCorners[2].x - width;
            }
            if (pos.x < canvasCorners[0].x)
            {
                pos.x = canvasCorners[0].x;
            }

            if (pos.y - height < canvasCorners[0].y)
            {
                pos.y = canvasCorners[0].y + height;
            }
            if (pos.y > canvasCorners[1].y)
            {
                pos.y = canvasCorners[1].y;
            }

            _rectTransform.position = pos;
        }
    }
}
