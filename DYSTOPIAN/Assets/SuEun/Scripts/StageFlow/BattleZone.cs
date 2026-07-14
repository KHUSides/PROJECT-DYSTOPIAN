using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Dystopian.SuEun.StageFlow
{
    [RequireComponent(typeof(Collider))]
    public class BattleZone : MonoBehaviour
    {
        [FormerlySerializedAs("zoneId")]
        [SerializeField] private string zoneId_ = "BattleZone";
        [FormerlySerializedAs("triggerOnce")]
        [SerializeField] private bool triggerOnce_ = true;
        [FormerlySerializedAs("startOnEnter")]
        [SerializeField] private bool startOnEnter_ = true;
        [FormerlySerializedAs("useOverlapPolling")]
        [SerializeField] private bool useOverlapPolling_ = true;
        [FormerlySerializedAs("activatorLayers")]
        [SerializeField] private LayerMask activatorLayers_ = ~0;
        [FormerlySerializedAs("requiredTag")]
        [SerializeField] private string requiredTag_ = "";
        [FormerlySerializedAs("barriers")]
        [SerializeField] private StageBarrier[] barriers_;

        [Header("Enemies")]
        [FormerlySerializedAs("findEnemiesInChildren")]
        [SerializeField] private bool findEnemiesInChildren_ = true;
        [FormerlySerializedAs("resetEnemiesOnBattleStart")]
        [SerializeField] private bool resetEnemiesOnBattleStart_ = true;
        [FormerlySerializedAs("enemies")]
        [SerializeField] private StageEnemy[] enemies_;

        [Header("Events")]
        [FormerlySerializedAs("onBattleStarted")]
        [SerializeField] private UnityEvent onBattleStarted_;
        [FormerlySerializedAs("onBattleCleared")]
        [SerializeField] private UnityEvent onBattleCleared_;
        [FormerlySerializedAs("onRemainingEnemyCountChanged")]
        [SerializeField] private UnityEvent<int> onRemainingEnemyCountChanged_;

        private readonly List<StageEnemy> trackedEnemies_ = new();
        private bool hasTriggered_;
        private bool isRunning_;
        private int remainingEnemyCount_;
        private Collider triggerCollider_;

        public string ZoneId => zoneId_;
        public bool IsRunning => isRunning_;
        public bool HasTriggered => hasTriggered_;
        public int RemainingEnemyCount => remainingEnemyCount_;
        public int TotalEnemyCount => trackedEnemies_.Count;

        public event Action<BattleZone> BattleStarted;
        public event Action<BattleZone> BattleCleared;
        public event Action<BattleZone, int> RemainingEnemyCountChanged;

        private void Reset()
        {
            triggerCollider_ = GetComponent<Collider>();
            triggerCollider_.isTrigger = true;
        }

        private void Awake()
        {
            triggerCollider_ = GetComponent<Collider>();
            triggerCollider_.isTrigger = true;
        }

        private void Update()
        {
            if (!useOverlapPolling_ || !startOnEnter_)
                return;

            if (triggerOnce_ && hasTriggered_)
                return;

            if (isRunning_ || triggerCollider_ == null)
                return;

            Bounds bounds = triggerCollider_.bounds;
            Collider[] overlaps = Physics.OverlapBox(
                bounds.center,
                bounds.extents,
                transform.rotation,
                activatorLayers_,
                QueryTriggerInteraction.Ignore
            );

            foreach (Collider overlap in overlaps)
            {
                if (overlap == null || overlap == triggerCollider_)
                    continue;

                if (!IsValidActivator(overlap.gameObject))
                    continue;

                StartBattle();
                return;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!startOnEnter_)
                return;

            if (triggerOnce_ && hasTriggered_)
                return;

            if (!IsValidActivator(other.gameObject))
                return;

            StartBattle();
        }

        private bool IsValidActivator(GameObject activator)
        {
            if (activator == null)
                return false;

            if (activatorLayers_.value != 0)
            {
                int activatorLayerMask = 1 << activator.layer;

                if ((activatorLayers_.value & activatorLayerMask) == 0)
                    return false;
            }

            return string.IsNullOrWhiteSpace(requiredTag_) || activator.CompareTag(requiredTag_);
        }

        [ContextMenu("Start Battle")]
        public void StartBattle()
        {
            if (isRunning_)
                return;

            PrepareEnemiesForBattle();

            hasTriggered_ = true;
            isRunning_ = true;

            SetBarriersClosed(true);
            StageFlowManager.Instance?.NotifyBattleStarted(this);
            BattleStarted?.Invoke(this);
            onBattleStarted_?.Invoke();
            NotifyRemainingEnemyCountChanged();

            Debug.Log($"[StageFlow] Battle started: {zoneId_}");

            TryClearIfNoEnemiesRemain();
        }

        [ContextMenu("Clear Battle")]
        public void ClearBattle()
        {
            if (!isRunning_)
                return;

            isRunning_ = false;

            SetBarriersClosed(false);
            NotifyRemainingEnemyCountChanged();
            StageFlowManager.Instance?.NotifyBattleCleared(this);
            BattleCleared?.Invoke(this);
            onBattleCleared_?.Invoke();

            Debug.Log($"[StageFlow] Battle cleared: {zoneId_}");
        }

        public void NotifyEnemyDefeated(StageEnemy enemy)
        {
            if (enemy != null && !trackedEnemies_.Contains(enemy))
                trackedEnemies_.Add(enemy);

            SetRemainingEnemyCount(remainingEnemyCount_ - 1);
        }

        public void NotifyEnemyDefeated()
        {
            SetRemainingEnemyCount(remainingEnemyCount_ - 1);
        }

        public void SetRemainingEnemyCount(int count)
        {
            remainingEnemyCount_ = Mathf.Max(0, count);
            NotifyRemainingEnemyCountChanged();
            TryClearIfNoEnemiesRemain();
        }

        [ContextMenu("Debug/Defeat One Enemy")]
        public void DefeatOneEnemyForDebug()
        {
            foreach (StageEnemy enemy in trackedEnemies_)
            {
                if (enemy == null || enemy.IsDefeated)
                    continue;

                enemy.Defeat();
                return;
            }

            Debug.LogWarning($"[StageFlow] No remaining enemies in {zoneId_}.");
        }

        [ContextMenu("Reset Zone")]
        public void ResetZone()
        {
            hasTriggered_ = false;
            isRunning_ = false;
            PrepareEnemiesForBattle();
            SetBarriersClosed(false);
        }

        private void PrepareEnemiesForBattle()
        {
            trackedEnemies_.Clear();

            AddEnemies(enemies_);

            if (findEnemiesInChildren_)
                AddEnemies(GetComponentsInChildren<StageEnemy>(true));

            remainingEnemyCount_ = 0;

            foreach (StageEnemy enemy in trackedEnemies_)
            {
                if (enemy == null)
                    continue;

                enemy.BindToZone(this);

                if (resetEnemiesOnBattleStart_)
                    enemy.ResetForBattle();

                if (!enemy.IsDefeated)
                    remainingEnemyCount_++;
            }
        }

        private void AddEnemies(StageEnemy[] enemiesToAdd)
        {
            if (enemiesToAdd == null)
                return;

            foreach (StageEnemy enemy in enemiesToAdd)
            {
                if (enemy == null || trackedEnemies_.Contains(enemy))
                    continue;

                trackedEnemies_.Add(enemy);
            }
        }

        private void NotifyRemainingEnemyCountChanged()
        {
            RemainingEnemyCountChanged?.Invoke(this, remainingEnemyCount_);
            StageFlowManager.Instance?.NotifyRemainingEnemyCountChanged(this, remainingEnemyCount_);
            onRemainingEnemyCountChanged_?.Invoke(remainingEnemyCount_);
        }

        private void TryClearIfNoEnemiesRemain()
        {
            if (!isRunning_ || remainingEnemyCount_ > 0)
                return;

            ClearBattle();
        }

        private void SetBarriersClosed(bool closed)
        {
            if (barriers_ == null)
                return;

            foreach (StageBarrier barrier in barriers_)
            {
                if (barrier == null)
                    continue;

                barrier.SetClosed(closed);
            }
        }
    }
}
