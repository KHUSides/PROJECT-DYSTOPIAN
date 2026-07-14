using UnityEngine;
using UnityEngine.UI;

namespace Dystopian.Rhythm
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(Image))]
    public sealed class RhythmNoteView : MonoBehaviour
    {
        private RectTransform rect;
        private Image image;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            image = GetComponent<Image>();
            image.raycastTarget = false;
        }

        public void Configure(float width, float height, float pivotX, Color color)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(pivotX, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            image.color = color;
        }

        public void SetPosition(float x, float y)
        {
            rect.anchoredPosition = new Vector2(x, y);
        }

        public void SetWidth(float width)
        {
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(0f, width));
        }
    }
}
