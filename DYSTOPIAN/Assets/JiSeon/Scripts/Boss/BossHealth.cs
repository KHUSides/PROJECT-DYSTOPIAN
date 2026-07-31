using System;
using UnityEngine;
using UnityEngine.Events;

namespace Dystopian.EnemyTest
{
    [Serializable]
    public sealed class BossHealthChangedEvent : UnityEvent<int, int>
    {
    }

    [DisallowMultipleComponent]
    public sealed class BossHealth : MonoBehaviour, IDamageable
    {
        [Header("Health")]
        [SerializeField, Min(1)] private int maxHealth = 500;
        [SerializeField, Min(0)] private int currentHealth = 500;

        [Header("Death")]
        [SerializeField] private bool disableCollidersOnDeath = true;
        [SerializeField] private bool deactivateObjectOnDeath;
        [SerializeField, Min(0f)] private float deactivateDelaySeconds = 2f;

        [Header("Events")]
        [SerializeField] private BossHealthChangedEvent onHealthChanged = new BossHealthChangedEvent();
        [SerializeField] private UnityEvent onDamaged = new UnityEvent();
        [SerializeField] private UnityEvent onDied = new UnityEvent();

        private Collider[] cachedColliders;
        private bool deathHandled;

        public event Action<BossHealth> Damaged;
        public event Action<BossHealth> Died;
        public event Action<BossHealth> HealthChanged;

        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public float NormalizedHealth => maxHealth > 0 ? currentHealth / (float)maxHealth : 0f;
        public bool IsAlive => currentHealth > 0;

        private void Awake()
        {
            maxHealth = Mathf.Max(1, maxHealth);
            currentHealth = Mathf.Clamp(currentHealth <= 0 ? maxHealth : currentHealth, 0, maxHealth);
            cachedColliders = GetComponentsInChildren<Collider>(true);
        }

        private void Start()
        {
            NotifyHealthChanged();
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (!IsAlive || damageInfo.Amount <= 0)
            {
                return;
            }

            currentHealth = Mathf.Max(0, currentHealth - damageInfo.Amount);
            NotifyHealthChanged();
            Damaged?.Invoke(this);
            onDamaged.Invoke();

            if (currentHealth <= 0)
            {
                HandleDeath();
            }
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || !IsAlive)
            {
                return;
            }

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            NotifyHealthChanged();
        }

        public void ResetHealth()
        {
            deathHandled = false;
            maxHealth = Mathf.Max(1, maxHealth);
            currentHealth = maxHealth;
            SetCollidersEnabled(true);
            gameObject.SetActive(true);
            NotifyHealthChanged();
        }

        private void HandleDeath()
        {
            if (deathHandled)
            {
                return;
            }

            deathHandled = true;

            if (disableCollidersOnDeath)
            {
                SetCollidersEnabled(false);
            }

            Died?.Invoke(this);
            onDied.Invoke();

            if (deactivateObjectOnDeath)
            {
                Invoke(nameof(DeactivateSelf), deactivateDelaySeconds);
            }
        }

        private void SetCollidersEnabled(bool enabled)
        {
            if (cachedColliders == null)
            {
                cachedColliders = GetComponentsInChildren<Collider>(true);
            }

            for (int i = 0; i < cachedColliders.Length; i++)
            {
                if (cachedColliders[i] != null)
                {
                    cachedColliders[i].enabled = enabled;
                }
            }
        }

        private void NotifyHealthChanged()
        {
            onHealthChanged.Invoke(currentHealth, maxHealth);
            HealthChanged?.Invoke(this);
        }

        private void DeactivateSelf()
        {
            gameObject.SetActive(false);
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1, maxHealth);
            currentHealth = Mathf.Clamp(currentHealth <= 0 ? maxHealth : currentHealth, 0, maxHealth);
            deactivateDelaySeconds = Mathf.Max(0f, deactivateDelaySeconds);
        }
    }
}
