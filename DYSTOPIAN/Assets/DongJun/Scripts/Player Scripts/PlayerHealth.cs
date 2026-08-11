using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [UnityEngine.Serialization.FormerlySerializedAs("maxHealth_")]
    [SerializeField] private int maxHealth = 100;
    [UnityEngine.Serialization.FormerlySerializedAs("currentHealth_")]
    [SerializeField] private int currentHealth;

    [Header("Debug Input")]
    [UnityEngine.Serialization.FormerlySerializedAs("enableDebugInput_")]
    [SerializeField] private bool enableDebugInput = true;
    [UnityEngine.Serialization.FormerlySerializedAs("debugDamageAmount_")]
    [SerializeField] private int debugDamageAmount = 10;
    [UnityEngine.Serialization.FormerlySerializedAs("debugHealAmount_")]
    [SerializeField] private int debugHealAmount = 10;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public float HealthRate =>
        maxHealth <= 0 ? 0f : (float)currentHealth / maxHealth;
    public bool IsEmpty => currentHealth <= 0;
    public bool IsFull => currentHealth >= maxHealth;

    private void Awake()
    {
        ClampHealthValues();
    }

    private void Update()
    {
        if (!enableDebugInput)
            return;

        if (Input.GetKeyDown(KeyCode.Minus))
            TakeDamage(debugDamageAmount);

        if (Input.GetKeyDown(KeyCode.Equals))
            Heal(debugHealAmount);
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0)
            return;

        ChangeHealth(-amount);
    }

    public void Heal(int amount)
    {
        if (amount <= 0)
            return;

        ChangeHealth(amount);
    }

    public void SetHealth(int value)
    {
        currentHealth = Mathf.Clamp(value, 0, maxHealth);
    }

    private void ChangeHealth(int amount)
    {
        if (amount == 0)
            return;

        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
    }

    private void ClampHealthValues()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }

    private void OnValidate()
    {
        ClampHealthValues();
    }
}

