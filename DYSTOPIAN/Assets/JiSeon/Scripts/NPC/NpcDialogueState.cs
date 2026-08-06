using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Dystopian.EnemyTest
{
    /// <summary>
    /// Small runtime store for NPC dialogue flags and tutorial progress.
    /// This intentionally stays independent from the stage/UI systems so other parts can connect later.
    /// </summary>
    public static class NpcDialogueState
    {
        private static readonly Dictionary<string, bool> Flags = new Dictionary<string, bool>();
        private static int tutorialStep;

        public static event Action<string, bool> FlagChanged;
        public static event Action<int> TutorialStepChanged;

        public static int TutorialStep => tutorialStep;

        public static bool GetFlag(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            return Flags.TryGetValue(key, out bool value) && value;
        }

        public static void SetFlag(string key, bool value)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            if (Flags.TryGetValue(key, out bool previous) && previous == value)
                return;

            Flags[key] = value;
            FlagChanged?.Invoke(key, value);
        }

        public static void ClearFlag(string key)
        {
            SetFlag(key, false);
        }

        public static void SetTutorialStep(int step)
        {
            int clampedStep = Mathf.Max(0, step);
            if (tutorialStep == clampedStep)
                return;

            tutorialStep = clampedStep;
            TutorialStepChanged?.Invoke(tutorialStep);
        }

        public static void AdvanceTutorialStep(int amount = 1)
        {
            SetTutorialStep(tutorialStep + Mathf.Max(1, amount));
        }

        public static void ResetAll()
        {
            Flags.Clear();
            tutorialStep = 0;
            TutorialStepChanged?.Invoke(tutorialStep);
        }

        public static string BuildDebugSummary()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("TutorialStep=");
            builder.Append(tutorialStep);

            foreach (KeyValuePair<string, bool> pair in Flags)
            {
                builder.Append(", ");
                builder.Append(pair.Key);
                builder.Append('=');
                builder.Append(pair.Value);
            }

            return builder.ToString();
        }
    }
}
