using System;
using Dystopian.Combat;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyHealthDummy : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField, Min(1)] private int maxHealth = 50;

    private int currentHealth;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsAlive => currentHealth > 0 && gameObject.activeSelf;
    public event Action<int, int> HealthChanged;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (!IsAlive || damageInfo.Amount <= 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - damageInfo.Amount);
        HealthChanged?.Invoke(currentHealth, maxHealth);
        if (currentHealth == 0)
            gameObject.SetActive(false);
    }

    public void Respawn(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
        currentHealth = maxHealth;
        gameObject.SetActive(true);
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
