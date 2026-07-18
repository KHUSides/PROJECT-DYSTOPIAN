using Dystopian.Rhythm;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController))]
public sealed class PlayerAnimationController : MonoBehaviour
{
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int ChargingHash = Animator.StringToHash("Charging");
    private static readonly int RhythmActiveHash = Animator.StringToHash("RhythmActive");
    private static readonly int Attack1Hash = Animator.StringToHash("Attack1");
    private static readonly int Attack2Hash = Animator.StringToHash("Attack2");

    [Header("Facing")]
    [SerializeField] private float facingRightYaw = 90f;
    [SerializeField] private float facingLeftYaw = -90f;

    private PlayerController playerController;
    private RhythmSystem rhythmSystem;
    private Animator animator;
    private Transform visualRoot;
    private bool useFirstAttack = true;

    private void Start()
    {
        playerController = GetComponent<PlayerController>();
        rhythmSystem = FindFirstObjectByType<RhythmSystem>();
        animator = GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogError("[PlayerAnimationController] A child Animator is required.", this);
            enabled = false;
            return;
        }

        visualRoot = animator.transform;
        animator.applyRootMotion = false;
        Subscribe();
        UpdateFacing();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void Update()
    {
        animator.SetFloat(SpeedHash, playerController.HorizontalSpeed);
        animator.SetFloat(VerticalSpeedHash, playerController.VerticalSpeed);
        animator.SetBool(GroundedHash, playerController.IsGrounded);
        animator.SetBool(ChargingHash, playerController.IsCharging);
        animator.SetBool(RhythmActiveHash, rhythmSystem != null && rhythmSystem.IsChartActive);
    }

    private void LateUpdate()
    {
        UpdateFacing();
    }

    private void HandleNormalAttack()
    {
        animator.SetTrigger(useFirstAttack ? Attack1Hash : Attack2Hash);
        useFirstAttack = !useFirstAttack;
    }

    private void UpdateFacing()
    {
        if (visualRoot == null)
            return;

        float yaw = playerController.FacingDirection.x < 0f ? facingLeftYaw : facingRightYaw;
        visualRoot.localRotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private void OnDisable()
    {
        if (playerController != null)
            playerController.NormalAttackPerformed -= HandleNormalAttack;
    }

    private void Subscribe()
    {
        if (playerController == null)
            return;

        playerController.NormalAttackPerformed -= HandleNormalAttack;
        playerController.NormalAttackPerformed += HandleNormalAttack;
    }
}
