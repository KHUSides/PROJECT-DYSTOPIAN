using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dystopian.EnemyTest
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerAttackEnemyDamageAdapter : MonoBehaviour
    {
        private const string NormalAttackColliderNamePrefix = "NormalAttackCollider_";
        private const string ChargedAttackHitboxNamePrefix = "ChargedAttackHitbox_";

        [Header("Damage")]
        [SerializeField, Min(1)] private int normalAttackDamage = 25;
        [SerializeField, Min(1)] private int chargedAttackDamage = 40;

        [Header("Target")]
        [SerializeField] private LayerMask enemyHitMask = ~0;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

        [Header("Fallback Hitbox")]
        [SerializeField, Min(0.01f)] private float normalFallbackForwardOffset = 1.15f;
        [SerializeField] private Vector3 normalFallbackCenterOffset = new Vector3(0f, 1f, 0f);
        [SerializeField] private Vector3 normalFallbackSize = new Vector3(1.8f, 1.5f, 1.2f);
        [SerializeField, Min(0.01f)] private float chargedFallbackForwardOffset = 1.6f;
        [SerializeField] private Vector3 chargedFallbackCenterOffset = new Vector3(0f, 1f, 0f);
        [SerializeField] private Vector3 chargedFallbackSize = new Vector3(2.8f, 1.8f, 1.4f);

        [Header("Debug")]
        [SerializeField] private bool logHits = true;

        private readonly List<IDamageable> damagedTargets = new List<IDamageable>();
        private readonly List<Collider> normalAttackColliders = new List<Collider>();
        private PlayerController playerController;
        private bool subscribed;

        private void Reset()
        {
            InitializeEnemyMaskIfNeeded();
        }

        private void Awake()
        {
            CacheReferences();
            CacheNormalAttackColliders();
            InitializeEnemyMaskIfNeeded();
        }

        private void OnEnable()
        {
            CacheReferences();
            Subscribe();
        }

        private void Start()
        {
            CacheReferences();
            CacheNormalAttackColliders();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void HandleNormalAttack()
        {
            Physics.SyncTransforms();
            damagedTargets.Clear();

            int activeColliderCount = 0;
            for (int i = 0; i < normalAttackColliders.Count; i++)
            {
                Collider attackCollider = normalAttackColliders[i];
                if (!IsUsableAttackCollider(attackCollider))
                {
                    continue;
                }

                activeColliderCount++;
                DamageTargetsInCollider(attackCollider, normalAttackDamage, "Player Normal Attack");
            }

            if (activeColliderCount == 0)
            {
                DamageTargetsInFallbackHitbox(
                    normalAttackDamage,
                    normalFallbackCenterOffset,
                    normalFallbackForwardOffset,
                    normalFallbackSize,
                    "Player Normal Attack Fallback");
            }

            LogNoHitIfNeeded("Player Normal Attack");
        }

        private void HandleChargedAttack()
        {
            Physics.SyncTransforms();
            damagedTargets.Clear();

            int activeColliderCount = 0;
            BoxCollider[] boxColliders = FindObjectsByType<BoxCollider>(FindObjectsSortMode.None);
            for (int i = 0; i < boxColliders.Length; i++)
            {
                BoxCollider chargedHitbox = boxColliders[i];
                if (!IsActiveChargedHitbox(chargedHitbox))
                {
                    continue;
                }

                activeColliderCount++;
                DamageTargetsInBoxCollider(chargedHitbox, chargedAttackDamage, "Player Charged Attack");
            }

            if (activeColliderCount == 0)
            {
                DamageTargetsInFallbackHitbox(
                    chargedAttackDamage,
                    chargedFallbackCenterOffset,
                    chargedFallbackForwardOffset,
                    chargedFallbackSize,
                    "Player Charged Attack Fallback");
            }

            LogNoHitIfNeeded("Player Charged Attack");
        }

        private void DamageTargetsInCollider(Collider attackCollider, int damage, string attackName)
        {
            if (attackCollider is BoxCollider boxCollider)
            {
                DamageTargetsInBoxCollider(boxCollider, damage, attackName);
                return;
            }

            Bounds bounds = attackCollider.bounds;
            DamageTargetsInBox(bounds.center, bounds.extents, Quaternion.identity, damage, attackName);
        }

        private void DamageTargetsInBoxCollider(BoxCollider boxCollider, int damage, string attackName)
        {
            Vector3 center = boxCollider.transform.TransformPoint(boxCollider.center);
            Vector3 halfExtents = Vector3.Scale(boxCollider.size * 0.5f, Abs(boxCollider.transform.lossyScale));
            DamageTargetsInBox(center, halfExtents, boxCollider.transform.rotation, damage, attackName);
        }

        private void DamageTargetsInFallbackHitbox(
            int damage,
            Vector3 centerOffset,
            float forwardOffset,
            Vector3 size,
            string attackName)
        {
            Vector3 direction = GetFacingDirection3D();
            Vector3 center = transform.position + centerOffset + direction * forwardOffset;
            Vector3 halfExtents = new Vector3(
                Mathf.Max(0.01f, size.x) * 0.5f,
                Mathf.Max(0.01f, size.y) * 0.5f,
                Mathf.Max(0.01f, size.z) * 0.5f);

            DamageTargetsInBox(center, halfExtents, Quaternion.identity, damage, attackName);
        }

        private void DamageTargetsInBox(
            Vector3 center,
            Vector3 halfExtents,
            Quaternion rotation,
            int damage,
            string attackName)
        {
            Collider[] hits = Physics.OverlapBox(
                center,
                halfExtents,
                rotation,
                enemyHitMask,
                triggerInteraction);

            for (int i = 0; i < hits.Length; i++)
            {
                TryDamageTarget(hits[i], damage, center, attackName);
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

            if (logHits)
            {
                Debug.Log($"[Player Attack Damage] {attackName} hit {hit.name} for {damage} damage.", this);
            }
        }

        private void LogNoHitIfNeeded(string attackName)
        {
            if (logHits && damagedTargets.Count == 0)
            {
                Debug.Log($"[Player Attack Damage] {attackName} did not hit an enemy.", this);
            }
        }

        private Vector3 GetFacingDirection3D()
        {
            Vector2 facing = playerController != null ? playerController.FacingDirection : Vector2.right;
            if (facing.sqrMagnitude <= 0.0001f)
            {
                facing = Vector2.right;
            }

            Vector2 normalized = facing.normalized;
            return new Vector3(normalized.x, normalized.y, 0f);
        }

        private bool IsUsableAttackCollider(Collider attackCollider)
        {
            return attackCollider != null &&
                   attackCollider.enabled &&
                   attackCollider.gameObject.activeInHierarchy;
        }

        private static bool IsNormalAttackCollider(Collider collider)
        {
            return collider != null &&
                   collider.name.StartsWith(NormalAttackColliderNamePrefix, StringComparison.Ordinal);
        }

        private static bool IsActiveChargedHitbox(BoxCollider boxCollider)
        {
            return boxCollider != null &&
                   boxCollider.enabled &&
                   boxCollider.gameObject.activeInHierarchy &&
                   boxCollider.name.StartsWith(ChargedAttackHitboxNamePrefix, StringComparison.Ordinal);
        }

        private void CacheReferences()
        {
            if (playerController == null)
            {
                playerController = GetComponent<PlayerController>();
            }
        }

        private void CacheNormalAttackColliders()
        {
            normalAttackColliders.Clear();
            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (IsNormalAttackCollider(colliders[i]))
                {
                    normalAttackColliders.Add(colliders[i]);
                }
            }
        }

        private void Subscribe()
        {
            if (subscribed || playerController == null)
            {
                return;
            }

            playerController.NormalAttackPerformed += HandleNormalAttack;
            playerController.ChargedAttackPerformed += HandleChargedAttack;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || playerController == null)
            {
                return;
            }

            playerController.NormalAttackPerformed -= HandleNormalAttack;
            playerController.ChargedAttackPerformed -= HandleChargedAttack;
            subscribed = false;
        }

        private void InitializeEnemyMaskIfNeeded()
        {
            if (enemyHitMask.value != 0)
            {
                return;
            }

            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer >= 0)
            {
                enemyHitMask = 1 << enemyLayer;
            }
        }

        private void OnValidate()
        {
            normalAttackDamage = Mathf.Max(1, normalAttackDamage);
            chargedAttackDamage = Mathf.Max(1, chargedAttackDamage);
            normalFallbackForwardOffset = Mathf.Max(0.01f, normalFallbackForwardOffset);
            chargedFallbackForwardOffset = Mathf.Max(0.01f, chargedFallbackForwardOffset);
            normalFallbackSize = ClampSize(normalFallbackSize);
            chargedFallbackSize = ClampSize(chargedFallbackSize);
            InitializeEnemyMaskIfNeeded();
        }

        private static Vector3 ClampSize(Vector3 size)
        {
            return new Vector3(
                Mathf.Max(0.01f, size.x),
                Mathf.Max(0.01f, size.y),
                Mathf.Max(0.01f, size.z));
        }

        private static Vector3 Abs(Vector3 value)
        {
            return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
        }
    }
}
