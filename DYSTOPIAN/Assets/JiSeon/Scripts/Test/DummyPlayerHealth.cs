using UnityEngine;
using UnityEngine.Events;

namespace Dystopian.EnemyTest
{
    [DisallowMultipleComponent]
    public sealed class DummyPlayerHealth : MonoBehaviour, IDamageable
    {
        [Header("Health")]
        [SerializeField, Min(1)] private int maxHealth = 100;
        [SerializeField] private int currentHealth = 100;
        [SerializeField] private bool logHealthChanges = true;

        [Header("Events")]
        [SerializeField] private EnemyHealthChangedEvent onHealthChanged;
        [SerializeField] private UnityEvent onDamaged;
        [SerializeField] private UnityEvent onDefeated;

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsAlive => currentHealth > 0;

        private void Awake()
        {
            currentHealth = Mathf.Clamp(currentHealth <= 0 ? maxHealth : currentHealth, 0, maxHealth);
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            ApplyDamage(damageInfo.Amount);
        }

        public void ApplyDamage(int amount)
        {
            if (amount <= 0 || !IsAlive)
            {
                return;
            }

            currentHealth = Mathf.Max(0, currentHealth - amount);
            onHealthChanged?.Invoke(currentHealth, maxHealth);
            onDamaged?.Invoke();

            if (logHealthChanges)
            {
                Debug.Log($"[Dummy Player Health] {name} took {amount} damage. HP: {currentHealth}/{maxHealth}", this);
            }

            if (currentHealth <= 0)
            {
                if (logHealthChanges)
                {
                    Debug.Log($"[Dummy Player Health] {name} defeated.", this);
                }

                onDefeated?.Invoke();
            }
        }

        [ContextMenu("Reset Health")]
        public void ResetHealth()
        {
            currentHealth = maxHealth;
            onHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1, maxHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        }
    }
}
