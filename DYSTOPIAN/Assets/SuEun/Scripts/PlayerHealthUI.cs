using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PlayerHealthUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Image healthFill;
    [SerializeField] private CanvasGroup bloodyScreen;

    [Header("High Health Effect")]
    [Range(0f, 1f)]
    [SerializeField] private float bloodyThreshold = 0.7f;
    [SerializeField] private float transitionSpeed = 6f;

    private void Awake()
    {
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();
    }

    private void Update()
    {
        if (playerHealth == null)
            return;

        float healthRate = Mathf.Clamp01(playerHealth.HealthRate);

        if (healthFill != null)
            healthFill.fillAmount = healthRate;

        if (bloodyScreen != null)
        {
            float targetAlpha = healthRate >= bloodyThreshold
                ? Mathf.InverseLerp(bloodyThreshold, 1f, healthRate)
                : 0f;

            bloodyScreen.alpha = Mathf.MoveTowards(
                bloodyScreen.alpha,
                targetAlpha,
                transitionSpeed * Time.unscaledDeltaTime);
        }
    }

    public void Configure(PlayerHealth health, Image fill, CanvasGroup overlay)
    {
        playerHealth = health;
        healthFill = fill;
        bloodyScreen = overlay;
    }
}
