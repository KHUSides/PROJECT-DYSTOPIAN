using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Dystopian.EnemyTest
{
    [Serializable]
    public sealed class EnemyHealthChangedEvent : UnityEvent<int, int>
    {
    }

    [DisallowMultipleComponent]
    public sealed class EnemyHealth : MonoBehaviour, IDamageable
    {
        [Header("Health")]
        [SerializeField, Min(1)] private int maxHealth = 100;
        [SerializeField] private int currentHealth = 100;

        [Header("Death")]
        [SerializeField] private bool disableCollidersOnDeath = true;
        [SerializeField] private bool deactivateObjectOnDeath = true;
        [SerializeField, Min(0f)] private float deactivateDelaySeconds = 2f;
        [SerializeField] private bool logHealthChanges = true;

        [Header("Events")]
        [SerializeField] private EnemyHealthChangedEvent onHealthChanged;
        [SerializeField] private UnityEvent onDamaged;
        [SerializeField] private UnityEvent onDied;

        private Collider[] cachedColliders;
        private bool isDead;
        private Coroutine deactivateRoutine;

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsAlive => !isDead && currentHealth > 0;

        public event Action<EnemyHealth> Damaged;
        public event Action<EnemyHealth> Died;

        private void Awake()
        {
            cachedColliders = GetComponentsInChildren<Collider>();
            currentHealth = Mathf.Clamp(currentHealth <= 0 ? maxHealth : currentHealth, 0, maxHealth);
            isDead = currentHealth <= 0;
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            ApplyDamage(damageInfo.Amount);
        }

        public void ApplyDamage(int amount)
        {
            if (amount <= 0 || isDead)
            {
                return;
            }

            currentHealth = Mathf.Max(0, currentHealth - amount);
            onHealthChanged?.Invoke(currentHealth, maxHealth);
            onDamaged?.Invoke();

            if (logHealthChanges)
            {
                Debug.Log($"[Enemy Health] {name} took {amount} damage. HP: {currentHealth}/{maxHealth}", this);
            }

            Damaged?.Invoke(this);

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        [ContextMenu("Reset Health")]
        public void ResetHealth()
        {
            if (deactivateRoutine != null)
            {
                StopCoroutine(deactivateRoutine);
                deactivateRoutine = null;
            }

            isDead = false;
            currentHealth = maxHealth;
            SetCollidersEnabled(true);
            gameObject.SetActive(true);
            onHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        [ContextMenu("Kill")]
        public void Kill()
        {
            if (isDead)
            {
                return;
            }

            currentHealth = 0;
            onHealthChanged?.Invoke(currentHealth, maxHealth);
            Die();
        }

        private void Die()
        {
            if (isDead)
            {
                return;
            }

            isDead = true;

            if (disableCollidersOnDeath)
            {
                SetCollidersEnabled(false);
            }

            if (logHealthChanges)
            {
                Debug.Log($"[Enemy Health] {name} died.", this);
            }

            Died?.Invoke(this);
            onDied?.Invoke();

            if (deactivateObjectOnDeath)
            {
                if (deactivateDelaySeconds <= 0f || !isActiveAndEnabled)
                {
                    gameObject.SetActive(false);
                }
                else
                {
                    deactivateRoutine = StartCoroutine(DeactivateAfterDelay());
                }
            }
        }

        private IEnumerator DeactivateAfterDelay()
        {
            yield return new WaitForSeconds(deactivateDelaySeconds);
            deactivateRoutine = null;
            gameObject.SetActive(false);
        }

        private void SetCollidersEnabled(bool enabled)
        {
            if (cachedColliders == null)
            {
                return;
            }

            foreach (Collider cachedCollider in cachedColliders)
            {
                if (cachedCollider != null)
                {
                    cachedCollider.enabled = enabled;
                }
            }
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1, maxHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            deactivateDelaySeconds = Mathf.Max(0f, deactivateDelaySeconds);
        }
    }
}
