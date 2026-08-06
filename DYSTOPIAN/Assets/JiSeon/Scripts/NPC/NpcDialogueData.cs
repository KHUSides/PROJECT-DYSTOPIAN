using System;
using UnityEngine;
using UnityEngine.Events;

namespace Dystopian.EnemyTest
{
    [Serializable]
    public sealed class NpcDialogueLine
    {
        [SerializeField] private string speakerName;
        [SerializeField, TextArea(2, 5)] private string text;
        [SerializeField] private UnityEvent onLineShown;
        [SerializeField] private UnityEvent onLineCompleted;

        public string SpeakerName => speakerName;
        public string Text => text;

        public void NotifyShown()
        {
            onLineShown?.Invoke();
        }

        public void NotifyCompleted()
        {
            onLineCompleted?.Invoke();
        }
    }

    [Serializable]
    public sealed class NpcDialogueBranch
    {
        [Header("Branch")]
        [SerializeField] private string branchId = "default";
        [SerializeField] private NpcDialogueLine[] lines = new NpcDialogueLine[0];

        [Header("Conditions")]
        [Tooltip("Empty means this branch does not require a flag.")]
        [SerializeField] private string requiredFlagKey;
        [SerializeField] private bool requiredFlagValue = true;
        [Tooltip("-1 means no minimum tutorial step condition.")]
        [SerializeField] private int minimumTutorialStep = -1;
        [Tooltip("-1 means no maximum tutorial step condition.")]
        [SerializeField] private int maximumTutorialStep = -1;

        [Header("Completion Result")]
        [Tooltip("Empty means no flag is changed when this branch completes.")]
        [SerializeField] private string flagKeyToSetOnComplete;
        [SerializeField] private bool flagValueOnComplete = true;
        [Tooltip("-1 means tutorial step is not set directly.")]
        [SerializeField] private int tutorialStepToSetOnComplete = -1;
        [SerializeField] private bool advanceTutorialStepOnComplete;
        [SerializeField] private string debugEventNameOnComplete;

        [Header("Events")]
        [SerializeField] private UnityEvent onDialogueStarted;
        [SerializeField] private UnityEvent onDialogueCompleted;

        public string BranchId => branchId;
        public NpcDialogueLine[] Lines => lines;
        public bool HasLines => lines != null && lines.Length > 0;

        public bool IsAvailable()
        {
            if (!string.IsNullOrWhiteSpace(requiredFlagKey) &&
                NpcDialogueState.GetFlag(requiredFlagKey) != requiredFlagValue)
                return false;

            if (minimumTutorialStep >= 0 && NpcDialogueState.TutorialStep < minimumTutorialStep)
                return false;

            if (maximumTutorialStep >= 0 && NpcDialogueState.TutorialStep > maximumTutorialStep)
                return false;

            return HasLines;
        }

        public void NotifyStarted()
        {
            onDialogueStarted?.Invoke();
        }

        public void NotifyCompleted(UnityEngine.Object logContext)
        {
            if (!string.IsNullOrWhiteSpace(flagKeyToSetOnComplete))
                NpcDialogueState.SetFlag(flagKeyToSetOnComplete, flagValueOnComplete);

            if (tutorialStepToSetOnComplete >= 0)
                NpcDialogueState.SetTutorialStep(tutorialStepToSetOnComplete);
            else if (advanceTutorialStepOnComplete)
                NpcDialogueState.AdvanceTutorialStep();

            if (!string.IsNullOrWhiteSpace(debugEventNameOnComplete))
                Debug.Log($"[NPC Event] {debugEventNameOnComplete}", logContext);

            onDialogueCompleted?.Invoke();
        }
    }
}
