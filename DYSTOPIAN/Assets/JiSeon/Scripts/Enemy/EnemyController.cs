using UnityEngine;

namespace Dystopian.EnemyTest
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class EnemyController : MonoBehaviour
    {
        private enum EnemyState
        {
            Idle,
            Wander,
            Chase,
            Dead
        }

        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private bool autoFindPlayerTarget = true;
        [SerializeField, Min(0.1f)] private float targetSearchRadius = 30f;
        [SerializeField, Min(0.1f)] private float detectionRange = 7f;
        [SerializeField, Min(0.1f)] private float attackRange = 1.5f;
        [SerializeField] private LayerMask targetMask;

        [Header("Movement")]
        [SerializeField] private float lockedZ = 0f;
        [SerializeField, Min(0f)] private float wanderSpeed = 1.8f;
        [SerializeField, Min(0f)] private float chaseSpeed = 3.2f;
        [SerializeField] private float gravity = -35f;
        [SerializeField] private float groundedStickVelocity = -2f;

        [Header("Wander")]
        [SerializeField, Min(0.01f)] private float minWanderMoveSeconds = 4f;
        [SerializeField, Min(0.01f)] private float maxWanderMoveSeconds = 7f;
        [SerializeField, Min(0.01f)] private float minWanderIdleSeconds = 0.25f;
        [SerializeField, Min(0.01f)] private float maxWanderIdleSeconds = 0.7f;
        [SerializeField, Range(0f, 1f)] private float wanderDirectionChangeChance = 0.25f;

        [Header("Platform Safety")]
        [SerializeField] private LayerMask groundMask;
        [SerializeField, Min(0.01f)] private float edgeCheckForwardDistance = 0.65f;
        [SerializeField, Min(0.01f)] private float edgeCheckStartHeight = 0.25f;
        [SerializeField, Min(0.01f)] private float edgeCheckDownDistance = 2f;
        [SerializeField, Min(0.01f)] private float wallCheckDistance = 0.45f;
        [SerializeField, Min(0.01f)] private float wallCheckHeight = 0.8f;

        [Header("Debug")]
        [SerializeField] private bool listenToDebugNoteEvents = true;
        [SerializeField] private bool logStateChanges = true;
        [SerializeField] private bool drawDebugGizmos = true;

        private CharacterController characterController;
        private EnemyHealth health;
        private EnemyState currentState;
        private float stateTimer;
        private float verticalVelocity;
        private int moveSign = 1;
        private int facingSign = 1;
        private float movementLockedUntil = -999f;
        private bool externalMovementLock;

        public bool IsDead => currentState == EnemyState.Dead;
        public bool IsStrictlyChasing => currentState == EnemyState.Chase;
        public bool IsChasingTarget => currentState == EnemyState.Chase;
        public bool IsMovementLocked => externalMovementLock || Time.time < movementLockedUntil;
        public string CurrentStateName => currentState.ToString();
        public float DetectionRange => detectionRange;
        public float AttackRange => attackRange;
        public Transform CurrentTarget => target;
        public int FacingSign => facingSign;

        public void LockMovement(float seconds)
        {
            if (seconds <= 0f)
            {
                return;
            }

            movementLockedUntil = Mathf.Max(movementLockedUntil, Time.time + seconds);
        }

        public void SetExternalMovementLock(bool locked)
        {
            externalMovementLock = locked;
        }

        private void Reset()
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
            {
                targetMask = 1 << playerLayer;
            }

            int environmentLayer = LayerMask.NameToLayer("Environment");
            if (environmentLayer >= 0)
            {
                groundMask = 1 << environmentLayer;
            }
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            health = GetComponent<EnemyHealth>();
            InitializeLayerMasksIfNeeded();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += HandleDied;
            }

            if (listenToDebugNoteEvents)
            {
                NoteDebugInput.NoteEventTriggered += HandleDebugNoteEvent;
            }
        }

        private void Start()
        {
            if (target == null && autoFindPlayerTarget)
            {
                TryFindTarget();
            }

            EnterState(EnemyState.Wander);
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= HandleDied;
            }

            NoteDebugInput.NoteEventTriggered -= HandleDebugNoteEvent;
        }

        private void Update()
        {
            if (currentState == EnemyState.Dead)
            {
                return;
            }

            if (target == null && autoFindPlayerTarget)
            {
                TryFindTarget();
            }

            EnemyState desiredState = EvaluateState();
            if (desiredState != currentState)
            {
                EnterState(desiredState);
            }

            float horizontalSpeed = UpdateStateAndGetHorizontalSpeed();
            if (IsMovementLocked)
            {
                horizontalSpeed = 0f;
            }

            ApplyGravity();
            Move(horizontalSpeed);
            LockSideViewPlane();
        }

        private EnemyState EvaluateState()
        {
            if (health != null && !health.IsAlive)
            {
                return EnemyState.Dead;
            }

            if (target == null)
            {
                return currentState == EnemyState.Idle ? EnemyState.Idle : EnemyState.Wander;
            }

            float distanceToTarget = Vector3.Distance(transform.position, target.position);
            if (distanceToTarget > detectionRange)
            {
                return currentState == EnemyState.Idle ? EnemyState.Idle : EnemyState.Wander;
            }

            FaceTarget();

            return EnemyState.Chase;
        }

        private float UpdateStateAndGetHorizontalSpeed()
        {
            stateTimer -= Time.deltaTime;

            switch (currentState)
            {
                case EnemyState.Idle:
                    if (stateTimer <= 0f)
                    {
                        UpdateWanderDirectionAfterIdle();
                        EnterState(EnemyState.Wander);
                    }
                    return 0f;

                case EnemyState.Wander:
                    if (!CanMove(moveSign))
                    {
                        moveSign *= -1;
                        EnterState(EnemyState.Idle);
                        return 0f;
                    }

                    if (stateTimer <= 0f)
                    {
                        EnterState(EnemyState.Idle);
                        return 0f;
                    }

                    facingSign = moveSign;
                    return moveSign * wanderSpeed;

                case EnemyState.Chase:
                    if (target == null)
                    {
                        return 0f;
                    }

                    int chaseSign = target.position.x < transform.position.x ? -1 : 1;
                    facingSign = chaseSign;

                    if (Mathf.Abs(target.position.x - transform.position.x) <= attackRange)
                    {
                        return 0f;
                    }

                    if (!CanMove(chaseSign))
                    {
                        return 0f;
                    }

                    return chaseSign * chaseSpeed;

                default:
                    return 0f;
            }
        }

        private void UpdateWanderDirectionAfterIdle()
        {
            if (moveSign == 0)
            {
                moveSign = Random.value < 0.5f ? -1 : 1;
                return;
            }

            if (Random.value < wanderDirectionChangeChance)
            {
                moveSign *= -1;
            }
        }

        private void EnterState(EnemyState nextState)
        {
            currentState = nextState;

            switch (currentState)
            {
                case EnemyState.Idle:
                    stateTimer = Random.Range(minWanderIdleSeconds, maxWanderIdleSeconds);
                    break;
                case EnemyState.Wander:
                    stateTimer = Random.Range(minWanderMoveSeconds, maxWanderMoveSeconds);
                    if (moveSign == 0)
                    {
                        moveSign = 1;
                    }
                    break;
                case EnemyState.Dead:
                    stateTimer = 0f;
                    break;
            }

            if (logStateChanges)
            {
                Debug.Log($"[Enemy AI] {name} -> {currentState}", this);
            }
        }

        private void TryFindTarget()
        {
            DummyPlayerController dummyPlayer = FindFirstObjectByType<DummyPlayerController>();
            if (dummyPlayer != null &&
                (dummyPlayer.transform.position - transform.position).sqrMagnitude <= targetSearchRadius * targetSearchRadius)
            {
                target = dummyPlayer.transform;
                return;
            }

            Collider[] candidates = Physics.OverlapSphere(
                transform.position,
                targetSearchRadius,
                targetMask,
                QueryTriggerInteraction.Ignore);

            float closestSqrDistance = float.MaxValue;
            Transform closestTarget = null;

            foreach (Collider candidate in candidates)
            {
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

        private void InitializeLayerMasksIfNeeded()
        {
            if (targetMask.value == 0)
            {
                int playerLayer = LayerMask.NameToLayer("Player");
                if (playerLayer >= 0)
                {
                    targetMask = 1 << playerLayer;
                }
            }

            if (groundMask.value == 0)
            {
                int environmentLayer = LayerMask.NameToLayer("Environment");
                if (environmentLayer >= 0)
                {
                    groundMask = 1 << environmentLayer;
                }
            }
        }

        private bool CanMove(int directionSign)
        {
            return HasGroundAhead(directionSign) && !HasWallAhead(directionSign);
        }

        private bool HasGroundAhead(int directionSign)
        {
            Vector3 origin = transform.position +
                Vector3.right * (directionSign * edgeCheckForwardDistance) +
                Vector3.up * edgeCheckStartHeight;

            return Physics.Raycast(
                origin,
                Vector3.down,
                edgeCheckDownDistance,
                groundMask,
                QueryTriggerInteraction.Ignore);
        }

        private bool HasWallAhead(int directionSign)
        {
            Vector3 origin = transform.position + Vector3.up * wallCheckHeight;
            return Physics.Raycast(
                origin,
                Vector3.right * directionSign,
                wallCheckDistance,
                groundMask,
                QueryTriggerInteraction.Ignore);
        }

        private void FaceTarget()
        {
            if (target == null)
            {
                return;
            }

            float deltaX = target.position.x - transform.position.x;
            if (Mathf.Abs(deltaX) > 0.01f)
            {
                facingSign = deltaX < 0f ? -1 : 1;
            }
        }

        private void ApplyGravity()
        {
            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = groundedStickVelocity;
            }

            verticalVelocity += gravity * Time.deltaTime;
        }

        private void Move(float horizontalSpeed)
        {
            characterController.Move(new Vector3(horizontalSpeed, verticalVelocity, 0f) * Time.deltaTime);
        }

        private void LockSideViewPlane()
        {
            Vector3 position = transform.position;
            position.z = lockedZ;
            transform.position = position;
        }

        private void HandleDied(EnemyHealth _)
        {
            EnterState(EnemyState.Dead);
        }

        private void HandleDebugNoteEvent(NoteDebugEventType eventType)
        {
            if (currentState == EnemyState.Dead)
            {
                return;
            }

            if (currentState != EnemyState.Chase)
            {
                if (logStateChanges)
                {
                    Debug.Log($"[Enemy AI] {name} received {eventType}, but target is not being chased.", this);
                }
                return;
            }

            Debug.Log($"[Enemy AI] {name} reacts to debug note: {eventType}.", this);
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos)
            {
                return;
            }

            Gizmos.color = new Color(1f, 0.6f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, attackRange);

            Gizmos.color = Color.green;
            DrawEdgeRay(facingSign == 0 ? 1 : facingSign);
        }

        private void DrawEdgeRay(int directionSign)
        {
            Vector3 origin = transform.position +
                Vector3.right * (directionSign * edgeCheckForwardDistance) +
                Vector3.up * edgeCheckStartHeight;
            Gizmos.DrawLine(origin, origin + Vector3.down * edgeCheckDownDistance);

            Vector3 wallOrigin = transform.position + Vector3.up * wallCheckHeight;
            Gizmos.DrawLine(wallOrigin, wallOrigin + Vector3.right * (directionSign * wallCheckDistance));
        }

        private void OnValidate()
        {
            targetSearchRadius = Mathf.Max(0.1f, targetSearchRadius);
            detectionRange = Mathf.Max(0.1f, detectionRange);
            attackRange = Mathf.Max(0.1f, attackRange);
            wanderSpeed = Mathf.Max(0f, wanderSpeed);
            chaseSpeed = Mathf.Max(0f, chaseSpeed);
            minWanderMoveSeconds = Mathf.Max(0.01f, minWanderMoveSeconds);
            maxWanderMoveSeconds = Mathf.Max(minWanderMoveSeconds, maxWanderMoveSeconds);
            minWanderIdleSeconds = Mathf.Max(0.01f, minWanderIdleSeconds);
            maxWanderIdleSeconds = Mathf.Max(minWanderIdleSeconds, maxWanderIdleSeconds);
            wanderDirectionChangeChance = Mathf.Clamp01(wanderDirectionChangeChance);
            edgeCheckForwardDistance = Mathf.Max(0.01f, edgeCheckForwardDistance);
            edgeCheckStartHeight = Mathf.Max(0.01f, edgeCheckStartHeight);
            edgeCheckDownDistance = Mathf.Max(0.01f, edgeCheckDownDistance);
            wallCheckDistance = Mathf.Max(0.01f, wallCheckDistance);
            wallCheckHeight = Mathf.Max(0.01f, wallCheckHeight);
        }
    }
}
