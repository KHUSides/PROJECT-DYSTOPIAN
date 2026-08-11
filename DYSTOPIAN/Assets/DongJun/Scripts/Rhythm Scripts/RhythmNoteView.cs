using TMPro;
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
        private Mask mask;
        private RectTransform attackLabelRect;
        private TextMeshProUGUI attackLabel;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            image = GetComponent<Image>();
            mask = GetComponent<Mask>();
            image.raycastTarget = false;
            CreateAttackLabel();
        }

        public void ConfigureSprite(float width, float height, float pivotX, Color color, Sprite sprite)
        {
            ConfigureRect(width, height, pivotX, color);
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            mask.enabled = false;
            attackLabel.gameObject.SetActive(false);
        }

        public void ConfigureAttack(
            float width,
            float height,
            float pivotX,
            Color color,
            string label,
            TMP_FontAsset font,
            float fontSize,
            Color textColor)
        {
            ConfigureRect(width, height, pivotX, color);
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            mask.enabled = true;
            mask.showMaskGraphic = true;

            attackLabel.text = label;
            attackLabel.fontSize = fontSize;
            attackLabel.color = textColor;
            if (font != null)
            {
                attackLabel.font = font;
            }

            attackLabelRect.sizeDelta = new Vector2(600f, height * 1.5f);
            attackLabel.gameObject.SetActive(true);
        }

        public void SetPosition(float x, float y)
        {
            rect.anchoredPosition = new Vector2(x, y);
        }

        public void SetWidth(float width)
        {
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(0f, width));
        }

        public void SetAttackLabelPosition(float trackPositionX)
        {
            if (!attackLabel.gameObject.activeSelf)
            {
                return;
            }

            attackLabelRect.anchoredPosition = new Vector2(
                trackPositionX - rect.anchoredPosition.x,
                0f);
        }

        private void ConfigureRect(float width, float height, float pivotX, Color color)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(pivotX, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            image.color = color;
            attackLabelRect.anchorMin = attackLabelRect.anchorMax = new Vector2(pivotX, 0.5f);
        }

        private void CreateAttackLabel()
        {
            GameObject labelObject = new GameObject(
                "Attack Label",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(transform, false);

            attackLabelRect = labelObject.GetComponent<RectTransform>();
            attackLabelRect.anchorMin = attackLabelRect.anchorMax = new Vector2(0f, 0.5f);
            attackLabelRect.pivot = new Vector2(0.5f, 0.5f);

            attackLabel = labelObject.GetComponent<TextMeshProUGUI>();
            attackLabel.alignment = TextAlignmentOptions.Center;
            attackLabel.overflowMode = TextOverflowModes.Overflow;
            attackLabel.raycastTarget = false;
            labelObject.SetActive(false);
        }
    }
}
