using UnityEngine;

namespace Dystopian.EnemyTest
{
    [DisallowMultipleComponent]
    public sealed class EnemyProjectile : MonoBehaviour
    {
        [Header("Projectile")]
        [SerializeField, Min(1)] private int damage = 8;
        [SerializeField, Min(0.01f)] private float speed = 8f;
        [SerializeField, Min(0.01f)] private float lifeTimeSeconds = 3f;
        [SerializeField, Min(0.01f)] private float hitRadius = 0.4f;
        [SerializeField] private LayerMask targetMask;
        [SerializeField] private bool destroyOnHit = true;
        [SerializeField] private bool logHits = true;

        private Vector3 direction = Vector3.right;
        private GameObject source;
        private float aliveSeconds;
        private bool hasHit;

        public void Initialize(
            int projectileDamage,
            Vector3 projectileDirection,
            GameObject projectileSource,
            LayerMask projectileTargetMask,
            float projectileSpeed,
            float projectileLifeTimeSeconds,
            float projectileHitRadius)
        {
            damage = Mathf.Max(1, projectileDamage);
            direction = projectileDirection.sqrMagnitude > 0f ? projectileDirection.normalized : Vector3.right;
            source = projectileSource;
            targetMask = projectileTargetMask;
            speed = Mathf.Max(0.01f, projectileSpeed);
            lifeTimeSeconds = Mathf.Max(0.01f, projectileLifeTimeSeconds);
            hitRadius = Mathf.Max(0.01f, projectileHitRadius);
            aliveSeconds = 0f;
            hasHit = false;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            if (hasHit)
            {
                return;
            }

            float safeDeltaTime = Mathf.Max(0f, deltaTime);
            transform.position += direction * (speed * safeDeltaTime);
            aliveSeconds += safeDeltaTime;
            Physics.SyncTransforms();

            CheckHit();

            if (!hasHit && aliveSeconds >= lifeTimeSeconds)
            {
                DestroySelf();
            }
        }

        private void CheckHit()
        {
            Collider[] hits = Physics.OverlapSphere(
                transform.position,
                hitRadius,
                targetMask,
                QueryTriggerInteraction.Collide);

            foreach (Collider hit in hits)
            {
                if (hit == null)
                {
                    continue;
                }

                if (source != null && hit.transform.IsChildOf(source.transform))
                {
                    continue;
                }

                IDamageable damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.IsAlive)
                {
                    continue;
                }

                hasHit = true;
                Vector3 hitPoint = hit.ClosestPoint(transform.position);
                damageable.TakeDamage(new DamageInfo(damage, hitPoint, source != null ? source : gameObject));

                if (logHits)
                {
                    Debug.Log($"[Enemy Projectile] {name} hit {hit.name} for {damage} damage.", this);
                }

                if (destroyOnHit)
                {
                    DestroySelf();
                }

                return;
            }
        }

        private void DestroySelf()
        {
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.45f, 0.15f, 1f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, hitRadius);
        }

        private void OnValidate()
        {
            damage = Mathf.Max(1, damage);
            speed = Mathf.Max(0.01f, speed);
            lifeTimeSeconds = Mathf.Max(0.01f, lifeTimeSeconds);
            hitRadius = Mathf.Max(0.01f, hitRadius);
        }
    }
}
