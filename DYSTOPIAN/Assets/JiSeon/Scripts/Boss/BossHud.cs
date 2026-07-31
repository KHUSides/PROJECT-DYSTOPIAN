using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace Dystopian.EnemyTest
{
    [DisallowMultipleComponent]
    public sealed class BossHud : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BossController bossController;
        [SerializeField] private BossHealth bossHealth;
        [SerializeField] private MerlinBossCombat merlinCombat;

        [Header("UI")]
        [SerializeField] private Text bossNameText;
        [SerializeField] private Image hpFillImage;
        [SerializeField] private Text hpValueText;
        [SerializeField] private Text singleCounterText;

        private void Reset()
        {
            CacheSceneReferences();
        }

        private void Awake()
        {
            CacheSceneReferences();
        }

        private void OnEnable()
        {
            Subscribe();
            RefreshAll();
        }

        private void Start()
        {
            CacheSceneReferences();
            Subscribe();
            RefreshAll();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void HandleHealthChanged(BossHealth _)
        {
            RefreshHealth();
        }

        private void HandleCounterChanged(int _)
        {
            RefreshCounter();
        }

        private void RefreshAll()
        {
            RefreshName();
            RefreshHealth();
            RefreshCounter();
        }

        private void RefreshName()
        {
            if (bossNameText == null)
            {
                return;
            }

            bossNameText.text = bossController != null
                ? bossController.BossDisplayName
                : "Boss";
        }

        private void RefreshHealth()
        {
            if (bossHealth == null)
            {
                return;
            }

            float normalizedHealth = Mathf.Clamp01(bossHealth.NormalizedHealth);

            if (hpFillImage != null)
            {
                ApplyHpFill(normalizedHealth);
            }

            if (hpValueText != null)
            {
                hpValueText.text = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}/{1}",
                    bossHealth.CurrentHealth,
                    bossHealth.MaxHealth);
            }
        }

        private void ApplyHpFill(float normalizedHealth)
        {
            hpFillImage.fillAmount = normalizedHealth;

            RectTransform fillRect = hpFillImage.rectTransform;
            if (fillRect == null)
            {
                return;
            }

            Vector2 anchorMin = fillRect.anchorMin;
            Vector2 anchorMax = fillRect.anchorMax;
            anchorMin.x = 0f;
            anchorMax.x = normalizedHealth;
            fillRect.anchorMin = anchorMin;
            fillRect.anchorMax = anchorMax;
            fillRect.offsetMin = new Vector2(0f, fillRect.offsetMin.y);
            fillRect.offsetMax = new Vector2(0f, fillRect.offsetMax.y);
            fillRect.pivot = new Vector2(0f, fillRect.pivot.y);

            Vector3 localScale = fillRect.localScale;
            localScale.x = 1f;
            fillRect.localScale = localScale;
        }

        private void RefreshCounter()
        {
            if (singleCounterText == null)
            {
                return;
            }

            if (merlinCombat == null)
            {
                singleCounterText.text = "Single Counter: 0";
                return;
            }

            singleCounterText.text = string.Format(
                CultureInfo.InvariantCulture,
                "Single Counter: {0}\nNext: {1}",
                merlinCombat.SingleNoteCounter,
                merlinCombat.NextSinglePatternName);
        }

        private void CacheSceneReferences()
        {
            if (bossController == null)
            {
                bossController = FindFirstObjectByType<BossController>();
            }

            if (bossHealth == null && bossController != null)
            {
                bossHealth = bossController.GetComponent<BossHealth>();
            }

            if (merlinCombat == null && bossController != null)
            {
                merlinCombat = bossController.GetComponent<MerlinBossCombat>();
            }
        }

        private void Subscribe()
        {
            if (bossHealth != null)
            {
                bossHealth.HealthChanged -= HandleHealthChanged;
                bossHealth.Damaged -= HandleHealthChanged;
                bossHealth.Died -= HandleHealthChanged;
                bossHealth.HealthChanged += HandleHealthChanged;
                bossHealth.Damaged += HandleHealthChanged;
                bossHealth.Died += HandleHealthChanged;
            }

            if (merlinCombat != null)
            {
                merlinCombat.SingleNoteCounterChanged -= HandleCounterChanged;
                merlinCombat.SingleNoteCounterChanged += HandleCounterChanged;
            }
        }

        private void Unsubscribe()
        {
            if (bossHealth != null)
            {
                bossHealth.HealthChanged -= HandleHealthChanged;
                bossHealth.Damaged -= HandleHealthChanged;
                bossHealth.Died -= HandleHealthChanged;
            }

            if (merlinCombat != null)
            {
                merlinCombat.SingleNoteCounterChanged -= HandleCounterChanged;
            }
        }
    }
}
