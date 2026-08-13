using UnityEngine;

namespace Dystopian.EnemyTest
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController))]
    public sealed class EnemyAnimationController : MonoBehaviour
    {
        private static readonly int MovingHash = Animator.StringToHash("Moving");
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int ChargingHash = Animator.StringToHash("Charging");
        private static readonly int DeadHash = Animator.StringToHash("Dead");
        private static readonly int SingleAttackHash = Animator.StringToHash("SingleAttack");
        private static readonly int ChargedAttackHash = Animator.StringToHash("ChargedAttack");
        private static readonly int HitHash = Animator.StringToHash("Hit");
        private static readonly int DieHash = Animator.StringToHash("Die");

        private const string IdleStateName = "Idle";
        private const string MoveStateName = "Move";
        private const string LongChargeStateName = "LongChargeStart";
        private const string SingleAttackStateName = "SingleNoteAttack";
        private const string ChargedAttackStateName = "LongNoteChargedAttack";
        private const string TurnLeftStateName = "TurnLeft";
        private const string TurnRightStateName = "TurnRight";
        private const string HitStateName = "Hit";
        private const string DeathStateName = "Death";

        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private EnemyController enemyController;
        [SerializeField] private EnemyHealth enemyHealth;
        [SerializeField] private EnemyMeleeCombat meleeCombat;
        [SerializeField] private EnemyRangedCombat rangedCombat;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float movingThreshold = 0.025f;
        [SerializeField, Min(0f)] private float speedDampSeconds = 0.08f;

        [Header("Playback")]
        [SerializeField, Min(0.01f)] private float normalSpeed = 1.2f;
        [SerializeField, Min(0.01f)] private float attackSpeed = 1.8f;
        [SerializeField, Min(0.01f)] private float hitSpeed = 1.4f;
        [SerializeField, Min(0.01f)] private float turnSpeed = 1.3f;
        [SerializeField, Min(0.01f)] private float deathSpeed = 1.15f;

        [Header("Action Locks")]
        [SerializeField, Min(0.01f)] private float singleAttackLockSeconds = 0.85f;
        [SerializeField, Min(0.01f)] private float chargedAttackLockSeconds = 0.95f;
        [SerializeField, Min(0.01f)] private float turnLockSeconds = 0.27f;
        [SerializeField, Min(0.01f)] private float hitLockSeconds = 0.25f;
        [SerializeField, Min(0f)] private float crossFadeSeconds = 0.04f;

        [Header("Attack Timing")]
        [SerializeField, Range(0f, 0.95f)] private float singleAttackStartNormalizedTime = 0.22f;
        [SerializeField, Range(0f, 0.95f)] private float longChargeStartNormalizedTime = 0.08f;
        [SerializeField, Range(0f, 0.95f)] private float chargedAttackStartNormalizedTime = 0.18f;

        private Vector3 lastPosition;
        private bool subscribed;
        private float actionLockedUntil;
        private string currentStateName;
        private int lastFacingSign = 1;

        private void Reset()
        {
            CacheReferences();
        }

        private void Awake()
        {
            CacheReferences();
            lastPosition = transform.position;
            lastFacingSign = GetCurrentFacingSign();
        }

        private void OnEnable()
        {
            CacheReferences();
            Subscribe();
        }

        private void Start()
        {
            CacheReferences();
            Subscribe();
            ConfigureAnimator();
            lastPosition = transform.position;
            lastFacingSign = GetCurrentFacingSign();
        }

        private void OnDisable()
        {
            Unsubscribe();

            if (enemyController != null)
            {
                enemyController.SetExternalMovementLock(false);
            }
        }

        private void Update()
        {
            if (animator == null)
            {
                return;
            }

            bool isDead = enemyController != null && enemyController.IsDead;
            bool isCharging = IsAnyCombatCharging();
            if (enemyController != null)
            {
                enemyController.SetExternalMovementLock(!isDead && isCharging);
            }

            float horizontalSpeed = Mathf.Abs(transform.position.x - lastPosition.x) / Mathf.Max(Time.deltaTime, 0.0001f);
            bool isMoving = !isDead && !isCharging && horizontalSpeed > movingThreshold;

            animator.SetBool(MovingHash, isMoving);
            animator.SetFloat(SpeedHash, horizontalSpeed, speedDampSeconds, Time.deltaTime);
            animator.SetBool(ChargingHash, !isDead && isCharging);
            animator.SetBool(DeadHash, isDead);

            if (isDead)
            {
                PlayState(DeathStateName, deathSpeed);
                lastPosition = transform.position;
                return;
            }

            if (Time.time < actionLockedUntil)
            {
                lastPosition = transform.position;
                return;
            }

            int currentFacingSign = GetCurrentFacingSign();
            if (lastFacingSign != currentFacingSign)
            {
                if (PlayState(currentFacingSign < lastFacingSign ? TurnLeftStateName : TurnRightStateName, turnSpeed))
                {
                    LockAction(turnLockSeconds);
                }

                lastFacingSign = currentFacingSign;
                lastPosition = transform.position;
                return;
            }

            if (isCharging)
            {
                HoldLongChargePose();
                lastPosition = transform.position;
                return;
            }

            PlayState(isMoving ? MoveStateName : IdleStateName);
            lastPosition = transform.position;
        }

        private void HandleSingleAttack()
        {
            if (!CanPlayAction())
            {
                return;
            }

            animator.ResetTrigger(HitHash);
            animator.ResetTrigger(SingleAttackHash);
            animator.ResetTrigger(ChargedAttackHash);
            animator.SetBool(ChargingHash, false);
            LockAttack(singleAttackLockSeconds);
            PlayStateImmediately(SingleAttackStateName, singleAttackStartNormalizedTime, attackSpeed);
        }

        private void HandleLongChargeStarted()
        {
            if (!CanPlayAction())
            {
                return;
            }

            animator.SetBool(ChargingHash, true);
            animator.ResetTrigger(HitHash);
            animator.ResetTrigger(SingleAttackHash);
            animator.ResetTrigger(ChargedAttackHash);
            enemyController?.SetExternalMovementLock(true);
            HoldLongChargePose();
        }

        private void HandleLongChargedAttack()
        {
            if (!CanPlayAction())
            {
                return;
            }

            animator.ResetTrigger(HitHash);
            animator.ResetTrigger(SingleAttackHash);
            animator.ResetTrigger(ChargedAttackHash);
            animator.SetBool(ChargingHash, false);
            enemyController?.SetExternalMovementLock(false);
            LockAttack(chargedAttackLockSeconds);
            PlayStateImmediately(ChargedAttackStateName, chargedAttackStartNormalizedTime, attackSpeed);
        }

        private void HandleLongChargeCancelled()
        {
            if (animator != null)
            {
                animator.SetBool(ChargingHash, false);
                currentStateName = string.Empty;
            }

            enemyController?.SetExternalMovementLock(false);
        }

        private void HandleDamaged(EnemyHealth _)
        {
            if (!CanPlayAction() || enemyHealth == null || !enemyHealth.IsAlive)
            {
                return;
            }

            animator.SetTrigger(HitHash);
            LockAction(hitLockSeconds);
            PlayState(HitStateName, hitSpeed);
        }

        private void HandleDied(EnemyHealth _)
        {
            if (animator == null)
            {
                return;
            }

            animator.SetBool(ChargingHash, false);
            animator.SetBool(MovingHash, false);
            animator.SetBool(DeadHash, true);
            animator.SetTrigger(DieHash);
            enemyController?.SetExternalMovementLock(false);
            actionLockedUntil = float.PositiveInfinity;
            PlayState(DeathStateName, deathSpeed);
        }

        private bool CanPlayAction()
        {
            return animator != null && (enemyHealth == null || enemyHealth.IsAlive);
        }

        private bool IsAnyCombatCharging()
        {
            return (meleeCombat != null && meleeCombat.IsCharging) ||
                   (rangedCombat != null && rangedCombat.IsCharging);
        }

        private void ConfigureAnimator()
        {
            if (animator == null)
            {
                return;
            }

            animator.applyRootMotion = false;
            animator.speed = normalSpeed;
            currentStateName = string.Empty;
        }

        private int GetCurrentFacingSign()
        {
            if (enemyController != null)
            {
                return enemyController.FacingSign >= 0 ? 1 : -1;
            }

            return transform.localScale.x >= 0f ? 1 : -1;
        }

        private void LockAction(float seconds)
        {
            actionLockedUntil = Time.time + Mathf.Max(0.01f, seconds);
        }

        private void LockAttack(float seconds)
        {
            float lockSeconds = Mathf.Max(0.01f, seconds);
            actionLockedUntil = Time.time + lockSeconds;
            enemyController?.LockMovement(lockSeconds);
        }

        private void HoldLongChargePose()
        {
            PlayStateImmediately(LongChargeStateName, longChargeStartNormalizedTime, 0f);
        }

        private bool PlayState(string stateName)
        {
            return PlayState(stateName, normalSpeed);
        }

        private bool PlayState(string stateName, float speed)
        {
            if (animator == null || string.IsNullOrEmpty(stateName) || currentStateName == stateName)
            {
                return false;
            }

            if (!TryGetStateHash(stateName, out int stateHash))
            {
                return false;
            }

            animator.speed = Mathf.Max(0f, speed);
            animator.CrossFadeInFixedTime(stateHash, crossFadeSeconds, 0, 0f);
            currentStateName = stateName;
            return true;
        }

        private bool PlayStateImmediately(string stateName, float normalizedTime)
        {
            return PlayStateImmediately(stateName, normalizedTime, normalSpeed);
        }

        private bool PlayStateImmediately(string stateName, float normalizedTime, float speed)
        {
            if (animator == null || string.IsNullOrEmpty(stateName))
            {
                return false;
            }

            if (!TryGetStateHash(stateName, out int stateHash))
            {
                return false;
            }

            animator.speed = Mathf.Max(0f, speed);
            animator.Play(stateHash, 0, Mathf.Clamp01(normalizedTime));
            animator.Update(0f);
            currentStateName = stateName;
            return true;
        }

        private bool TryGetStateHash(string stateName, out int stateHash)
        {
            stateHash = Animator.StringToHash(stateName);
            if (animator.HasState(0, stateHash))
            {
                return true;
            }

            stateHash = Animator.StringToHash("Base Layer." + stateName);
            return animator.HasState(0, stateHash);
        }

        private void CacheReferences()
        {
            if (enemyController == null)
            {
                enemyController = GetComponent<EnemyController>();
            }

            if (enemyHealth == null)
            {
                enemyHealth = GetComponent<EnemyHealth>();
            }

            if (meleeCombat == null)
            {
                meleeCombat = GetComponent<EnemyMeleeCombat>();
            }

            if (rangedCombat == null)
            {
                rangedCombat = GetComponent<EnemyRangedCombat>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }
        }

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            if (meleeCombat != null)
            {
                meleeCombat.SingleNoteAttackPerformed += HandleSingleAttack;
                meleeCombat.LongNoteChargeStarted += HandleLongChargeStarted;
                meleeCombat.LongNoteChargedAttackPerformed += HandleLongChargedAttack;
                meleeCombat.LongNoteChargeCancelled += HandleLongChargeCancelled;
            }

            if (rangedCombat != null)
            {
                rangedCombat.SingleNoteAttackPerformed += HandleSingleAttack;
                rangedCombat.LongNoteChargeStarted += HandleLongChargeStarted;
                rangedCombat.LongNoteChargedAttackPerformed += HandleLongChargedAttack;
                rangedCombat.LongNoteChargeCancelled += HandleLongChargeCancelled;
            }

            if (enemyHealth != null)
            {
                enemyHealth.Damaged += HandleDamaged;
                enemyHealth.Died += HandleDied;
            }

            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            if (meleeCombat != null)
            {
                meleeCombat.SingleNoteAttackPerformed -= HandleSingleAttack;
                meleeCombat.LongNoteChargeStarted -= HandleLongChargeStarted;
                meleeCombat.LongNoteChargedAttackPerformed -= HandleLongChargedAttack;
                meleeCombat.LongNoteChargeCancelled -= HandleLongChargeCancelled;
            }

            if (rangedCombat != null)
            {
                rangedCombat.SingleNoteAttackPerformed -= HandleSingleAttack;
                rangedCombat.LongNoteChargeStarted -= HandleLongChargeStarted;
                rangedCombat.LongNoteChargedAttackPerformed -= HandleLongChargedAttack;
                rangedCombat.LongNoteChargeCancelled -= HandleLongChargeCancelled;
            }

            if (enemyHealth != null)
            {
                enemyHealth.Damaged -= HandleDamaged;
                enemyHealth.Died -= HandleDied;
            }

            subscribed = false;
        }

        private void OnValidate()
        {
            movingThreshold = Mathf.Max(0f, movingThreshold);
            speedDampSeconds = Mathf.Max(0f, speedDampSeconds);
            normalSpeed = Mathf.Max(0.01f, normalSpeed);
            attackSpeed = Mathf.Max(0.01f, attackSpeed);
            hitSpeed = Mathf.Max(0.01f, hitSpeed);
            turnSpeed = Mathf.Max(0.01f, turnSpeed);
            deathSpeed = Mathf.Max(0.01f, deathSpeed);
            singleAttackLockSeconds = Mathf.Max(0.01f, singleAttackLockSeconds);
            chargedAttackLockSeconds = Mathf.Max(0.01f, chargedAttackLockSeconds);
            turnLockSeconds = Mathf.Max(0.01f, turnLockSeconds);
            hitLockSeconds = Mathf.Max(0.01f, hitLockSeconds);
            crossFadeSeconds = Mathf.Max(0f, crossFadeSeconds);
            singleAttackStartNormalizedTime = Mathf.Clamp01(singleAttackStartNormalizedTime);
            longChargeStartNormalizedTime = Mathf.Clamp01(longChargeStartNormalizedTime);
            chargedAttackStartNormalizedTime = Mathf.Clamp01(chargedAttackStartNormalizedTime);
            CacheReferences();
        }
    }
}
