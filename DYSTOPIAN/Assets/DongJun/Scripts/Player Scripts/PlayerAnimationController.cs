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
    private static readonly int ChargedAttackHash = Animator.StringToHash("ChargedAttack");
    private static readonly int JumpStartedHash = Animator.StringToHash("JumpStarted");
    private static readonly int ChargePrepareIdleStateHash =
        Animator.StringToHash("Base Layer.Charge Prepare Idle");
    private static readonly int ChargePrepareWalkStateHash =
        Animator.StringToHash("Base Layer.Charge Prepare Walk");

    [Header("Facing")]
    [SerializeField] private float facingRightYaw = 90f;
    [SerializeField] private float facingLeftYaw = -90f;

    private PlayerController playerController;
    private RhythmSystem rhythmSystem;
    private Animator animator;
    private Transform visualRoot;
    private bool useFirstAttack = true;
    private int appliedFacingSign;

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
        EnforceChargedAttackAnimation();
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

    private void HandleChargedAttack()
    {
        animator.SetTrigger(ChargedAttackHash);
    }

    private void HandleJumpStarted()
    {
        if (playerController.IsCharging)
            return;

        animator.SetTrigger(JumpStartedHash);
    }

    // Keeps charge preparation above locomotion states, including airborne states.
    private void EnforceChargedAttackAnimation()
    {
        if (!playerController.IsCharging)
            return;

        int targetStateHash = playerController.HorizontalSpeed > 0.1f
            ? ChargePrepareWalkStateHash
            : ChargePrepareIdleStateHash;

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        if (currentState.fullPathHash == targetStateHash)
            return;

        if (animator.IsInTransition(0))
        {
            AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);
            if (nextState.fullPathHash == targetStateHash)
                return;
        }

        animator.CrossFade(targetStateHash, 0.05f, 0);
    }

    private void UpdateFacing()
    {
        if (visualRoot == null)
            return;

        int facingSign = playerController.FacingDirection.x < 0f ? -1 : 1;
        if (facingSign == appliedFacingSign)
            return;

        appliedFacingSign = facingSign;
        float yaw = facingSign < 0 ? facingLeftYaw : facingRightYaw;
        visualRoot.localRotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private void OnDisable()
    {
        if (playerController != null)
        {
            playerController.NormalAttackPerformed -= HandleNormalAttack;
            playerController.ChargedAttackPerformed -= HandleChargedAttack;
            playerController.JumpStarted -= HandleJumpStarted;
        }
    }

    private void Subscribe()
    {
        if (playerController == null)
            return;

        playerController.NormalAttackPerformed -= HandleNormalAttack;
        playerController.NormalAttackPerformed += HandleNormalAttack;
        playerController.ChargedAttackPerformed -= HandleChargedAttack;
        playerController.ChargedAttackPerformed += HandleChargedAttack;
        playerController.JumpStarted -= HandleJumpStarted;
        playerController.JumpStarted += HandleJumpStarted;
    }
}
