using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Board
{
    /// <summary>
    /// Draws a UI line (link) between two points in a RectTransform overlay.
    /// </summary>
    public sealed class SynergyLinkView : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Image _image;
        private float _thickness;

        public static SynergyLinkView Create(RectTransform parent, Vector2 start, Vector2 end, Color color, float thickness = 4f)
        {
            var go = new GameObject("SynergyLink", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one;
            go.transform.localRotation = Quaternion.identity;
            
            var view = go.AddComponent<SynergyLinkView>();
            view.Initialize(color, thickness);
            view.UpdatePoints(start, end);
            
            return view;
        }

        private void Initialize(Color color, float thickness)
        {
            _rectTransform = GetComponent<RectTransform>();
            _image = GetComponent<Image>();
            _thickness = thickness;

            _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _rectTransform.pivot = new Vector2(0.5f, 0.5f);

            _image.color = color;
            _image.raycastTarget = false;
        }

        public void UpdatePoints(Vector2 start, Vector2 end)
        {
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();

            Vector2 dir = end - start;
            float distance = dir.magnitude;
            
            // Avoid division by zero or tiny lines
            if (distance < 0.001f)
            {
                _rectTransform.sizeDelta = new Vector2(0, _thickness);
                return;
            }

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            _rectTransform.sizeDelta = new Vector2(distance, _thickness);
            _rectTransform.anchoredPosition = (start + end) * 0.5f;
            _rectTransform.localRotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        public void SetColor(Color color)
        {
            if (_image == null) _image = GetComponent<Image>();
            _image.color = color;
        }
    }
}
