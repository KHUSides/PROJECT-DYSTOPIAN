using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class PlayerUpgradePanel : MonoBehaviour
{
    private const int MaxWaveUpgradeLevel = 5;

    [Header("Follow Positions")]
    [SerializeField] private Vector2 topRightOffset = new Vector2(1.35f, 1.75f);
    [SerializeField] private Vector2 topLeftOffset = new Vector2(-1.35f, 1.75f);
    [SerializeField] private Vector2 bottomRightOffset = new Vector2(1.35f, -0.85f);
    [SerializeField] private Vector2 bottomLeftOffset = new Vector2(-1.35f, -0.85f);
    [SerializeField] private float screenPadding = 24f;

    [Header("Toggle")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode alternateToggleKey = KeyCode.Keypad1;
    [SerializeField] private bool isVisibleOnStart;

    [Header("Upgrade Costs")]
    [SerializeField, Min(0)] private int attackPowerCost = 20;
    [SerializeField, Min(0)] private int darknessResistanceCost = 20;
    [SerializeField, Min(0)] private int waveSwordCost = 20;
    [SerializeField, Min(0)] private int waveImpactCost = 20;
    [SerializeField, Min(0)] private int soundBarrierCost = 30;

    [Header("Upgrade Values")]
    [SerializeField, Min(1)] private int attackPowerPerLevel = 2;
    [SerializeField, Min(1)] private int darknessResistancePerLevel = 2;

    [Header("Presentation")]
    [SerializeField, Min(0.01f)] private float openDuration = 0.18f;
    [SerializeField, Min(0.01f)] private float closeDuration = 0.14f;
    [SerializeField, Min(0.1f)] private float borderPulseDuration = 1.2f;
    [SerializeField] private Color enabledButtonColor = new Color(0.51f, 1f, 0.77f, 1f);
    [SerializeField] private Color disabledButtonColor = new Color(0.85f, 0.85f, 0.85f, 1f);

    private readonly Vector2[] positionCandidates = new Vector2[4];
    private Transform player;
    private Camera targetCamera;
    private RectTransform canvasRect;
    private RectTransform panel;
    private CanvasGroup panelCanvasGroup;
    private Outline borderOutline;
    private Button attackPowerButton;
    private Button darknessResistanceButton;
    private Button waveSwordButton;
    private Button waveImpactButton;
    private Button soundBarrierButton;
    private TextMeshProUGUI attackPowerLevelText;
    private TextMeshProUGUI darknessResistanceLevelText;
    private TextMeshProUGUI waveSwordLevelText;
    private TextMeshProUGUI waveImpactLevelText;
    private TextMeshProUGUI attackPowerCostText;
    private TextMeshProUGUI darknessResistanceCostText;
    private TextMeshProUGUI waveSwordCostText;
    private TextMeshProUGUI waveImpactCostText;
    private TextMeshProUGUI soundBarrierCostText;
    private TextMeshProUGUI chargeReinforceStatusText;
    private TextMeshProUGUI attackReinforceStatusText;
    private PlayerRhythmAttackBridge rhythmAttackBridge;
    private PlayerController playerController;
    private PlayerHealth playerHealth;
    private Sequence visibilitySequence;
    private Tween borderTween;
    private Vector3 panelBaseScale;
    private int attackPowerLevel = 1;
    private int darknessResistanceLevel = 1;
    private int waveSwordLevel;
    private int waveImpactLevel;
    private bool isVisible;
    private bool isInitialized;

    private void Awake()
    {
        panel = transform as RectTransform;
        panelCanvasGroup = GetComponent<CanvasGroup>();
        borderOutline = GetComponent<Outline>();
        panelBaseScale = panel != null ? panel.localScale : Vector3.one;
        isVisible = isVisibleOnStart;

        positionCandidates[0] = topRightOffset;
        positionCandidates[1] = topLeftOffset;
        positionCandidates[2] = bottomRightOffset;
        positionCandidates[3] = bottomLeftOffset;
    }

    public void Initialize(
        PlayerController ownerController,
        PlayerHealth ownerHealth,
        PlayerRhythmAttackBridge ownerRhythmAttackBridge)
    {
        if (isInitialized)
            return;

        playerController = ownerController;
        playerHealth = ownerHealth;
        rhythmAttackBridge = ownerRhythmAttackBridge;
        player = playerController != null ? playerController.transform : null;
        isInitialized = true;

        ResolveViewReferences();
        ConnectUpgradeButtons();
        ApplyLevelBonuses();
        RefreshView();
        SetVisible(isVisible, false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey) || Input.GetKeyDown(alternateToggleKey))
            SetVisible(!isVisible, true);
    }

    private void LateUpdate()
    {
        if (isVisible)
            UpdatePanelPosition();
    }

    private void ResolveViewReferences()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        targetCamera = Camera.main;

        Transform statusRoot = canvas != null ? canvas.transform.Find("ReinforceStatus") : null;
        chargeReinforceStatusText = FindText(statusRoot, "Charge Reinforce Status");
        attackReinforceStatusText = FindText(statusRoot, "Attack Reinforce Status");

        attackPowerButton = FindButton("AttackPowerButton");
        darknessResistanceButton = FindButton("DarknessResistanceButton");
        waveSwordButton = FindButton("WaveSwordButton");
        waveImpactButton = FindButton("WaveImpactButton");
        soundBarrierButton = FindButton("SoundBarrierButton");

        attackPowerLevelText = FindText(attackPowerButton, "Level");
        darknessResistanceLevelText = FindText(darknessResistanceButton, "Level");
        waveSwordLevelText = FindText(waveSwordButton, "Level");
        waveImpactLevelText = FindText(waveImpactButton, "Level");
        attackPowerCostText = FindText(attackPowerButton, "Cost");
        darknessResistanceCostText = FindText(darknessResistanceButton, "Cost");
        waveSwordCostText = FindText(waveSwordButton, "Cost");
        waveImpactCostText = FindText(waveImpactButton, "Cost");
        soundBarrierCostText = FindText(soundBarrierButton, "Cost");
    }

    private void ConnectUpgradeButtons()
    {
        AddListener(attackPowerButton, UpgradeAttackPower);
        AddListener(darknessResistanceButton, UpgradeDarknessResistance);
        AddListener(waveSwordButton, UpgradeWaveSword);
        AddListener(waveImpactButton, UpgradeWaveImpact);
        AddListener(soundBarrierButton, EnableSoundBarrier);

        if (playerHealth != null)
            playerHealth.SoundBarrierChanged += HandleSoundBarrierChanged;
    }

    private void UpgradeAttackPower()
    {
        attackPowerLevel++;
        ApplyLevelBonuses();
        Debug.Log($"[PlayerUpgradePanel] Attack Power upgraded to Lv.{attackPowerLevel}. Cost: {attackPowerCost}", this);
        RefreshView();
    }

    private void UpgradeDarknessResistance()
    {
        darknessResistanceLevel++;
        ApplyLevelBonuses();
        Debug.Log($"[PlayerUpgradePanel] Darkness Resistance upgraded to Lv.{darknessResistanceLevel}. Cost: {darknessResistanceCost}", this);
        RefreshView();
    }

    private void UpgradeWaveSword()
    {
        if (playerController == null || waveSwordLevel >= MaxWaveUpgradeLevel)
            return;

        waveSwordLevel++;
        if (waveSwordLevel == 1)
            playerController.SetAttackReinforceEnabled(true);

        Debug.Log($"[PlayerUpgradePanel] Wave Sword upgraded to Lv.{waveSwordLevel}. Cost: {waveSwordCost}", this);
        RefreshView();
    }

    private void UpgradeWaveImpact()
    {
        if (rhythmAttackBridge == null || waveImpactLevel >= MaxWaveUpgradeLevel)
            return;

        waveImpactLevel++;
        if (waveImpactLevel == 1)
            rhythmAttackBridge.SetChargeReinforceEnabled(true);

        Debug.Log($"[PlayerUpgradePanel] Wave Impact upgraded to Lv.{waveImpactLevel}. Cost: {waveImpactCost}", this);
        RefreshView();
    }

    private void EnableSoundBarrier()
    {
        playerHealth?.EnableSoundBarrier();
    }

    private void ApplyLevelBonuses()
    {
        playerController?.SetAttackPowerBonus(attackPowerLevel * attackPowerPerLevel);
        playerHealth?.SetDarknessResistance(darknessResistanceLevel * darknessResistancePerLevel);
    }

    private void HandleSoundBarrierChanged(bool isActive)
    {
        RefreshButtonColor(soundBarrierButton, isActive);
    }

    private void RefreshView()
    {
        SetText(attackPowerLevelText, $"Lv.{attackPowerLevel}");
        SetText(darknessResistanceLevelText, $"Lv.{darknessResistanceLevel}");
        SetText(waveSwordLevelText, GetWaveLevelText(waveSwordLevel));
        SetText(waveImpactLevelText, GetWaveLevelText(waveImpactLevel));
        SetText(attackPowerCostText, attackPowerCost.ToString());
        SetText(darknessResistanceCostText, darknessResistanceCost.ToString());
        SetText(waveSwordCostText, waveSwordCost.ToString());
        SetText(waveImpactCostText, waveImpactCost.ToString());
        SetText(soundBarrierCostText, soundBarrierCost.ToString());

        bool waveSwordEnabled = waveSwordLevel > 0;
        bool waveImpactEnabled = waveImpactLevel > 0;
        bool soundBarrierEnabled = playerHealth != null && playerHealth.IsSoundBarrierActive;
        RefreshButtonColor(waveSwordButton, waveSwordEnabled);
        RefreshButtonColor(waveImpactButton, waveImpactEnabled);
        RefreshButtonColor(soundBarrierButton, soundBarrierEnabled);

        SetReinforceStatus(attackReinforceStatusText, "Attack Reinforce", waveSwordEnabled);
        SetReinforceStatus(chargeReinforceStatusText, "Charge Reinforce", waveImpactEnabled);
    }

    private void SetVisible(bool visible, bool animate)
    {
        isVisible = visible;
        visibilitySequence?.Kill();
        borderTween?.Kill();

        panelCanvasGroup.interactable = visible;
        panelCanvasGroup.blocksRaycasts = visible;

        if (!animate)
        {
            panelCanvasGroup.alpha = visible ? 1f : 0f;
            panel.localScale = visible ? panelBaseScale : panelBaseScale * 0.15f;
            if (visible)
                StartBorderPulse();
            return;
        }

        if (visible)
        {
            UpdatePanelPosition();
            panel.localScale = panelBaseScale * 0.15f;
            panelCanvasGroup.alpha = 0f;
            visibilitySequence = DOTween.Sequence()
                .SetUpdate(true)
                .Join(panel.DOScale(panelBaseScale, openDuration).SetEase(Ease.OutBack))
                .Join(panelCanvasGroup.DOFade(1f, openDuration).SetEase(Ease.OutQuad))
                .OnComplete(StartBorderPulse);
            return;
        }

        visibilitySequence = DOTween.Sequence()
            .SetUpdate(true)
            .Join(panel.DOScale(panelBaseScale * 0.15f, closeDuration).SetEase(Ease.InBack))
            .Join(panelCanvasGroup.DOFade(0f, closeDuration).SetEase(Ease.InQuad));
    }

    private void StartBorderPulse()
    {
        if (borderOutline == null || !isVisible)
            return;

        Color dimColor = borderOutline.effectColor;
        dimColor.a = 0.4f;
        Color brightColor = borderOutline.effectColor;
        brightColor.a = 1f;
        borderOutline.effectColor = dimColor;
        borderTween = DOTween.To(
                () => borderOutline.effectColor,
                color => borderOutline.effectColor = color,
                brightColor,
                borderPulseDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true);
    }

    private void UpdatePanelPosition()
    {
        if (player == null || targetCamera == null || canvasRect == null || panel == null)
            return;

        Vector2 fallbackPosition = Vector2.zero;
        for (int i = 0; i < positionCandidates.Length; i++)
        {
            if (!TryGetCanvasPosition(positionCandidates[i], out Vector2 canvasPosition))
                continue;

            if (i == 0)
                fallbackPosition = canvasPosition;

            if (IsPanelFullyVisible(canvasPosition))
            {
                panel.anchoredPosition = canvasPosition;
                return;
            }
        }

        panel.anchoredPosition = ClampToCanvas(fallbackPosition);
    }

    private bool TryGetCanvasPosition(Vector2 worldOffset, out Vector2 canvasPosition)
    {
        Vector3 worldPosition = player.position + new Vector3(worldOffset.x, worldOffset.y, 0f);
        Vector3 screenPosition = targetCamera.WorldToScreenPoint(worldPosition);
        if (screenPosition.z < 0f)
        {
            canvasPosition = Vector2.zero;
            return false;
        }

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            null,
            out canvasPosition);
    }

    private bool IsPanelFullyVisible(Vector2 canvasPosition)
    {
        return (ClampToCanvas(canvasPosition) - canvasPosition).sqrMagnitude < 0.01f;
    }

    private Vector2 ClampToCanvas(Vector2 canvasPosition)
    {
        Vector2 halfCanvas = canvasRect.rect.size * 0.5f;
        Vector2 halfPanel = panel.rect.size * 0.5f;
        float minX = -halfCanvas.x + screenPadding + halfPanel.x;
        float maxX = halfCanvas.x - screenPadding - halfPanel.x;
        float minY = -halfCanvas.y + screenPadding + halfPanel.y;
        float maxY = halfCanvas.y - screenPadding - halfPanel.y;
        canvasPosition.x = Mathf.Clamp(canvasPosition.x, minX, maxX);
        canvasPosition.y = Mathf.Clamp(canvasPosition.y, minY, maxY);
        return canvasPosition;
    }

    private Button FindButton(string objectName)
    {
        return transform.Find(objectName)?.GetComponent<Button>();
    }

    private static TextMeshProUGUI FindText(Component root, string objectName)
    {
        return root != null ? FindText(root.transform, objectName) : null;
    }

    private static TextMeshProUGUI FindText(Transform root, string objectName)
    {
        return root != null ? root.Find(objectName)?.GetComponent<TextMeshProUGUI>() : null;
    }

    private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
            button.onClick.AddListener(action);
    }

    private static void RemoveListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
            button.onClick.RemoveListener(action);
    }

    private static void SetText(TextMeshProUGUI text, string value)
    {
        if (text != null)
            text.text = value;
    }

    private static string GetWaveLevelText(int level)
    {
        return level > 0 ? $"Lv.{level}" : "-";
    }

    private void RefreshButtonColor(Button button, bool enabled)
    {
        if (button != null && button.targetGraphic != null)
            button.targetGraphic.color = enabled ? enabledButtonColor : disabledButtonColor;
    }

    private static void SetReinforceStatus(TextMeshProUGUI statusText, string label, bool enabled)
    {
        if (statusText == null)
            return;

        statusText.text = label + ": " + (enabled ? "ON" : "OFF");
        statusText.color = enabled
            ? new Color(0.35f, 1f, 0.55f, 1f)
            : new Color(0.7f, 0.72f, 0.76f, 1f);
    }

    private void OnDestroy()
    {
        visibilitySequence?.Kill();
        borderTween?.Kill();
        RemoveListener(attackPowerButton, UpgradeAttackPower);
        RemoveListener(darknessResistanceButton, UpgradeDarknessResistance);
        RemoveListener(waveSwordButton, UpgradeWaveSword);
        RemoveListener(waveImpactButton, UpgradeWaveImpact);
        RemoveListener(soundBarrierButton, EnableSoundBarrier);

        if (playerHealth != null)
            playerHealth.SoundBarrierChanged -= HandleSoundBarrierChanged;
    }
}
