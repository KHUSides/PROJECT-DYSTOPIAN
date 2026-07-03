using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Dystopian.SuEun.StageFlow
{
    public class StageFlowManager : MonoBehaviour
    {
        [SerializeField] private BattleZone firstZone;
        [SerializeField] private bool allowDebugHotkeys = true;

        [Header("Events")]
        [SerializeField] private UnityEvent onAnyBattleStarted;
        [SerializeField] private UnityEvent onAnyBattleCleared;

        private readonly List<BattleZone> clearedZones = new();
        private BattleZone activeZone;

        public static StageFlowManager Instance { get; private set; }
        public BattleZone ActiveZone => activeZone;
        public IReadOnlyList<BattleZone> ClearedZones => clearedZones;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[StageFlow] Multiple StageFlowManagers found. Using the newest instance.");
            }

            Instance = this;
        }

        private void Update()
        {
            if (!allowDebugHotkeys)
                return;

            if (Input.GetKeyDown(KeyCode.B))
                StartFirstZoneForDebug();

            if (Input.GetKeyDown(KeyCode.C))
                ClearActiveZoneForDebug();

            if (Input.GetKeyDown(KeyCode.K))
                DefeatOneEnemyForDebug();
        }

        public void NotifyBattleStarted(BattleZone zone)
        {
            activeZone = zone;
            onAnyBattleStarted?.Invoke();
        }

        public void NotifyBattleCleared(BattleZone zone)
        {
            if (activeZone == zone)
                activeZone = null;

            if (zone != null && !clearedZones.Contains(zone))
                clearedZones.Add(zone);

            onAnyBattleCleared?.Invoke();
        }

        [ContextMenu("Debug/Start First Zone")]
        public void StartFirstZoneForDebug()
        {
            if (firstZone == null)
            {
                Debug.LogWarning("[StageFlow] First zone is not assigned.");
                return;
            }

            firstZone.StartBattle();
        }

        [ContextMenu("Debug/Clear Active Zone")]
        public void ClearActiveZoneForDebug()
        {
            if (activeZone == null)
            {
                Debug.LogWarning("[StageFlow] No active battle zone to clear.");
                return;
            }

            activeZone.ClearBattle();
        }

        [ContextMenu("Debug/Defeat One Enemy")]
        public void DefeatOneEnemyForDebug()
        {
            if (activeZone == null)
            {
                Debug.LogWarning("[StageFlow] No active battle zone to defeat an enemy in.");
                return;
            }

            activeZone.DefeatOneEnemyForDebug();
        }
    }
}
