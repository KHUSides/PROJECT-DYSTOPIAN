using Dystopian.SuEun.StageFlow;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class RemainEnemyUI : MonoBehaviour
{
    [SerializeField] private Text remainEnemyText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeSpeed = 5f;
    [SerializeField] private float slideSpeed = 520f;
    [SerializeField] private float enterOffset = 80f;

    private StageFlowManager stageFlowManager;
    private RectTransform rectTransform;
    private Vector2 visiblePosition;
    private Vector2 hiddenPosition;
    private bool shouldShow;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        visiblePosition = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
        hiddenPosition = visiblePosition + Vector2.up * enterOffset;

        if (rectTransform != null)
            rectTransform.anchoredPosition = hiddenPosition;

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    private void Start()
    {
        ConnectToStageFlow();
    }

    private void Update()
    {
        if (stageFlowManager == null)
            ConnectToStageFlow();

        if (canvasGroup != null)
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, shouldShow ? 1f : 0f, fadeSpeed * Time.unscaledDeltaTime);

        if (rectTransform != null)
        {
            Vector2 targetPosition = shouldShow ? visiblePosition : hiddenPosition;
            rectTransform.anchoredPosition = Vector2.MoveTowards(
                rectTransform.anchoredPosition,
                targetPosition,
                slideSpeed * Time.unscaledDeltaTime);
        }
    }

    private void OnDestroy()
    {
        DisconnectFromStageFlow();
    }

    public void Configure(Text text, CanvasGroup group)
    {
        remainEnemyText = text;
        canvasGroup = group;
    }

    private void ConnectToStageFlow()
    {
        if (stageFlowManager != null)
            return;

        stageFlowManager = FindFirstObjectByType<StageFlowManager>();
        if (stageFlowManager == null)
            return;

        stageFlowManager.BattleStarted += HandleBattleStarted;
        stageFlowManager.BattleCleared += HandleBattleCleared;
        stageFlowManager.RemainingEnemyCountChanged += HandleRemainingEnemyCountChanged;

        if (stageFlowManager.ActiveZone != null && stageFlowManager.ActiveZone.IsRunning)
            HandleBattleStarted(stageFlowManager.ActiveZone);
    }

    private void DisconnectFromStageFlow()
    {
        if (stageFlowManager == null)
            return;

        stageFlowManager.BattleStarted -= HandleBattleStarted;
        stageFlowManager.BattleCleared -= HandleBattleCleared;
        stageFlowManager.RemainingEnemyCountChanged -= HandleRemainingEnemyCountChanged;
        stageFlowManager = null;
    }

    private void HandleBattleStarted(BattleZone zone)
    {
        UpdateCount(zone != null ? zone.RemainingEnemyCount : 0);
        shouldShow = true;
    }

    private void HandleBattleCleared(BattleZone zone)
    {
        shouldShow = false;
    }

    private void HandleRemainingEnemyCountChanged(BattleZone zone, int remainingEnemyCount)
    {
        if (stageFlowManager != null && stageFlowManager.ActiveZone != null && zone != stageFlowManager.ActiveZone)
            return;

        UpdateCount(remainingEnemyCount);
    }

    private void UpdateCount(int remainingEnemyCount)
    {
        if (remainEnemyText != null)
            remainEnemyText.text = "  Remain :" + "\n" + Mathf.Max(0, remainingEnemyCount);
    }
}
