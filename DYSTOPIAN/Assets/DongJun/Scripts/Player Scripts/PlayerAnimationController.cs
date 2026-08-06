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

    [Header("Frame Rate")]
    [SerializeField] private bool limitAnimationFrameRate = true;
    [SerializeField, Range(1f, 60f)] private float animationFrameRate = 12f;

    private PlayerController playerController;
    private RhythmSystem rhythmSystem;
    private Animator animator;
    private Transform visualRoot;
    private bool useFirstAttack = true;
    private int appliedFacingSign;
    private float normalAnimatorSpeed = 1f;
    private float animationUpdateTimer;
    private bool animatorInitialized;
    private bool frameRateLimitApplied;

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

        normalAnimatorSpeed = animator.speed;
        animatorInitialized = true;
        SetFrameRateLimit(limitAnimationFrameRate);
        visualRoot = animator.transform;
        Subscribe();
        UpdateFacing();
    }

    private void OnEnable()
    {
        Subscribe();

        if (animatorInitialized)
            SetFrameRateLimit(limitAnimationFrameRate);
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
        UpdateAnimationFrame();
        UpdateFacing();
    }

    private void UpdateAnimationFrame()
    {
        if (!animatorInitialized)
            return;

        if (frameRateLimitApplied != limitAnimationFrameRate)
            SetFrameRateLimit(limitAnimationFrameRate);

        if (!frameRateLimitApplied)
            return;

        float frameInterval = 1f / Mathf.Max(1f, animationFrameRate);
        animationUpdateTimer += Time.deltaTime;
        if (animationUpdateTimer < frameInterval)
            return;

        float animationDeltaTime = animationUpdateTimer;
        animationUpdateTimer %= frameInterval;
        animator.speed = normalAnimatorSpeed;
        animator.Update(animationDeltaTime);
        animator.speed = 0f;
    }

    private void SetFrameRateLimit(bool shouldLimit)
    {
        if (animator == null || frameRateLimitApplied == shouldLimit)
            return;

        if (shouldLimit)
        {
            normalAnimatorSpeed = animator.speed > 0f ? animator.speed : normalAnimatorSpeed;
            animator.speed = 0f;
            animationUpdateTimer = 0f;
        }
        else
        {
            animator.speed = normalAnimatorSpeed;
            animationUpdateTimer = 0f;
        }

        frameRateLimitApplied = shouldLimit;
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
        SetFrameRateLimit(false);

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
