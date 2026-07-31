using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dystopian.EnemyTest
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BossController))]
    [RequireComponent(typeof(BossHealth))]
    public sealed class MerlinBossCombat : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BossController bossController;
        [SerializeField] private BossHealth bossHealth;
        [SerializeField] private Transform target;
        [SerializeField] private bool autoFindPlayerTarget = true;
        [SerializeField] private Transform heldSpear;
        [SerializeField] private Transform spearSocket;
        [SerializeField] private Transform throwOrigin;
        [SerializeField] private MerlinSpearProjectile spearProjectilePrefab;
        [SerializeField] private MerlinHeldSpearPoseController heldSpearPoseController;

        [Header("Target")]
        [SerializeField] private LayerMask targetMask;
        [SerializeField] private LayerMask spearBlockingMask;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

        [Header("Single Note Pattern")]
        [SerializeField, Min(1)] private int attackPower = 18;
        [SerializeField, Min(0.05f)] private float thrustActionSeconds = 0.85f;
        [SerializeField, Min(0.05f)] private float swingActionSeconds = 0.95f;
        [SerializeField] private Vector3 thrustCenterOffset = new Vector3(1.35f, 1.05f, 0f);
        [SerializeField] private Vector3 thrustSize = new Vector3(2.2f, 1.25f, 1.2f);
        [SerializeField] private Vector3 swingCenterOffset = new Vector3(1.15f, 1.1f, 0f);
        [SerializeField] private Vector3 swingSize = new Vector3(2.6f, 1.65f, 1.4f);

        [Header("Long Note Pattern")]
        [SerializeField, Min(1)] private int throwAttackPower = 28;
        [SerializeField, Min(0.05f)] private float throwActionSeconds = 1.25f;
        [SerializeField, Min(0.05f)] private float returnRecoverSeconds = 0.1f;
        [SerializeField, Min(0.1f)] private float spearThrowSpeed = 42f;
        [SerializeField, Min(0.1f)] private float spearReturnSpeed = 180f;
        [SerializeField, Min(0.1f)] private float spearMaxDistance = 14f;
        [SerializeField, Min(0.01f)] private float spearHitRadius = 0.45f;
        [SerializeField, Min(0f)] private float spearReleaseDelaySeconds = 0.45f;
        [SerializeField] private Vector3 spearAimOffset = new Vector3(0f, 1.35f, 0f);
        [SerializeField] private bool lockThrowReleaseToHorizontalLine = true;
        [SerializeField] private bool forceThrowAimToReleaseHeight;
        [SerializeField, Min(0f)] private float minimumHorizontalThrowHeight = 1.5f;
        [SerializeField] private float horizontalThrowLockedZ = 0f;

        [Header("Debug")]
        [SerializeField] private bool drawDebugGizmos = true;
        [SerializeField] private bool logPattern = true;

        private readonly List<IDamageable> damagedTargets = new List<IDamageable>();
        private bool isChargingLongNote;
        private int singleNoteCounter;
        private MerlinSpearProjectile activeSpearProjectile;
        private Coroutine pendingSpearThrowRoutine;
        private bool queuedSingleAfterSpearReturn;
        private bool queuedLongChargeAfterSpearReturn;
        private bool queuedLongReleaseAfterSpearReturn;

        public event Action<int> SingleNoteCounterChanged;
        public event Action ThrustStarted;
        public event Action SwingStarted;
        public event Action LongChargeStarted;
        public event Action LongChargeReleased;
        public event Action SpearReturned;
        public event Action LongChargeCancelled;

        public int SingleNoteCounter => singleNoteCounter;
        public bool IsChargingLongNote => isChargingLongNote;
        public string NextSinglePatternName => singleNoteCounter % 2 == 0 ? "Thrust" : "Swing";
        private bool IsSpearThrowBusy => pendingSpearThrowRoutine != null || activeSpearProjectile != null;

        private void Reset()
        {
            CacheReferences();
            InitializeTargetMaskIfNeeded();
        }

        private void Awake()
        {
            CacheReferences();
            InitializeTargetMaskIfNeeded();
        }

        private void Start()
        {
            CacheReferences();
            if (target == null && autoFindPlayerTarget)
            {
                TryFindTarget();
            }
        }

        public void HandleSingleNoteAttack()
        {
            if (!CanReceivePattern())
            {
                return;
            }

            if (IsSpearThrowBusy)
            {
                queuedLongChargeAfterSpearReturn = false;
                queuedLongReleaseAfterSpearReturn = false;
                queuedSingleAfterSpearReturn = true;
                LogQueuedPattern("single note");
                return;
            }

            isChargingLongNote = false;
            CacheTargetIfNeeded();
            bossController.FaceTarget();

            singleNoteCounter++;
            SingleNoteCounterChanged?.Invoke(singleNoteCounter);

            bool useThrust = singleNoteCounter % 2 == 1;
            if (useThrust)
            {
                ExecuteThrust();
            }
            else
            {
                ExecuteSwing();
            }
        }

        public void BeginLongNoteCharge()
        {
            if (!CanReceivePattern())
            {
                return;
            }

            if (IsSpearThrowBusy)
            {
                queuedSingleAfterSpearReturn = false;
                queuedLongChargeAfterSpearReturn = true;
                LogQueuedPattern("long note charge");
                return;
            }

            CacheTargetIfNeeded();
            bossController.FaceTarget();
            isChargingLongNote = true;
            SetHeldSpearVisible(true);
            bossController.BeginCharging();
            LongChargeStarted?.Invoke();

            if (logPattern)
            {
                Debug.Log("[Merlin Boss] Long note charge started.", this);
            }
        }

        public void ReleaseLongNoteCharge()
        {
            if (!CanReceivePattern())
            {
                return;
            }

            if (!isChargingLongNote && IsSpearThrowBusy)
            {
                queuedSingleAfterSpearReturn = false;
                queuedLongChargeAfterSpearReturn = true;
                queuedLongReleaseAfterSpearReturn = true;
                LogQueuedPattern("long note release");
                return;
            }

            if (!isChargingLongNote)
            {
                BeginLongNoteCharge();
                if (!isChargingLongNote)
                {
                    return;
                }
            }

            isChargingLongNote = false;
            CacheTargetIfNeeded();
            bossController.FaceTarget();
            bossController.BeginAttack(throwActionSeconds);
            LongChargeReleased?.Invoke();
            QueueSpearThrow();

            if (logPattern)
            {
                Debug.Log("[Merlin Boss] Long note charge released. Spear thrown.", this);
            }
        }

        public void CancelLongNoteCharge()
        {
            ClearQueuedPatterns();

            if (!isChargingLongNote)
            {
                return;
            }

            isChargingLongNote = false;
            SetHeldSpearVisible(true);
            bossController.EnterIdle();
            LongChargeCancelled?.Invoke();
        }

        private void ExecuteThrust()
        {
            bossController.BeginAttack(thrustActionSeconds);
            ThrustStarted?.Invoke();
            ApplyHitboxDamage(thrustCenterOffset, thrustSize, attackPower, "Merlin Spear Thrust");

            if (logPattern)
            {
                Debug.Log("[Merlin Boss] Single note pattern: thrust.", this);
            }
        }

        private void ExecuteSwing()
        {
            bossController.BeginAttack(swingActionSeconds);
            SwingStarted?.Invoke();
            ApplyHitboxDamage(swingCenterOffset, swingSize, attackPower, "Merlin Spear Swing");

            if (logPattern)
            {
                Debug.Log("[Merlin Boss] Single note pattern: swing.", this);
            }
        }

        private void ThrowSpear()
        {
            Transform origin = heldSpear != null ? heldSpear : (throwOrigin != null ? throwOrigin : spearSocket);
            if (spearProjectilePrefab == null || origin == null)
            {
                SetHeldSpearVisible(false);
                ApplyHitboxDamage(
                    new Vector3(Mathf.Max(1.5f, spearMaxDistance * 0.5f), 1.05f, 0f),
                    new Vector3(spearMaxDistance, 1.2f, 1.2f),
                    throwAttackPower,
                    "Merlin Spear Throw Fallback");
                SetHeldSpearVisible(true);
                bossController.BeginRecovering(returnRecoverSeconds);
                SpearReturned?.Invoke();
                return;
            }

            Vector3 releasePosition = origin.position;
            Quaternion releaseRotation = origin.rotation;
            Vector3 aimPoint = GetSpearAimPoint(origin);

            if (heldSpearPoseController != null)
            {
                heldSpearPoseController.ForceUpdatePose();
                releasePosition = origin.position;
                releaseRotation = origin.rotation;
            }

            if (lockThrowReleaseToHorizontalLine)
            {
                releasePosition = LockReleasePositionToHorizontalLine(releasePosition);
                aimPoint = LockAimPointToReleaseLine(aimPoint, releasePosition);
            }

            SetHeldSpearVisible(false);

            activeSpearProjectile = Instantiate(spearProjectilePrefab, releasePosition, releaseRotation);
            activeSpearProjectile.name = "MerlinSpearProjectile_Runtime";
            activeSpearProjectile.Initialize(
                gameObject,
                spearSocket != null ? spearSocket : origin,
                aimPoint,
                bossController.FacingSign,
                throwAttackPower,
                spearThrowSpeed,
                spearReturnSpeed,
                spearMaxDistance,
                spearHitRadius,
                targetMask,
                spearBlockingMask,
                HandleSpearReturned);
        }

        private void QueueSpearThrow()
        {
            CancelPendingSpearThrow();

            if (spearReleaseDelaySeconds <= 0f)
            {
                ThrowSpear();
                return;
            }

            pendingSpearThrowRoutine = StartCoroutine(ThrowSpearAfterDelay());
        }

        private IEnumerator ThrowSpearAfterDelay()
        {
            yield return new WaitForSeconds(spearReleaseDelaySeconds);
            pendingSpearThrowRoutine = null;

            if (bossHealth == null || !bossHealth.IsAlive)
            {
                yield break;
            }

            ThrowSpear();
        }

        private Vector3 GetSpearAimPoint(Transform origin)
        {
            if (target != null)
            {
                return target.position + spearAimOffset;
            }

            int facing = bossController != null ? bossController.FacingSign : 1;
            Vector3 originPosition = origin != null ? origin.position : transform.position + Vector3.up;
            return originPosition + Vector3.right * facing * spearMaxDistance;
        }

        private Vector3 LockReleasePositionToHorizontalLine(Vector3 releasePosition)
        {
            releasePosition.y = Mathf.Max(releasePosition.y, transform.position.y + minimumHorizontalThrowHeight);
            releasePosition.z = horizontalThrowLockedZ;
            return releasePosition;
        }

        private Vector3 LockAimPointToReleaseLine(Vector3 aimPoint, Vector3 releasePosition)
        {
            if (forceThrowAimToReleaseHeight)
            {
                aimPoint.y = releasePosition.y;
            }

            aimPoint.z = releasePosition.z;
            return aimPoint;
        }

        private void HandleSpearReturned(MerlinSpearProjectile projectile)
        {
            if (activeSpearProjectile == projectile)
            {
                activeSpearProjectile = null;
            }

            SetHeldSpearVisible(true);
            SpearReturned?.Invoke();

            if (bossHealth != null && bossHealth.IsAlive)
            {
                bossController.BeginRecovering(returnRecoverSeconds);
            }

            ProcessQueuedPatternAfterSpearReturn();
        }

        private void CancelActiveSpearProjectile()
        {
            CancelPendingSpearThrow();

            if (activeSpearProjectile == null)
            {
                return;
            }

            Destroy(activeSpearProjectile.gameObject);
            activeSpearProjectile = null;
            SetHeldSpearVisible(true);
        }

        private void LogIgnoredPattern(string patternName)
        {
            if (!logPattern || !IsSpearThrowBusy)
            {
                return;
            }

            Debug.Log($"[Merlin Boss] Ignored {patternName} while spear throw is in progress.", this);
        }

        private void LogQueuedPattern(string patternName)
        {
            if (!logPattern)
            {
                return;
            }

            Debug.Log($"[Merlin Boss] Queued {patternName} until spear returns.", this);
        }

        private void ProcessQueuedPatternAfterSpearReturn()
        {
            if (!CanReceivePattern())
            {
                ClearQueuedPatterns();
                return;
            }

            if (queuedLongChargeAfterSpearReturn)
            {
                queuedLongChargeAfterSpearReturn = false;
                bool releaseImmediately = queuedLongReleaseAfterSpearReturn;
                queuedLongReleaseAfterSpearReturn = false;

                BeginLongNoteCharge();
                if (releaseImmediately)
                {
                    ReleaseLongNoteCharge();
                }

                return;
            }

            if (queuedLongReleaseAfterSpearReturn)
            {
                queuedLongReleaseAfterSpearReturn = false;
                ReleaseLongNoteCharge();
                return;
            }

            if (queuedSingleAfterSpearReturn)
            {
                queuedSingleAfterSpearReturn = false;
                HandleSingleNoteAttack();
            }
        }

        private void ClearQueuedPatterns()
        {
            queuedSingleAfterSpearReturn = false;
            queuedLongChargeAfterSpearReturn = false;
            queuedLongReleaseAfterSpearReturn = false;
        }

        private void CancelPendingSpearThrow()
        {
            if (pendingSpearThrowRoutine == null)
            {
                return;
            }

            StopCoroutine(pendingSpearThrowRoutine);
            pendingSpearThrowRoutine = null;
            SetHeldSpearVisible(true);
        }

        private void ApplyHitboxDamage(Vector3 localCenterOffset, Vector3 size, int damage, string attackName)
        {
            damagedTargets.Clear();
            Physics.SyncTransforms();

            int facing = bossController != null ? bossController.FacingSign : 1;
            Vector3 worldCenter = transform.position + new Vector3(
                localCenterOffset.x * facing,
                localCenterOffset.y,
                localCenterOffset.z);

            Vector3 halfExtents = new Vector3(
                Mathf.Max(0.01f, size.x) * 0.5f,
                Mathf.Max(0.01f, size.y) * 0.5f,
                Mathf.Max(0.01f, size.z) * 0.5f);

            Collider[] hits = Physics.OverlapBox(
                worldCenter,
                halfExtents,
                Quaternion.identity,
                targetMask,
                triggerInteraction);

            for (int i = 0; i < hits.Length; i++)
            {
                TryDamageTarget(hits[i], damage, worldCenter, attackName);
            }
        }

        private void TryDamageTarget(Collider hit, int damage, Vector3 hitboxCenter, string attackName)
        {
            if (hit == null || hit.transform.IsChildOf(transform))
            {
                return;
            }

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable == null || !damageable.IsAlive || damagedTargets.Contains(damageable))
            {
                return;
            }

            damagedTargets.Add(damageable);
            Vector3 hitPoint = hit.ClosestPoint(hitboxCenter);
            damageable.TakeDamage(new DamageInfo(damage, hitPoint, gameObject));

            if (logPattern)
            {
                Debug.Log($"[Merlin Boss] {attackName} hit {hit.name} for {damage} damage.", this);
            }
        }

        private bool CanReceivePattern()
        {
            return bossHealth != null &&
                   bossHealth.IsAlive &&
                   bossController != null &&
                   bossController.CanReceiveNote;
        }

        private void SetHeldSpearVisible(bool visible)
        {
            if (heldSpear != null)
            {
                heldSpear.gameObject.SetActive(visible);
            }
        }

        private void CacheTargetIfNeeded()
        {
            if (target == null && autoFindPlayerTarget)
            {
                TryFindTarget();
            }

            if (bossController != null && bossController.CurrentTarget == null && target != null)
            {
                bossController.ForceTarget(target);
            }
        }

        private void TryFindTarget()
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                target = player.transform;
                if (bossController != null)
                {
                    bossController.ForceTarget(target);
                }
            }
        }

        private void CacheReferences()
        {
            if (bossController == null)
            {
                bossController = GetComponent<BossController>();
            }

            if (bossHealth == null)
            {
                bossHealth = GetComponent<BossHealth>();
            }

            if (heldSpearPoseController == null)
            {
                heldSpearPoseController = GetComponent<MerlinHeldSpearPoseController>();
            }
        }

        private void InitializeTargetMaskIfNeeded()
        {
            if (targetMask.value != 0)
            {
                InitializeBlockingMaskIfNeeded();
                return;
            }

            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
            {
                targetMask = 1 << playerLayer;
            }

            InitializeBlockingMaskIfNeeded();
        }

        private void InitializeBlockingMaskIfNeeded()
        {
            if (spearBlockingMask.value != 0)
            {
                return;
            }

            int environmentLayer = LayerMask.NameToLayer("Environment");
            if (environmentLayer >= 0)
            {
                spearBlockingMask = 1 << environmentLayer;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos)
            {
                return;
            }

            int facing = bossController != null ? bossController.FacingSign : 1;
            DrawHitbox(thrustCenterOffset, thrustSize, facing, new Color(1f, 0.35f, 0.1f, 0.25f));
            DrawHitbox(swingCenterOffset, swingSize, facing, new Color(1f, 0.1f, 0.1f, 0.25f));

            Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.8f);
            Transform origin = throwOrigin != null ? throwOrigin : spearSocket;
            if (origin != null)
            {
                Vector3 releasePosition = origin.position;
                Vector3 aimPoint = GetSpearAimPoint(origin);

                if (lockThrowReleaseToHorizontalLine)
                {
                    releasePosition = LockReleasePositionToHorizontalLine(releasePosition);
                    aimPoint = LockAimPointToReleaseLine(aimPoint, releasePosition);
                }

                Vector3 direction = aimPoint - releasePosition;
                if (direction.sqrMagnitude <= 0.01f || Mathf.Sign(direction.x) != facing)
                {
                    direction = Vector3.right * facing;
                }

                direction.Normalize();
                Vector3 endPoint = releasePosition + direction * spearMaxDistance;
                Gizmos.DrawLine(releasePosition, endPoint);
                Gizmos.DrawWireSphere(endPoint, spearHitRadius);
            }
        }

        private void DrawHitbox(Vector3 localCenterOffset, Vector3 size, int facing, Color color)
        {
            Gizmos.color = color;
            Vector3 center = transform.position + new Vector3(localCenterOffset.x * facing, localCenterOffset.y, localCenterOffset.z);
            Gizmos.DrawWireCube(center, size);
        }

        private void OnValidate()
        {
            attackPower = Mathf.Max(1, attackPower);
            throwAttackPower = Mathf.Max(1, throwAttackPower);
            thrustActionSeconds = Mathf.Max(0.05f, thrustActionSeconds);
            swingActionSeconds = Mathf.Max(0.05f, swingActionSeconds);
            throwActionSeconds = Mathf.Max(0.05f, throwActionSeconds);
            returnRecoverSeconds = Mathf.Max(0.05f, returnRecoverSeconds);
            spearThrowSpeed = Mathf.Max(0.1f, spearThrowSpeed);
            spearReturnSpeed = Mathf.Max(0.1f, spearReturnSpeed);
            spearMaxDistance = Mathf.Max(0.1f, spearMaxDistance);
            spearHitRadius = Mathf.Max(0.01f, spearHitRadius);
            spearReleaseDelaySeconds = Mathf.Max(0f, spearReleaseDelaySeconds);
            minimumHorizontalThrowHeight = Mathf.Max(0f, minimumHorizontalThrowHeight);
            CacheReferences();
            InitializeTargetMaskIfNeeded();
        }
    }
}
