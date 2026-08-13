using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dystopian.EnemyTest
{
    [DisallowMultipleComponent]
    public sealed class MerlinSpearProjectile : MonoBehaviour
    {
        private enum ProjectileState
        {
            Outgoing,
            Stuck,
            Returning
        }

        [SerializeField] private bool drawDebugGizmos = true;
        [SerializeField] private bool logLifecycle = true;
        [SerializeField] private bool lockOutgoingToHorizontalLine;
        [SerializeField] private bool lockToSideViewPlane = true;
        [SerializeField] private float lockedZ = 0f;
        [SerializeField] private bool clampOutgoingVerticalAngle = true;
        [SerializeField, Range(0f, 89f)] private float maxOutgoingUpAngleDegrees = 45f;
        [SerializeField, Range(0f, 89f)] private float maxOutgoingDownAngleDegrees = 18f;
        [SerializeField] private bool preventOutgoingGroundHit = true;
        [SerializeField] private LayerMask outgoingGroundSafetyMask;
        [SerializeField] private float fallbackOutgoingGroundY = 0f;
        [SerializeField, Min(0f)] private float outgoingGroundClearance = 0.35f;
        [SerializeField, Min(0.1f)] private float outgoingGroundProbeDistance = 20f;
        [SerializeField] private bool useTipAsBlockingProbe = true;
        [SerializeField] private bool autoCalculateTipForwardOffset = true;
        [SerializeField, Min(0f)] private float spearTipForwardOffset = 1.9f;
        [SerializeField] private float spearVisualRollDegrees = 90f;
        [SerializeField] private bool onlyStickToFacingWallsDuringHorizontalThrow = true;
        [SerializeField, Range(0f, 1f)] private float minimumFacingWallNormalDot = 0.35f;
        [SerializeField] private bool addRuntimeTrail = true;
        [SerializeField, Min(0.01f)] private float trailSeconds = 0.35f;
        [SerializeField, Min(0.001f)] private float trailStartWidth = 0.08f;
        [SerializeField, Min(0.001f)] private float trailEndWidth = 0.015f;
        [SerializeField] private LayerMask blockingMask;
        [SerializeField] private QueryTriggerInteraction targetTriggerInteraction = QueryTriggerInteraction.Collide;
        [SerializeField] private QueryTriggerInteraction blockingTriggerInteraction = QueryTriggerInteraction.Ignore;
        [SerializeField, Min(0f)] private float minimumBlockingDistance = 0.05f;
        [SerializeField, Min(0f)] private float stuckSecondsBeforeReturn = 0.18f;
        [SerializeField, Min(0f)] private float maxDistanceReturnDelaySeconds = 0.12f;
        [SerializeField, Min(0.001f)] private float stickSurfaceOffset = 0.03f;
        [SerializeField, Min(0.01f)] private float returnArriveDistance = 0.15f;

        [Header("Blocking Impact Feedback")]
        [SerializeField] private bool shakeCameraOnBlockingImpact = true;
        [SerializeField, Min(0f)] private float blockingImpactShakeDuration = 0.28f;
        [SerializeField, Min(0f)] private float blockingImpactShakeAmplitude = 0.35f;
        [SerializeField, Min(0.1f)] private float blockingImpactShakeFrequency = 55f;

        private readonly List<IDamageable> damagedTargets = new List<IDamageable>();
        private readonly RaycastHit[] blockerHitBuffer = new RaycastHit[16];
        private GameObject owner;
        private Transform returnSocket;
        private Action<MerlinSpearProjectile> returned;
        private LayerMask targetMask;
        private ProjectileState state;
        private Vector3 startPosition;
        private Vector3 previousPosition;
        private Vector3 direction;
        private Vector3 plannedEndPosition;
        private int damage;
        private float throwSpeed;
        private float returnSpeed;
        private float maxDistance;
        private float hitRadius;
        private float travelledDistance;
        private float stuckElapsed;
        private float currentStuckDuration;
        private bool initialized;
        private TrailRenderer runtimeTrail;
        private Transform runtimeTrailAnchor;
        private float currentTipForwardOffset;
        private static Material runtimeTrailMaterial;

        public void Initialize(
            GameObject ownerObject,
            Transform returnTarget,
            Vector3 targetWorldPosition,
            int facingSign,
            int attackDamage,
            float outgoingSpeed,
            float incomingSpeed,
            float maximumDistance,
            float radius,
            LayerMask hitMask,
            LayerMask blockMask,
            Action<MerlinSpearProjectile> onReturned)
        {
            owner = ownerObject;
            returnSocket = returnTarget;
            returned = onReturned;
            targetMask = hitMask;
            blockingMask = blockMask;
            InitializeGroundSafetyMaskIfNeeded();
            damage = Mathf.Max(1, attackDamage);
            throwSpeed = Mathf.Max(0.1f, outgoingSpeed);
            returnSpeed = Mathf.Max(0.1f, incomingSpeed);
            maxDistance = Mathf.Max(0.1f, maximumDistance);
            hitRadius = Mathf.Max(0.01f, radius);
            transform.position = LockToSideViewPlane(transform.position);
            startPosition = transform.position;
            previousPosition = startPosition;
            travelledDistance = 0f;
            stuckElapsed = 0f;
            currentStuckDuration = stuckSecondsBeforeReturn;
            direction = CalculateThrowDirection(targetWorldPosition, facingSign);
            plannedEndPosition = startPosition + direction * maxDistance;
            state = ProjectileState.Outgoing;
            initialized = true;

            ApplyRotationAlongVelocity(direction);
            currentTipForwardOffset = ResolveTipForwardOffset();
            SetupRuntimeTrail();

            if (logLifecycle)
            {
                Debug.Log(
                    $"[Merlin Spear] spawned at {FormatVector(startPosition)}, tip {FormatVector(GetTipPosition(startPosition))}, direction {FormatVector(direction)}, maxDistance {maxDistance:F2}.",
                    this);
            }
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            switch (state)
            {
                case ProjectileState.Outgoing:
                    UpdateOutgoing();
                    break;
                case ProjectileState.Stuck:
                    UpdateStuck();
                    break;
                case ProjectileState.Returning:
                    UpdateReturning();
                    break;
            }
        }

        private void UpdateOutgoing()
        {
            float stepDistance = throwSpeed * Time.deltaTime;
            float remainingDistance = maxDistance - travelledDistance;
            if (remainingDistance <= 0f)
            {
                BeginStuck(maxDistanceReturnDelaySeconds, "max distance", false);
                return;
            }

            stepDistance = Mathf.Min(stepDistance, remainingDistance);
            Vector3 from = LockToSideViewPlane(transform.position);
            Vector3 to = LockToSideViewPlane(from + direction * stepDistance);
            transform.position = from;
            Physics.SyncTransforms();

            if (TryHitBlocker(from, stepDistance, out RaycastHit blockerHit))
            {
                Vector3 blockedPosition = GetRootPositionForTipHit(blockerHit.point);
                DamageTargetsAlongSpearSweep(from, blockedPosition);
                transform.position = blockedPosition;
                previousPosition = blockedPosition;
                travelledDistance += blockerHit.distance;
                BeginStuck(stuckSecondsBeforeReturn, blockerHit.collider != null ? blockerHit.collider.name : "environment", true);
                return;
            }

            transform.position = to;
            DamageTargetsAlongSpearSweep(from, to);
            ApplyRotationAlongVelocity(to - previousPosition);
            previousPosition = to;
            travelledDistance += stepDistance;

            if (travelledDistance >= maxDistance)
            {
                BeginStuck(maxDistanceReturnDelaySeconds, "max distance", false);
            }
        }

        private void UpdateStuck()
        {
            stuckElapsed += Time.deltaTime;
            if (stuckElapsed >= currentStuckDuration)
            {
                BeginReturn();
            }
        }

        private void UpdateReturning()
        {
            if (returnSocket == null)
            {
                CompleteReturn();
                return;
            }

            Vector3 from = LockToSideViewPlane(transform.position);
            Vector3 targetPosition = LockToSideViewPlane(returnSocket.position);
            Vector3 nextPosition = LockToSideViewPlane(Vector3.MoveTowards(from, targetPosition, returnSpeed * Time.deltaTime));

            transform.position = nextPosition;
            ApplyRotationAlongVelocity(nextPosition - from);
            previousPosition = nextPosition;

            if (Vector3.Distance(nextPosition, targetPosition) <= returnArriveDistance)
            {
                CompleteReturn();
            }
        }

        private bool TryHitBlocker(Vector3 from, float distance, out RaycastHit hit)
        {
            if (blockingMask.value == 0 || distance <= 0f)
            {
                hit = default;
                return false;
            }

            Vector3 castOrigin = useTipAsBlockingProbe ? GetTipPosition(from) : from;
            Ray ray = new Ray(castOrigin, direction);
            int hitCount = Physics.SphereCastNonAlloc(
                ray,
                hitRadius,
                blockerHitBuffer,
                distance,
                blockingMask,
                blockingTriggerInteraction);

            int closestValidHitIndex = -1;
            float closestValidHitDistance = float.PositiveInfinity;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit candidate = blockerHitBuffer[i];
                if (candidate.collider == null)
                {
                    continue;
                }

                if (travelledDistance + candidate.distance < minimumBlockingDistance)
                {
                    continue;
                }

                if (!IsValidHorizontalBlocker(candidate))
                {
                    continue;
                }

                if (candidate.distance < closestValidHitDistance)
                {
                    closestValidHitIndex = i;
                    closestValidHitDistance = candidate.distance;
                }
            }

            if (closestValidHitIndex < 0)
            {
                hit = default;
                return false;
            }

            hit = blockerHitBuffer[closestValidHitIndex];
            return true;
        }

        private bool IsValidHorizontalBlocker(RaycastHit candidate)
        {
            if (!onlyStickToFacingWallsDuringHorizontalThrow || !lockOutgoingToHorizontalLine)
            {
                return true;
            }

            Vector3 horizontalDirection = new Vector3(direction.x, 0f, 0f);
            if (horizontalDirection.sqrMagnitude <= 0.0001f)
            {
                return true;
            }

            horizontalDirection.Normalize();
            return Vector3.Dot(candidate.normal, -horizontalDirection) >= minimumFacingWallNormalDot;
        }

        private Vector3 GetRootPositionForTipHit(Vector3 hitPoint)
        {
            Vector3 lockedHitPoint = LockToSideViewPlane(hitPoint);

            if (!useTipAsBlockingProbe)
            {
                return lockedHitPoint - direction * stickSurfaceOffset;
            }

            return lockedHitPoint - direction * (currentTipForwardOffset + stickSurfaceOffset);
        }

        private void DamageTargetsAlongSpearSweep(Vector3 fromRoot, Vector3 toRoot)
        {
            if (!useTipAsBlockingProbe)
            {
                DamageTargetsBetween(fromRoot, toRoot);
                return;
            }

            Vector3 toTip = GetTipPosition(toRoot);
            DamageTargetsBetween(fromRoot, toTip);
        }

        private void DamageTargetsBetween(Vector3 from, Vector3 to)
        {
            Physics.SyncTransforms();

            Collider[] hits;
            if ((to - from).sqrMagnitude <= 0.0001f)
            {
                hits = Physics.OverlapSphere(
                    to,
                    hitRadius,
                    targetMask,
                    targetTriggerInteraction);
            }
            else
            {
                hits = Physics.OverlapCapsule(
                    from,
                    to,
                    hitRadius,
                    targetMask,
                    targetTriggerInteraction);
            }

            for (int i = 0; i < hits.Length; i++)
            {
                TryDamageTarget(hits[i], to);
            }
        }

        private void TryDamageTarget(Collider hit, Vector3 hitPointFallback)
        {
            if (hit == null)
            {
                return;
            }

            if (owner != null && hit.transform.IsChildOf(owner.transform))
            {
                return;
            }

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable == null || !damageable.IsAlive || damagedTargets.Contains(damageable))
            {
                return;
            }

            damagedTargets.Add(damageable);
            Vector3 hitPoint = hit.ClosestPoint(hitPointFallback);
            damageable.TakeDamage(new DamageInfo(damage, hitPoint, owner));

            if (logLifecycle)
            {
                Debug.Log($"[Merlin Spear] pierced {hit.name} for {damage} damage at {FormatVector(hitPoint)}.", this);
            }
        }

        private Vector3 CalculateThrowDirection(Vector3 targetWorldPosition, int facingSign)
        {
            int sign = facingSign >= 0 ? 1 : -1;

            if (lockOutgoingToHorizontalLine)
            {
                return Vector3.right * sign;
            }

            Vector3 toTarget = LockToSideViewPlane(targetWorldPosition) - startPosition;

            if (toTarget.sqrMagnitude > 0.01f && Mathf.Sign(toTarget.x) == sign)
            {
                return ClampOutgoingVerticalAngle(toTarget.normalized, sign);
            }

            return Vector3.right * sign;
        }

        private Vector3 ClampOutgoingVerticalAngle(Vector3 rawDirection, int facingSign)
        {
            if (!clampOutgoingVerticalAngle)
            {
                return rawDirection;
            }

            int sign = facingSign >= 0 ? 1 : -1;
            float horizontalMagnitude = Mathf.Abs(rawDirection.x);
            if (horizontalMagnitude <= 0.0001f)
            {
                return Vector3.right * sign;
            }

            float angleDegrees = Mathf.Atan2(rawDirection.y, horizontalMagnitude) * Mathf.Rad2Deg;
            float minAngle = -Mathf.Clamp(maxOutgoingDownAngleDegrees, 0f, 89f);
            float maxAngle = Mathf.Clamp(maxOutgoingUpAngleDegrees, 0f, 89f);
            if (preventOutgoingGroundHit)
            {
                minAngle = Mathf.Max(minAngle, CalculateGroundSafeMinimumAngle());
            }

            float clampedAngle = Mathf.Clamp(angleDegrees, minAngle, maxAngle);
            float clampedRadians = clampedAngle * Mathf.Deg2Rad;

            return new Vector3(
                Mathf.Cos(clampedRadians) * sign,
                Mathf.Sin(clampedRadians),
                0f).normalized;
        }

        private float CalculateGroundSafeMinimumAngle()
        {
            float safetyDistance = Mathf.Max(0.1f, maxDistance + Mathf.Max(0f, spearTipForwardOffset));
            float minimumY = ResolveOutgoingGroundSafetyY();

            if (startPosition.y <= minimumY)
            {
                return 0f;
            }

            return Mathf.Atan2(minimumY - startPosition.y, safetyDistance) * Mathf.Rad2Deg;
        }

        private float ResolveOutgoingGroundSafetyY()
        {
            float groundY = fallbackOutgoingGroundY;

            if (outgoingGroundSafetyMask.value != 0)
            {
                Vector3 rayOrigin = startPosition + Vector3.up * 0.05f;
                if (Physics.Raycast(
                        rayOrigin,
                        Vector3.down,
                        out RaycastHit hit,
                        outgoingGroundProbeDistance,
                        outgoingGroundSafetyMask,
                        QueryTriggerInteraction.Ignore))
                {
                    groundY = hit.point.y;
                }
            }

            return groundY + outgoingGroundClearance;
        }

        private void InitializeGroundSafetyMaskIfNeeded()
        {
            if (outgoingGroundSafetyMask.value != 0)
            {
                return;
            }

            int environmentLayer = LayerMask.NameToLayer("Environment");
            if (environmentLayer >= 0)
            {
                outgoingGroundSafetyMask = 1 << environmentLayer;
            }
        }

        private Vector3 LockToSideViewPlane(Vector3 position)
        {
            if (lockToSideViewPlane)
            {
                position.z = lockedZ;
            }

            return position;
        }

        private Vector3 GetTipPosition(Vector3 rootPosition)
        {
            if (!useTipAsBlockingProbe)
            {
                return rootPosition;
            }

            return LockToSideViewPlane(rootPosition + direction * currentTipForwardOffset);
        }

        private float ResolveTipForwardOffset()
        {
            if (!autoCalculateTipForwardOffset)
            {
                return Mathf.Max(0f, spearTipForwardOffset);
            }

            float calculatedOffset = CalculateRendererTipForwardOffset();
            if (calculatedOffset > 0.01f)
            {
                spearTipForwardOffset = calculatedOffset;
            }

            return Mathf.Max(0f, spearTipForwardOffset);
        }

        private float CalculateRendererTipForwardOffset()
        {
            float maxLocalY = float.MinValue;
            bool foundRendererBounds = false;

            MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < meshFilters.Length; i++)
            {
                MeshFilter meshFilter = meshFilters[i];
                if (meshFilter == null || meshFilter.sharedMesh == null)
                {
                    continue;
                }

                Bounds meshBounds = meshFilter.sharedMesh.bounds;
                Vector3 min = meshBounds.min;
                Vector3 max = meshBounds.max;

                AddMeshBoundsCorner(meshFilter.transform, new Vector3(min.x, min.y, min.z), ref maxLocalY, ref foundRendererBounds);
                AddMeshBoundsCorner(meshFilter.transform, new Vector3(min.x, min.y, max.z), ref maxLocalY, ref foundRendererBounds);
                AddMeshBoundsCorner(meshFilter.transform, new Vector3(min.x, max.y, min.z), ref maxLocalY, ref foundRendererBounds);
                AddMeshBoundsCorner(meshFilter.transform, new Vector3(min.x, max.y, max.z), ref maxLocalY, ref foundRendererBounds);
                AddMeshBoundsCorner(meshFilter.transform, new Vector3(max.x, min.y, min.z), ref maxLocalY, ref foundRendererBounds);
                AddMeshBoundsCorner(meshFilter.transform, new Vector3(max.x, min.y, max.z), ref maxLocalY, ref foundRendererBounds);
                AddMeshBoundsCorner(meshFilter.transform, new Vector3(max.x, max.y, min.z), ref maxLocalY, ref foundRendererBounds);
                AddMeshBoundsCorner(meshFilter.transform, new Vector3(max.x, max.y, max.z), ref maxLocalY, ref foundRendererBounds);
            }

            return foundRendererBounds ? Mathf.Max(0f, maxLocalY) : 0f;
        }

        private void AddMeshBoundsCorner(Transform meshTransform, Vector3 localCorner, ref float maxLocalY, ref bool foundRendererBounds)
        {
            Vector3 rootLocalCorner = transform.InverseTransformPoint(meshTransform.TransformPoint(localCorner));
            maxLocalY = Mathf.Max(maxLocalY, rootLocalCorner.y);
            foundRendererBounds = true;
        }

        private void SetupRuntimeTrail()
        {
            if (!addRuntimeTrail)
            {
                return;
            }

            if (runtimeTrail == null)
            {
                if (runtimeTrailAnchor == null)
                {
                    runtimeTrailAnchor = new GameObject("Runtime_TipTrail").transform;
                    runtimeTrailAnchor.SetParent(transform, false);
                }

                runtimeTrailAnchor.localPosition = new Vector3(0f, currentTipForwardOffset, 0f);
                runtimeTrailAnchor.localRotation = Quaternion.identity;
                runtimeTrailAnchor.localScale = Vector3.one;

                runtimeTrail = runtimeTrailAnchor.GetComponent<TrailRenderer>();
                if (runtimeTrail == null)
                {
                    runtimeTrail = runtimeTrailAnchor.gameObject.AddComponent<TrailRenderer>();
                }
            }
            else if (runtimeTrailAnchor != null)
            {
                runtimeTrailAnchor.localPosition = new Vector3(0f, currentTipForwardOffset, 0f);
            }

            runtimeTrail.time = trailSeconds;
            runtimeTrail.startWidth = trailStartWidth;
            runtimeTrail.endWidth = trailEndWidth;
            runtimeTrail.autodestruct = false;
            runtimeTrail.emitting = true;
            runtimeTrail.shadowCastingMode = ShadowCastingMode.Off;
            runtimeTrail.receiveShadows = false;
            runtimeTrail.alignment = LineAlignment.View;
            runtimeTrail.material = GetRuntimeTrailMaterial();
            runtimeTrail.startColor = new Color(1f, 0.86f, 0.15f, 0.95f);
            runtimeTrail.endColor = new Color(1f, 0.25f, 0.05f, 0f);
            runtimeTrail.Clear();
        }

        private static Material GetRuntimeTrailMaterial()
        {
            if (runtimeTrailMaterial != null)
            {
                return runtimeTrailMaterial;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            runtimeTrailMaterial = new Material(shader)
            {
                name = "MerlinSpearTrail_Runtime"
            };

            if (runtimeTrailMaterial.HasProperty("_BaseColor"))
            {
                runtimeTrailMaterial.SetColor("_BaseColor", new Color(1f, 0.75f, 0.05f, 1f));
            }
            else if (runtimeTrailMaterial.HasProperty("_Color"))
            {
                runtimeTrailMaterial.SetColor("_Color", new Color(1f, 0.75f, 0.05f, 1f));
            }

            return runtimeTrailMaterial;
        }

        private void OnValidate()
        {
            trailSeconds = Mathf.Max(0.01f, trailSeconds);
            trailStartWidth = Mathf.Max(0.001f, trailStartWidth);
            trailEndWidth = Mathf.Max(0.001f, trailEndWidth);
            spearTipForwardOffset = Mathf.Max(0f, spearTipForwardOffset);
            spearVisualRollDegrees = Mathf.Repeat(spearVisualRollDegrees + 180f, 360f) - 180f;
            maxOutgoingUpAngleDegrees = Mathf.Clamp(maxOutgoingUpAngleDegrees, 0f, 89f);
            maxOutgoingDownAngleDegrees = Mathf.Clamp(maxOutgoingDownAngleDegrees, 0f, 89f);
            fallbackOutgoingGroundY = Mathf.Max(-1000f, fallbackOutgoingGroundY);
            outgoingGroundClearance = Mathf.Max(0f, outgoingGroundClearance);
            outgoingGroundProbeDistance = Mathf.Max(0.1f, outgoingGroundProbeDistance);
            InitializeGroundSafetyMaskIfNeeded();
            minimumFacingWallNormalDot = Mathf.Clamp01(minimumFacingWallNormalDot);
            stuckSecondsBeforeReturn = Mathf.Max(0f, stuckSecondsBeforeReturn);
            maxDistanceReturnDelaySeconds = Mathf.Max(0f, maxDistanceReturnDelaySeconds);
            minimumBlockingDistance = Mathf.Max(0f, minimumBlockingDistance);
            stickSurfaceOffset = Mathf.Max(0.001f, stickSurfaceOffset);
            returnArriveDistance = Mathf.Max(0.01f, returnArriveDistance);
            blockingImpactShakeDuration = Mathf.Max(0f, blockingImpactShakeDuration);
            blockingImpactShakeAmplitude = Mathf.Max(0f, blockingImpactShakeAmplitude);
            blockingImpactShakeFrequency = Mathf.Max(0.1f, blockingImpactShakeFrequency);
        }

        private void BeginStuck(float duration, string reason, bool triggerBlockingImpactShake)
        {
            state = ProjectileState.Stuck;
            stuckElapsed = 0f;
            currentStuckDuration = Mathf.Max(0f, duration);
            ApplyRotationAlongVelocity(direction);

            if (triggerBlockingImpactShake)
            {
                TriggerBlockingImpactCameraShake();
            }

            if (logLifecycle)
            {
                Debug.Log($"[Merlin Spear] stuck by {reason} at {FormatVector(transform.position)} for {currentStuckDuration:F2}s.", this);
            }
        }

        private void TriggerBlockingImpactCameraShake()
        {
            if (!shakeCameraOnBlockingImpact)
            {
                return;
            }

            MerlinCameraShake.ShakeMainCamera(
                blockingImpactShakeDuration,
                blockingImpactShakeAmplitude,
                blockingImpactShakeFrequency);
        }

        private void BeginReturn()
        {
            if (state == ProjectileState.Returning)
            {
                return;
            }

            state = ProjectileState.Returning;
            previousPosition = transform.position;

            if (logLifecycle)
            {
                Debug.Log($"[Merlin Spear] returning from {FormatVector(transform.position)}.", this);
            }
        }

        private void CompleteReturn()
        {
            if (!initialized)
            {
                return;
            }

            initialized = false;
            returned?.Invoke(this);
            Destroy(gameObject);
        }

        private static string FormatVector(Vector3 value)
        {
            return $"({value.x:F2}, {value.y:F2}, {value.z:F2})";
        }

        private void ApplyRotationAlongVelocity(Vector3 velocity)
        {
            if (velocity.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg - 90f;
            Quaternion directionRotation = Quaternion.Euler(0f, 0f, angle);
            Quaternion sideViewBladeRoll = Quaternion.AngleAxis(spearVisualRollDegrees, Vector3.up);
            transform.rotation = directionRotation * sideViewBladeRoll;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos)
            {
                return;
            }

            Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.85f);
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.01f, hitRadius));

            if (initialized)
            {
                Gizmos.DrawLine(startPosition, plannedEndPosition);
            }
        }
    }
}
