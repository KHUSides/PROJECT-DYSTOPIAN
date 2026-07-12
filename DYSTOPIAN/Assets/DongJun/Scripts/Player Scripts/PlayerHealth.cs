using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth = 0;

    [Header("Temporary Canvas Health Bar")]
    [SerializeField] private bool showHealthBar = true;
    [SerializeField] private Vector2 healthBarAnchoredPosition = new Vector2(24f, -24f);
    [SerializeField] private Vector2 healthBarSize = new Vector2(260f, 28f);
    [SerializeField] private Color healthBarBackColor = new Color(0.1f, 0.1f, 0.1f, 0.75f);
    [SerializeField] private Color healthBarFillColor = new Color(0.85f, 0.1f, 0.1f, 0.9f);
    [SerializeField] private Color healthBarTextColor = Color.white;

    [Header("Debug Input")]
    [SerializeField] private bool enableDebugInput = true;
    [SerializeField] private int debugDamageAmount = 10;
    [SerializeField] private int debugHealAmount = 10;

    [SerializeField] private Canvas healthCanvas;
    [SerializeField] private RectTransform healthBarRoot;
    [SerializeField] private RectTransform healthBarBack;
    [SerializeField] private RectTransform healthBarFill;
    [SerializeField] private Text healthBarText;
    [SerializeField] private Image healthBarBackImage;
    [SerializeField] private Image healthBarFillImage;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public float HealthRate => maxHealth <= 0 ? 0f : (float)currentHealth / maxHealth;
    public bool IsEmpty => currentHealth <= 0;
    public bool IsFull => currentHealth >= maxHealth;
    public bool IsDead => IsFull;

private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        CreateTemporaryHealthBar();
        UpdateHealthBar();
    }

private void Update()
    {
        if (enableDebugInput)
            HandleDebugInput();

        UpdateHealthBarTransform();
    }

public void TakeDamage(int amount)
    {
        ReduceHealth(amount);
    }

public void Heal(int amount)
    {
        AddHealth(amount);
    }

public void AddHealth(int amount)
    {
        if (amount <= 0)
            return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateHealthBar();

        Debug.Log($"[Health] Add {amount} / Value {currentHealth}/{maxHealth}");
    }

    public void ReduceHealth(int amount)
    {
        if (amount <= 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        UpdateHealthBar();

        Debug.Log($"[Health] Reduce {amount} / Value {currentHealth}/{maxHealth}");
    }


    public void SetHealth(int value)
    {
        currentHealth = Mathf.Clamp(value, 0, maxHealth);
        UpdateHealthBar();
    }

private void HandleDebugInput()
    {
        if (Input.GetKeyDown(KeyCode.Minus))
            ReduceHealth(debugDamageAmount);

        if (Input.GetKeyDown(KeyCode.Equals))
            AddHealth(debugHealAmount);
    }

private void CreateTemporaryHealthBar()
    {
        if (healthBarRoot != null)
        {
            ResolveHealthBarReferencesFromHierarchy();
            return;
        }

        GameObject canvasObject = new GameObject("TemporaryHealthCanvas");
        healthCanvas = canvasObject.AddComponent<Canvas>();
        healthCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        healthCanvas.sortingOrder = 100;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject rootObject = new GameObject("HealthBar");
        rootObject.transform.SetParent(canvasObject.transform, false);
        healthBarRoot = rootObject.AddComponent<RectTransform>();
        healthBarRoot.anchorMin = new Vector2(0f, 1f);
        healthBarRoot.anchorMax = new Vector2(0f, 1f);
        healthBarRoot.pivot = new Vector2(0f, 1f);

        GameObject backObject = new GameObject("Back");
        backObject.transform.SetParent(rootObject.transform, false);
        healthBarBack = backObject.AddComponent<RectTransform>();
        healthBarBackImage = backObject.AddComponent<Image>();

        GameObject fillObject = new GameObject("Fill");
        fillObject.transform.SetParent(backObject.transform, false);
        healthBarFill = fillObject.AddComponent<RectTransform>();
        healthBarFillImage = fillObject.AddComponent<Image>();

        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(rootObject.transform, false);
        RectTransform textRect = textObject.AddComponent<RectTransform>();
        healthBarText = textObject.AddComponent<Text>();
        healthBarText.alignment = TextAnchor.MiddleCenter;
        healthBarText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        healthBarText.fontSize = 14;

        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

private void ResolveHealthBarReferencesFromHierarchy()
    {
        if (healthCanvas == null)
            healthCanvas = healthBarRoot.GetComponentInParent<Canvas>();

        if (healthBarBack == null)
        {
            Transform back = healthBarRoot.Find("Back");
            if (back != null)
                healthBarBack = back as RectTransform;
        }

        if (healthBarFill == null && healthBarBack != null)
        {
            Transform fill = healthBarBack.Find("Fill");
            if (fill != null)
                healthBarFill = fill as RectTransform;
        }

        if (healthBarText == null)
        {
            Transform text = healthBarRoot.Find("Text");
            if (text != null)
                healthBarText = text.GetComponent<Text>();
        }

        if (healthBarBackImage == null && healthBarBack != null)
            healthBarBackImage = healthBarBack.GetComponent<Image>();

        if (healthBarFillImage == null && healthBarFill != null)
            healthBarFillImage = healthBarFill.GetComponent<Image>();
    }


private Transform CreateBarPart(string partName, Color color, float zOffset)
    {
        return null;
    }

private void UpdateHealthBarTransform()
    {
        if (healthBarRoot == null)
            return;

        healthBarRoot.gameObject.SetActive(showHealthBar);
        healthBarRoot.anchoredPosition = healthBarAnchoredPosition;
        healthBarRoot.sizeDelta = healthBarSize;

        healthBarBack.anchorMin = Vector2.zero;
        healthBarBack.anchorMax = Vector2.one;
        healthBarBack.offsetMin = Vector2.zero;
        healthBarBack.offsetMax = Vector2.zero;

        healthBarFill.anchorMin = new Vector2(0f, 0f);
        healthBarFill.anchorMax = new Vector2(Mathf.Clamp01(HealthRate), 1f);
        healthBarFill.offsetMin = Vector2.zero;
        healthBarFill.offsetMax = Vector2.zero;
    }

private void UpdateHealthBar()
    {
        if (healthBarRoot == null)
            return;

        if (healthBarBackImage != null)
            healthBarBackImage.color = healthBarBackColor;

        if (healthBarFillImage != null)
            healthBarFillImage.color = healthBarFillColor;

        if (healthBarText != null)
        {
            healthBarText.color = healthBarTextColor;
            healthBarText.text = $"{currentHealth} / {maxHealth}";
        }

        UpdateHealthBarTransform();
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (Application.isPlaying)
            UpdateHealthBar();
    }
}
