using System;
using UnityEngine;
using UnityEngine.Events;

namespace Dystopian.EnemyTest
{
    public enum NoteDebugEventType
    {
        Single,
        LongStart,
        LongEnd
    }

    [Serializable]
    public sealed class NoteDebugUnityEvent : UnityEvent<NoteDebugEventType>
    {
    }

    [DisallowMultipleComponent]
    public sealed class NoteDebugInput : MonoBehaviour
    {
        [Header("Keys")]
        [SerializeField] private KeyCode singleNoteKey = KeyCode.A;
        [SerializeField] private KeyCode longNoteStartKey = KeyCode.D;
        [SerializeField] private KeyCode longNoteEndKey = KeyCode.D;

        [Header("Events")]
        [SerializeField] private UnityEvent onSingleNote;
        [SerializeField] private UnityEvent onLongNoteStart;
        [SerializeField] private UnityEvent onLongNoteEnd;
        [SerializeField] private NoteDebugUnityEvent onAnyNoteEvent;

        [Header("Debug")]
        [SerializeField] private bool logEvents = true;
        [SerializeField] private bool showOnGuiHelp = true;

        public static event Action<NoteDebugEventType> NoteEventTriggered;
        public static event Action SingleNoteTriggered;
        public static event Action LongNoteStarted;
        public static event Action LongNoteEnded;

        private void Update()
        {
            if (Input.GetKeyDown(singleNoteKey))
            {
                TriggerSingleNote();
            }

            if (Input.GetKeyDown(longNoteStartKey))
            {
                TriggerLongNoteStart();
            }

            if (Input.GetKeyUp(longNoteEndKey))
            {
                TriggerLongNoteEnd();
            }
        }

        public void TriggerSingleNote()
        {
            Raise(NoteDebugEventType.Single);
        }

        public void TriggerLongNoteStart()
        {
            Raise(NoteDebugEventType.LongStart);
        }

        public void TriggerLongNoteEnd()
        {
            Raise(NoteDebugEventType.LongEnd);
        }

        private void Raise(NoteDebugEventType eventType)
        {
            NoteEventTriggered?.Invoke(eventType);
            onAnyNoteEvent?.Invoke(eventType);

            switch (eventType)
            {
                case NoteDebugEventType.Single:
                    SingleNoteTriggered?.Invoke();
                    onSingleNote?.Invoke();
                    break;
                case NoteDebugEventType.LongStart:
                    LongNoteStarted?.Invoke();
                    onLongNoteStart?.Invoke();
                    break;
                case NoteDebugEventType.LongEnd:
                    LongNoteEnded?.Invoke();
                    onLongNoteEnd?.Invoke();
                    break;
            }

            if (logEvents)
            {
                Debug.Log($"[Note Debug] {eventType} event triggered.", this);
            }
        }

        private void OnGUI()
        {
            if (!showOnGuiHelp)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(16f, Screen.height - 196f, 420f, 180f), GUI.skin.box);
            GUILayout.Label("Enemy Test Controls");
            GUILayout.Label("Left / Right Arrow: Move");
            GUILayout.Label("Up Arrow: Jump");
            GUILayout.Label("J: Temporary Attack");
            GUILayout.Label("A near Enemy: Single Note + Contact Damage");
            GUILayout.Space(4f);
            GUILayout.Label("Note Debug");
            GUILayout.Label($"{singleNoteKey}: Single Note");
            GUILayout.Label($"{longNoteStartKey} Down: Long Note Start");
            GUILayout.Label($"{longNoteEndKey} Up: Long Note End");
            GUILayout.EndArea();
        }
    }
}
