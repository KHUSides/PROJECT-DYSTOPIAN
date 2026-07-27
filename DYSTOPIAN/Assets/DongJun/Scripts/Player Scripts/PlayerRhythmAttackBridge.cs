using Dystopian.Rhythm;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerRhythmAttackBridge : MonoBehaviour
{
    private const float ChargedAttackRepeatDelayBeats = 0.5f;

    [Header("Charge Reinforce")]
    [SerializeField] private bool enableChargedAttackRepeat;

    [Header("Fallback")]
    [Tooltip("Allows A/D attacks while the rhythm chart is stopped.")]
    [SerializeField] private bool allowAttacksWhileRhythmStopped;
    [SerializeField] private KeyCode fallbackSingleAttackKey = KeyCode.A;
    [SerializeField] private KeyCode fallbackChargedAttackKey = KeyCode.D;

    private RhythmSystem rhythmSystem;
    private PlayerController playerController;
    private bool subscribed;

    public bool IsChargeReinforceEnabled => enableChargedAttackRepeat;

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        rhythmSystem = GetComponent<RhythmSystem>();
        playerController = FindFirstObjectByType<PlayerController>();

        if (rhythmSystem == null || playerController == null)
        {
            Debug.LogError("[PlayerRhythmAttackBridge] RhythmSystem and PlayerController are required.", this);
            enabled = false;
            return;
        }

        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();

        if (playerController != null)
        {
            playerController.CancelChargedAttack();
            playerController.CancelPendingChargedAttackRepeats();
        }
    }

    private void Update()
    {
        if (!allowAttacksWhileRhythmStopped || rhythmSystem.IsChartActive)
        {
            return;
        }

        if (Input.GetKeyDown(fallbackSingleAttackKey))
        {
            playerController.PerformNormalAttack();
        }

        if (Input.GetKeyDown(fallbackChargedAttackKey))
        {
            playerController.BeginChargedAttack();
        }

        if (Input.GetKeyUp(fallbackChargedAttackKey))
        {
            ReleaseChargedAttack();
        }
    }

    private void HandleSingleAccepted()
    {
        playerController.PerformNormalAttack();
    }

    private void HandleLongStarted()
    {
        playerController.BeginChargedAttack();
    }

    private void HandleLongEnded()
    {
        ReleaseChargedAttack();
    }

    private void ReleaseChargedAttack()
    {
        if (!enableChargedAttackRepeat)
        {
            playerController.ReleaseChargedAttack();
            return;
        }

        float repeatDelaySeconds = ChargedAttackRepeatDelayBeats * 60f / rhythmSystem.Bpm;
        playerController.ReleaseChargedAttackWithRepeat(repeatDelaySeconds);
    }

    public void SetChargeReinforceEnabled(bool enabled)
    {
        if (enableChargedAttackRepeat == enabled)
            return;

        enableChargedAttackRepeat = enabled;
        if (!enableChargedAttackRepeat && playerController != null)
            playerController.CancelPendingChargedAttackRepeats();
    }

    public void ToggleChargeReinforce()
    {
        SetChargeReinforceEnabled(!enableChargedAttackRepeat);
    }

    private void HandleChartStopped()
    {
        playerController.CancelChargedAttack();
        playerController.CancelPendingChargedAttackRepeats();
    }

    private void Subscribe()
    {
        if (subscribed || rhythmSystem == null || playerController == null)
        {
            return;
        }

        rhythmSystem.PlayerSingleAccepted += HandleSingleAccepted;
        rhythmSystem.PlayerLongStarted += HandleLongStarted;
        rhythmSystem.PlayerLongEnded += HandleLongEnded;
        rhythmSystem.ChartStopped += HandleChartStopped;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed || rhythmSystem == null)
        {
            return;
        }

        rhythmSystem.PlayerSingleAccepted -= HandleSingleAccepted;
        rhythmSystem.PlayerLongStarted -= HandleLongStarted;
        rhythmSystem.PlayerLongEnded -= HandleLongEnded;
        rhythmSystem.ChartStopped -= HandleChartStopped;
        subscribed = false;
    }
}
