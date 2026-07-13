using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Dystopian.Rhythm
{
    public enum RhythmActor { Player, Enemy, Boss }
    public enum RhythmNoteType { Single, Long, AttackState, Ghost }
    public enum RhythmJudgement { Critical, Cool, Good, Bad, Miss }

    [Serializable]
    public sealed class RhythmChartNote
    {
        public string label = "Note";
        public bool enabled = true;
        public RhythmActor actor = RhythmActor.Player;
        public RhythmNoteType type = RhythmNoteType.Single;
        [Min(0f)] public float beat;
        [Min(0f)] public float durationBeats;
        [Range(-1, 1)] public int lane;
        public string actionId;
    }

    [Serializable]
    public sealed class RhythmJudgementWindows
    {
        [Min(0.001f)] public float criticalSeconds = 0.045f;
        [Min(0.001f)] public float coolSeconds = 0.09f;
        [Min(0.001f)] public float goodSeconds = 0.14f;
        [Min(0.001f)] public float badSeconds = 0.22f;

        public void Clamp()
        {
            criticalSeconds = Mathf.Max(0.001f, criticalSeconds);
            coolSeconds = Mathf.Max(criticalSeconds, coolSeconds);
            goodSeconds = Mathf.Max(coolSeconds, goodSeconds);
            badSeconds = Mathf.Max(goodSeconds, badSeconds);
        }
    }

    [Serializable]
    public sealed class RhythmLayout
    {
        public Vector2 referenceResolution = new Vector2(1920f, 1080f);
        public Vector2 panelSize = new Vector2(1500f, 230f);
        [Min(0f)] public float bottomMargin = 35f;
        public float playerTargetX = -35f;
        public float opponentTargetX = 35f;
        [Min(0f)] public float edgePadding = 25f;
        [Min(0f)] public float offscreenSpawnPadding = 80f;
        [Min(1f)] public float judgementLineWidth = 7f;
        [Min(1f)] public float judgementLineHeight = 190f;
        [Min(1f)] public float railThickness = 4f;
        [Min(1f)] public float judgementZoneHeight = 104f;
        [Min(1f)] public float singleNoteWidth = 20f;
        [Min(1f)] public float noteHeight = 72f;
        [Min(0f)] public float laneOffset = 48f;
        [Min(0f)] public float minimumLongWidth = 42f;
        public int sortingOrder = 100;
    }

    [Serializable]
    public sealed class RhythmColors
    {
        public Color panel = new Color(0.025f, 0.035f, 0.055f, 0.92f);
        public Color playerLane = new Color(0.04f, 0.16f, 0.22f, 0.9f);
        public Color enemyLane = new Color(0.22f, 0.055f, 0.07f, 0.9f);
        public Color playerNote = new Color(0.1f, 0.85f, 1f, 1f);
        public Color enemyNote = new Color(1f, 0.2f, 0.26f, 1f);
        public Color bossNote = new Color(1f, 0.72f, 0.1f, 1f);
        public Color attackState = new Color(1f, 0.25f, 0.82f, 0.9f);
        public Color judgementLine = Color.white;
        public Color critical = new Color(1f, 0.9f, 0.25f, 1f);
        public Color cool = new Color(0.2f, 0.9f, 1f, 1f);
        public Color good = new Color(0.25f, 1f, 0.48f, 1f);
        public Color bad = new Color(1f, 0.45f, 0.18f, 1f);
        public Color miss = new Color(1f, 0.15f, 0.2f, 1f);
    }

    [Serializable] public sealed class NoteUnityEvent : UnityEvent<RhythmChartNote> { }
    [Serializable] public sealed class JudgementUnityEvent : UnityEvent<RhythmJudgement, float> { }

    [DisallowMultipleComponent]
    public sealed class RhythmSystem : MonoBehaviour
    {
        [Header("Song / Clock")]
        public AudioSource musicSource;
        public AudioClip musicClip;
        [Min(1f)] public float bpm = 120f;
        public float firstBeatOffsetSeconds;
        [Min(0f)] public float startDelaySeconds = 0.5f;
        public bool playMusicOnStart = true;
        public bool useUnscaledClockWithoutMusic = true;
        public bool startMusicOnFirstPlayerJudgement = true;
        public bool autoStartDelayFromTravelBeats = true;
        public bool useDspScheduling = true;
        [Min(0.01f)] public float minimumDspLeadSeconds = 0.1f;
        public bool preloadMusicOnAwake = true;
        public bool primeMusicSourceOnLoad = true;

        [Header("BMS Chart Sources")]
        public bool loadBmsOnStart = true;
        public TextAsset playerBms;
        public TextAsset opponentBms;
        public RhythmActor opponentActor = RhythmActor.Enemy;
        [Min(1f)] public float beatsPerMeasure = 4f;

        [Header("Input")]
        public bool waitForStartInput = true;
        public bool toggleChartWithStartKey = true;
        public KeyCode startChartKey = KeyCode.S;
        public KeyCode singleNoteKey = KeyCode.A;
        public KeyCode longNoteKey = KeyCode.D;
        public bool enableFreeInputDuringAttackState = true;

        [Header("Timing")]
        [Min(0.1f)] public float travelBeats = 4f;
        public bool returnNoteToPoolAtTarget = true;
        public RhythmJudgementWindows judgement = new RhythmJudgementWindows();

        [Header("Pooling")]
        [Min(1)] public int initialPoolSize = 24;
        public bool allowPoolExpansion = true;

        [Header("Chart (beat 0 is the first beat)")]
        public List<RhythmChartNote> chart = new List<RhythmChartNote>();

        [Header("Presentation")]
        public RhythmLayout layout = new RhythmLayout();
        public RhythmColors colors = new RhythmColors();
        public bool showPlayerJudgementZones = true;
        public bool showOpponentJudgementZones;
        [Range(0f, 1f)] public float judgementZoneAlpha = 0.24f;
        [Min(0f)] public float judgementTextSeconds = 0.55f;
        [Min(8)] public int judgementFontSize = 42;
        public bool logJudgements;

        [Header("Player Events")]
        public UnityEvent onChartStarted = new UnityEvent();
        public UnityEvent onChartStopped = new UnityEvent();
        public NoteUnityEvent onPlayerSingle = new NoteUnityEvent();
        public NoteUnityEvent onChargeStart = new NoteUnityEvent();
        public NoteUnityEvent onChargeEnd = new NoteUnityEvent();
        public NoteUnityEvent onAttackStateStart = new NoteUnityEvent();
        public NoteUnityEvent onAttackStateEnd = new NoteUnityEvent();
        public UnityEvent onFreeSingleInput = new UnityEvent();
        public UnityEvent onFreeLongInput = new UnityEvent();
        public JudgementUnityEvent onJudged = new JudgementUnityEvent();

        [Header("Enemy / Boss Events")]
        public NoteUnityEvent onEnemyAction = new NoteUnityEvent();
        public NoteUnityEvent onEnemyLongEnd = new NoteUnityEvent();
        public NoteUnityEvent onBossAction = new NoteUnityEvent();
        public NoteUnityEvent onBossLongEnd = new NoteUnityEvent();
        public NoteUnityEvent onGhostAction = new NoteUnityEvent();

        [Header("Generated Scene References")]
        [SerializeField] private Canvas rhythmCanvas;
        [SerializeField] private RectTransform panel;
        [SerializeField] private RectTransform noteLayer;
        [SerializeField] private RectTransform playerJudgementLine;
        [SerializeField] private RectTransform opponentJudgementLine;
        [SerializeField] private Text judgementText;

        private readonly Queue<RhythmNoteView> pool = new Queue<RhythmNoteView>();
        private readonly List<RuntimeNote> runtimeNotes = new List<RuntimeNote>();
        private float clockStartedAt;
        private double dspSongStartTime;
        private bool clockRunning;
        private bool dspClockActive;
        private bool pendingChartStart;
        private bool musicStarted;
        private bool musicReady;
        private bool musicPriming;
        private bool pendingMusicStart;
        private int activeAttackStates;
        private float hideJudgementAt;

        private sealed class RuntimeNote
        {
            public RhythmChartNote data;
            public RhythmNoteView view;
            public bool spawned;
            public bool resolved;
            public bool holding;
            public bool autoStarted;
            public bool missedLongStart;
            public RhythmJudgement startJudgement;
            public float visualWidth;
        }

        public float CurrentSongSeconds
        {
            get
            {
                if (!clockRunning) return -startDelaySeconds;
                if (useDspScheduling && dspClockActive)
                    return (float)(AudioSettings.dspTime - dspSongStartTime);
                float elapsed = (useUnscaledClockWithoutMusic ? Time.unscaledTime : Time.time) - clockStartedAt;
                return elapsed - startDelaySeconds;
            }
        }

        public float CurrentBeat => (CurrentSongSeconds - firstBeatOffsetSeconds) * Mathf.Max(1f, bpm) / 60f;
        public bool IsChartRunning => clockRunning;
        public bool IsAttackStateActive => activeAttackStates > 0;

        private void Reset()
        {
            PopulateExampleChart();
            BuildPreview();
        }

        private void OnValidate()
        {
            bpm = Mathf.Max(1f, bpm);
            travelBeats = Mathf.Max(0.1f, travelBeats);
            initialPoolSize = Mathf.Max(1, initialPoolSize);
            judgement.Clamp();
        }

        private void Awake()
        {
            EnsureVisuals();
            if (musicSource == null) musicSource = GetComponent<AudioSource>();
            if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
            if (musicClip != null) musicSource.clip = musicClip;
            if (preloadMusicOnAwake && musicSource.clip != null)
            {
                musicSource.clip.LoadAudioData();
                musicReady = musicSource.clip.loadState == AudioDataLoadState.Loaded;
            }
        }

        private void Start()
        {
            EnsureVisuals();
            if (loadBmsOnStart && playerBms != null && opponentBms != null) LoadBmsChart();
            PreparePool();
            RestartSong();
            if (preloadMusicOnAwake && musicSource != null && musicSource.clip != null)
                StartCoroutine(PrepareMusicForPlayback());
        }

        private void Update()
        {
            if (!Application.isPlaying) return;
            if (pendingMusicStart && musicReady && !musicPriming) PlayPreparedMusic();
            if (pendingChartStart && musicReady && !musicPriming)
            {
                pendingChartStart = false;
                StartChart();
            }
            if (waitForStartInput && Input.GetKeyDown(startChartKey))
            {
                if (toggleChartWithStartKey && (clockRunning || pendingChartStart)) StopChart();
                else StartChart();
                return;
            }
            if (!clockRunning)
            {
                return;
            }
            float beat = CurrentBeat;
            SpawnDueNotes(beat);
            UpdateNotes(beat);
            HandleInput(beat);
            if (judgementText != null && judgementText.gameObject.activeSelf && Time.unscaledTime >= hideJudgementAt)
                judgementText.gameObject.SetActive(false);
        }

        [ContextMenu("Restart Song")]
        public void RestartSong()
        {
            if (!Application.isPlaying) return;
            PrepareStoppedChart();
            if (!waitForStartInput) StartChart();
        }

        private void PrepareStoppedChart()
        {
            foreach (RuntimeNote note in runtimeNotes) if (note.view != null) ReturnToPool(note.view);
            runtimeNotes.Clear();
            activeAttackStates = 0;
            musicStarted = false;
            pendingMusicStart = false;
            pendingChartStart = false;
            dspClockActive = false;
            if (autoStartDelayFromTravelBeats) startDelaySeconds = travelBeats * 60f / Mathf.Max(1f, bpm);
            for (int i = 0; i < chart.Count; i++)
                if (chart[i] != null && chart[i].enabled) runtimeNotes.Add(new RuntimeNote { data = chart[i] });
            runtimeNotes.Sort((a, b) => a.data.beat.CompareTo(b.data.beat));
            clockRunning = false;
            if (musicSource != null)
            {
                musicSource.Stop();
                if (musicClip != null) musicSource.clip = musicClip;
            }
        }

        [ContextMenu("Start Chart")]
        public void StartChart()
        {
            if (!Application.isPlaying || clockRunning || pendingChartStart) return;

            if (useDspScheduling && musicSource != null && musicSource.clip != null && (!musicReady || musicPriming))
            {
                pendingChartStart = true;
                if (musicSource.clip.loadState == AudioDataLoadState.Unloaded) musicSource.clip.LoadAudioData();
                return;
            }

            clockStartedAt = useUnscaledClockWithoutMusic ? Time.unscaledTime : Time.time;
            if (useDspScheduling)
            {
                double lead = Math.Max(startDelaySeconds, minimumDspLeadSeconds);
                dspSongStartTime = AudioSettings.dspTime + lead;
                dspClockActive = true;
            }
            clockRunning = true;
            if (playMusicOnStart && musicSource != null && musicSource.clip != null)
            {
                musicSource.time = 0f;
                if (useDspScheduling)
                {
                    musicSource.PlayScheduled(dspSongStartTime);
                    musicStarted = true;
                }
                else if (!startMusicOnFirstPlayerJudgement)
                {
                    musicSource.PlayDelayed(startDelaySeconds);
                    musicStarted = true;
                }
            }
            onChartStarted.Invoke();
        }

        [ContextMenu("Stop Chart")]
        public void StopChart()
        {
            if (!Application.isPlaying) return;
            bool wasActive = clockRunning || pendingChartStart;
            PrepareStoppedChart();
            if (wasActive) onChartStopped.Invoke();
        }

        public void SetChartActive(bool active)
        {
            if (active) StartChart();
            else StopChart();
        }

        private IEnumerator PrepareMusicForPlayback()
        {
            AudioClip clip = musicSource != null ? musicSource.clip : null;
            if (clip == null) yield break;

            if (clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
            while (clip.loadState == AudioDataLoadState.Loading) yield return null;

            if (clip.loadState != AudioDataLoadState.Loaded)
            {
                Debug.LogWarning("[Rhythm] Music preload failed: " + clip.name, this);
                yield break;
            }

            musicReady = true;
            if (primeMusicSourceOnLoad)
            {
                musicPriming = true;
                bool previousMute = musicSource.mute;
                musicSource.mute = true;
                musicSource.Play();
                yield return null;
                musicSource.Stop();
                musicSource.mute = previousMute;
                musicPriming = false;
            }

            if (pendingChartStart)
            {
                pendingChartStart = false;
                StartChart();
            }
            else if (pendingMusicStart) PlayPreparedMusic();
        }

        [ContextMenu("Load Player + Opponent BMS")]
        public void LoadBmsChart()
        {
            if (playerBms == null || opponentBms == null)
            {
                Debug.LogWarning("[Rhythm] Assign both Player BMS and Opponent BMS.", this);
                return;
            }

            float parsedBpm;
            if (TryReadBpm(playerBms.text, out parsedBpm)) bpm = parsedBpm;

            List<RhythmChartNote> imported = new List<RhythmChartNote>();
            ParseBms(playerBms.text, RhythmActor.Player, imported);
            ParseBms(opponentBms.text, opponentActor == RhythmActor.Player ? RhythmActor.Enemy : opponentActor, imported);
            imported.Sort((a, b) => a.beat.CompareTo(b.beat));
            chart = imported;
        }

        private void ParseBms(string source, RhythmActor actor, List<RhythmChartNote> output)
        {
            Dictionary<int, List<float>> longMarkers = new Dictionary<int, List<float>>();
            string[] lines = source.Replace("\r", string.Empty).Split('\n');

            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex].Trim();
                int colon = line.IndexOf(':');
                if (!line.StartsWith("#", StringComparison.Ordinal) || colon != 6) continue;

                int measure;
                int channel;
                if (!int.TryParse(line.Substring(1, 3), out measure) || !int.TryParse(line.Substring(4, 2), out channel)) continue;
                string data = line.Substring(colon + 1).Trim();
                if (data.Length < 2 || data.Length % 2 != 0) continue;

                bool singleChannel = channel >= 11 && channel <= 19;
                bool longChannel = channel >= 51 && channel <= 59;
                if (!singleChannel && !longChannel) continue;

                int slots = data.Length / 2;
                for (int slot = 0; slot < slots; slot++)
                {
                    string token = data.Substring(slot * 2, 2);
                    if (token == "00") continue;
                    float beat = measure * beatsPerMeasure + slot * beatsPerMeasure / slots;

                    if (singleChannel)
                    {
                        output.Add(NewNote(actor + " Single", actor, RhythmNoteType.Single, beat, 0f, 0));
                    }
                    else
                    {
                        List<float> markers;
                        if (!longMarkers.TryGetValue(channel, out markers))
                        {
                            markers = new List<float>();
                            longMarkers.Add(channel, markers);
                        }
                        markers.Add(beat);
                    }
                }
            }

            foreach (KeyValuePair<int, List<float>> pair in longMarkers)
            {
                pair.Value.Sort();
                for (int i = 0; i + 1 < pair.Value.Count; i += 2)
                {
                    float start = pair.Value[i];
                    float duration = Mathf.Max(0.01f, pair.Value[i + 1] - start);
                    output.Add(NewNote(actor + " Long", actor, RhythmNoteType.Long, start, duration, 0));
                }
            }
        }

        private static bool TryReadBpm(string source, out float parsedBpm)
        {
            string[] lines = source.Replace("\r", string.Empty).Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (!line.StartsWith("#BPM ", StringComparison.OrdinalIgnoreCase)) continue;
                return float.TryParse(line.Substring(5).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out parsedBpm);
            }
            parsedBpm = 0f;
            return false;
        }

        [ContextMenu("Populate Example Chart")]
        public void PopulateExampleChart()
        {
            chart = new List<RhythmChartNote>
            {
                NewNote("Player A", RhythmActor.Player, RhythmNoteType.Single, 4f, 0f, -1),
                NewNote("Enemy", RhythmActor.Enemy, RhythmNoteType.Single, 5f, 0f, 0),
                NewNote("Player Hold", RhythmActor.Player, RhythmNoteType.Long, 6f, 2f, 1),
                NewNote("Ghost Telegraph", RhythmActor.Boss, RhythmNoteType.Ghost, 8f, 0f, 0),
                NewNote("Boss Hold", RhythmActor.Boss, RhythmNoteType.Long, 9f, 2f, 0),
                NewNote("ATTACK!", RhythmActor.Player, RhythmNoteType.AttackState, 12f, 4f, 0),
                NewNote("Player A", RhythmActor.Player, RhythmNoteType.Single, 17f, 0f, -1)
            };
        }

        private static RhythmChartNote NewNote(string label, RhythmActor actor, RhythmNoteType type, float beat, float duration, int lane)
        {
            return new RhythmChartNote { label = label, actor = actor, type = type, beat = beat, durationBeats = duration, lane = lane, actionId = label };
        }

        [ContextMenu("Rebuild Rhythm UI Preview")]
        public void BuildPreview()
        {
            if (playerJudgementLine != null) layout.playerTargetX = playerJudgementLine.anchoredPosition.x;
            if (opponentJudgementLine != null) layout.opponentTargetX = opponentJudgementLine.anchoredPosition.x;
            EnsureVisuals(true);
        }

        private void EnsureVisuals(bool rebuild = false)
        {
            Transform existing = transform.Find("Rhythm UI");
            if (rebuild && existing != null)
            {
                if (Application.isPlaying) Destroy(existing.gameObject); else DestroyImmediate(existing.gameObject);
                existing = null;
            }

            if (existing == null)
            {
                GameObject canvasObject = new GameObject("Rhythm UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasObject.transform.SetParent(transform, false);
                rhythmCanvas = canvasObject.GetComponent<Canvas>();
                rhythmCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                rhythmCanvas.sortingOrder = layout.sortingOrder;
                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = layout.referenceResolution;
                scaler.matchWidthOrHeight = 0.5f;

                panel = CreateImage("Track Panel", canvasObject.transform, Color.clear);
                panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0f);
                panel.pivot = new Vector2(0.5f, 0f);
                panel.sizeDelta = layout.panelSize;
                panel.anchoredPosition = new Vector2(0f, layout.bottomMargin);

                RectTransform rail = CreateImage("Note Rail", panel, colors.judgementLine);
                SetRect(rail, Vector2.zero, new Vector2(layout.referenceResolution.x, layout.railThickness));

                playerJudgementLine = CreateImage("Player Judgement Line", panel, colors.judgementLine);
                SetRect(playerJudgementLine, new Vector2(layout.playerTargetX, 0f), new Vector2(layout.judgementLineWidth, layout.judgementLineHeight));
                if (showPlayerJudgementZones) CreateJudgementBands(playerJudgementLine, 0f, "Player");

                opponentJudgementLine = CreateImage("Opponent Judgement Line", panel, colors.judgementLine);
                SetRect(opponentJudgementLine, new Vector2(layout.opponentTargetX, 0f), new Vector2(layout.judgementLineWidth, layout.judgementLineHeight));
                if (showOpponentJudgementZones) CreateJudgementBands(opponentJudgementLine, 0f, "Opponent");

                noteLayer = CreateRect("Pooled Notes", panel);
                Stretch(noteLayer);

                GameObject textObject = new GameObject("Judgement Text", typeof(RectTransform), typeof(Text));
                textObject.transform.SetParent(canvasObject.transform, false);
                judgementText = textObject.GetComponent<Text>();
                RectTransform textRect = textObject.GetComponent<RectTransform>();
                textRect.anchorMin = textRect.anchorMax = new Vector2(0.5f, 0f);
                textRect.pivot = new Vector2(0.5f, 0f);
                textRect.sizeDelta = new Vector2(600f, 80f);
                textRect.anchoredPosition = new Vector2(0f, layout.bottomMargin + layout.panelSize.y + 15f);
                judgementText.alignment = TextAnchor.MiddleCenter;
                judgementText.fontSize = judgementFontSize;
                judgementText.fontStyle = FontStyle.Bold;
                judgementText.raycastTarget = false;
                judgementText.text = string.Empty;
                judgementText.gameObject.SetActive(false);
            }
            else
            {
                rhythmCanvas = existing.GetComponent<Canvas>();
                panel = existing.Find("Track Panel") as RectTransform;
                noteLayer = panel != null ? panel.Find("Pooled Notes") as RectTransform : null;
                playerJudgementLine = panel != null ? panel.Find("Player Judgement Line") as RectTransform : null;
                opponentJudgementLine = panel != null ? panel.Find("Opponent Judgement Line") as RectTransform : null;
                Transform textTransform = existing.Find("Judgement Text");
                judgementText = textTransform != null ? textTransform.GetComponent<Text>() : null;
            }
            AssignRuntimeFont();
        }

        private void AssignRuntimeFont()
        {
            if (judgementText == null || judgementText.font != null) return;
            try { judgementText.font = Font.CreateDynamicFontFromOSFont("Arial", judgementFontSize); } catch { }
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static RectTransform CreateImage(string name, Transform parent, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return go.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position; rect.sizeDelta = size;
        }

        private void CreateJudgementBands(Transform parent, float targetX, string prefix)
        {
            float travelSeconds = Mathf.Max(0.001f, travelBeats * 60f / Mathf.Max(1f, bpm));
            float pixelsPerSecond = TrackTravelDistance / travelSeconds;
            float critical = judgement.criticalSeconds * pixelsPerSecond;
            float cool = judgement.coolSeconds * pixelsPerSecond;
            float good = judgement.goodSeconds * pixelsPerSecond;
            float bad = judgement.badSeconds * pixelsPerSecond;

            CreateJudgementRange(parent, prefix, "BAD", targetX, good, bad, colors.bad);
            CreateJudgementRange(parent, prefix, "GOOD", targetX, cool, good, colors.good);
            CreateJudgementRange(parent, prefix, "COOL", targetX, critical, cool, colors.cool);

            RectTransform criticalBand = CreateImage(prefix + " CRITICAL Zone", parent, WithAlpha(colors.critical, judgementZoneAlpha));
            SetRect(criticalBand, new Vector2(targetX, 0f), new Vector2(critical * 2f, layout.judgementZoneHeight));
        }

        private void CreateJudgementRange(Transform parent, string prefix, string rating, float targetX, float inner, float outer, Color color)
        {
            float width = Mathf.Max(0f, outer - inner);
            if (width <= 0f) return;
            float centerOffset = (inner + outer) * 0.5f;
            Color bandColor = WithAlpha(color, judgementZoneAlpha);

            RectTransform early = CreateImage(prefix + " " + rating + " Early Zone", parent, bandColor);
            SetRect(early, new Vector2(targetX - centerOffset, 0f), new Vector2(width, layout.judgementZoneHeight));
            RectTransform late = CreateImage(prefix + " " + rating + " Late Zone", parent, bandColor);
            SetRect(late, new Vector2(targetX + centerOffset, 0f), new Vector2(width, layout.judgementZoneHeight));
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        private void PreparePool()
        {
            if (noteLayer == null) return;
            while (pool.Count < initialPoolSize) pool.Enqueue(CreatePooledNote());
        }

        private RhythmNoteView CreatePooledNote()
        {
            GameObject go = new GameObject("Pooled Note", typeof(RectTransform), typeof(Image), typeof(RhythmNoteView));
            go.transform.SetParent(noteLayer, false);
            RhythmNoteView view = go.GetComponent<RhythmNoteView>();
            view.Initialize();
            go.SetActive(false);
            return view;
        }

        private RhythmNoteView RentNote()
        {
            RhythmNoteView view = pool.Count > 0 ? pool.Dequeue() : null;
            if (view == null && allowPoolExpansion) view = CreatePooledNote();
            if (view != null) view.gameObject.SetActive(true);
            return view;
        }

        private void ReturnToPool(RhythmNoteView view)
        {
            if (view == null) return;
            view.gameObject.SetActive(false);
            view.transform.SetParent(noteLayer, false);
            pool.Enqueue(view);
        }

        private void SpawnDueNotes(float beat)
        {
            foreach (RuntimeNote note in runtimeNotes)
            {
                if (note.spawned || beat < note.data.beat - travelBeats) continue;
                note.spawned = true;
                if (note.data.type == RhythmNoteType.Ghost) continue;
                note.view = RentNote();
                if (note.view != null) ConfigureView(note);
            }
        }

        private void ConfigureView(RuntimeNote note)
        {
            bool player = note.data.actor == RhythmActor.Player;
            float width = layout.singleNoteWidth;
            if (note.data.type == RhythmNoteType.Long || note.data.type == RhythmNoteType.AttackState)
                width = Mathf.Max(layout.minimumLongWidth, TrackTravelDistance * note.data.durationBeats / Mathf.Max(0.1f, travelBeats));
            Color color = note.data.type == RhythmNoteType.AttackState ? colors.attackState :
                note.data.actor == RhythmActor.Player ? colors.playerNote : note.data.actor == RhythmActor.Enemy ? colors.enemyNote : colors.bossNote;
            note.visualWidth = width;
            note.view.Configure(width, layout.noteHeight, player ? 1f : 0f, color);
        }

        private float TrackTravelDistance => Mathf.Max(1f, Mathf.Abs(PlayerJudgeX - PlayerSpawnX));
        private float PlayerJudgeX => playerJudgementLine != null ? playerJudgementLine.anchoredPosition.x : layout.playerTargetX;
        private float EnemyJudgeX => opponentJudgementLine != null ? opponentJudgementLine.anchoredPosition.x : layout.opponentTargetX;
        private float PlayerLateBadX => PlayerJudgeX + TrackTravelDistance * SecondsToBeats(judgement.badSeconds) / Mathf.Max(0.1f, travelBeats);
        private float PlayerSpawnX => -layout.referenceResolution.x * 0.5f - layout.offscreenSpawnPadding;
        private float EnemySpawnX => layout.referenceResolution.x * 0.5f + layout.offscreenSpawnPadding;

        private void UpdateNotes(float beat)
        {
            float badBeats = SecondsToBeats(judgement.badSeconds);
            foreach (RuntimeNote note in runtimeNotes)
            {
                if (!note.spawned) continue;
                if (note.view != null)
                {
                    bool player = note.data.actor == RhythmActor.Player;
                    float progress = (beat - (note.data.beat - travelBeats)) / travelBeats;
                    float x = Mathf.LerpUnclamped(player ? PlayerSpawnX : EnemySpawnX, player ? PlayerJudgeX : EnemyJudgeX, progress);
                    bool automaticLong = note.data.type == RhythmNoteType.Long && !player && beat >= note.data.beat;
                    bool shrinkingLong = note.data.type == RhythmNoteType.Long &&
                        (note.holding || note.missedLongStart || automaticLong) && note.data.durationBeats > 0f;
                    if (shrinkingLong)
                    {
                        float shrinkStartBeat = note.missedLongStart ? note.data.beat + badBeats : note.data.beat;
                        float remaining = Mathf.Clamp01(1f - (beat - shrinkStartBeat) / note.data.durationBeats);
                        float anchorX = note.holding ? PlayerJudgeX :
                            note.missedLongStart ? PlayerLateBadX : player ? PlayerJudgeX : EnemyJudgeX;
                        note.view.SetWidth(note.visualWidth * remaining);
                        note.view.SetPosition(anchorX, 0f);
                    }
                    else
                    {
                        note.view.SetWidth(note.visualWidth);
                        note.view.SetPosition(x, 0f);
                    }
                }

                if (note.data.actor != RhythmActor.Player) UpdateAutomaticNote(note, beat);
                else if (note.data.type == RhythmNoteType.AttackState) UpdateAttackState(note, beat);
                else if (note.data.type == RhythmNoteType.Single && !note.resolved && beat > note.data.beat + badBeats)
                {
                    note.resolved = true;
                    StartMusicFromPlayerJudgement();
                    ShowJudgement(RhythmJudgement.Miss, BeatToSeconds(beat - note.data.beat));
                    ReleaseNoteView(note);
                }
                else if (note.data.type == RhythmNoteType.Long && !note.resolved && !note.holding &&
                    !note.missedLongStart && beat > note.data.beat + badBeats)
                {
                    note.missedLongStart = true;
                    StartMusicFromPlayerJudgement();
                    ShowJudgement(RhythmJudgement.Miss, BeatToSeconds(beat - note.data.beat));
                }
                else if (note.data.type == RhythmNoteType.Long && note.missedLongStart && !note.holding &&
                    beat >= note.data.beat + badBeats + note.data.durationBeats)
                {
                    note.resolved = true;
                    ReleaseNoteView(note);
                }
                else if (note.holding && beat > note.data.beat + note.data.durationBeats + badBeats)
                {
                    note.holding = false; note.resolved = true; onChargeEnd.Invoke(note.data);
                    StartMusicFromPlayerJudgement();
                    ShowJudgement(RhythmJudgement.Miss, BeatToSeconds(beat - note.data.beat - note.data.durationBeats));
                    ReleaseNoteView(note);
                }

                float visualEndBeat = note.data.beat;
                if (note.data.type == RhythmNoteType.Long || note.data.type == RhythmNoteType.AttackState)
                    visualEndBeat += Mathf.Max(0f, note.data.durationBeats);
                bool playerInputNote = note.data.actor == RhythmActor.Player &&
                    (note.data.type == RhythmNoteType.Single || note.data.type == RhythmNoteType.Long);
                bool shouldRelease = playerInputNote ? note.resolved : beat >= visualEndBeat;
                if (returnNoteToPoolAtTarget && shouldRelease)
                {
                    ReleaseNoteView(note);
                }
            }
        }

        private void UpdateAutomaticNote(RuntimeNote note, float beat)
        {
            if (!note.autoStarted && beat >= note.data.beat)
            {
                note.autoStarted = true;
                if (note.data.type == RhythmNoteType.Ghost) onGhostAction.Invoke(note.data);
                else if (note.data.actor == RhythmActor.Boss) onBossAction.Invoke(note.data);
                else onEnemyAction.Invoke(note.data);
                if (note.data.type != RhythmNoteType.Long) note.resolved = true;
            }
            if (note.data.type == RhythmNoteType.Long && note.autoStarted && !note.resolved && beat >= note.data.beat + note.data.durationBeats)
            {
                note.resolved = true;
                if (note.data.actor == RhythmActor.Boss) onBossLongEnd.Invoke(note.data); else onEnemyLongEnd.Invoke(note.data);
            }
        }

        private void UpdateAttackState(RuntimeNote note, float beat)
        {
            if (!note.autoStarted && beat >= note.data.beat)
            {
                note.autoStarted = true; activeAttackStates++; onAttackStateStart.Invoke(note.data);
            }
            if (note.autoStarted && !note.resolved && beat >= note.data.beat + note.data.durationBeats)
            {
                note.resolved = true; activeAttackStates = Mathf.Max(0, activeAttackStates - 1); onAttackStateEnd.Invoke(note.data);
            }
        }

        private void HandleInput(float beat)
        {
            if (Input.GetKeyDown(singleNoteKey))
            {
                if (IsAttackStateActive && enableFreeInputDuringAttackState) onFreeSingleInput.Invoke(); else JudgeSingleInput(beat);
            }
            if (Input.GetKeyDown(longNoteKey))
            {
                if (IsAttackStateActive && enableFreeInputDuringAttackState) onFreeLongInput.Invoke(); else StartLongInput(beat);
            }
            if (Input.GetKeyUp(longNoteKey)) EndLongInput(beat);
        }

        private void JudgeSingleInput(float beat)
        {
            RuntimeNote note = FindClosestPlayerNote(RhythmNoteType.Single, beat);
            if (note == null) { ShowJudgement(RhythmJudgement.Miss, 0f); return; }
            float delta = BeatToSeconds(beat - note.data.beat);
            RhythmJudgement rating = Rate(delta);
            note.resolved = true;
            StartMusicFromPlayerJudgement();
            onPlayerSingle.Invoke(note.data);
            ShowJudgement(rating, delta);
            ReleaseNoteView(note);
        }

        private void StartLongInput(float beat)
        {
            RuntimeNote note = FindActivePlayerLongNote(beat);
            if (note == null) { ShowJudgement(RhythmJudgement.Miss, 0f); return; }
            float delta = BeatToSeconds(beat - note.data.beat);
            note.startJudgement = Rate(delta);
            if (note.startJudgement == RhythmJudgement.Miss &&
                beat <= note.data.beat + note.data.durationBeats + SecondsToBeats(judgement.badSeconds))
                note.startJudgement = RhythmJudgement.Bad;
            note.holding = true;
            StartMusicFromPlayerJudgement();
            onChargeStart.Invoke(note.data);
            ShowJudgement(note.startJudgement, delta);
        }

        private void EndLongInput(float beat)
        {
            RuntimeNote note = runtimeNotes.Find(item => item.holding);
            if (note == null) return;
            note.holding = false; note.resolved = true;
            float delta = BeatToSeconds(beat - note.data.beat - note.data.durationBeats);
            RhythmJudgement endRating = Rate(delta);
            onChargeEnd.Invoke(note.data); ShowJudgement(endRating, delta);
            ReleaseNoteView(note);
        }

        private void ReleaseNoteView(RuntimeNote note)
        {
            if (note == null || note.view == null) return;
            ReturnToPool(note.view);
            note.view = null;
        }

        private void StartMusicFromPlayerJudgement()
        {
            if (musicStarted || !startMusicOnFirstPlayerJudgement || musicSource == null || musicSource.clip == null) return;
            musicReady = musicSource.clip.loadState == AudioDataLoadState.Loaded;
            if (!musicReady || musicPriming)
            {
                pendingMusicStart = true;
                if (musicSource.clip.loadState == AudioDataLoadState.Unloaded) musicSource.clip.LoadAudioData();
                return;
            }
            PlayPreparedMusic();
        }

        private void PlayPreparedMusic()
        {
            if (musicStarted || musicSource == null || musicSource.clip == null) return;
            pendingMusicStart = false;
            musicSource.Play();
            musicStarted = true;
        }

        private RuntimeNote FindClosestPlayerNote(RhythmNoteType type, float beat)
        {
            RuntimeNote best = null;
            float bestDistance = float.MaxValue;
            float maxDistance = SecondsToBeats(judgement.badSeconds);
            foreach (RuntimeNote note in runtimeNotes)
            {
                if (note.data.actor != RhythmActor.Player || note.data.type != type || note.resolved) continue;
                float distance = Mathf.Abs(beat - note.data.beat);
                if (distance <= maxDistance && distance < bestDistance) { best = note; bestDistance = distance; }
            }
            return best;
        }

        private RuntimeNote FindActivePlayerLongNote(float beat)
        {
            float earlyWindow = SecondsToBeats(judgement.badSeconds);
            RuntimeNote best = null;
            float bestEndBeat = float.MaxValue;

            foreach (RuntimeNote note in runtimeNotes)
            {
                if (note.data.actor != RhythmActor.Player || note.data.type != RhythmNoteType.Long ||
                    note.resolved || note.holding)
                    continue;

                float startBeat = note.data.beat;
                float endBeat = startBeat + note.data.durationBeats + earlyWindow;
                if (beat < startBeat - earlyWindow || beat > endBeat || endBeat >= bestEndBeat) continue;
                if (endBeat < bestEndBeat)
                {
                    best = note;
                    bestEndBeat = endBeat;
                }
            }

            return best;
        }

        private RhythmJudgement Rate(float deltaSeconds)
        {
            float value = Mathf.Abs(deltaSeconds);
            if (value <= judgement.criticalSeconds) return RhythmJudgement.Critical;
            if (value <= judgement.coolSeconds) return RhythmJudgement.Cool;
            if (value <= judgement.goodSeconds) return RhythmJudgement.Good;
            if (value <= judgement.badSeconds) return RhythmJudgement.Bad;
            return RhythmJudgement.Miss;
        }

        private void ShowJudgement(RhythmJudgement rating, float deltaSeconds)
        {
            onJudged.Invoke(rating, deltaSeconds);
            if (logJudgements) Debug.Log("[Rhythm] " + rating.ToString().ToUpperInvariant() + " (" + deltaSeconds.ToString("+0.000;-0.000;0.000") + "s)", this);
            if (judgementText == null) return;
            AssignRuntimeFont();
            judgementText.fontSize = judgementFontSize;
            judgementText.text = rating == RhythmJudgement.Critical ? "CRITICAL!" : rating.ToString().ToUpperInvariant();
            judgementText.color = GetJudgementColor(rating);
            judgementText.gameObject.SetActive(true);
            hideJudgementAt = Time.unscaledTime + judgementTextSeconds;
        }

        private Color GetJudgementColor(RhythmJudgement rating)
        {
            switch (rating)
            {
                case RhythmJudgement.Critical: return colors.critical;
                case RhythmJudgement.Cool: return colors.cool;
                case RhythmJudgement.Good: return colors.good;
                case RhythmJudgement.Bad: return colors.bad;
                default: return colors.miss;
            }
        }

        private float SecondsToBeats(float seconds) => seconds * Mathf.Max(1f, bpm) / 60f;
        private float BeatToSeconds(float beats) => beats * 60f / Mathf.Max(1f, bpm);
    }

    [DisallowMultipleComponent]
    public sealed class RhythmNoteView : MonoBehaviour
    {
        private RectTransform rect;
        private Image image;
        public void Initialize() { rect = GetComponent<RectTransform>(); image = GetComponent<Image>(); image.raycastTarget = false; }
        public void Configure(float width, float height, float pivotX, Color color)
        {
            if (rect == null || image == null) Initialize();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.pivot = new Vector2(pivotX, 0.5f);
            rect.sizeDelta = new Vector2(width, height); image.color = color;
        }
        public void SetPosition(float x, float y)
        {
            if (rect == null) rect = GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(x, y);
        }

        public void SetWidth(float width)
        {
            if (rect == null) rect = GetComponent<RectTransform>();
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(0f, width));
        }
    }
}
