using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyHealthDummy))]
public sealed class EnemyHealthBarDummy : MonoBehaviour
{
    [Header("Health Bar")]
    [SerializeField] private Transform fill;

    private EnemyHealthDummy health;
    private Vector3 fullScale;
    private Vector3 fullPosition;

    private void Awake()
    {
        health = GetComponent<EnemyHealthDummy>();
        fullScale = fill.localScale;
        fullPosition = fill.localPosition;
    }

    private void OnEnable()
    {
        health.HealthChanged += HandleHealthChanged;
    }

    private void Start()
    {
        UpdateBar(health.CurrentHealth, health.MaxHealth);
    }

    private void OnDisable()
    {
        health.HealthChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        UpdateBar(currentHealth, maxHealth);
    }

    private void UpdateBar(int currentHealth, int maxHealth)
    {
        float ratio = maxHealth > 0 ? Mathf.Clamp01((float)currentHealth / maxHealth) : 0f;
        Vector3 scale = fullScale;
        scale.x = fullScale.x * ratio;
        fill.localScale = scale;

        Vector3 position = fullPosition;
        position.x = fullPosition.x - fullScale.x * (1f - ratio) * 0.5f;
        fill.localPosition = position;
    }
}
