using UnityEngine;
using UnityEngine.UI;

namespace Dystopian.EnemyTest
{
    [DisallowMultipleComponent]
    public sealed class NpcInteractable : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string npcDisplayName = "NPC";

        [Header("Player Detection")]
        [SerializeField] private Transform playerTarget;
        [SerializeField] private bool autoFindPlayerTarget = true;
        [SerializeField, Min(0.1f)] private float interactionRadius = 2.4f;
        [SerializeField, Min(0.1f)] private float verticalRange = 2.2f;
        [SerializeField] private bool useSideViewDistance = true;
        [SerializeField] private bool facePlayerWhenInRange = true;

        [Header("Input")]
        [SerializeField] private KeyCode interactKey = KeyCode.E;
        [SerializeField] private bool canRepeatDialogue = true;

        [Header("Prompt")]
        [SerializeField] private GameObject promptRoot;
        [SerializeField] private Text promptText;
        [SerializeField] private string promptFormat = "E : 대화";
        [SerializeField] private bool keepPromptFacingCamera = true;

        [Header("Dialogue UI Anchor")]
        [SerializeField] private Transform dialogueAnchor;
        [SerializeField] private Vector3 dialogueAnchorOffset = new Vector3(0f, 2.75f, 0f);

        [Header("Dialogue")]
        [SerializeField] private NpcDialogueBranch[] dialogueBranches = new NpcDialogueBranch[0];

        [Header("Debug")]
        [SerializeField] private bool drawRangeGizmo = true;
        [SerializeField] private Color rangeGizmoColor = new Color(0.15f, 0.9f, 1f, 0.35f);

        private NpcDialogueManager dialogueManager;
        private bool hasCompletedDialogueOnce;
        private bool playerInRange;
        private int facingSign = 1;

        public string NpcDisplayName => npcDisplayName;
        public bool IsPlayerInRange => playerInRange;
        public Vector3 DialogueAnchorPosition => dialogueAnchor != null
            ? dialogueAnchor.position
            : transform.position + dialogueAnchorOffset;

        private void Awake()
        {
            dialogueManager = FindFirstObjectByType<NpcDialogueManager>();
            SetPromptVisible(false);
        }

        private void Update()
        {
            ResolvePlayerTargetIfNeeded();
            playerInRange = IsTargetInRange();

            if (facePlayerWhenInRange && playerInRange)
                FacePlayer();

            bool canInteractNow = CanInteractNow();
            SetPromptVisible(canInteractNow);
            UpdatePromptRotation();

            if (canInteractNow && Input.GetKeyDown(interactKey))
                TryStartDialogue();
        }

        public bool TryStartDialogue()
        {
            if (!CanInteractNow())
                return false;

            if (dialogueManager == null)
                dialogueManager = FindFirstObjectByType<NpcDialogueManager>();

            if (dialogueManager == null)
            {
                Debug.LogWarning("[NPC Dialogue] NpcDialogueManager is missing in this scene.", this);
                return false;
            }

            NpcDialogueBranch branch = SelectDialogueBranch();
            if (branch == null)
            {
                Debug.LogWarning($"[NPC Dialogue] {npcDisplayName} has no available dialogue branch.", this);
                return false;
            }

            return dialogueManager.BeginDialogue(this, branch);
        }

        public void NotifyDialogueCompleted(NpcDialogueBranch branch)
        {
            hasCompletedDialogueOnce = true;
        }

        private bool CanInteractNow()
        {
            if (dialogueManager == null)
                dialogueManager = FindFirstObjectByType<NpcDialogueManager>();

            if (!playerInRange)
                return false;

            if (!canRepeatDialogue && hasCompletedDialogueOnce)
                return false;

            if (dialogueManager != null)
            {
                if (dialogueManager.IsDialogueActive)
                    return false;

                if (!dialogueManager.CanStartInteractionThisFrame)
                    return false;
            }

            return SelectDialogueBranch() != null;
        }

        private NpcDialogueBranch SelectDialogueBranch()
        {
            if (dialogueBranches == null)
                return null;

            for (int i = 0; i < dialogueBranches.Length; i++)
            {
                NpcDialogueBranch branch = dialogueBranches[i];
                if (branch != null && branch.IsAvailable())
                    return branch;
            }

            return null;
        }

        private bool IsTargetInRange()
        {
            if (playerTarget == null)
                return false;

            Vector3 delta = playerTarget.position - transform.position;
            if (useSideViewDistance)
            {
                float horizontalDistance = Mathf.Abs(delta.x);
                float verticalDistance = Mathf.Abs(delta.y);
                return horizontalDistance <= interactionRadius && verticalDistance <= verticalRange;
            }

            return delta.sqrMagnitude <= interactionRadius * interactionRadius;
        }

        private void FacePlayer()
        {
            if (playerTarget == null)
                return;

            float deltaX = playerTarget.position.x - transform.position.x;
            if (Mathf.Abs(deltaX) < 0.05f)
                return;

            facingSign = deltaX < 0f ? -1 : 1;
            transform.rotation = facingSign > 0
                ? Quaternion.Euler(0f, 90f, 0f)
                : Quaternion.Euler(0f, -90f, 0f);
        }

        private void SetPromptVisible(bool visible)
        {
            if (promptRoot != null && promptRoot.activeSelf != visible)
                promptRoot.SetActive(visible);

            if (promptText != null)
                promptText.text = promptFormat;
        }

        private void UpdatePromptRotation()
        {
            if (!keepPromptFacingCamera || promptRoot == null || !promptRoot.activeSelf)
                return;

            Camera mainCamera = Camera.main;
            promptRoot.transform.rotation = mainCamera != null
                ? mainCamera.transform.rotation
                : Quaternion.identity;
        }

        private void ResolvePlayerTargetIfNeeded()
        {
            if (playerTarget != null || !autoFindPlayerTarget)
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
                playerTarget = taggedPlayer.transform;
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
                    playerTarget = behaviour.transform;
                    return;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawRangeGizmo)
                return;

            Gizmos.color = rangeGizmoColor;
            if (useSideViewDistance)
            {
                Vector3 size = new Vector3(interactionRadius * 2f, verticalRange * 2f, 0.1f);
                Gizmos.DrawWireCube(transform.position, size);
            }
            else
            {
                Gizmos.DrawWireSphere(transform.position, interactionRadius);
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(DialogueAnchorPosition, 0.12f);
        }
    }
}
