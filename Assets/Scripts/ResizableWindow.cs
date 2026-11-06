using UnityEngine;
using UnityEngine.EventSystems;

namespace CrystalFrost.UI
{
    /// <summary>
    /// Allows resizing a RectTransform window panel via UI drag interactions.
    /// </summary>
    public class ResizableWindow : MonoBehaviour, IDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Resize Settings")]
        public RectTransform targetRectTransform;
        public Vector2 minSize = new Vector2(280f, 320f);
        public Vector2 maxSize = new Vector2(1200f, 1000f);

        private Canvas _parentCanvas;

        private void Start()
        {
            if (targetRectTransform == null)
            {
                if (transform.parent != null)
                {
                    targetRectTransform = transform.parent.GetComponent<RectTransform>();
                }
                else
                {
                    targetRectTransform = GetComponent<RectTransform>();
                }
            }

            _parentCanvas = GetComponentInParent<Canvas>();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (targetRectTransform == null) return;

            Vector2 scale = Vector2.one;
            if (_parentCanvas != null && _parentCanvas.transform.localScale.x > 0)
            {
                scale = _parentCanvas.transform.localScale;
            }

            Vector2 currentSize = targetRectTransform.sizeDelta;
            float newWidth = currentSize.x + (eventData.delta.x / scale.x);
            float newHeight = currentSize.y - (eventData.delta.y / scale.y);

            newWidth = Mathf.Clamp(newWidth, minSize.x, maxSize.x);
            newHeight = Mathf.Clamp(newHeight, minSize.y, maxSize.y);

            targetRectTransform.sizeDelta = new Vector2(newWidth, newHeight);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Hover feedback if needed
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Reset hover state if needed
        }
    }
}
