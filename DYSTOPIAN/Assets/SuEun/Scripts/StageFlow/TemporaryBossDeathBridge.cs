using UnityEngine;

namespace Dystopian.SuEun.StageFlow
{
    [RequireComponent(typeof(TemporaryBossHealth), typeof(StageEnemy))]
    public sealed class TemporaryBossDeathBridge : MonoBehaviour
    {
        private TemporaryBossHealth health_;
        private StageEnemy stageEnemy_;

        private void Awake()
        {
            health_ = GetComponent<TemporaryBossHealth>();
            stageEnemy_ = GetComponent<StageEnemy>();
        }

        private void OnEnable()
        {
            health_.HealthChanged += HandleHealthChanged;
        }

        private void OnDisable()
        {
            health_.HealthChanged -= HandleHealthChanged;
        }

        private void HandleHealthChanged(int currentHealth, int maxHealth)
        {
            if (currentHealth <= 0)
                stageEnemy_.Defeat();
        }
    }
}
