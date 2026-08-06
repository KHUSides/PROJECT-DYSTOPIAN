using UnityEngine;
using UnityEngine.UI;

namespace Dystopian.SuEun.StageFlow
{
    public sealed class TemporaryBossHealthHud : MonoBehaviour
    {
        [SerializeField] private BattleZone bossZone_;
        [Tooltip("IHealthSource를 구현한 실제 보스 체력 컴포넌트를 연결합니다.")]
        [SerializeField] private MonoBehaviour healthSource_;
        [SerializeField] private GameObject panel_;
        [SerializeField] private Image fillImage_;

        private IHealthSource HealthSource => healthSource_ as IHealthSource;

        private void Awake()
        {
            if (panel_ != null)
                panel_.SetActive(false);
        }

        private void OnEnable()
        {
            if (bossZone_ != null)
            {
                bossZone_.BattleStarted += HandleBattleStarted;
                bossZone_.BattleCleared += HandleBattleCleared;
            }

            if (HealthSource != null)
                HealthSource.HealthChanged += UpdateFill;
        }

        private void OnDisable()
        {
            if (bossZone_ != null)
            {
                bossZone_.BattleStarted -= HandleBattleStarted;
                bossZone_.BattleCleared -= HandleBattleCleared;
            }

            if (HealthSource != null)
                HealthSource.HealthChanged -= UpdateFill;
        }

        private void HandleBattleStarted(BattleZone zone)
        {
            if (panel_ != null)
                panel_.SetActive(true);

            if (HealthSource != null)
                UpdateFill(HealthSource.CurrentHealth, HealthSource.MaxHealth);
        }

        private void HandleBattleCleared(BattleZone zone)
        {
            if (panel_ != null)
                panel_.SetActive(false);
        }

        private void UpdateFill(int currentHealth, int maxHealth)
        {
            if (fillImage_ != null)
                fillImage_.fillAmount = maxHealth > 0
                    ? Mathf.Clamp01(currentHealth / (float)maxHealth)
                    : 0f;
        }
    }
}
