using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Dystopian.SuEun.StageFlow
{
    [RequireComponent(typeof(Collider))]
    public class BattleZone : MonoBehaviour
    {
        [SerializeField] private string zoneId = "BattleZone";
        [SerializeField] private bool triggerOnce = true;
        [SerializeField] private bool startOnEnter = true;
        [SerializeField] private bool useOverlapPolling = true;
        [SerializeField] private LayerMask activatorLayers = ~0;
        [SerializeField] private string requiredTag = "";
        [SerializeField] private StageBarrier[] barriers;

        [Header("Enemies")]
        [SerializeField] private bool findEnemiesInChildren = true;
        [SerializeField] private bool resetEnemiesOnBattleStart = true;
        [SerializeField] private StageEnemy[] enemies;

        [Header("Events")]
        [SerializeField] private UnityEvent onBattleStarted;
        [SerializeField] private UnityEvent onBattleCleared;
        [SerializeField] private UnityEvent<int> onRemainingEnemyCountChanged;

        private readonly List<StageEnemy> trackedEnemies = new();
        private bool hasTriggered;
        private bool isRunning;
        private int remainingEnemyCount;
        private Collider triggerCollider;

        public string ZoneId => zoneId;
        public bool IsRunning => isRunning;
        public bool HasTriggered => hasTriggered;
        public int RemainingEnemyCount => remainingEnemyCount;
        public int TotalEnemyCount => trackedEnemies.Count;

        private void Reset()
        {
            triggerCollider = GetComponent<Collider>();
            triggerCollider.isTrigger = true;
        }

        private void Awake()
        {
            triggerCollider = GetComponent<Collider>();
            triggerCollider.isTrigger = true;
        }

        private void Update()
        {
            if (!useOverlapPolling || !startOnEnter)
                return;

            if (triggerOnce && hasTriggered)
                return;

            if (isRunning || triggerCollider == null)
                return;

            Bounds bounds = triggerCollider.bounds;
            Collider[] overlaps = Physics.OverlapBox(
                bounds.center,
                bounds.extents,
                transform.rotation,
                activatorLayers,
                QueryTriggerInteraction.Ignore
            );

            foreach (Collider overlap in overlaps)
            {
                if (overlap == null || overlap == triggerCollider)
                    continue;

                if (!IsValidActivator(overlap.gameObject))
                    continue;

                StartBattle();
                return;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!startOnEnter)
                return;

            if (triggerOnce && hasTriggered)
                return;

            if (!IsValidActivator(other.gameObject))
                return;

            StartBattle();
        }

        private bool IsValidActivator(GameObject activator)
        {
            if (activator == null)
                return false;

            if (activatorLayers.value != 0)
            {
                int activatorLayerMask = 1 << activator.layer;

                if ((activatorLayers.value & activatorLayerMask) == 0)
                    return false;
            }

            return string.IsNullOrWhiteSpace(requiredTag) || activator.CompareTag(requiredTag);
        }

        [ContextMenu("Start Battle")]
        public void StartBattle()
        {
            if (isRunning)
                return;

            PrepareEnemiesForBattle();

            hasTriggered = true;
            isRunning = true;

            SetBarriersClosed(true);
            StageFlowManager.Instance?.NotifyBattleStarted(this);
            onBattleStarted?.Invoke();
            NotifyRemainingEnemyCountChanged();

            Debug.Log($"[StageFlow] Battle started: {zoneId}");

            TryClearIfNoEnemiesRemain();
        }

        [ContextMenu("Clear Battle")]
        public void ClearBattle()
        {
            if (!isRunning)
                return;

            isRunning = false;

            SetBarriersClosed(false);
            NotifyRemainingEnemyCountChanged();
            StageFlowManager.Instance?.NotifyBattleCleared(this);
            onBattleCleared?.Invoke();

            Debug.Log($"[StageFlow] Battle cleared: {zoneId}");
        }

        public void NotifyEnemyDefeated(StageEnemy enemy)
        {
            if (enemy != null && !trackedEnemies.Contains(enemy))
                trackedEnemies.Add(enemy);

            SetRemainingEnemyCount(remainingEnemyCount - 1);
        }

        public void NotifyEnemyDefeated()
        {
            SetRemainingEnemyCount(remainingEnemyCount - 1);
        }

        public void SetRemainingEnemyCount(int count)
        {
            remainingEnemyCount = Mathf.Max(0, count);
            NotifyRemainingEnemyCountChanged();
            TryClearIfNoEnemiesRemain();
        }

        [ContextMenu("Debug/Defeat One Enemy")]
        public void DefeatOneEnemyForDebug()
        {
            foreach (StageEnemy enemy in trackedEnemies)
            {
                if (enemy == null || enemy.IsDefeated)
                    continue;

                enemy.Defeat();
                return;
            }

            Debug.LogWarning($"[StageFlow] No remaining enemies in {zoneId}.");
        }

        [ContextMenu("Reset Zone")]
        public void ResetZone()
        {
            hasTriggered = false;
            isRunning = false;
            PrepareEnemiesForBattle();
            SetBarriersClosed(false);
        }

        private void PrepareEnemiesForBattle()
        {
            trackedEnemies.Clear();

            AddEnemies(enemies);

            if (findEnemiesInChildren)
                AddEnemies(GetComponentsInChildren<StageEnemy>(true));

            remainingEnemyCount = 0;

            foreach (StageEnemy enemy in trackedEnemies)
            {
                if (enemy == null)
                    continue;

                enemy.BindToZone(this);

                if (resetEnemiesOnBattleStart)
                    enemy.ResetForBattle();

                if (!enemy.IsDefeated)
                    remainingEnemyCount++;
            }
        }

        private void AddEnemies(StageEnemy[] enemiesToAdd)
        {
            if (enemiesToAdd == null)
                return;

            foreach (StageEnemy enemy in enemiesToAdd)
            {
                if (enemy == null || trackedEnemies.Contains(enemy))
                    continue;

                trackedEnemies.Add(enemy);
            }
        }

        private void NotifyRemainingEnemyCountChanged()
        {
            onRemainingEnemyCountChanged?.Invoke(remainingEnemyCount);
        }

        private void TryClearIfNoEnemiesRemain()
        {
            if (!isRunning || remainingEnemyCount > 0)
                return;

            ClearBattle();
        }

        private void SetBarriersClosed(bool closed)
        {
            if (barriers == null)
                return;

            foreach (StageBarrier barrier in barriers)
            {
                if (barrier == null)
                    continue;

                barrier.SetClosed(closed);
            }
        }
    }
}
