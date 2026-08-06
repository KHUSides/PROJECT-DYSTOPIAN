using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Dystopian.EnemyTest
{
    /// <summary>
    /// Temporarily disables player input components during NPC dialogue.
    /// It uses component type names so the NPC system does not need to modify player-side scripts.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NpcPlayerControlLock : MonoBehaviour
    {
        [SerializeField] private Transform playerRoot;
        [SerializeField] private bool autoFindPlayer = true;
        [SerializeField] private bool disableMatchingComponentsGlobally = true;
        [SerializeField] private string[] componentTypeNamesToDisable =
        {
            "PlayerController",
            "PlayerRhythmAttackBridge",
            "DummyPlayerController",
            "DummyPlayerAttack"
        };

        private readonly List<LockedBehaviour> lockedBehaviours = new List<LockedBehaviour>();

        public bool IsLocked => lockedBehaviours.Count > 0;

        public void Lock()
        {
            if (IsLocked)
                return;

            ResolvePlayerRootIfNeeded();
            CacheAndDisableInputBehaviours();
        }

        public void Unlock()
        {
            for (int i = 0; i < lockedBehaviours.Count; i++)
            {
                LockedBehaviour lockedBehaviour = lockedBehaviours[i];
                if (lockedBehaviour.Behaviour == null)
                    continue;

                lockedBehaviour.Behaviour.enabled = lockedBehaviour.WasEnabled;
            }

            lockedBehaviours.Clear();
        }

        private void CacheAndDisableInputBehaviours()
        {
            HashSet<MonoBehaviour> visited = new HashSet<MonoBehaviour>();

            if (playerRoot != null)
                AddMatchingBehaviours(playerRoot.GetComponentsInChildren<MonoBehaviour>(true), visited);

            if (disableMatchingComponentsGlobally)
            {
                MonoBehaviour[] allBehaviours = FindObjectsByType<MonoBehaviour>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
                AddMatchingBehaviours(allBehaviours, visited);
            }

            for (int i = 0; i < lockedBehaviours.Count; i++)
            {
                MonoBehaviour behaviour = lockedBehaviours[i].Behaviour;
                if (behaviour == null || !behaviour.enabled)
                    continue;

                TryCancelPlayerAction(behaviour);
                behaviour.enabled = false;
            }
        }

        private void AddMatchingBehaviours(MonoBehaviour[] behaviours, HashSet<MonoBehaviour> visited)
        {
            if (behaviours == null)
                return;

            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || !visited.Add(behaviour))
                    continue;

                if (!ShouldDisable(behaviour))
                    continue;

                lockedBehaviours.Add(new LockedBehaviour(behaviour, behaviour.enabled));
            }
        }

        private bool ShouldDisable(MonoBehaviour behaviour)
        {
            if (behaviour == null || componentTypeNamesToDisable == null)
                return false;

            string typeName = behaviour.GetType().Name;
            string fullName = behaviour.GetType().FullName;

            for (int i = 0; i < componentTypeNamesToDisable.Length; i++)
            {
                string candidate = componentTypeNamesToDisable[i];
                if (string.IsNullOrWhiteSpace(candidate))
                    continue;

                if (string.Equals(typeName, candidate, StringComparison.Ordinal) ||
                    string.Equals(fullName, candidate, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private void TryCancelPlayerAction(MonoBehaviour behaviour)
        {
            TryInvokeNoArgMethod(behaviour, "CancelChargedAttack");
            TryInvokeNoArgMethod(behaviour, "CancelPendingChargedAttackRepeats");
        }

        private static void TryInvokeNoArgMethod(MonoBehaviour behaviour, string methodName)
        {
            if (behaviour == null)
                return;

            MethodInfo method = behaviour.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (method == null || method.GetParameters().Length > 0)
                return;

            method.Invoke(behaviour, null);
        }

        private void ResolvePlayerRootIfNeeded()
        {
            if (playerRoot != null || !autoFindPlayer)
                return;

            GameObject taggedPlayer = null;
            try
            {
                taggedPlayer = GameObject.FindGameObjectWithTag("Player");
            }
            catch (UnityException)
            {
                taggedPlayer = null;
            }

            if (taggedPlayer != null)
            {
                playerRoot = taggedPlayer.transform;
                return;
            }

            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null)
                    continue;

                string typeName = behaviour.GetType().Name;
                if (typeName == "PlayerController" || typeName == "DummyPlayerController")
                {
                    playerRoot = behaviour.transform;
                    return;
                }
            }
        }

        private readonly struct LockedBehaviour
        {
            public readonly MonoBehaviour Behaviour;
            public readonly bool WasEnabled;

            public LockedBehaviour(MonoBehaviour behaviour, bool wasEnabled)
            {
                Behaviour = behaviour;
                WasEnabled = wasEnabled;
            }
        }
    }
}
