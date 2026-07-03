using UnityEngine;

namespace Dystopian.SuEun.StageFlow
{
    public class StageEnemy : MonoBehaviour
    {
        [SerializeField] private BattleZone battleZone;
        [SerializeField] private bool disableOnDefeated = true;

        public bool IsDefeated { get; private set; }

        private void Awake()
        {
            if (battleZone == null)
                battleZone = GetComponentInParent<BattleZone>();
        }

        public void BindToZone(BattleZone zone)
        {
            battleZone = zone;
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
            battleZone?.NotifyEnemyDefeated(this);

            if (disableOnDefeated)
                gameObject.SetActive(false);
        }
    }
}
