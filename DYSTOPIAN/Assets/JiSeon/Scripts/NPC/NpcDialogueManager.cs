using System;
using UnityEngine;
using UnityEngine.UI;

namespace Dystopian.EnemyTest
{
    [DisallowMultipleComponent]
    public sealed class NpcDialogueManager : MonoBehaviour
    {
        private const int NoLineIndex = -1;

        [Header("UI")]
        [SerializeField] private bool autoCreateUiIfMissing = true;
        [SerializeField] private Canvas dialogueCanvas;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;
        [SerializeField] private Text speakerNameText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text continueHintText;

        [Header("Input")]
        [SerializeField] private KeyCode advanceKey = KeyCode.Space;
        [SerializeField] private KeyCode alternateAdvanceKey = KeyCode.Return;
        [SerializeField] private KeyCode closeKey = KeyCode.Escape;
        [SerializeField] private bool interactionKeyAlsoAdvances = true;
        [SerializeField] private KeyCode interactionKey = KeyCode.E;

        [Header("Player Lock")]
        [SerializeField] private NpcPlayerControlLock playerControlLock;
        [SerializeField] private bool autoFindPlayerControlLock = true;

        [Header("Text")]
        [SerializeField] private string defaultContinueHint = "Space / Enter / E : 다음";

        private NpcInteractable currentNpc;
        private NpcDialogueBranch currentBranch;
        private int currentLineIndex = NoLineIndex;
        private int ignoreAdvanceInputFrame = -1;
        private int suppressInteractionUntilFrame = -1;
        private int lastDialogueEndedFrame = -1;

        public static NpcDialogueManager Instance { get; private set; }
        public bool IsDialogueActive => currentBranch != null;
        public bool CanStartInteractionThisFrame => Time.frameCount > suppressInteractionUntilFrame;
        public bool WasDialogueEndedThisFrame => Time.frameCount == lastDialogueEndedFrame;

        public event Action<NpcInteractable, NpcDialogueBranch> DialogueStarted;
        public event Action<NpcInteractable, NpcDialogueBranch, bool> DialogueEnded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ResolvePlayerControlLock();
            EnsureUiReferences();
            SetDialogueVisible(false);
        }

        private void Update()
        {
            if (!IsDialogueActive)
                return;

            if (Input.GetKeyDown(closeKey))
            {
                EndDialogue(false);
                return;
            }

            if (Time.frameCount <= ignoreAdvanceInputFrame)
                return;

            if (Input.GetKeyDown(advanceKey) ||
                Input.GetKeyDown(alternateAdvanceKey) ||
                (interactionKeyAlsoAdvances && Input.GetKeyDown(interactionKey)))
            {
                ShowNextLineOrEnd();
            }
        }

        public bool BeginDialogue(NpcInteractable npc, NpcDialogueBranch branch)
        {
            if (branch == null || !branch.HasLines)
            {
                Debug.LogWarning("[NPC Dialogue] Cannot start dialogue because the branch has no lines.", npc);
                return false;
            }

            if (IsDialogueActive)
                EndDialogue(false);

            currentNpc = npc;
            currentBranch = branch;
            currentLineIndex = NoLineIndex;
            ignoreAdvanceInputFrame = Time.frameCount;

            ResolvePlayerControlLock();
            playerControlLock?.Lock();

            SetDialogueVisible(true);
            currentBranch.NotifyStarted();
            DialogueStarted?.Invoke(currentNpc, currentBranch);
            ShowNextLineOrEnd();
            return true;
        }

        public void EndDialogue(bool completed)
        {
            if (!IsDialogueActive)
                return;

            NpcDialogueBranch endingBranch = currentBranch;
            NpcInteractable endingNpc = currentNpc;

            if (completed && TryGetCurrentLine(out NpcDialogueLine currentLine))
                currentLine.NotifyCompleted();

            currentNpc = null;
            currentBranch = null;
            currentLineIndex = NoLineIndex;
            lastDialogueEndedFrame = Time.frameCount;
            suppressInteractionUntilFrame = Time.frameCount + 1;

            if (completed)
            {
                endingBranch.NotifyCompleted(endingNpc != null ? endingNpc : this);
                endingNpc?.NotifyDialogueCompleted(endingBranch);
            }

            SetDialogueVisible(false);
            playerControlLock?.Unlock();
            DialogueEnded?.Invoke(endingNpc, endingBranch, completed);
        }

