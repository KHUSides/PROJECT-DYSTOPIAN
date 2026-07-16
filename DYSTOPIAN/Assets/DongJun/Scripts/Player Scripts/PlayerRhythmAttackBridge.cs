using Dystopian.Rhythm;
using UnityEngine;

[DisallowMultipleComponent]
// Routes rhythm events to the player attack API and owns optional fallback input.
public sealed class PlayerRhythmAttackBridge : MonoBehaviour
{
    [Header("Fallback")]
    [Tooltip("Allows A/D attacks while the rhythm chart is stopped.")]
    [SerializeField] private bool allowAttacksWhileRhythmStopped;
    [SerializeField] private KeyCode fallbackSingleAttackKey = KeyCode.A;
    [SerializeField] private KeyCode fallbackChargedAttackKey = KeyCode.D;

    private RhythmSystem rhythmSystem;
    private PlayerController playerController;
    private bool subscribed;

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
            playerController.ReleaseChargedAttack();
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
        playerController.ReleaseChargedAttack();
    }

    private void HandleChartStopped()
    {
        playerController.CancelChargedAttack();
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
