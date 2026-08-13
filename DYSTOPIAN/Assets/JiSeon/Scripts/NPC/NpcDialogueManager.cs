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
        [SerializeField] private bool useWorldSpaceDialogue = true;
        [SerializeField] private Vector2 worldPanelSize = new Vector2(560f, 220f);
        [SerializeField, Min(0.001f)] private float worldCanvasScale = 0.0085f;
        [SerializeField, Min(0f)] private float worldCameraForwardOffset = 0.05f;
        [SerializeField] private bool keepWorldDialogueFacingCamera = true;
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
        private RectTransform dialogueCanvasRect;
        private RectTransform dialoguePanelRect;
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

            UpdateDialogueWorldTransform();

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

        private void LateUpdate()
        {
            if (IsDialogueActive)
                UpdateDialogueWorldTransform();
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
            UpdateDialogueWorldTransform();
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

            if (dialogueCanvasRect == null && dialogueCanvas != null)
                dialogueCanvasRect = dialogueCanvas.GetComponent<RectTransform>();
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
            dialogueCanvas.renderMode = useWorldSpaceDialogue
                ? RenderMode.WorldSpace
                : RenderMode.ScreenSpaceOverlay;
            dialogueCanvas.worldCamera = Camera.main;
            dialogueCanvas.overrideSorting = true;
            dialogueCanvas.sortingOrder = 100;
            dialogueCanvasGroup = canvasObject.AddComponent<CanvasGroup>();
            canvasObject.AddComponent<GraphicRaycaster>();
            dialogueCanvasRect = canvasObject.GetComponent<RectTransform>();

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = useWorldSpaceDialogue
                ? CanvasScaler.ScaleMode.ConstantPixelSize
                : CanvasScaler.ScaleMode.ScaleWithScreenSize;
            if (!useWorldSpaceDialogue)
            {
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            if (useWorldSpaceDialogue && dialogueCanvasRect != null)
            {
                dialogueCanvasRect.sizeDelta = worldPanelSize;
                dialogueCanvasRect.localScale = Vector3.one * worldCanvasScale;
            }

            Font defaultFont = GetDefaultFont();

            GameObject panelObject = new GameObject("DialoguePanel");
            panelObject.transform.SetParent(canvasObject.transform, false);
            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0.02f, 0.02f, 0.025f, 0.82f);
            dialoguePanelRect = panelImage.rectTransform;
            dialoguePanelRect.anchorMin = new Vector2(0.5f, 0.5f);
            dialoguePanelRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialoguePanelRect.pivot = new Vector2(0.5f, 0.5f);
            dialoguePanelRect.anchoredPosition = Vector2.zero;
            dialoguePanelRect.sizeDelta = useWorldSpaceDialogue
                ? worldPanelSize
                : new Vector2(1000f, 210f);

            if (useWorldSpaceDialogue)
                CreateSpeechBubbleTail(panelObject.transform, panelImage.color);

            int speakerFontSize = useWorldSpaceDialogue ? 24 : 30;
            int bodyFontSize = useWorldSpaceDialogue ? 28 : 28;
            int hintFontSize = useWorldSpaceDialogue ? 18 : 20;
            float sidePadding = useWorldSpaceDialogue ? 26f : 32f;
            float topPadding = useWorldSpaceDialogue ? 18f : 18f;
            float speakerHeight = useWorldSpaceDialogue ? 34f : 44f;
            float hintHeight = useWorldSpaceDialogue ? 30f : 32f;
            float bodyTitleGap = useWorldSpaceDialogue ? 12f : 4f;

            speakerNameText = CreateText(panelObject.transform, "SpeakerName", defaultFont, speakerFontSize, FontStyle.Bold);
            speakerNameText.verticalOverflow = VerticalWrapMode.Truncate;
            RectTransform speakerRect = speakerNameText.rectTransform;
            speakerRect.anchorMin = new Vector2(0f, 1f);
            speakerRect.anchorMax = new Vector2(1f, 1f);
            speakerRect.pivot = new Vector2(0f, 1f);
            speakerRect.offsetMin = new Vector2(sidePadding, -topPadding - speakerHeight);
            speakerRect.offsetMax = new Vector2(-sidePadding, -topPadding);

            bodyText = CreateText(panelObject.transform, "DialogueText", defaultFont, bodyFontSize, FontStyle.Normal);
            bodyText.lineSpacing = useWorldSpaceDialogue ? 0.95f : 1f;
            bodyText.verticalOverflow = VerticalWrapMode.Truncate;
            RectTransform bodyRect = bodyText.rectTransform;
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.offsetMin = new Vector2(sidePadding, hintHeight + topPadding);
            bodyRect.offsetMax = new Vector2(-sidePadding, -topPadding - speakerHeight - bodyTitleGap);

            continueHintText = CreateText(panelObject.transform, "ContinueHint", defaultFont, hintFontSize, FontStyle.Normal);
            continueHintText.alignment = TextAnchor.MiddleRight;
            continueHintText.color = new Color(1f, 1f, 1f, 0.72f);
            continueHintText.verticalOverflow = VerticalWrapMode.Truncate;
            RectTransform hintRect = continueHintText.rectTransform;
            hintRect.anchorMin = new Vector2(0f, 0f);
            hintRect.anchorMax = new Vector2(1f, 0f);
            hintRect.pivot = new Vector2(1f, 0f);
            hintRect.offsetMin = new Vector2(sidePadding, topPadding * 0.6f);
            hintRect.offsetMax = new Vector2(-sidePadding, topPadding * 0.6f + hintHeight);
        }

        private void CreateSpeechBubbleTail(Transform panelTransform, Color color)
        {
            GameObject tailObject = new GameObject("DialogueTail");
            tailObject.transform.SetParent(panelTransform, false);

            Image tailImage = tailObject.AddComponent<Image>();
            tailImage.color = color;

            RectTransform tailRect = tailImage.rectTransform;
            tailRect.anchorMin = new Vector2(0.5f, 0f);
            tailRect.anchorMax = new Vector2(0.5f, 0f);
            tailRect.pivot = new Vector2(0.5f, 0.5f);
            tailRect.anchoredPosition = new Vector2(0f, -10f);
            tailRect.sizeDelta = new Vector2(24f, 24f);
            tailRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        private void UpdateDialogueWorldTransform()
        {
            if (!useWorldSpaceDialogue || dialogueCanvas == null || currentNpc == null)
                return;

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
                dialogueCanvas.worldCamera = mainCamera;

            dialogueCanvas.overrideSorting = true;
            dialogueCanvas.sortingOrder = 100;

            Vector3 targetPosition = currentNpc.DialogueAnchorPosition;
            if (mainCamera != null && worldCameraForwardOffset > 0f)
                targetPosition += (mainCamera.transform.position - targetPosition).normalized * worldCameraForwardOffset;

            Transform canvasTransform = dialogueCanvas.transform;
            canvasTransform.position = targetPosition;
            canvasTransform.localScale = Vector3.one * worldCanvasScale;

            if (keepWorldDialogueFacingCamera)
                canvasTransform.rotation = mainCamera != null
                    ? mainCamera.transform.rotation
                    : Quaternion.identity;

            if (dialogueCanvasRect != null)
                dialogueCanvasRect.sizeDelta = worldPanelSize;

            if (dialoguePanelRect != null)
                dialoguePanelRect.sizeDelta = worldPanelSize;
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
