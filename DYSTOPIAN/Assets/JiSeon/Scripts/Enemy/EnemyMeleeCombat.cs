using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dystopian.EnemyTest
{
    [DefaultExecutionOrder(110)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController))]
    public sealed class EnemyMeleeCombat : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private bool autoFindPlayerTarget = true;
        [SerializeField, Min(0.1f)] private float targetSearchRadius = 12f;
        [SerializeField] private LayerMask targetMask;

        [Header("Visual Facing")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private bool rotateVisualToFacing = true;
        [SerializeField] private float rightFacingYRotation = 90f;
        [SerializeField] private float leftFacingYRotation = -90f;

        [Header("Single Note Attack")]
        [SerializeField, Min(1)] private int singleNoteDamage = 12;
        [SerializeField] private Vector3 singleHitboxOffset = new Vector3(1.15f, 1f, 0f);
        [SerializeField] private Vector3 singleHitboxSize = new Vector3(1.8f, 1.8f, 1.2f);

        [Header("Long Note Charging Attack")]
        [SerializeField, Min(1)] private int longNoteMinDamage = 20;
        [SerializeField, Min(1)] private int longNoteMaxDamage = 40;
        [SerializeField, Min(0.01f)] private float fullChargeSeconds = 1.5f;
        [SerializeField] private Vector3 chargedHitboxOffset = new Vector3(1.45f, 1f, 0f);
        [SerializeField] private Vector3 chargedHitboxSize = new Vector3(2.6f, 2f, 1.4f);

        [Header("Timing")]
        [SerializeField, Min(0f)] private float attackCooldownSeconds = 0f;
        [SerializeField] private bool listenToDebugNoteEvents = true;

        [Header("Debug")]
        [SerializeField] private bool logAttacks = true;
        [SerializeField, Min(0.01f)] private float activeGizmoSeconds = 0.15f;
        [SerializeField] private Color idleSingleHitboxColor = new Color(1f, 0.35f, 0.05f, 0.2f);
        [SerializeField] private Color idleChargedHitboxColor = new Color(1f, 0.05f, 0.8f, 0.18f);
        [SerializeField] private Color activeHitboxColor = new Color(1f, 0f, 0f, 0.4f);

        private readonly List<IDamageable> damagedTargets = new List<IDamageable>();
        private EnemyController enemyController;
        private EnemyHealth health;
        private int facingSign = 1;
        private bool isCharging;
        private float chargeStartTime;
        private float lastAttackTime = -999f;
        private Vector3 lastHitboxOffset;
        private Vector3 lastHitboxSize;

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
                    TrySingleNoteAttack();
                    break;
                case NoteDebugEventType.LongStart:
                    BeginLongNoteCharge();
                    break;
                case NoteDebugEventType.LongEnd:
                    ReleaseLongNoteCharge();
                    break;
            }
        }

        [ContextMenu("Test Single Note Attack")]
        public void TrySingleNoteAttack()
        {
            if (!CanUseAttackNow())
            {
                return;
            }

            RefreshTargetFacing();
            SingleNoteAttackPerformed?.Invoke();
            PerformMeleeAttack("Single Note Attack", singleNoteDamage, singleHitboxOffset, singleHitboxSize);
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
                Debug.Log($"[Enemy Melee Combat] {name} started charging.", this);
            }
        }

        [ContextMenu("Release Long Note Charge")]
        public void ReleaseLongNoteCharge()
        {
            if (!CanAct() || !isCharging)
            {
                return;
            }

            isCharging = false;

            if (!CanUseAttackNow())
            {
                return;
            }

            float chargeSeconds = Mathf.Max(0f, Time.time - chargeStartTime);
            float chargeRatio = Mathf.Clamp01(chargeSeconds / fullChargeSeconds);
            int damage = Mathf.RoundToInt(Mathf.Lerp(longNoteMinDamage, longNoteMaxDamage, chargeRatio));
            RefreshTargetFacing();
            LongNoteChargedAttackPerformed?.Invoke();
            PerformMeleeAttack("Charged Long Note Attack", damage, chargedHitboxOffset, chargedHitboxSize);
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

        private void PerformMeleeAttack(string attackName, int damage, Vector3 hitboxOffset, Vector3 hitboxSize)
        {
            Physics.SyncTransforms();
            lastAttackTime = Time.time;
            lastHitboxOffset = hitboxOffset;
            lastHitboxSize = hitboxSize;
            damagedTargets.Clear();

            Collider[] hits = Physics.OverlapBox(
                GetHitboxCenter(hitboxOffset),
                GetHitboxHalfExtents(hitboxSize),
                Quaternion.identity,
                targetMask,
                QueryTriggerInteraction.Collide);

            foreach (Collider hit in hits)
            {
                if (hit == null || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (!IsInFacingDirection(hit.bounds.center))
                {
                    continue;
                }

                IDamageable damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.IsAlive || damagedTargets.Contains(damageable))
                {
                    continue;
                }

                damagedTargets.Add(damageable);
                Vector3 hitPoint = hit.ClosestPoint(transform.position + Vector3.up);
                damageable.TakeDamage(new DamageInfo(damage, hitPoint, gameObject));

                if (logAttacks)
                {
                    Debug.Log($"[Enemy Melee Combat] {name} used {attackName} and hit {hit.name} for {damage} damage.", this);
                }
            }

            if (logAttacks && damagedTargets.Count == 0)
            {
                Debug.Log($"[Enemy Melee Combat] {name} used {attackName}, but no target was in the melee hitbox.", this);
            }
        }

        private bool CanAct()
        {
            return health == null || health.IsAlive;
        }

        private bool CanUseAttackNow()
        {
            return CanAct() && Time.time - lastAttackTime >= attackCooldownSeconds;
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

        private bool IsInFacingDirection(Vector3 point)
        {
            float deltaX = point.x - transform.position.x;
            return Mathf.Abs(deltaX) <= 0.05f || Mathf.Sign(deltaX) == facingSign;
        }

        private Vector3 GetHitboxCenter(Vector3 hitboxOffset)
        {
            return transform.position + new Vector3(hitboxOffset.x * facingSign, hitboxOffset.y, hitboxOffset.z);
        }

        private static Vector3 GetHitboxHalfExtents(Vector3 hitboxSize)
        {
            return new Vector3(
                Mathf.Max(0.01f, hitboxSize.x) * 0.5f,
                Mathf.Max(0.01f, hitboxSize.y) * 0.5f,
                Mathf.Max(0.01f, hitboxSize.z) * 0.5f);
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

            bool isActive = Application.isPlaying && Time.time - lastAttackTime <= activeGizmoSeconds;
            Gizmos.color = isActive ? activeHitboxColor : idleSingleHitboxColor;
            Gizmos.DrawWireCube(GetHitboxCenter(singleHitboxOffset), singleHitboxSize);

            Gizmos.color = isActive ? activeHitboxColor : idleChargedHitboxColor;
            Gizmos.DrawWireCube(GetHitboxCenter(chargedHitboxOffset), chargedHitboxSize);

            if (Application.isPlaying && isActive)
            {
                Gizmos.color = activeHitboxColor;
                Gizmos.DrawWireCube(GetHitboxCenter(lastHitboxOffset), lastHitboxSize);
            }
        }

        private void OnValidate()
        {
            targetSearchRadius = Mathf.Max(0.1f, targetSearchRadius);
            singleNoteDamage = Mathf.Max(1, singleNoteDamage);
            singleHitboxSize = ClampHitboxSize(singleHitboxSize);
            longNoteMinDamage = Mathf.Max(1, longNoteMinDamage);
            longNoteMaxDamage = Mathf.Max(longNoteMinDamage, longNoteMaxDamage);
            fullChargeSeconds = Mathf.Max(0.01f, fullChargeSeconds);
            chargedHitboxSize = ClampHitboxSize(chargedHitboxSize);
            attackCooldownSeconds = Mathf.Max(0f, attackCooldownSeconds);
            activeGizmoSeconds = Mathf.Max(0.01f, activeGizmoSeconds);
        }

        private static Vector3 ClampHitboxSize(Vector3 size)
        {
            return new Vector3(
                Mathf.Max(0.01f, size.x),
                Mathf.Max(0.01f, size.y),
                Mathf.Max(0.01f, size.z));
        }
    }
}
