using System;
using Dystopian.Combat;
using UnityEngine;

namespace Dystopian.SuEun.StageFlow
{
    [DisallowMultipleComponent]
    public sealed class TemporaryBossHealth : MonoBehaviour, IDamageable, IHealthSource
    {
        [SerializeField] private int maxHealth_ = 300;
        [SerializeField] private int currentHealth_ = 300;

        public int CurrentHealth => currentHealth_;
        public int MaxHealth => maxHealth_;
        public bool IsAlive => currentHealth_ > 0;
        public event Action<int, int> HealthChanged;

        private void Awake()
        {
            currentHealth_ = Mathf.Clamp(currentHealth_, 0, maxHealth_);
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (!IsAlive || damageInfo.Amount <= 0)
                return;

            currentHealth_ = Mathf.Max(0, currentHealth_ - damageInfo.Amount);
            HealthChanged?.Invoke(currentHealth_, maxHealth_);
        }
    }
}
