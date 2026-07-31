using System;
using System.Collections;
using UnityEngine;

namespace Dystopian.EnemyTest
{
    public enum BossState
    {
        Intro,
        Idle,
        Charging,
        Attacking,
        Recovering,
        Dead
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(BossHealth))]
    public sealed class BossController : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string bossDisplayName = "Merlin, The Indomitable Gatekeeper";

        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private bool autoFindPlayerTarget = true;
        [SerializeField, Min(0.1f)] private float targetSearchRadius = 35f;
        [SerializeField] private LayerMask targetMask;

        [Header("Side View")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private float lockedZ = 0f;
        [SerializeField] private Vector3 rightFacingEuler = new Vector3(0f, 90f, 0f);
        [SerializeField] private Vector3 leftFacingEuler = new Vector3(0f, -90f, 0f);

        [Header("Movement")]
        [SerializeField] private float gravity = -35f;
        [SerializeField] private float groundedStickVelocity = -2f;

        [Header("Flow")]
        [SerializeField, Min(0f)] private float introSeconds = 0.5f;
        [SerializeField, Min(0f)] private float deathEndDelaySeconds = 2f;
        [SerializeField] private bool logStateChanges = true;

        private CharacterController characterController;
        private BossHealth health;
        private BossState currentState = BossState.Intro;
        private Coroutine stateRoutine;
        private float verticalVelocity;
        private int facingSign = -1;

        public event Action<BossState> StateChanged;

        public string BossDisplayName => bossDisplayName;
        public BossState CurrentState => currentState;
        public string CurrentStateName => currentState.ToString();
        public Transform CurrentTarget => target;
        public int FacingSign => facingSign;
        public bool IsDead => currentState == BossState.Dead || health == null || !health.IsAlive;
        public bool CanReceiveNote => !IsDead && currentState != BossState.Intro;

        private void Reset()
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
            {
                targetMask = 1 << playerLayer;
            }
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            health = GetComponent<BossHealth>();
            InitializeLayerMaskIfNeeded();
            CacheVisualRootIfNeeded();
            ApplyFacing();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += HandleDied;
            }
        }

        private void Start()
        {
            if (target == null && autoFindPlayerTarget)
            {
                TryFindTarget();
            }

            EnterState(BossState.Intro);
            if (introSeconds <= 0f)
            {
                EnterIdle();
            }
            else
            {
                StartStateRoutine(EnterIdleAfterDelay(introSeconds));
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= HandleDied;
            }
        }

        private void Update()
        {
            if (currentState == BossState.Dead)
            {
                LockSideViewPlane();
                return;
            }

            if (target == null && autoFindPlayerTarget)
            {
                TryFindTarget();
            }

            ApplyGravity();
            characterController.Move(new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);
            LockSideViewPlane();
        }

        public void FaceTarget()
        {
            Transform currentTarget = target;
            if (currentTarget == null && autoFindPlayerTarget)
            {
                TryFindTarget();
                currentTarget = target;
            }

            if (currentTarget == null)
            {
                return;
            }

            float deltaX = currentTarget.position.x - transform.position.x;
            if (Mathf.Abs(deltaX) > 0.01f)
            {
                facingSign = deltaX < 0f ? -1 : 1;
                ApplyFacing();
            }
        }

        public void EnterIdle()
        {
            if (IsDead)
            {
                return;
            }

            EnterState(BossState.Idle);
        }

        public void BeginCharging()
        {
            if (IsDead)
            {
                return;
            }

            StopStateRoutine();
            EnterState(BossState.Charging);
        }

        public void BeginAttack(float seconds)
        {
            if (IsDead)
            {
                return;
            }

            EnterState(BossState.Attacking);
            StartStateRoutine(EnterIdleAfterDelay(seconds));
        }

        public void BeginRecovering(float seconds)
        {
            if (IsDead)
            {
                return;
            }

            EnterState(BossState.Recovering);
            StartStateRoutine(EnterIdleAfterDelay(seconds));
        }

        public void ForceTarget(Transform nextTarget)
        {
            target = nextTarget;
        }

        private void EnterState(BossState nextState)
        {
            if (currentState == nextState)
            {
                return;
            }

            currentState = nextState;
            StateChanged?.Invoke(currentState);

            if (logStateChanges)
            {
                Debug.Log($"[Boss AI] {name} -> {currentState}", this);
            }
        }

        private IEnumerator EnterIdleAfterDelay(float seconds)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, seconds));
            if (!IsDead && currentState != BossState.Charging)
            {
                EnterIdle();
            }
        }

        private void StartStateRoutine(IEnumerator routine)
        {
            StopStateRoutine();
            stateRoutine = StartCoroutine(routine);
        }

        private void StopStateRoutine()
        {
            if (stateRoutine != null)
            {
                StopCoroutine(stateRoutine);
                stateRoutine = null;
            }
        }

        private void HandleDied(BossHealth _)
        {
            StopStateRoutine();
            EnterState(BossState.Dead);
            if (deathEndDelaySeconds > 0f)
            {
                StartStateRoutine(DeathFinishedAfterDelay(deathEndDelaySeconds));
            }
        }

        private IEnumerator DeathFinishedAfterDelay(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            Debug.Log($"[Boss AI] {name} death sequence finished.", this);
        }

        private void TryFindTarget()
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null &&
                (player.transform.position - transform.position).sqrMagnitude <= targetSearchRadius * targetSearchRadius)
            {
                target = player.transform;
                return;
            }

            Collider[] candidates = Physics.OverlapSphere(
                transform.position,
                targetSearchRadius,
                targetMask,
                QueryTriggerInteraction.Ignore);

            float closestSqrDistance = float.MaxValue;
            Transform closestTarget = null;

            for (int i = 0; i < candidates.Length; i++)
            {
                Collider candidate = candidates[i];
                if (candidate == null)
                {
                    continue;
                }

                float sqrDistance = (candidate.transform.position - transform.position).sqrMagnitude;
                if (sqrDistance < closestSqrDistance)
                {
                    closestSqrDistance = sqrDistance;
                    closestTarget = candidate.transform;
                }
            }

            target = closestTarget;
        }

        private void InitializeLayerMaskIfNeeded()
        {
            if (targetMask.value != 0)
            {
                return;
            }

            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
            {
                targetMask = 1 << playerLayer;
            }
        }

        private void CacheVisualRootIfNeeded()
        {
            if (visualRoot != null)
            {
                return;
            }

            Animator animator = GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                visualRoot = animator.transform;
            }
        }

        private void ApplyGravity()
        {
            if (characterController == null)
            {
                return;
            }

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = groundedStickVelocity;
            }

            verticalVelocity += gravity * Time.deltaTime;
        }

        private void LockSideViewPlane()
        {
            Vector3 position = transform.position;
            position.z = lockedZ;
            transform.position = position;
        }

        private void ApplyFacing()
        {
            if (visualRoot == null)
            {
                return;
            }

            visualRoot.localRotation = Quaternion.Euler(facingSign >= 0 ? rightFacingEuler : leftFacingEuler);
        }

        private void OnValidate()
        {
            targetSearchRadius = Mathf.Max(0.1f, targetSearchRadius);
            introSeconds = Mathf.Max(0f, introSeconds);
            deathEndDelaySeconds = Mathf.Max(0f, deathEndDelaySeconds);
            CacheVisualRootIfNeeded();
        }
    }
}
