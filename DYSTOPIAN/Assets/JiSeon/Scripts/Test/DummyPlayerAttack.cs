using System.Collections.Generic;
using UnityEngine;

namespace Dystopian.EnemyTest
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class DummyPlayerAttack : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private KeyCode attackKey = KeyCode.J;

        [Header("Attack")]
        [SerializeField, Min(1)] private int damage = 25;
        [SerializeField, Min(0.01f)] private float forwardOffset = 1.15f;
        [SerializeField, Min(0.01f)] private float width = 1.6f;
        [SerializeField, Min(0.01f)] private float height = 1.4f;
        [SerializeField, Min(0.01f)] private float depth = 1.2f;
        [SerializeField] private Vector3 centerOffset = new Vector3(0f, 1f, 0f);
        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField] private bool logHits = true;

        [Header("Temporary Contact Note Hit")]
        [SerializeField] private bool enableContactNoteHit = true;
        [SerializeField] private KeyCode contactNoteHitKey = KeyCode.A;
        [SerializeField, Min(1)] private int contactNoteDamage = 25;
        [SerializeField, Min(0.01f)] private float contactSearchRadius = 2f;
        [SerializeField, Min(0f)] private float contactDistanceTolerance = 0.65f;
        [SerializeField, Min(0f)] private float contactVerticalTolerance = 0.35f;
        [SerializeField, Min(0f)] private float contactDepthTolerance = 0.45f;
        [SerializeField] private Vector3 contactCenterOffset = Vector3.zero;

        [Header("Debug View")]
        [SerializeField, Min(0.01f)] private float activeGizmoSeconds = 0.12f;
        [SerializeField] private Color idleGizmoColor = new Color(1f, 0.85f, 0.1f, 0.2f);
        [SerializeField] private Color activeGizmoColor = new Color(1f, 0.1f, 0.1f, 0.35f);
        [SerializeField] private Color contactGizmoColor = new Color(0.1f, 1f, 0.2f, 0.25f);

        private readonly List<IDamageable> damagedTargets = new List<IDamageable>();
        private DummyPlayerController dummyController;
        private Collider playerContactCollider;
        private float lastAttackTime = -999f;
        private int lastContactNoteHitFrame = -1;
        private bool contactNoteHitQueued;
        private int contactNoteHitQueuedFrame = -1;
        private int fallbackFacingSign = 1;

        private void Awake()
        {
            CacheComponentsIfNeeded();
        }

        private void Update()
        {
            UpdateFallbackFacing();

            if (Input.GetKeyDown(attackKey))
            {
                PerformAttack();
            }

            if (enableContactNoteHit && Input.GetKeyDown(contactNoteHitKey))
            {
                contactNoteHitQueued = true;
                contactNoteHitQueuedFrame = Time.frameCount;
            }
        }

        private void LateUpdate()
        {
            if (!contactNoteHitQueued)
            {
                return;
            }

            if (contactNoteHitQueuedFrame != Time.frameCount)
            {
                contactNoteHitQueued = false;
                return;
            }

            contactNoteHitQueued = false;
            PerformContactNoteHit();
        }

        public void PerformAttack()
        {
            lastAttackTime = Time.time;
            damagedTargets.Clear();

            Collider[] hits = Physics.OverlapBox(
                GetAttackCenter(),
                GetAttackHalfExtents(),
                Quaternion.identity,
                hitMask,
                QueryTriggerInteraction.Collide);

            foreach (Collider hit in hits)
            {
                if (hit == null)
                {
                    continue;
                }

                if (hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                IDamageable damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.IsAlive || damagedTargets.Contains(damageable))
                {
                    continue;
                }

                damagedTargets.Add(damageable);
                Vector3 hitPoint = hit.ClosestPoint(transform.position);
                damageable.TakeDamage(new DamageInfo(damage, hitPoint, gameObject));

                if (logHits)
                {
                    Debug.Log($"[Test Attack] Hit {hit.name} for {damage} damage.", this);
                }
            }

            if (logHits && damagedTargets.Count == 0)
            {
                Debug.Log("[Test Attack] No damageable target in range.", this);
            }
        }

        public void PerformContactNoteHit()
        {
            CacheComponentsIfNeeded();
            Physics.SyncTransforms();

            if (lastContactNoteHitFrame == Time.frameCount)
            {
                return;
            }

            lastContactNoteHitFrame = Time.frameCount;
            lastAttackTime = Time.time;
            damagedTargets.Clear();

            Collider[] hits = Physics.OverlapSphere(
                GetContactCenter(),
                contactSearchRadius + contactDistanceTolerance,
                hitMask,
                QueryTriggerInteraction.Collide);

            foreach (Collider hit in hits)
            {
                if (hit == null)
                {
                    continue;
                }

                if (!IsTouchingAtInputMoment(hit))
                {
                    continue;
                }

                IDamageable damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.IsAlive || damagedTargets.Contains(damageable))
                {
                    continue;
                }

                damagedTargets.Add(damageable);
                Vector3 hitPoint = hit.ClosestPoint(transform.position);
                damageable.TakeDamage(new DamageInfo(contactNoteDamage, hitPoint, gameObject));

                if (logHits)
                {
                    Debug.Log($"[Contact Note Hit] Hit {hit.name} for {contactNoteDamage} damage.", this);
                }
            }

            if (logHits && damagedTargets.Count == 0)
            {
                Debug.Log("[Contact Note Hit] No damageable target touching the player.", this);
            }
        }

        private void UpdateFallbackFacing()
        {
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                fallbackFacingSign = -1;
            }
            else if (Input.GetKey(KeyCode.RightArrow))
            {
                fallbackFacingSign = 1;
            }
        }

        private int GetFacingSign()
        {
            CacheComponentsIfNeeded();
            return dummyController != null ? dummyController.FacingSign : fallbackFacingSign;
        }

        private Vector3 GetAttackCenter()
        {
            return transform.position + centerOffset + Vector3.right * (GetFacingSign() * forwardOffset);
        }

        private Vector3 GetAttackHalfExtents()
        {
            return new Vector3(width * 0.5f, height * 0.5f, depth * 0.5f);
        }

        private Vector3 GetContactCenter()
        {
            CacheComponentsIfNeeded();

            if (playerContactCollider != null)
            {
                return playerContactCollider.bounds.center + contactCenterOffset;
            }

            return transform.position + contactCenterOffset;
        }

        private bool IsTouchingAtInputMoment(Collider target)
        {
            CacheComponentsIfNeeded();

            if (target == null || target.transform.IsChildOf(transform))
            {
                return false;
            }

            if (playerContactCollider == null)
            {
                Vector3 fallbackPoint = target.ClosestPoint(GetContactCenter());
                return (fallbackPoint - GetContactCenter()).sqrMagnitude <= contactDistanceTolerance * contactDistanceTolerance;
            }

            Bounds playerBounds = playerContactCollider.bounds;
            Bounds targetBounds = target.bounds;
            float xGap = GetAxisGap(playerBounds.min.x, playerBounds.max.x, targetBounds.min.x, targetBounds.max.x);
            float yGap = GetAxisGap(playerBounds.min.y, playerBounds.max.y, targetBounds.min.y, targetBounds.max.y);
            float zGap = GetAxisGap(playerBounds.min.z, playerBounds.max.z, targetBounds.min.z, targetBounds.max.z);

            if (playerBounds.Intersects(targetBounds))
            {
                return true;
            }

            if (xGap <= contactDistanceTolerance &&
                yGap <= contactVerticalTolerance &&
                zGap <= contactDepthTolerance)
            {
                return true;
            }

            Vector3 playerClosestPoint = playerContactCollider.ClosestPoint(targetBounds.center);
            Vector3 targetClosestPoint = target.ClosestPoint(playerClosestPoint);
            float sqrDistance = (playerClosestPoint - targetClosestPoint).sqrMagnitude;
            return sqrDistance <= contactDistanceTolerance * contactDistanceTolerance;
        }

        private static float GetAxisGap(float minA, float maxA, float minB, float maxB)
        {
            if (maxA < minB)
            {
                return minB - maxA;
            }

            if (maxB < minA)
            {
                return minA - maxB;
            }

            return 0f;
        }

        private void CacheComponentsIfNeeded()
        {
            if (dummyController == null)
            {
                dummyController = GetComponent<DummyPlayerController>();
            }

            if (playerContactCollider == null)
            {
                playerContactCollider = GetComponent<Collider>();
            }
        }

        private void OnDrawGizmosSelected()
        {
            bool isActive = Application.isPlaying && Time.time - lastAttackTime <= activeGizmoSeconds;
            Gizmos.color = isActive ? activeGizmoColor : idleGizmoColor;
            Gizmos.DrawCube(GetAttackCenter(), GetAttackHalfExtents() * 2f);

            if (enableContactNoteHit)
            {
                Gizmos.color = contactGizmoColor;
                Gizmos.DrawWireSphere(GetContactCenter(), contactSearchRadius);

                CacheComponentsIfNeeded();
                if (playerContactCollider != null)
                {
                    Bounds contactBounds = playerContactCollider.bounds;
                    contactBounds.Expand(new Vector3(
                        contactDistanceTolerance * 2f,
                        contactVerticalTolerance * 2f,
                        contactDepthTolerance * 2f));
                    Gizmos.DrawWireCube(contactBounds.center + contactCenterOffset, contactBounds.size);
                }
            }
        }

        private void OnValidate()
        {
            damage = Mathf.Max(1, damage);
            forwardOffset = Mathf.Max(0.01f, forwardOffset);
            width = Mathf.Max(0.01f, width);
            height = Mathf.Max(0.01f, height);
            depth = Mathf.Max(0.01f, depth);
            contactNoteDamage = Mathf.Max(1, contactNoteDamage);
            contactSearchRadius = Mathf.Max(0.01f, contactSearchRadius);
            contactDistanceTolerance = Mathf.Max(0f, contactDistanceTolerance);
            contactVerticalTolerance = Mathf.Max(0f, contactVerticalTolerance);
            contactDepthTolerance = Mathf.Max(0f, contactDepthTolerance);
            activeGizmoSeconds = Mathf.Max(0.01f, activeGizmoSeconds);
        }
    }
}
