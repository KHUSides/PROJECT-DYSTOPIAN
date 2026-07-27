using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [UnityEngine.Serialization.FormerlySerializedAs("maxHealth_")]
    [SerializeField] private int maxHealth = 100;
    [UnityEngine.Serialization.FormerlySerializedAs("currentHealth_")]
    [SerializeField] private int currentHealth = 0;

    [Header("Canvas Health Bar")]
    [UnityEngine.Serialization.FormerlySerializedAs("showHealthBar_")]
    [SerializeField] private bool showHealthBar = true;
    [UnityEngine.Serialization.FormerlySerializedAs("healthBarAnchoredPosition_")]
    [SerializeField] private Vector2 healthBarAnchoredPosition = new Vector2(24f, -24f);
    [UnityEngine.Serialization.FormerlySerializedAs("healthBarSize_")]
    [SerializeField] private Vector2 healthBarSize = new Vector2(260f, 28f);
    [UnityEngine.Serialization.FormerlySerializedAs("healthBarBackColor_")]
    [SerializeField] private Color healthBarBackColor = new Color(0.1f, 0.1f, 0.1f, 0.75f);
    [UnityEngine.Serialization.FormerlySerializedAs("healthBarFillColor_")]
    [SerializeField] private Color healthBarFillColor = new Color(0.85f, 0.1f, 0.1f, 0.9f);
    [UnityEngine.Serialization.FormerlySerializedAs("healthBarTextColor_")]
    [SerializeField] private Color healthBarTextColor = Color.white;

    [Header("Debug Input")]
    [UnityEngine.Serialization.FormerlySerializedAs("enableDebugInput_")]
    [SerializeField] private bool enableDebugInput = true;
    [UnityEngine.Serialization.FormerlySerializedAs("debugDamageAmount_")]
    [SerializeField] private int debugDamageAmount = 10;
    [UnityEngine.Serialization.FormerlySerializedAs("debugHealAmount_")]
    [SerializeField] private int debugHealAmount = 10;

    private RectTransform healthBarRoot;
    private RectTransform healthBarBack;
    private RectTransform healthBarFill;
    private Text healthBarText;
    private Image healthBarBackImage;
    private Image healthBarFillImage;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public float HealthRate => maxHealth <= 0 ? 0f : (float)currentHealth / maxHealth;
    public bool IsEmpty => currentHealth <= 0;
    public bool IsFull => currentHealth >= maxHealth;

    private void Awake()
    {
        ClampHealthValues();
    }

    private void Start()
    {
        ResolveHealthBar();
        ApplyHealthBarSettings();
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
        ApplyHealthBarSettings();
    }

    private void ResolveHealthBar()
    {
        Transform healthBar = FindHealthBarInScene();
        healthBarRoot = healthBar as RectTransform;
        healthBarBack = healthBarRoot != null ? healthBarRoot.Find("Back") as RectTransform : null;
        healthBarFill = healthBarBack != null ? healthBarBack.Find("Fill") as RectTransform : null;

        Transform textTransform = healthBarRoot != null ? healthBarRoot.Find("Text") : null;
        healthBarText = textTransform != null ? textTransform.GetComponent<Text>() : null;
        healthBarBackImage = healthBarBack != null ? healthBarBack.GetComponent<Image>() : null;
        healthBarFillImage = healthBarFill != null ? healthBarFill.GetComponent<Image>() : null;
    }

    private Transform FindHealthBarInScene()
    {
        Transform localHealthBar = transform.Find("HealthCanvas/HealthBar");
        if (localHealthBar != null)
            return localHealthBar;

        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);

        for (int i = 0; i < canvases.Length; i++)
        {
            Transform healthBar = canvases[i].transform.Find("HealthBar");

            if (healthBar != null)
                return healthBar;
        }

        return null;
    }

    private void ChangeHealth(int amount)
    {
        if (amount == 0)
            return;

        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        ApplyHealthBarSettings();
    }

    private void ApplyHealthBarSettings()
    {
        if (healthBarRoot == null)
            return;

        healthBarRoot.gameObject.SetActive(showHealthBar);
        healthBarRoot.anchoredPosition = healthBarAnchoredPosition;
        healthBarRoot.sizeDelta = healthBarSize;
        StretchToParent(healthBarBack);

        if (healthBarFill != null)
        {
            healthBarFill.anchorMin = Vector2.zero;
            healthBarFill.anchorMax = new Vector2(Mathf.Clamp01(HealthRate), 1f);
            healthBarFill.offsetMin = Vector2.zero;
            healthBarFill.offsetMax = Vector2.zero;
        }

        if (healthBarBackImage != null)
            healthBarBackImage.color = healthBarBackColor;

        if (healthBarFillImage != null)
            healthBarFillImage.color = healthBarFillColor;

        if (healthBarText != null)
        {
            healthBarText.color = healthBarTextColor;
            healthBarText.text = currentHealth + " / " + maxHealth;
        }
    }

    private static void StretchToParent(RectTransform rectTransform)
    {
        if (rectTransform == null)
            return;

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private void ClampHealthValues()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }

    private void OnValidate()
    {
        ClampHealthValues();

        if (Application.isPlaying)
            ApplyHealthBarSettings();
    }
}
