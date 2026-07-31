using System.Reflection;
using Dystopian.Rhythm;
using UnityEngine;
using UnityEngine.Events;

namespace Dystopian.EnemyTest
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MerlinBossCombat))]
    public sealed class BossRhythmCombatBridge : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RhythmSystem rhythmSystem;
        [SerializeField] private MerlinBossCombat merlinCombat;

        [Header("Accepted Notes")]
        [SerializeField] private bool listenToBossEvents = true;
        [SerializeField] private bool listenToEnemyEventsForTesting;
        [SerializeField] private bool requireBossActor = true;

        [Header("Debug")]
        [SerializeField] private bool logIgnoredNotes = true;

        private NoteUnityEvent bossActionEvent;
        private NoteUnityEvent bossLongEndEvent;
        private NoteUnityEvent enemyActionEvent;
        private NoteUnityEvent enemyLongEndEvent;
        private UnityEvent chartStoppedEvent;
        private bool subscribed;

        private void Reset()
        {
            CacheReferences();
        }

        private void Awake()
        {
            CacheReferences();
        }

        private void OnEnable()
        {
            CacheReferences();
            Subscribe();
        }

        private void Start()
        {
            CacheReferences();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void HandleActionNote(RhythmChartNote note)
        {
            if (!ShouldAcceptNote(note))
            {
                return;
            }

            switch (note.Type)
            {
                case RhythmNoteType.Single:
                    merlinCombat.HandleSingleNoteAttack();
                    break;
                case RhythmNoteType.Long:
                    merlinCombat.BeginLongNoteCharge();
                    break;
                default:
                    if (logIgnoredNotes)
                    {
                        Debug.Log($"[Boss Rhythm] Ignored unsupported note type: {note.Type}", this);
                    }
                    break;
            }
        }

        private void HandleLongEndNote(RhythmChartNote note)
        {
            if (!ShouldAcceptNote(note) || note.Type != RhythmNoteType.Long)
            {
                return;
            }

            merlinCombat.ReleaseLongNoteCharge();
        }

        private void HandleChartStopped()
        {
            merlinCombat.CancelLongNoteCharge();
        }

        private bool ShouldAcceptNote(RhythmChartNote note)
        {
            if (note == null)
            {
                return false;
            }

            if (!requireBossActor)
            {
                return true;
            }

            bool accepted = note.Actor == RhythmActor.Boss ||
                            (listenToEnemyEventsForTesting && note.Actor == RhythmActor.Enemy);

            if (!accepted && logIgnoredNotes)
            {
                Debug.Log($"[Boss Rhythm] Ignored note for actor {note.Actor}.", this);
            }

            return accepted;
        }

        private void CacheReferences()
        {
            if (merlinCombat == null)
            {
                merlinCombat = GetComponent<MerlinBossCombat>();
            }

            if (rhythmSystem == null)
            {
                rhythmSystem = FindFirstObjectByType<RhythmSystem>();
            }
        }

        private void Subscribe()
        {
            if (subscribed || rhythmSystem == null || merlinCombat == null)
            {
                return;
            }

            if (listenToBossEvents)
            {
                bossActionEvent = GetPrivateNoteEvent("onBossAction");
                bossLongEndEvent = GetPrivateNoteEvent("onBossLongEnd");
                bossActionEvent?.AddListener(HandleActionNote);
                bossLongEndEvent?.AddListener(HandleLongEndNote);
            }

            if (listenToEnemyEventsForTesting)
            {
                enemyActionEvent = GetPrivateNoteEvent("onEnemyAction");
                enemyLongEndEvent = GetPrivateNoteEvent("onEnemyLongEnd");
                enemyActionEvent?.AddListener(HandleActionNote);
                enemyLongEndEvent?.AddListener(HandleLongEndNote);
            }

            chartStoppedEvent = GetPrivateUnityEvent("onChartStopped");
            chartStoppedEvent?.AddListener(HandleChartStopped);
            rhythmSystem.ChartStopped += HandleChartStopped;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            bossActionEvent?.RemoveListener(HandleActionNote);
            bossLongEndEvent?.RemoveListener(HandleLongEndNote);
            enemyActionEvent?.RemoveListener(HandleActionNote);
            enemyLongEndEvent?.RemoveListener(HandleLongEndNote);
            chartStoppedEvent?.RemoveListener(HandleChartStopped);

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

        private UnityEvent GetPrivateUnityEvent(string fieldName)
        {
            FieldInfo field = typeof(RhythmSystem).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            return field != null ? field.GetValue(rhythmSystem) as UnityEvent : null;
        }

        private void OnValidate()
        {
            CacheReferences();
        }
    }
}
