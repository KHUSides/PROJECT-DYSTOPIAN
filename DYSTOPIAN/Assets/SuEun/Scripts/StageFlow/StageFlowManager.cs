using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Dystopian.SuEun.StageFlow
{
    public class StageFlowManager : MonoBehaviour
    {
        [FormerlySerializedAs("firstZone")]
        [SerializeField] private BattleZone firstZone_;
        [FormerlySerializedAs("allowDebugHotkeys")]
        [SerializeField] private bool allowDebugHotkeys_ = true;

        [Header("Events")]
        [FormerlySerializedAs("onAnyBattleStarted")]
        [SerializeField] private UnityEvent onAnyBattleStarted_;
        [FormerlySerializedAs("onAnyBattleCleared")]
        [SerializeField] private UnityEvent onAnyBattleCleared_;
        [FormerlySerializedAs("onBattleStarted")]
        [SerializeField] private BattleZoneEvent onBattleStarted_;
        [FormerlySerializedAs("onBattleCleared")]
        [SerializeField] private BattleZoneEvent onBattleCleared_;
        [FormerlySerializedAs("onRemainingEnemyCountChanged")]
        [SerializeField] private BattleZoneEnemyCountEvent onRemainingEnemyCountChanged_;

        private readonly List<BattleZone> clearedZones_ = new();
        private BattleZone activeZone_;

        public static StageFlowManager Instance { get; private set; }
        public BattleZone ActiveZone => activeZone_;
        public IReadOnlyList<BattleZone> ClearedZones => clearedZones_;

        public event Action<BattleZone> BattleStarted;
        public event Action<BattleZone> BattleCleared;
        public event Action<BattleZone, int> RemainingEnemyCountChanged;

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
            if (!allowDebugHotkeys_)
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
            activeZone_ = zone;
            BattleStarted?.Invoke(zone);
            onBattleStarted_?.Invoke(zone);
            onAnyBattleStarted_?.Invoke();
        }

        public void NotifyBattleCleared(BattleZone zone)
        {
            if (activeZone_ == zone)
                activeZone_ = null;

            if (zone != null && !clearedZones_.Contains(zone))
                clearedZones_.Add(zone);

            BattleCleared?.Invoke(zone);
            onBattleCleared_?.Invoke(zone);
            onAnyBattleCleared_?.Invoke();
        }

        public void NotifyRemainingEnemyCountChanged(BattleZone zone, int remainingEnemyCount)
        {
            RemainingEnemyCountChanged?.Invoke(zone, remainingEnemyCount);
            onRemainingEnemyCountChanged_?.Invoke(zone, remainingEnemyCount);
        }

        [ContextMenu("Debug/Start First Zone")]
        public void StartFirstZoneForDebug()
        {
            if (firstZone_ == null)
            {
                Debug.LogWarning("[StageFlow] First zone is not assigned.");
                return;
            }

            firstZone_.StartBattle();
        }

        [ContextMenu("Debug/Clear Active Zone")]
        public void ClearActiveZoneForDebug()
        {
            if (activeZone_ == null)
            {
                Debug.LogWarning("[StageFlow] No active battle zone to clear.");
                return;
            }

            activeZone_.ClearBattle();
        }

        [ContextMenu("Debug/Defeat One Enemy")]
        public void DefeatOneEnemyForDebug()
        {
            if (activeZone_ == null)
            {
                Debug.LogWarning("[StageFlow] No active battle zone to defeat an enemy in.");
                return;
            }

            activeZone_.DefeatOneEnemyForDebug();
        }
    }
}
