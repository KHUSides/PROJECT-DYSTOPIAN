using System;
using UnityEngine;

namespace Dystopian.EnemyTest
{
    [DefaultExecutionOrder(110)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController))]
    public sealed class EnemyRangedCombat : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private bool autoFindPlayerTarget = true;
        [SerializeField, Min(0.1f)] private float targetSearchRadius = 14f;
        [SerializeField, Min(0.1f)] private float fireRange = 8f;
        [SerializeField, Min(0f)] private float verticalFireTolerance = 2.2f;
        [SerializeField] private LayerMask targetMask;

        [Header("Visual Facing")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private bool rotateVisualToFacing = true;
        [SerializeField] private float rightFacingYRotation = 90f;
        [SerializeField] private float leftFacingYRotation = -90f;

        [Header("Projectile")]
        [SerializeField] private EnemyProjectile projectilePrefab;
        [SerializeField] private Vector3 projectileSpawnOffset = new Vector3(1.15f, 1.15f, 0f);
        [SerializeField, Min(0.01f)] private float projectileSpeed = 8f;
        [SerializeField, Min(0.01f)] private float projectileLifeTimeSeconds = 3f;
        [SerializeField, Min(0.01f)] private float projectileHitRadius = 0.45f;

        [Header("Single Note Shot")]
        [SerializeField, Min(1)] private int singleNoteDamage = 8;

        [Header("Long Note Charging Shot")]
        [SerializeField, Min(1)] private int longNoteMinDamage = 16;
        [SerializeField, Min(1)] private int longNoteMaxDamage = 32;
        [SerializeField, Min(0.01f)] private float fullChargeSeconds = 1.5f;
        [SerializeField, Min(0.01f)] private float chargedProjectileSpeed = 10f;
        [SerializeField, Min(0.01f)] private float chargedProjectileHitRadius = 0.6f;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float attackCooldownSeconds = 0f;
        [SerializeField] private bool listenToDebugNoteEvents = true;

        [Header("Debug")]
        [SerializeField] private bool logAttacks = true;
        [SerializeField, Min(0.01f)] private float activeGizmoSeconds = 0.15f;
        [SerializeField] private Color fireRangeColor = new Color(0.45f, 0.15f, 1f, 0.2f);
        [SerializeField] private Color activeShotColor = new Color(0.8f, 0.1f, 1f, 0.45f);

        private EnemyHealth health;
        private EnemyController enemyController;
        private int facingSign = 1;
        private bool isCharging;
        private float chargeStartTime;
        private float lastAttackTime = -999f;

        public bool IsCharging => isCharging;
        public int FacingSign => facingSign;

        public event Action SingleNoteAttackPerformed;
        public event Action LongNoteChargeStarted;
        public event Action LongNoteChargedAttackPerformed;
        public event Action LongNoteChargeCancelled;

        private void Reset()
        {
            InitializeTargetMaskIfNeeded();
        }

        private void Awake()
        {
            enemyController = GetComponent<EnemyController>();
            health = GetComponent<EnemyHealth>();
            InitializeTargetMaskIfNeeded();
            FindVisualRootIfNeeded();
        }

        private void OnEnable()
        {
            if (listenToDebugNoteEvents)
            {
                NoteDebugInput.NoteEventTriggered += HandleNoteEvent;
            }
        }

        private void OnDisable()
        {
            NoteDebugInput.NoteEventTriggered -= HandleNoteEvent;
            isCharging = false;
        }

        private void Update()
        {
            if (!CanAct())
            {
                return;
            }

            RefreshVisualFacingForCurrentState();
        }

        private void HandleNoteEvent(NoteDebugEventType eventType)
        {
            if (!CanAct())
            {
                return;
            }

            if (target == null && autoFindPlayerTarget)
            {
                TryFindTarget();
            }

            UpdateFacing();
            UpdateVisualFacing();

            switch (eventType)
            {
                case NoteDebugEventType.Single:
                    TrySingleNoteShot();
                    break;
                case NoteDebugEventType.LongStart:
                    BeginLongNoteCharge();
                    break;
                case NoteDebugEventType.LongEnd:
                    ReleaseLongNoteCharge();
                    break;
            }
        }

        public GameObject TrySingleNoteShot()
        {
            RefreshTargetFacing();

            if (!CanUseAttackNow() || !CanFireAtTarget())
            {
                LogShotBlocked("Single Note Shot");
                return null;
            }

            SingleNoteAttackPerformed?.Invoke();
            return FireProjectile("Single Note Shot", singleNoteDamage, projectileSpeed, projectileHitRadius);
        }

        [ContextMenu("Test Single Note Shot")]
        private void ContextMenuSingleNoteShot()
        {
            TrySingleNoteShot();
        }

        [ContextMenu("Begin Long Note Charge")]
        public void BeginLongNoteCharge()
        {
            if (!CanAct())
            {
                return;
            }

            isCharging = true;
            chargeStartTime = Time.time;
            LongNoteChargeStarted?.Invoke();

            if (logAttacks)
            {
                Debug.Log($"[Enemy Ranged Combat] {name} started charging.", this);
            }
        }

        public GameObject ReleaseLongNoteCharge()
        {
            if (!CanAct() || !isCharging)
            {
                return null;
            }

            isCharging = false;
            RefreshTargetFacing();

            if (!CanUseAttackNow() || !CanFireAtTarget())
            {
                LogShotBlocked("Charged Long Note Shot");
                return null;
            }

            float chargeSeconds = Mathf.Max(0f, Time.time - chargeStartTime);
            float chargeRatio = Mathf.Clamp01(chargeSeconds / fullChargeSeconds);
            int damage = Mathf.RoundToInt(Mathf.Lerp(longNoteMinDamage, longNoteMaxDamage, chargeRatio));
            LongNoteChargedAttackPerformed?.Invoke();
            return FireProjectile("Charged Long Note Shot", damage, chargedProjectileSpeed, chargedProjectileHitRadius);
        }

        [ContextMenu("Release Long Note Charge")]
        private void ContextMenuReleaseLongNoteCharge()
        {
            ReleaseLongNoteCharge();
        }

        public void CancelLongNoteCharge()
        {
            if (isCharging)
            {
                LongNoteChargeCancelled?.Invoke();
            }

            isCharging = false;
        }

        public void RefreshTargetFacing()
        {
            if (target == null && autoFindPlayerTarget)
            {
                TryFindTarget();
            }

            UpdateFacing();
            UpdateVisualFacing();
        }

        private void RefreshVisualFacingForCurrentState()
        {
            if (ShouldFaceTarget())
            {
                RefreshTargetFacing();
                return;
            }

            SyncFacingFromEnemyController();
            UpdateVisualFacing();
        }

        private bool ShouldFaceTarget()
        {
            return isCharging || (enemyController != null && enemyController.IsStrictlyChasing);
        }

        private void SyncFacingFromEnemyController()
        {
            if (enemyController == null)
            {
                return;
            }

            facingSign = enemyController.FacingSign >= 0 ? 1 : -1;
        }

        private GameObject FireProjectile(string shotName, int damage, float shotSpeed, float hitRadius)
        {
            if (projectilePrefab == null)
            {
                if (logAttacks)
                {
                    Debug.LogWarning($"[Enemy Ranged Combat] {name} cannot fire {shotName} because projectilePrefab is missing.", this);
                }

                return null;
            }

            lastAttackTime = Time.time;
            Vector3 direction = Vector3.right * facingSign;
            Vector3 spawnPosition = GetProjectileSpawnPosition();
            EnemyProjectile projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
            projectile.name = $"{name}_{shotName.Replace(" ", string.Empty)}";
            projectile.Initialize(
                damage,
                direction,
                gameObject,
                targetMask,
                shotSpeed,
                projectileLifeTimeSeconds,
                hitRadius);

            if (logAttacks)
            {
                Debug.Log($"[Enemy Ranged Combat] {name} used {shotName} for {damage} damage.", this);
            }

            return projectile.gameObject;
        }

        private void LogShotBlocked(string shotName)
        {
            if (!logAttacks)
            {
                return;
            }

            Debug.Log($"[Enemy Ranged Combat] {name} did not use {shotName}. Target is missing, out of range, or attack is cooling down.", this);
        }

        private bool CanAct()
        {
            return health == null || health.IsAlive;
        }

        private bool CanUseAttackNow()
        {
            return CanAct() && Time.time - lastAttackTime >= attackCooldownSeconds;
        }

        private bool CanFireAtTarget()
        {
            if (target == null)
            {
                return false;
            }

            Vector3 delta = target.position - transform.position;
            if (Mathf.Abs(delta.x) > fireRange)
            {
                return false;
            }

            if (Mathf.Abs(delta.y) > verticalFireTolerance)
            {
                return false;
            }

            return Mathf.Abs(delta.x) <= 0.05f || Mathf.Sign(delta.x) == facingSign;
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
                if (candidate == null || candidate.transform.IsChildOf(transform))
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

        private void UpdateFacing()
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

        private void UpdateVisualFacing()
        {
            if (!rotateVisualToFacing || visualRoot == null)
            {
                return;
            }

            float yRotation = facingSign < 0 ? leftFacingYRotation : rightFacingYRotation;
            visualRoot.localRotation = Quaternion.Euler(0f, yRotation, 0f);
        }

        private Vector3 GetProjectileSpawnPosition()
        {
            return transform.position + new Vector3(projectileSpawnOffset.x * facingSign, projectileSpawnOffset.y, projectileSpawnOffset.z);
        }

        private void InitializeTargetMaskIfNeeded()
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

        private void FindVisualRootIfNeeded()
        {
            if (visualRoot != null)
            {
                return;
            }

            Transform found = transform.Find("VisualRoot");
            if (found != null)
            {
                visualRoot = found;
            }
        }

        private void OnDrawGizmosSelected()
        {
            InitializeTargetMaskIfNeeded();

            Gizmos.color = fireRangeColor;
            Vector3 center = transform.position + Vector3.up * projectileSpawnOffset.y;
            Vector3 right = center + Vector3.right * fireRange;
            Vector3 left = center + Vector3.left * fireRange;
            Gizmos.DrawLine(left, right);

            bool isActive = Application.isPlaying && Time.time - lastAttackTime <= activeGizmoSeconds;
            Gizmos.color = isActive ? activeShotColor : fireRangeColor;
            Gizmos.DrawWireSphere(GetProjectileSpawnPosition(), projectileHitRadius);
        }

        private void OnValidate()
        {
            targetSearchRadius = Mathf.Max(0.1f, targetSearchRadius);
            fireRange = Mathf.Max(0.1f, fireRange);
            verticalFireTolerance = Mathf.Max(0f, verticalFireTolerance);
            projectileSpeed = Mathf.Max(0.01f, projectileSpeed);
            projectileLifeTimeSeconds = Mathf.Max(0.01f, projectileLifeTimeSeconds);
            projectileHitRadius = Mathf.Max(0.01f, projectileHitRadius);
            singleNoteDamage = Mathf.Max(1, singleNoteDamage);
            longNoteMinDamage = Mathf.Max(1, longNoteMinDamage);
            longNoteMaxDamage = Mathf.Max(longNoteMinDamage, longNoteMaxDamage);
            fullChargeSeconds = Mathf.Max(0.01f, fullChargeSeconds);
            chargedProjectileSpeed = Mathf.Max(0.01f, chargedProjectileSpeed);
            chargedProjectileHitRadius = Mathf.Max(0.01f, chargedProjectileHitRadius);
            attackCooldownSeconds = Mathf.Max(0f, attackCooldownSeconds);
            activeGizmoSeconds = Mathf.Max(0.01f, activeGizmoSeconds);
        }
    }
}
