using UnityEngine;

public class PlayerUpgradeUI : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private RectTransform panel;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private Vector2 topRightOffset = new Vector2(1.35f, 1.75f);
    [SerializeField] private Vector2 topLeftOffset = new Vector2(-1.35f, 1.75f);
    [SerializeField] private Vector2 bottomRightOffset = new Vector2(1.35f, -0.85f);
    [SerializeField] private Vector2 bottomLeftOffset = new Vector2(-1.35f, -0.85f);
    [SerializeField] private float screenPadding = 24f;
    [SerializeField] private KeyCode toggleKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode alternateToggleKey = KeyCode.Keypad1;
    [SerializeField] private bool isVisibleOnStart = false;

    private readonly Vector2[] cachedOffsets = new Vector2[4];
    private bool isVisible;

    private void Awake()
    {
        ResolveReferences(true);
        SetVisible(isVisibleOnStart);
    }

    private void LateUpdate()
    {
        HandleToggleInput();

        if (!isVisible)
            return;

        ResolveReferences(false);
        UpdatePanelPosition();
    }

    private void HandleToggleInput()
    {
        if (!Input.GetKeyDown(toggleKey) && !Input.GetKeyDown(alternateToggleKey))
            return;

        SetVisible(!isVisible);
    }

    private void SetVisible(bool visible)
    {
        isVisible = visible;

        if (panelCanvasGroup == null && panel != null)
            panelCanvasGroup = panel.GetComponent<CanvasGroup>();

        if (panelCanvasGroup == null)
            return;

        panelCanvasGroup.alpha = isVisible ? 1f : 0f;
        panelCanvasGroup.interactable = isVisible;
        panelCanvasGroup.blocksRaycasts = isVisible;
    }

    private void ResolveReferences(bool allowSceneSearch)
    {
        if (player == null && allowSceneSearch)
        {
            GameObject playerObject = GameObject.Find("Player");

            if (playerObject != null)
                player = playerObject.transform;
        }

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (panel == null)
            panel = transform as RectTransform;

        if (panelCanvasGroup == null && panel != null)
        {
            panelCanvasGroup = panel.GetComponent<CanvasGroup>();

            if (panelCanvasGroup == null)
                panelCanvasGroup = panel.gameObject.AddComponent<CanvasGroup>();
        }

        if (canvasRect == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();

            if (canvas != null)
                canvasRect = canvas.transform as RectTransform;
        }
    }

    private void UpdatePanelPosition()
    {
        if (player == null || targetCamera == null || canvasRect == null || panel == null)
            return;

        cachedOffsets[0] = topRightOffset;
        cachedOffsets[1] = topLeftOffset;
        cachedOffsets[2] = bottomRightOffset;
        cachedOffsets[3] = bottomLeftOffset;

        Vector2 fallbackAnchoredPosition = Vector2.zero;

        for (int i = 0; i < cachedOffsets.Length; i++)
        {
            if (!TryGetAnchoredPosition(cachedOffsets[i], out Vector2 anchoredPosition))
                continue;

            if (i == 0)
                fallbackAnchoredPosition = anchoredPosition;

            if (IsPanelFullyVisible(anchoredPosition))
            {
                panel.anchoredPosition = anchoredPosition;
                return;
            }
        }

        panel.anchoredPosition = ClampAnchoredPositionToCanvas(fallbackAnchoredPosition);
    }

    private bool TryGetAnchoredPosition(Vector2 worldOffset, out Vector2 anchoredPosition)
    {
        Vector3 worldPosition = player.position + new Vector3(worldOffset.x, worldOffset.y, 0f);
        Vector3 screenPosition = targetCamera.WorldToScreenPoint(worldPosition);

        if (screenPosition.z < 0f)
        {
            anchoredPosition = Vector2.zero;
            return false;
        }

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            null,
            out anchoredPosition
        );
    }

    private bool IsPanelFullyVisible(Vector2 anchoredPosition)
    {
        Vector2 halfCanvas = canvasRect.rect.size * 0.5f;
        Vector2 halfPanel = panel.rect.size * 0.5f;

        float minX = -halfCanvas.x + screenPadding + halfPanel.x;
        float maxX = halfCanvas.x - screenPadding - halfPanel.x;
        float minY = -halfCanvas.y + screenPadding + halfPanel.y;
        float maxY = halfCanvas.y - screenPadding - halfPanel.y;

        return anchoredPosition.x >= minX
            && anchoredPosition.x <= maxX
            && anchoredPosition.y >= minY
            && anchoredPosition.y <= maxY;
    }

    private Vector2 ClampAnchoredPositionToCanvas(Vector2 anchoredPosition)
    {
        Vector2 halfCanvas = canvasRect.rect.size * 0.5f;
        Vector2 halfPanel = panel.rect.size * 0.5f;

        float minX = -halfCanvas.x + screenPadding + halfPanel.x;
        float maxX = halfCanvas.x - screenPadding - halfPanel.x;
        float minY = -halfCanvas.y + screenPadding + halfPanel.y;
        float maxY = halfCanvas.y - screenPadding - halfPanel.y;

        anchoredPosition.x = Mathf.Clamp(anchoredPosition.x, minX, maxX);
        anchoredPosition.y = Mathf.Clamp(anchoredPosition.y, minY, maxY);
        return anchoredPosition;
    }
}
