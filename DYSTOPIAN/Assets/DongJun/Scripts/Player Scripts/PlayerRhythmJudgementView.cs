using Dystopian.Rhythm;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TextMeshPro))]
public sealed class PlayerRhythmJudgementView : MonoBehaviour
{
    [Header("Presentation")]
    [SerializeField, Min(0f)] private float visibleSeconds = 0.55f;

    private RhythmSystem rhythmSystem;
    private TextMeshPro label;
    private float hideAt;
    private bool subscribed;

    private void Awake()
    {
        label = GetComponent<TextMeshPro>();
        Hide();
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
        Hide();
    }

    private void LateUpdate()
    {
        if (label.enabled && Time.unscaledTime >= hideAt)
            Hide();
    }

    private void Show(RhythmJudgement rating, float _)
    {
        label.text = rating.ToString().ToUpperInvariant();
        label.color = rhythmSystem.GetJudgementColor(rating);
        label.enabled = true;
        hideAt = Time.unscaledTime + visibleSeconds;
    }

    private void Hide()
    {
        if (label != null)
            label.enabled = false;
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