        private void ShowNextLineOrEnd()
        {
            if (!IsDialogueActive)
                return;

            if (TryGetCurrentLine(out NpcDialogueLine previousLine))
                previousLine.NotifyCompleted();

            currentLineIndex++;
            NpcDialogueLine[] lines = currentBranch.Lines;

            while (lines != null && currentLineIndex < lines.Length && lines[currentLineIndex] == null)
                currentLineIndex++;

            if (lines == null || currentLineIndex >= lines.Length)
            {
                EndDialogue(true);
                return;
            }

            NpcDialogueLine line = lines[currentLineIndex];
            string speaker = string.IsNullOrWhiteSpace(line.SpeakerName)
                ? currentNpc != null ? currentNpc.NpcDisplayName : "NPC"
                : line.SpeakerName;

            if (speakerNameText != null)
                speakerNameText.text = speaker;

            if (bodyText != null)
                bodyText.text = line.Text;

            if (continueHintText != null)
                continueHintText.text = defaultContinueHint;

            line.NotifyShown();
        }

        private bool TryGetCurrentLine(out NpcDialogueLine line)
        {
            line = null;

            if (currentBranch == null ||
                currentBranch.Lines == null ||
                currentLineIndex < 0 ||
                currentLineIndex >= currentBranch.Lines.Length)
                return false;

            line = currentBranch.Lines[currentLineIndex];
            return line != null;
        }

        private void ResolvePlayerControlLock()
        {
            if (playerControlLock != null || !autoFindPlayerControlLock)
                return;

            playerControlLock = GetComponent<NpcPlayerControlLock>();
            if (playerControlLock == null)
                playerControlLock = FindFirstObjectByType<NpcPlayerControlLock>();
        }

        private void EnsureUiReferences()
        {
            if (dialogueCanvas == null && autoCreateUiIfMissing)
                CreateRuntimeDialogueUi();

            if (dialogueCanvasGroup == null && dialogueCanvas != null)
                dialogueCanvasGroup = dialogueCanvas.GetComponent<CanvasGroup>();
        }

        private void SetDialogueVisible(bool visible)
        {
            if (dialogueCanvasGroup != null)
            {
                dialogueCanvasGroup.alpha = visible ? 1f : 0f;
                dialogueCanvasGroup.blocksRaycasts = visible;
                dialogueCanvasGroup.interactable = visible;
            }

            if (dialogueCanvas != null)
                dialogueCanvas.gameObject.SetActive(visible);
        }

        private void CreateRuntimeDialogueUi()
        {
            GameObject canvasObject = new GameObject("NpcDialogueCanvas");
            canvasObject.transform.SetParent(transform, false);

            dialogueCanvas = canvasObject.AddComponent<Canvas>();
            dialogueCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            dialogueCanvas.sortingOrder = 100;
            dialogueCanvasGroup = canvasObject.AddComponent<CanvasGroup>();
            canvasObject.AddComponent<GraphicRaycaster>();

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Font defaultFont = GetDefaultFont();

            GameObject panelObject = new GameObject("DialoguePanel");
            panelObject.transform.SetParent(canvasObject.transform, false);
            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0.02f, 0.02f, 0.025f, 0.82f);
            RectTransform panelRect = panelImage.rectTransform;
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 52f);
            panelRect.sizeDelta = new Vector2(1000f, 210f);

            speakerNameText = CreateText(panelObject.transform, "SpeakerName", defaultFont, 30, FontStyle.Bold);
            RectTransform speakerRect = speakerNameText.rectTransform;
            speakerRect.anchorMin = new Vector2(0f, 1f);
            speakerRect.anchorMax = new Vector2(1f, 1f);
            speakerRect.pivot = new Vector2(0f, 1f);
            speakerRect.offsetMin = new Vector2(32f, -62f);
            speakerRect.offsetMax = new Vector2(-32f, -18f);

            bodyText = CreateText(panelObject.transform, "DialogueText", defaultFont, 28, FontStyle.Normal);
            RectTransform bodyRect = bodyText.rectTransform;
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.offsetMin = new Vector2(32f, 54f);
            bodyRect.offsetMax = new Vector2(-32f, -72f);

            continueHintText = CreateText(panelObject.transform, "ContinueHint", defaultFont, 20, FontStyle.Normal);
            continueHintText.alignment = TextAnchor.MiddleRight;
            continueHintText.color = new Color(1f, 1f, 1f, 0.72f);
            RectTransform hintRect = continueHintText.rectTransform;
            hintRect.anchorMin = new Vector2(0f, 0f);
            hintRect.anchorMax = new Vector2(1f, 0f);
            hintRect.pivot = new Vector2(1f, 0f);
            hintRect.offsetMin = new Vector2(32f, 18f);
            hintRect.offsetMax = new Vector2(-32f, 50f);
        }

        private static Text CreateText(Transform parent, string objectName, Font font, int size, FontStyle fontStyle)
        {
            GameObject textObject = new GameObject(objectName);
            textObject.transform.SetParent(parent, false);

            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = fontStyle;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Font GetDefaultFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
                return font;

            try
            {
                return Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }
}
