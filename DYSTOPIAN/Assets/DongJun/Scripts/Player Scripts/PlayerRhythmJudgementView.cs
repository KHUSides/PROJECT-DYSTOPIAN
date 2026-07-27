using Dystopian.Rhythm;
using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerRhythmJudgementView : MonoBehaviour
{
    [Header("Template")]
    [SerializeField] private GameObject imageTemplate;
    [SerializeField, Min(1)] private int poolSize = 10;
    [SerializeField] private int sortingOrder = 200;

    [Header("Images")]
    [SerializeField] private Sprite criticalImage;
    [SerializeField] private Sprite coolImage;
    [SerializeField] private Sprite goodImage;
    [SerializeField] private Sprite badImage;
    [SerializeField] private Sprite missImage;

    [Header("Presentation")]
    [SerializeField, Min(0f)] private float visibleSeconds = 0.55f;
    [SerializeField] private Vector2 imageScale = Vector2.one;

    [Header("Animation")]
    [SerializeField, Min(0f)] private float amount = 1f;

    private RhythmSystem rhythmSystem;
    private JudgementItem[] items;
    private int nextItemIndex;
    private bool subscribed;

    private sealed class JudgementItem
    {
        private readonly GameObject gameObject_;
        private readonly Transform transform_;
        private readonly SpriteRenderer image_;
        private readonly Vector3 initialLocalPosition_;
        private Sequence sequence_;

        public bool IsVisible => gameObject_.activeSelf;

        public JudgementItem(GameObject gameObject, SpriteRenderer image)
        {
            gameObject_ = gameObject;
            transform_ = gameObject.transform;
            image_ = image;
            initialLocalPosition_ = transform_.localPosition;
        }

        public void Show(Sprite sprite, float duration, float movementAmount)
        {
            Hide();

            image_.sprite = sprite;
            image_.color = Color.white;
            image_.enabled = true;
            gameObject_.SetActive(true);

            sequence_ = DOTween.Sequence()
                .SetUpdate(true)
                .Join(image_.DOFade(0f, duration).SetEase(Ease.OutQuad))
                .Join(transform_
                    .DOLocalMoveY(initialLocalPosition_.y + movementAmount, duration)
                    .SetEase(Ease.OutSine))
                .OnComplete(Complete);
        }

        public void Hide()
        {
            sequence_?.Kill();
            sequence_ = null;
            Reset();
        }

        private void Complete()
        {
            sequence_ = null;
            Reset();
        }

        private void Reset()
        {
            transform_.localPosition = initialLocalPosition_;
            image_.color = Color.white;
            image_.enabled = false;
            gameObject_.SetActive(false);
        }
    }

    private void Awake()
    {
        if (imageTemplate == null)
        {
            Debug.LogError("[PlayerRhythmJudgementView] An image template is required.", this);
            enabled = false;
            return;
        }

        PrepareImageTemplate();
        CreateImagePool();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        rhythmSystem = FindFirstObjectByType<RhythmSystem>();
        if (rhythmSystem == null)
        {
            Debug.LogError("[PlayerRhythmJudgementView] RhythmSystem is required.", this);
            enabled = false;
            return;
        }

        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
        HideAll();
    }

    private void Show(RhythmJudgement rating, float _)
    {
        Sprite image = GetJudgementImage(rating);
        if (image == null)
            return;

        GetNextItem().Show(image, visibleSeconds, amount);
    }

    private void PrepareImageTemplate()
    {
        SpriteRenderer image = imageTemplate.GetComponentInChildren<SpriteRenderer>(true);
        Transform imageTransform = image.transform;
        imageTransform.localScale = new Vector3(imageScale.x, imageScale.y, imageTransform.localScale.z);
        image.enabled = false;
        image.sortingOrder = sortingOrder;
        imageTemplate.SetActive(false);
    }

    private void CreateImagePool()
    {
        int safePoolSize = Mathf.Max(1, poolSize);
        items = new JudgementItem[safePoolSize];

        for (int i = 0; i < items.Length; i++)
        {
            GameObject itemObject = Instantiate(imageTemplate, imageTemplate.transform.parent);
            itemObject.name = $"Judgement Image {i + 1}";
            itemObject.SetActive(false);
            SpriteRenderer image = itemObject.GetComponentInChildren<SpriteRenderer>(true);
            items[i] = new JudgementItem(itemObject, image);
        }
    }

    private JudgementItem GetNextItem()
    {
        for (int i = 0; i < items.Length; i++)
        {
            int index = (nextItemIndex + i) % items.Length;
            if (items[index].IsVisible)
                continue;

            nextItemIndex = (index + 1) % items.Length;
            return items[index];
        }

        JudgementItem recycledItem = items[nextItemIndex];
        recycledItem.Hide();
        nextItemIndex = (nextItemIndex + 1) % items.Length;
        return recycledItem;
    }

    private Sprite GetJudgementImage(RhythmJudgement rating)
    {
        switch (rating)
        {
            case RhythmJudgement.Critical:
                return criticalImage;
            case RhythmJudgement.Cool:
                return coolImage;
            case RhythmJudgement.Good:
                return goodImage;
            case RhythmJudgement.Bad:
                return badImage;
            case RhythmJudgement.Miss:
                return missImage;
            default:
                return null;
        }
    }

    private void HideAll()
    {
        if (items == null)
            return;

        for (int i = 0; i < items.Length; i++)
            items[i].Hide();
    }

    private void Subscribe()
    {
        if (subscribed || rhythmSystem == null)
            return;

        rhythmSystem.JudgementPerformed += Show;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || rhythmSystem == null)
            return;

        rhythmSystem.JudgementPerformed -= Show;
        subscribed = false;
    }
}
