using Dystopian.Combat;
using UnityEngine;

namespace Dystopian.SuEun.StageFlow
{
    /// <summary>
    /// 임시 보스 처치 흐름을 확인하기 위한 개발용 입력입니다.
    /// </summary>
    [RequireComponent(typeof(TemporaryBossHealth))]
    public sealed class TemporaryBossDebugDamage : MonoBehaviour
    {
        [SerializeField] private KeyCode damageKey_ = KeyCode.P;
        [SerializeField, Min(1)] private int damagePerPress_ = 25;

        private TemporaryBossHealth health_;

        private void Awake()
        {
            health_ = GetComponent<TemporaryBossHealth>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(damageKey_) && health_ != null)
            {
                health_.TakeDamage(new DamageInfo(
                    damagePerPress_,
                    transform.position,
                    gameObject));
            }
        }
    }
}
