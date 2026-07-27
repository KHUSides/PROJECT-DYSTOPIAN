using System.Reflection;
using Dystopian.Rhythm;
using UnityEngine;

namespace Dystopian.EnemyTest
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyController))]
    public sealed class EnemyRhythmCombatBridge : MonoBehaviour
    {
        [Header("Rhythm Source")]
        [SerializeField] private RhythmSystem rhythmSystem;
        [SerializeField] private bool autoFindRhythmSystem = true;

        [Header("Enemy References")]
        [SerializeField] private EnemyController enemyController;
        [SerializeField] private EnemyMeleeCombat meleeCombat;
        [SerializeField] private EnemyRangedCombat rangedCombat;

        [Header("Attack Condition")]
        [SerializeField] private bool requireChasingTarget = true;

        [Header("Note Response")]
        [SerializeField] private bool reactToSingleNotes = true;
        [SerializeField] private bool reactToLongNotes = true;
        [SerializeField] private bool useMeleeCombat = true;
        [SerializeField] private bool useRangedCombat;

        [Header("Debug")]
        [SerializeField] private bool logRhythmAttacks = true;

        private NoteUnityEvent enemyActionEvent;
        private NoteUnityEvent enemyLongEndEvent;
        private bool subscribed;

        private void Reset()
        {
            CacheLocalReferences();
        }

        private void Awake()
        {
            CacheLocalReferences();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void Start()
        {
            CacheLocalReferences();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            CancelLongCharge();
        }

        public void HandleEnemyAction(RhythmChartNote note)
        {
            if (note == null || note.Actor != RhythmActor.Enemy)
            {
                return;
            }

            switch (note.Type)
            {
                case RhythmNoteType.Single:
                    HandleSingleNote();
                    break;
                case RhythmNoteType.Long:
                    HandleLongNoteStart();
                    break;
            }
        }

        public void HandleEnemyLongEnd(RhythmChartNote note)
        {
            if (note == null ||
                note.Actor != RhythmActor.Enemy ||
                note.Type != RhythmNoteType.Long ||
                !reactToLongNotes)
            {
                return;
            }

            if (!CanAttackFromCurrentState("long note end"))
            {
                CancelLongCharge();
                return;
            }

            FaceTargetBeforeAttack();

            if (useMeleeCombat && meleeCombat != null)
            {
                meleeCombat.ReleaseLongNoteCharge();
            }

            if (useRangedCombat && rangedCombat != null)
            {
                rangedCombat.ReleaseLongNoteCharge();
            }
        }

        private void HandleSingleNote()
        {
            if (!reactToSingleNotes || !CanAttackFromCurrentState("single note"))
            {
                return;
            }

            FaceTargetBeforeAttack();

            if (useMeleeCombat && meleeCombat != null)
            {
                meleeCombat.TrySingleNoteAttack();
            }

            if (useRangedCombat && rangedCombat != null)
            {
                rangedCombat.TrySingleNoteShot();
            }
        }

        private void HandleLongNoteStart()
        {
            if (!reactToLongNotes || !CanAttackFromCurrentState("long note start"))
            {
                return;
            }

            if (useMeleeCombat && meleeCombat != null)
            {
                meleeCombat.BeginLongNoteCharge();
            }

            if (useRangedCombat && rangedCombat != null)
            {
                rangedCombat.BeginLongNoteCharge();
            }
        }

        private bool CanAttackFromCurrentState(string noteLabel)
        {
            if (enemyController == null || enemyController.IsDead)
            {
                return false;
            }

            if (!requireChasingTarget)
            {
                return true;
            }

            bool canAttack = enemyController.IsChasingTarget;

            if (!canAttack && logRhythmAttacks)
            {
                Debug.Log(
                    $"[Enemy Rhythm Combat] {name} ignored {noteLabel}. Current state is {enemyController.CurrentStateName}, not Chase.",
                    this);
            }

            return canAttack;
        }

        private void FaceTargetBeforeAttack()
        {
            Transform target = enemyController != null ? enemyController.CurrentTarget : null;
            if (target == null)
            {
                return;
            }

            if (meleeCombat != null)
            {
                meleeCombat.RefreshTargetFacing();
            }

            if (rangedCombat != null)
            {
                rangedCombat.RefreshTargetFacing();
            }
        }

        private void CancelLongCharge()
        {
            if (meleeCombat != null)
            {
                meleeCombat.CancelLongNoteCharge();
            }

            if (rangedCombat != null)
            {
                rangedCombat.CancelLongNoteCharge();
            }
        }

        private void HandleChartStopped()
        {
            CancelLongCharge();
        }

        private void CacheLocalReferences()
        {
            if (enemyController == null)
            {
                enemyController = GetComponent<EnemyController>();
            }

            if (meleeCombat == null)
            {
                meleeCombat = GetComponent<EnemyMeleeCombat>();
            }

            if (rangedCombat == null)
            {
                rangedCombat = GetComponent<EnemyRangedCombat>();
            }

            if (rangedCombat != null && meleeCombat == null)
            {
                useRangedCombat = true;
                useMeleeCombat = false;
            }

            if (meleeCombat != null && rangedCombat == null)
            {
                useMeleeCombat = true;
                useRangedCombat = false;
            }
        }

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            if (rhythmSystem == null && autoFindRhythmSystem)
            {
                rhythmSystem = FindFirstObjectByType<RhythmSystem>();
            }

            if (rhythmSystem == null)
            {
                return;
            }

            enemyActionEvent = GetPrivateNoteEvent("onEnemyAction");
            enemyLongEndEvent = GetPrivateNoteEvent("onEnemyLongEnd");

            if (enemyActionEvent == null || enemyLongEndEvent == null)
            {
                Debug.LogError("[Enemy Rhythm Combat] RhythmSystem enemy events could not be found.", this);
                enabled = false;
                return;
            }

            enemyActionEvent.AddListener(HandleEnemyAction);
            enemyLongEndEvent.AddListener(HandleEnemyLongEnd);
            rhythmSystem.ChartStopped += HandleChartStopped;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            if (enemyActionEvent != null)
            {
                enemyActionEvent.RemoveListener(HandleEnemyAction);
            }

            if (enemyLongEndEvent != null)
            {
                enemyLongEndEvent.RemoveListener(HandleEnemyLongEnd);
            }

            if (rhythmSystem != null)
            {
                rhythmSystem.ChartStopped -= HandleChartStopped;
            }

            subscribed = false;
        }

        private NoteUnityEvent GetPrivateNoteEvent(string fieldName)
        {
            FieldInfo field = typeof(RhythmSystem).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            return field != null ? field.GetValue(rhythmSystem) as NoteUnityEvent : null;
        }

        private void OnValidate()
        {
            CacheLocalReferences();
        }
    }
}
