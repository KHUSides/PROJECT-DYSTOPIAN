using UnityEngine;

public class PlayerUpgradeUI : MonoBehaviour
{
    [Header("Follow Positions")]
    [UnityEngine.Serialization.FormerlySerializedAs("topRightOffset_")]
    [SerializeField] private Vector2 topRightOffset = new Vector2(1.35f, 1.75f);
    [UnityEngine.Serialization.FormerlySerializedAs("topLeftOffset_")]
    [SerializeField] private Vector2 topLeftOffset = new Vector2(-1.35f, 1.75f);
    [UnityEngine.Serialization.FormerlySerializedAs("bottomRightOffset_")]
    [SerializeField] private Vector2 bottomRightOffset = new Vector2(1.35f, -0.85f);
    [UnityEngine.Serialization.FormerlySerializedAs("bottomLeftOffset_")]
    [SerializeField] private Vector2 bottomLeftOffset = new Vector2(-1.35f, -0.85f);
    [UnityEngine.Serialization.FormerlySerializedAs("screenPadding_")]
    [SerializeField] private float screenPadding = 24f;

    [Header("Toggle")]
    [UnityEngine.Serialization.FormerlySerializedAs("toggleKey_")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Alpha1;
    [UnityEngine.Serialization.FormerlySerializedAs("alternateToggleKey_")]
    [SerializeField] private KeyCode alternateToggleKey = KeyCode.Keypad1;
    [UnityEngine.Serialization.FormerlySerializedAs("isVisibleOnStart_")]
    [SerializeField] private bool isVisibleOnStart = false;

    private readonly Vector2[] positionCandidates = new Vector2[4];
    private Transform player;
    private Camera targetCamera;
    private RectTransform canvasRect;
    private RectTransform panel;
    private CanvasGroup panelCanvasGroup;
    private UnityEngine.UI.Button chargedAttackRepeatButton;
    private UnityEngine.UI.Button distanceChargeButton;
    private PlayerRhythmAttackBridge rhythmAttackBridge;
    private PlayerController playerController;
    private TMPro.TextMeshProUGUI chargeReinforceStatusText;
    private TMPro.TextMeshProUGUI attackReinforceStatusText;
    private bool isVisible;

    private void Awake()
    {
        isVisible = isVisibleOnStart;
    }

    private void Start()
    {
        ResolveReferences();
        ConnectUpgradeButtons();
        SetVisible(isVisible);
        UpdateReinforceStatus();
    }

    private void Update()
    {
        HandleToggleInput();
    }

    private void LateUpdate()
    {
        if (isVisible)
            UpdatePanelPosition();
    }

    private void HandleToggleInput()
    {
        if (Input.GetKeyDown(toggleKey) || Input.GetKeyDown(alternateToggleKey))
            SetVisible(!isVisible);
    }

    private void SetVisible(bool visible)
    {
        isVisible = visible;

        if (panelCanvasGroup == null)
            return;

        panelCanvasGroup.alpha = isVisible ? 1f : 0f;
        panelCanvasGroup.interactable = isVisible;
        panelCanvasGroup.blocksRaycasts = isVisible;
    }

    private void ResolveReferences()
    {
        panel = transform as RectTransform;
        panelCanvasGroup = GetComponent<CanvasGroup>();

        if (panelCanvasGroup == null)
            panelCanvasGroup = gameObject.AddComponent<CanvasGroup>();

        Canvas canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        targetCamera = Camera.main;

        Transform statusRoot = canvas != null ? canvas.transform.Find("ReinforceStatus") : null;
        chargeReinforceStatusText = statusRoot != null
            ? statusRoot.Find("Charge Reinforce Status")?.GetComponent<TMPro.TextMeshProUGUI>()
            : null;
        attackReinforceStatusText = statusRoot != null
            ? statusRoot.Find("Attack Reinforce Status")?.GetComponent<TMPro.TextMeshProUGUI>()
            : null;

        playerController = FindFirstObjectByType<PlayerController>();
        player = playerController != null ? playerController.transform : null;
    }

    private void ConnectUpgradeButtons()
    {
        chargedAttackRepeatButton = transform.Find("UpgradeButton_1")?.GetComponent<UnityEngine.UI.Button>();
        distanceChargeButton = transform.Find("UpgradeButton_2")?.GetComponent<UnityEngine.UI.Button>();
        rhythmAttackBridge = FindFirstObjectByType<PlayerRhythmAttackBridge>();

        if (chargedAttackRepeatButton != null && rhythmAttackBridge != null)
            chargedAttackRepeatButton.onClick.AddListener(ToggleChargedAttackRepeat);
        else
            Debug.LogError("[PlayerUpgradeUI] UpgradeButton_1 and PlayerRhythmAttackBridge are required.", this);

        if (distanceChargeButton != null && playerController != null)
            distanceChargeButton.onClick.AddListener(ToggleDistanceChargeUpgrade);
        else
            Debug.LogError("[PlayerUpgradeUI] UpgradeButton_2 and PlayerController are required.", this);
    }

    private void ToggleChargedAttackRepeat()
    {
        rhythmAttackBridge.ToggleChargeReinforce();
        UpdateReinforceStatus();
    }

    private void ToggleDistanceChargeUpgrade()
    {
        playerController.ToggleAttackReinforce();
        UpdateReinforceStatus();
    }

    private void UpdateReinforceStatus()
    {
        SetReinforceStatus(
            chargeReinforceStatusText,
            "Charge Reinforce",
            rhythmAttackBridge != null && rhythmAttackBridge.IsChargeReinforceEnabled);
        SetReinforceStatus(
            attackReinforceStatusText,
            "Attack Reinforce",
            playerController != null && playerController.IsAttackReinforceEnabled);
    }

    private static void SetReinforceStatus(
        TMPro.TextMeshProUGUI statusText,
        string label,
        bool enabled)
    {
        if (statusText == null)
            return;

        statusText.text = label + ": " + (enabled ? "ON" : "OFF");
        statusText.color = enabled
            ? new Color(0.35f, 1f, 0.55f, 1f)
            : new Color(0.7f, 0.72f, 0.76f, 1f);
    }

    private void UpdatePanelPosition()
    {
        if (player == null || targetCamera == null || canvasRect == null || panel == null)
            return;

        positionCandidates[0] = topRightOffset;
        positionCandidates[1] = topLeftOffset;
        positionCandidates[2] = bottomRightOffset;
        positionCandidates[3] = bottomLeftOffset;

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

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, null, out canvasPosition);
    }

    private bool IsPanelFullyVisible(Vector2 canvasPosition)
    {
        Vector2 clamped = ClampToCanvas(canvasPosition);
        return (clamped - canvasPosition).sqrMagnitude < 0.01f;
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

    private void OnDestroy()
    {
        if (chargedAttackRepeatButton != null)
            chargedAttackRepeatButton.onClick.RemoveListener(ToggleChargedAttackRepeat);

        if (distanceChargeButton != null)
            distanceChargeButton.onClick.RemoveListener(ToggleDistanceChargeUpgrade);
    }
}
