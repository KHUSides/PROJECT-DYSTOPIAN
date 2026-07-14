using UnityEngine;
using UnityEngine.Serialization;

namespace Dystopian.SuEun.StageFlow
{
    public class StageEnemy : MonoBehaviour
    {
        [FormerlySerializedAs("battleZone")]
        [SerializeField] private BattleZone battleZone_;
        [FormerlySerializedAs("disableOnDefeated")]
        [SerializeField] private bool disableOnDefeated_ = true;

        public bool IsDefeated { get; private set; }

        private void Awake()
        {
            if (battleZone_ == null)
                battleZone_ = GetComponentInParent<BattleZone>();
        }

        public void BindToZone(BattleZone zone)
        {
            battleZone_ = zone;
        }

        public void ResetForBattle()
        {
            IsDefeated = false;

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
        }

        [ContextMenu("Defeat")]
        public void Defeat()
        {
            if (IsDefeated)
                return;

            IsDefeated = true;
            battleZone_?.NotifyEnemyDefeated(this);

            if (disableOnDefeated_)
                gameObject.SetActive(false);
        }
    }
}
