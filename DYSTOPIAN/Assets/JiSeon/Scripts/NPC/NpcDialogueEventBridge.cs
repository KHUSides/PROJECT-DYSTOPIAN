using UnityEngine;

namespace Dystopian.EnemyTest
{
    /// <summary>
    /// Inspector-callable bridge for NPC dialogue events.
    /// Other teams can replace or call these methods without depending on the temporary UI.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NpcDialogueEventBridge : MonoBehaviour
    {
        [SerializeField] private bool logCalls = true;

        public void SetFlagTrue(string key)
        {
            NpcDialogueState.SetFlag(key, true);
            Log($"SetFlagTrue: {key}");
        }

        public void SetFlagFalse(string key)
        {
            NpcDialogueState.SetFlag(key, false);
            Log($"SetFlagFalse: {key}");
        }

        public void SetTutorialStep(int step)
        {
            NpcDialogueState.SetTutorialStep(step);
            Log($"SetTutorialStep: {step}");
        }

        public void AdvanceTutorialStep()
        {
            NpcDialogueState.AdvanceTutorialStep();
            Log($"AdvanceTutorialStep -> {NpcDialogueState.TutorialStep}");
        }

        public void AdvanceTutorialStepBy(int amount)
        {
            NpcDialogueState.AdvanceTutorialStep(amount);
            Log($"AdvanceTutorialStepBy({amount}) -> {NpcDialogueState.TutorialStep}");
        }

        public void ResetDialogueState()
        {
            NpcDialogueState.ResetAll();
            Log("ResetDialogueState");
        }

        public void LogEvent(string eventName)
        {
            Log(eventName);
        }

        private void Log(string message)
        {
            if (logCalls)
                Debug.Log($"[NPC Event Bridge] {message}", this);
        }
    }
}
