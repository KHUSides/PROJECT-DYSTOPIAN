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
    private bool isVisible;

    private void Awake()
    {
        isVisible = isVisibleOnStart;
    }

    private void Start()
    {
        ResolveReferences();
        SetVisible(isVisible);
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

        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        player = playerController != null ? playerController.transform : null;
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
}
