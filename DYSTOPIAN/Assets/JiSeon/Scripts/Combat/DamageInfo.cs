using UnityEngine;

namespace Dystopian.EnemyTest
{
    public struct DamageInfo
    {
        public int Amount { get; }
        public Vector3 HitPoint { get; }
        public GameObject Source { get; }

        public DamageInfo(int amount, Vector3 hitPoint, GameObject source)
        {
            Amount = Mathf.Max(0, amount);
            HitPoint = hitPoint;
            Source = source;
        }
    }

    public interface IDamageable
    {
        bool IsAlive { get; }
        void TakeDamage(DamageInfo damageInfo);
    }
}
