using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class PlayerWaveSwordGauge : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] private Color emptyColor = new Color(0.42f, 0.46f, 0.5f, 0.55f);
    [SerializeField] private Color chargeColor = new Color(0.1f, 0.68f, 1f, 0.95f);
    [SerializeField] private Color readyOutlineColor = new Color(0.25f, 0.85f, 1f, 1f);

    [Header("Ready Effect")]
    [SerializeField, Min(0.05f)] private float readyPulseDuration = 0.45f;

    [Header("Consume Effect")]
    [SerializeField, Min(0.01f)] private float consumeDuration = 0.12f;
    [SerializeField] private Vector2 shakeStrength = new Vector2(7f, 5f);
    [SerializeField, Min(1)] private int shakeVibrato = 18;
    [SerializeField, Range(0f, 180f)] private float shakeRandomness = 70f;

    private PlayerController playerController;
    private RectTransform gaugeRect;
    private CanvasGroup canvasGroup;
    private Image emptyImage;
    private Image chargeImage;
    private Outline readyOutline;
    private Tween readyTween;
    private Sequence consumeSequence;
    private Vector2 baseAnchoredPosition;
    private bool isReady;
    private bool isConsuming;

    private void Awake()
    {
        gaugeRect = transform as RectTransform;
        canvasGroup = GetComponent<CanvasGroup>();
        emptyImage = transform.Find("Empty")?.GetComponent<Image>();
        chargeImage = transform.Find("Charge")?.GetComponent<Image>();
        readyOutline = emptyImage != null ? emptyImage.GetComponent<Outline>() : null;
        baseAnchoredPosition = gaugeRect != null ? gaugeRect.anchoredPosition : Vector2.zero;

        if (emptyImage != null)
            emptyImage.color = emptyColor;

        if (chargeImage != null)
        {
            chargeImage.color = chargeColor;
            chargeImage.fillAmount = 0f;
        }

        SetVisible(false);
        SetReady(false);
    }

    public void Initialize(PlayerController ownerController)
    {
        if (playerController == ownerController)
            return;

        DisconnectController();
        playerController = ownerController;

        if (playerController != null)
            playerController.NormalAttackPerformed += HandleNormalAttackPerformed;

        RefreshGauge();
    }

    private void Update()
    {
        RefreshGauge();
    }

    private void RefreshGauge()
    {
        bool isEnabled = playerController != null && playerController.IsAttackReinforceEnabled;
        SetVisible(isEnabled);

        if (!isEnabled)
        {
            if (!isConsuming && chargeImage != null)
                chargeImage.fillAmount = 0f;

            SetReady(false);
            return;
        }

        if (isConsuming || chargeImage == null)
            return;

        float charge = Mathf.Clamp01(playerController.AttackReinforceChargePercent * 0.01f);
        chargeImage.fillAmount = charge;
        SetReady(charge >= 1f);
    }

    private void HandleNormalAttackPerformed()
    {
        if (playerController == null || !playerController.LastNormalAttackWasEmpowered)
            return;

        PlayConsumeEffect();
    }

    private void PlayConsumeEffect()
    {
        consumeSequence?.Kill();
        readyTween?.Kill();
        readyTween = null;
        isConsuming = true;
        isReady = false;

        if (readyOutline != null)
            readyOutline.effectColor = WithAlpha(readyOutlineColor, 0f);

        if (chargeImage != null)
            chargeImage.fillAmount = 1f;

        if (gaugeRect != null)
            gaugeRect.anchoredPosition = baseAnchoredPosition;

        consumeSequence = DOTween.Sequence().SetUpdate(true);
        if (chargeImage != null)
            consumeSequence.Join(chargeImage.DOFillAmount(0f, consumeDuration).SetEase(Ease.InQuad));

        if (gaugeRect != null)
        {
            consumeSequence.Join(gaugeRect.DOShakeAnchorPos(
                consumeDuration,
                shakeStrength,
                shakeVibrato,
                shakeRandomness,
                false,
                true));
        }

        consumeSequence.OnComplete(CompleteConsumeEffect);
    }

    private void CompleteConsumeEffect()
    {
        isConsuming = false;
        consumeSequence = null;

        if (gaugeRect != null)
            gaugeRect.anchoredPosition = baseAnchoredPosition;

        RefreshGauge();
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void SetReady(bool ready)
    {
        if (isReady == ready)
            return;

        isReady = ready;
        readyTween?.Kill();
        readyTween = null;

        if (readyOutline == null)
            return;

        if (!ready)
        {
            readyOutline.effectColor = WithAlpha(readyOutlineColor, 0f);
            return;
        }

        readyOutline.effectColor = WithAlpha(readyOutlineColor, 0.25f);
        readyTween = DOTween.To(
                () => readyOutline.effectColor,
                color => readyOutline.effectColor = color,
                readyOutlineColor,
                readyPulseDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private void DisconnectController()
    {
        if (playerController != null)
            playerController.NormalAttackPerformed -= HandleNormalAttackPerformed;
    }

    private void OnEnable()
    {
        if (playerController != null)
            RefreshGauge();
    }

    private void OnDisable()
    {
        readyTween?.Kill();
        consumeSequence?.Kill();
        readyTween = null;
        consumeSequence = null;
        isReady = false;
        isConsuming = false;

        if (gaugeRect != null)
            gaugeRect.anchoredPosition = baseAnchoredPosition;

        if (readyOutline != null)
            readyOutline.effectColor = WithAlpha(readyOutlineColor, 0f);
    }

    private void OnDestroy()
    {
        DisconnectController();
        readyTween?.Kill();
        consumeSequence?.Kill();
    }
}
