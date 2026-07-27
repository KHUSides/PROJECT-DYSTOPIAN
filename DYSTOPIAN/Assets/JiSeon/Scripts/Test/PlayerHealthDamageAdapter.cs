using UnityEngine;

namespace Dystopian.EnemyTest
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerHealth))]
    public sealed class PlayerHealthDamageAdapter : MonoBehaviour, IDamageable
    {
        private PlayerHealth playerHealth;

        public bool IsAlive => playerHealth != null && !playerHealth.IsEmpty;

        private void Awake()
        {
            playerHealth = GetComponent<PlayerHealth>();
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (playerHealth == null || playerHealth.IsEmpty)
            {
                return;
            }

            playerHealth.TakeDamage(damageInfo.Amount);
        }
    }
}
