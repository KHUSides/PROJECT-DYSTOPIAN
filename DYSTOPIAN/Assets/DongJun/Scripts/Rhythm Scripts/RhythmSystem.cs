using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Dystopian.Rhythm
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class RhythmSystem : MonoBehaviour
    {
        [Header("Song / Clock")]
        [SerializeField] private RhythmMusicStartMode musicStartMode = RhythmMusicStartMode.FirstPlayerJudgement;
        [SerializeField, Min(1f)] private float bpm = 120f;
        [SerializeField] private float firstBeatOffsetSeconds;
        [SerializeField, Min(0f)] private float startDelaySeconds = 0.5f;
        [SerializeField] private bool autoStartDelayFromTravelBeats = true;
        [SerializeField] private bool useDspScheduling = true;
        [SerializeField] private bool useUnscaledClockWithoutMusic = true;
        [SerializeField, Min(0.01f)] private float minimumDspLeadSeconds = 0.1f;
        [SerializeField] private bool primeMusicSourceOnLoad = true;

        [Header("BMS Chart Sources")]
        [SerializeField] private TextAsset playerBms;
        [SerializeField] private TextAsset opponentBms;
        [SerializeField] private RhythmActor opponentActor = RhythmActor.Enemy;
        [SerializeField, Min(1f)] private float beatsPerMeasure = 4f;
        [SerializeField] private bool readBpmFromPlayerBms = true;

        [Header("Input")]
        [SerializeField] private bool waitForStartInput = true;
        [SerializeField] private bool toggleChartWithStartKey = true;
        [SerializeField] private KeyCode startChartKey = KeyCode.T;
        [SerializeField] private KeyCode singleNoteKey = KeyCode.A;
        [SerializeField] private KeyCode longNoteKey = KeyCode.D;
        [SerializeField] private bool enableFreeInputDuringAttackState = true;

        [Header("Visibility")]
        [SerializeField] private bool hideUiOnStart = true;
        [SerializeField] private KeyCode toggleUiKey = KeyCode.R;

        [Header("Timing")]
        [SerializeField, Min(0.1f)] private float travelBeats = 4f;
        [SerializeField] private RhythmJudgementWindows judgement = new RhythmJudgementWindows();

        [Header("Pooling")]
        [SerializeField, Min(1)] private int initialPoolSize = 24;

        [Header("Presentation")]
        [SerializeField] private RhythmLayout layout = new RhythmLayout();
        [SerializeField] private RhythmColors colors = new RhythmColors();
        [SerializeField] private bool logJudgements;

        [Header("Combo")]
        [SerializeField] private bool showCombo = true;
        [SerializeField] private bool hideComboAtZero;
        [SerializeField] private bool resetComboOnBadOrMiss = true;
        [SerializeField] private string comboFormat = "{0}";

        [Header("Player Events")]
        [SerializeField] private UnityEvent onChartStarted = new UnityEvent();
        [SerializeField] private UnityEvent onChartStopped = new UnityEvent();
        [SerializeField] private NoteUnityEvent onPlayerSingle = new NoteUnityEvent();
        [SerializeField] private NoteUnityEvent onChargeStart = new NoteUnityEvent();
        [SerializeField] private NoteUnityEvent onChargeEnd = new NoteUnityEvent();
        [SerializeField] private NoteUnityEvent onAttackStateStart = new NoteUnityEvent();
        [SerializeField] private NoteUnityEvent onAttackStateEnd = new NoteUnityEvent();
        [SerializeField] private UnityEvent onFreeSingleInput = new UnityEvent();
        [SerializeField] private UnityEvent onFreeLongInput = new UnityEvent();
        [SerializeField] private JudgementUnityEvent onJudged = new JudgementUnityEvent();
        [SerializeField] private IntUnityEvent onComboChanged = new IntUnityEvent();

        [Header("Enemy / Boss Events")]
        [SerializeField] private NoteUnityEvent onEnemyAction = new NoteUnityEvent();
        [SerializeField] private NoteUnityEvent onEnemyLongEnd = new NoteUnityEvent();
        [SerializeField] private NoteUnityEvent onBossAction = new NoteUnityEvent();
        [SerializeField] private NoteUnityEvent onBossLongEnd = new NoteUnityEvent();
        [SerializeField] private NoteUnityEvent onGhostAction = new NoteUnityEvent();

        private readonly List<RhythmChartNote> chart = new List<RhythmChartNote>();
        private readonly List<RuntimeNote> runtimeNotes = new List<RuntimeNote>();
        private readonly List<RuntimeNote> activeNotes = new List<RuntimeNote>();
        private readonly Queue<RhythmNoteView> notePool = new Queue<RhythmNoteView>();

        private AudioSource musicSource;
        private Transform rhythmUiRoot;
        private CanvasScaler canvasScaler;
        private RectTransform noteLayer;
        private RectTransform playerJudgementLine;
        private RectTransform opponentJudgementLine;
        private TextMeshProUGUI comboText;

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
        private int combo;
        private int nextSpawnIndex;

        public event Action PlayerSingleAccepted;
        public event Action PlayerLongStarted;
        public event Action PlayerLongEnded;
        public event Action ChartStopped;
        public event Action<RhythmJudgement, float> JudgementPerformed;

        private sealed class RuntimeNote
        {
            public readonly RhythmChartNote data_;
            public RhythmNoteView view_;
            public bool resolved_;
            public bool holding_;
            public bool started_;
            public bool missedLongStart_;
            public float visualWidth_;

            public RuntimeNote(RhythmChartNote data)
            {
                data_ = data;
            }
        }

        public float CurrentSongSeconds
        {
            get
            {
                if (!clockRunning)
                {
                    return -EffectiveStartDelaySeconds;
                }

                if (useDspScheduling && dspClockActive)
                {
                    return (float)(AudioSettings.dspTime - dspSongStartTime);
                }

                float currentTime = useUnscaledClockWithoutMusic ? Time.unscaledTime : Time.time;
                return currentTime - clockStartedAt - EffectiveStartDelaySeconds;
            }
        }

        public float CurrentBeat => (CurrentSongSeconds - firstBeatOffsetSeconds) * Bpm / 60f;
        public bool IsChartRunning => clockRunning;
        public bool IsChartActive => clockRunning || pendingChartStart;
        public bool IsAttackStateActive => activeAttackStates > 0;
        public int CurrentCombo => combo;
        public float Bpm => Mathf.Max(1f, bpm);

        private AudioClip MusicClip => musicSource != null ? musicSource.clip : null;
        private float EffectiveStartDelaySeconds => autoStartDelayFromTravelBeats
            ? travelBeats * 60f / Bpm
            : startDelaySeconds;

        private void OnValidate()
        {
            bpm = Mathf.Max(1f, bpm);
            travelBeats = Mathf.Max(0.1f, travelBeats);
            initialPoolSize = Mathf.Max(1, initialPoolSize);
            beatsPerMeasure = Mathf.Max(1f, beatsPerMeasure);
            judgement.ClampValues();
        }

        private void Awake()
        {
            musicSource = GetComponent<AudioSource>();

            if (MusicClip == null)
            {
                return;
            }

            if (MusicClip.loadState == AudioDataLoadState.Unloaded)
            {
                MusicClip.LoadAudioData();
            }
            musicReady = MusicClip.loadState == AudioDataLoadState.Loaded;
        }

        private void Start()
        {
            if (!CacheSceneReferences())
            {
                enabled = false;
                return;
            }

            LoadBmsChart();
            WarmNotePool();
            RestartSong();

            if (hideUiOnStart)
            {
                SetUiVisible(false);
            }

            if (MusicClip != null)
            {
                StartCoroutine(PrepareMusicForPlaybackCrtn());
            }
        }

        // Uses one beat snapshot per frame so spawning, movement, and input stay synchronized.
        private void Update()
        {
            if (Input.GetKeyDown(toggleUiKey))
            {
                ToggleUiVisibility();
            }

            if (pendingMusicStart && musicReady && !musicPriming)
            {
                PlayPreparedMusic();
            }

            if (pendingChartStart && musicReady && !musicPriming)
            {
                pendingChartStart = false;
                StartChart();
            }

            if (waitForStartInput && Input.GetKeyDown(startChartKey))
            {
                if (toggleChartWithStartKey && (clockRunning || pendingChartStart))
                {
                    StopChart();
                }
                else
                {
                    StartChart();
                }
                return;
            }

            if (!clockRunning)
            {
                return;
            }

            float beat = CurrentBeat;
            SpawnDueNotes(beat);
            UpdateActiveNotes(beat);
            HandleRhythmInput(beat);
        }

        [ContextMenu("Restart Song")]
        public void RestartSong()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            ResetChartRuntime();
            if (!waitForStartInput)
            {
                StartChart();
            }
        }

        [ContextMenu("Start Chart")]
        public void StartChart()
        {
            if (!Application.isPlaying || clockRunning || pendingChartStart)
            {
                return;
            }

            if (useDspScheduling && MusicClip != null && (!musicReady || musicPriming))
            {
                pendingChartStart = true;
                if (MusicClip.loadState == AudioDataLoadState.Unloaded)
                {
                    MusicClip.LoadAudioData();
                }
                return;
            }

            clockStartedAt = useUnscaledClockWithoutMusic ? Time.unscaledTime : Time.time;
            if (useDspScheduling)
            {
                double leadSeconds = Math.Max(EffectiveStartDelaySeconds, minimumDspLeadSeconds);
                dspSongStartTime = AudioSettings.dspTime + leadSeconds;
                dspClockActive = true;
            }

            clockRunning = true;
            if (musicStartMode == RhythmMusicStartMode.ChartStart)
            {
                StartMusicWithChart();
            }
            onChartStarted.Invoke();
        }

        [ContextMenu("Stop Chart")]
        public void StopChart()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            bool wasActive = clockRunning || pendingChartStart;
            ResetChartRuntime();
            if (wasActive)
            {
                onChartStopped.Invoke();
                ChartStopped?.Invoke();
            }
        }

        private void ToggleUiVisibility()
        {
            SetUiVisible(rhythmUiRoot != null && !rhythmUiRoot.gameObject.activeSelf);
        }

        private void SetUiVisible(bool visible)
        {
            if (rhythmUiRoot != null)
            {
                rhythmUiRoot.gameObject.SetActive(visible);
            }
        }

        public void SetChartActive(bool active)
        {
            if (active)
            {
                StartChart();
            }
            else
            {
                StopChart();
            }
        }

        public void SetChart(IEnumerable<RhythmChartNote> notes)
        {
            chart.Clear();
            if (notes != null)
            {
                chart.AddRange(notes);
                chart.Sort((left, right) => left.Beat.CompareTo(right.Beat));
            }

            if (Application.isPlaying)
            {
                RestartSong();
            }
        }

        [ContextMenu("Load Player + Opponent BMS")]
        public void LoadBmsChart()
        {
            if (playerBms == null || opponentBms == null)
            {
                Debug.LogWarning("[Rhythm] Assign both Player BMS and Opponent BMS.", this);
                return;
            }

            RhythmActor parsedOpponent = opponentActor == RhythmActor.Player
                ? RhythmActor.Enemy
                : opponentActor;
            ParsedBmsChart parsedChart = BmsChartParser.Parse(
                playerBms.text,
                opponentBms.text,
                parsedOpponent,
                beatsPerMeasure);

            chart.Clear();
            chart.AddRange(parsedChart.Notes);
            if (readBpmFromPlayerBms && parsedChart.Bpm.HasValue)
            {
                bpm = Mathf.Max(1f, parsedChart.Bpm.Value);
            }
        }

        public void SetCombo(int value)
        {
            combo = Mathf.Max(0, value);
            UpdateComboText();
            onComboChanged.Invoke(combo);
        }

        public void ResetCombo()
        {
            SetCombo(0);
        }

        private void ResetChartRuntime()
        {
            foreach (RuntimeNote note in activeNotes)
            {
                DetachNoteView(note);
            }

            runtimeNotes.Clear();
            activeNotes.Clear();
            nextSpawnIndex = 0;
            activeAttackStates = 0;
            SetCombo(0);
            musicStarted = false;
            pendingMusicStart = false;
            pendingChartStart = false;
            dspClockActive = false;
            clockRunning = false;

            foreach (RhythmChartNote note in chart)
            {
                runtimeNotes.Add(new RuntimeNote(note));
            }

            musicSource.Stop();
        }

        private IEnumerator PrepareMusicForPlaybackCrtn()
        {
            AudioClip clip = MusicClip;
            if (clip == null)
            {
                yield break;
            }

            if (clip.loadState == AudioDataLoadState.Unloaded)
            {
                clip.LoadAudioData();
            }

            while (clip.loadState == AudioDataLoadState.Loading)
            {
                yield return null;
            }

            if (clip.loadState != AudioDataLoadState.Loaded)
            {
                Debug.LogWarning("[Rhythm] Music preload failed: " + clip.name, this);
                yield break;
            }

            musicReady = true;
            if (primeMusicSourceOnLoad)
            {
                musicPriming = true;
                bool wasMuted = musicSource.mute;
                musicSource.mute = true;
                musicSource.Play();
                yield return null;
                musicSource.Stop();
                musicSource.mute = wasMuted;
                musicPriming = false;
            }

            if (pendingChartStart)
            {
                pendingChartStart = false;
                StartChart();
            }
            else if (pendingMusicStart)
            {
                PlayPreparedMusic();
            }
        }

        // Scene-owned UI is discovered once and never overwritten at runtime.
        private bool CacheSceneReferences()
        {
            rhythmUiRoot = transform.Find("Rhythm UI");
            Transform panel = rhythmUiRoot != null ? rhythmUiRoot.Find("Track Panel") : null;

            canvasScaler = rhythmUiRoot != null ? rhythmUiRoot.GetComponent<CanvasScaler>() : null;
            noteLayer = panel != null ? panel.Find("Pooled Notes") as RectTransform : null;
            playerJudgementLine = panel != null ? panel.Find("Player Judgement Line") as RectTransform : null;
            opponentJudgementLine = panel != null ? panel.Find("Opponent Judgement Line") as RectTransform : null;
            comboText = panel != null
                ? panel.Find("Combo Text")?.GetComponent<TextMeshProUGUI>()
                : null;

            bool hasRequiredReferences = noteLayer != null &&
                playerJudgementLine != null &&
                opponentJudgementLine != null;
            if (!hasRequiredReferences)
            {
                Debug.LogError("[Rhythm] Rhythm UI hierarchy is incomplete.", this);
            }

            UpdateComboText();
            return hasRequiredReferences;
        }

        private void WarmNotePool()
        {
            while (notePool.Count < initialPoolSize)
            {
                notePool.Enqueue(CreateNoteView());
            }
        }

        private RhythmNoteView CreateNoteView()
        {
            GameObject noteObject = new GameObject(
                "Pooled Note",
                typeof(RectTransform),
                typeof(Image),
                typeof(RhythmNoteView));
            noteObject.transform.SetParent(noteLayer, false);
            noteObject.SetActive(false);
            return noteObject.GetComponent<RhythmNoteView>();
        }

        private RhythmNoteView AcquireNoteView()
        {
            RhythmNoteView view = notePool.Count > 0 ? notePool.Dequeue() : CreateNoteView();
            view.gameObject.SetActive(true);
            return view;
        }

        private void ReturnNoteView(RhythmNoteView view)
        {
            view.gameObject.SetActive(false);
            view.transform.SetParent(noteLayer, false);
            notePool.Enqueue(view);
        }

        private void SpawnDueNotes(float beat)
        {
            while (nextSpawnIndex < runtimeNotes.Count)
            {
                RuntimeNote note = runtimeNotes[nextSpawnIndex];
                if (beat < note.data_.Beat - travelBeats)
                {
                    break;
                }

                nextSpawnIndex++;
                activeNotes.Add(note);
                if (note.data_.Type == RhythmNoteType.Ghost)
                {
                    continue;
                }

                note.view_ = AcquireNoteView();
                ConfigureNoteView(note);
            }
        }

        private void ConfigureNoteView(RuntimeNote note)
        {
            bool isPlayer = note.data_.Actor == RhythmActor.Player;
            float width = layout.SingleNoteWidth;
            if (note.data_.Type == RhythmNoteType.Long || note.data_.Type == RhythmNoteType.AttackState)
            {
                width = Mathf.Max(
                    layout.MinimumLongWidth,
                    TrackTravelDistance * note.data_.DurationBeats / travelBeats);
            }

            Color noteColor = GetNoteColor(note.data_);
            note.visualWidth_ = width;
            note.view_.Configure(width, layout.NoteHeight, isPlayer ? 1f : 0f, noteColor);
        }

        private Color GetNoteColor(RhythmChartNote note)
        {
            if (note.Type == RhythmNoteType.AttackState)
            {
                return colors.AttackState;
            }

            switch (note.Actor)
            {
                case RhythmActor.Player:
                    return colors.PlayerNote;
                case RhythmActor.Enemy:
                    return colors.EnemyNote;
                default:
                    return colors.BossNote;
            }
        }

        // Advances active notes and resolves all automatic or timed-out states.
        private void UpdateActiveNotes(float beat)
        {
            float badBeats = SecondsToBeats(judgement.BadSeconds);
            for (int index = activeNotes.Count - 1; index >= 0; index--)
            {
                RuntimeNote note = activeNotes[index];
                UpdateNoteView(note, beat, badBeats);

                if (note.data_.Actor == RhythmActor.Player)
                {
                    UpdatePlayerNote(note, beat, badBeats);
                }
                else
                {
                    UpdateAutomaticNote(note, beat);
                }

                ReleaseFinishedView(note, beat);
                if (note.resolved_ && note.view_ == null)
                {
                    activeNotes.RemoveAt(index);
                }
            }
        }

        private void UpdateNoteView(RuntimeNote note, float beat, float badBeats)
        {
            if (note.view_ == null)
            {
                return;
            }

            bool isPlayer = note.data_.Actor == RhythmActor.Player;
            float progress = (beat - (note.data_.Beat - travelBeats)) / travelBeats;
            float movingX = Mathf.LerpUnclamped(
                isPlayer ? PlayerSpawnX : EnemySpawnX,
                isPlayer ? PlayerJudgeX : EnemyJudgeX,
                progress);

            bool isAutomaticLong = note.data_.Type == RhythmNoteType.Long &&
                !isPlayer &&
                beat >= note.data_.Beat;
            bool isShrinkingLong = note.data_.Type == RhythmNoteType.Long &&
                note.data_.DurationBeats > 0f &&
                (note.holding_ || note.missedLongStart_ || isAutomaticLong);

            if (!isShrinkingLong)
            {
                note.view_.SetWidth(note.visualWidth_);
                note.view_.SetPosition(movingX, 0f);
                return;
            }

            float shrinkStartBeat = note.missedLongStart_
                ? note.data_.Beat + badBeats
                : note.data_.Beat;
            float remaining = Mathf.Clamp01(
                1f - (beat - shrinkStartBeat) / note.data_.DurationBeats);
            float anchorX = note.holding_
                ? PlayerJudgeX
                : note.missedLongStart_ ? PlayerLateBadX : EnemyJudgeX;

            note.view_.SetWidth(note.visualWidth_ * remaining);
            note.view_.SetPosition(anchorX, 0f);
        }

        private void UpdatePlayerNote(RuntimeNote note, float beat, float badBeats)
        {
            if (note.data_.Type == RhythmNoteType.AttackState)
            {
                UpdateAttackState(note, beat);
                return;
            }

            if (note.data_.Type == RhythmNoteType.Single &&
                !note.resolved_ &&
                beat > note.data_.Beat + badBeats)
            {
                note.resolved_ = true;
                TryStartMusicFromPlayerJudgement();
                ShowJudgement(RhythmJudgement.Miss, BeatToSeconds(beat - note.data_.Beat));
                DetachNoteView(note);
                return;
            }

            if (note.data_.Type != RhythmNoteType.Long)
            {
                return;
            }

            if (!note.resolved_ && !note.holding_ && !note.missedLongStart_ &&
                beat > note.data_.Beat + badBeats)
            {
                note.missedLongStart_ = true;
                TryStartMusicFromPlayerJudgement();
                ShowJudgement(RhythmJudgement.Miss, BeatToSeconds(beat - note.data_.Beat));
                return;
            }

            if (note.missedLongStart_ && !note.holding_ &&
                beat >= note.data_.Beat + badBeats + note.data_.DurationBeats)
            {
                note.resolved_ = true;
                DetachNoteView(note);
                return;
            }

            if (note.holding_ && beat > note.data_.Beat + note.data_.DurationBeats + badBeats)
            {
                note.holding_ = false;
                note.resolved_ = true;
                onChargeEnd.Invoke(note.data_);
                PlayerLongEnded?.Invoke();
                TryStartMusicFromPlayerJudgement();
                ShowJudgement(
                    RhythmJudgement.Miss,
                    BeatToSeconds(beat - note.data_.Beat - note.data_.DurationBeats));
                DetachNoteView(note);
            }
        }

        private void UpdateAutomaticNote(RuntimeNote note, float beat)
        {
            if (!note.started_ && beat >= note.data_.Beat)
            {
                note.started_ = true;
                if (note.data_.Type == RhythmNoteType.Ghost)
                {
                    onGhostAction.Invoke(note.data_);
                }
                else if (note.data_.Actor == RhythmActor.Boss)
                {
                    onBossAction.Invoke(note.data_);
                }
                else
                {
                    onEnemyAction.Invoke(note.data_);
                }

                if (note.data_.Type != RhythmNoteType.Long)
                {
                    note.resolved_ = true;
                }
            }

            if (note.data_.Type != RhythmNoteType.Long ||
                !note.started_ ||
                note.resolved_ ||
                beat < note.data_.Beat + note.data_.DurationBeats)
            {
                return;
            }

            note.resolved_ = true;
            if (note.data_.Actor == RhythmActor.Boss)
            {
                onBossLongEnd.Invoke(note.data_);
            }
            else
            {
                onEnemyLongEnd.Invoke(note.data_);
            }
        }

        private void UpdateAttackState(RuntimeNote note, float beat)
        {
            if (!note.started_ && beat >= note.data_.Beat)
            {
                note.started_ = true;
                activeAttackStates++;
                onAttackStateStart.Invoke(note.data_);
            }

            if (!note.started_ || note.resolved_ || beat < note.data_.Beat + note.data_.DurationBeats)
            {
                return;
            }

            note.resolved_ = true;
            activeAttackStates = Mathf.Max(0, activeAttackStates - 1);
            onAttackStateEnd.Invoke(note.data_);
        }

        private void ReleaseFinishedView(RuntimeNote note, float beat)
        {
            bool isPlayerInputNote = note.data_.Actor == RhythmActor.Player &&
                (note.data_.Type == RhythmNoteType.Single || note.data_.Type == RhythmNoteType.Long);
            float visualEndBeat = note.data_.Beat;
            if (note.data_.Type == RhythmNoteType.Long || note.data_.Type == RhythmNoteType.AttackState)
            {
                visualEndBeat += note.data_.DurationBeats;
            }

            if ((isPlayerInputNote && note.resolved_) || (!isPlayerInputNote && beat >= visualEndBeat))
            {
                DetachNoteView(note);
            }
        }

        private void HandleRhythmInput(float beat)
        {
            if (Input.GetKeyDown(singleNoteKey))
            {
                if (IsAttackStateActive && enableFreeInputDuringAttackState)
                {
                    onFreeSingleInput.Invoke();
                }
                else
                {
                    JudgeSingleInput(beat);
                }
            }

            if (Input.GetKeyDown(longNoteKey))
            {
                if (IsAttackStateActive && enableFreeInputDuringAttackState)
                {
                    onFreeLongInput.Invoke();
                }
                else
                {
                    StartLongInput(beat);
                }
            }

            if (Input.GetKeyUp(longNoteKey))
            {
                EndLongInput(beat);
            }
        }

        private void JudgeSingleInput(float beat)
        {
            RuntimeNote note = FindClosestPlayerNote(RhythmNoteType.Single, beat);
            if (note == null)
            {
                ShowJudgement(RhythmJudgement.Miss, 0f);
                return;
            }

            float deltaSeconds = BeatToSeconds(beat - note.data_.Beat);
            RhythmJudgement rating = EvaluateJudgement(deltaSeconds);
            note.resolved_ = true;
            TryStartMusicFromPlayerJudgement();
            onPlayerSingle.Invoke(note.data_);
            PlayerSingleAccepted?.Invoke();
            ShowJudgement(rating, deltaSeconds);
            DetachNoteView(note);
        }

        private void StartLongInput(float beat)
        {
            RuntimeNote note = FindActivePlayerLongNote(beat);
            if (note == null)
            {
                ShowJudgement(RhythmJudgement.Miss, 0f);
                return;
            }

            float deltaSeconds = BeatToSeconds(beat - note.data_.Beat);
            RhythmJudgement startRating = EvaluateJudgement(deltaSeconds);
            if (startRating == RhythmJudgement.Miss)
            {
                startRating = RhythmJudgement.Bad;
            }

            note.holding_ = true;
            TryStartMusicFromPlayerJudgement();
            onChargeStart.Invoke(note.data_);
            PlayerLongStarted?.Invoke();
            ShowJudgement(startRating, deltaSeconds);
        }

        private void EndLongInput(float beat)
        {
            RuntimeNote note = activeNotes.Find(item => item.holding_);
            if (note == null)
            {
                return;
            }

            note.holding_ = false;
            note.resolved_ = true;
            float deltaSeconds = BeatToSeconds(
                beat - note.data_.Beat - note.data_.DurationBeats);
            RhythmJudgement endRating = EvaluateJudgement(deltaSeconds);
            onChargeEnd.Invoke(note.data_);
            PlayerLongEnded?.Invoke();
            ShowJudgement(endRating, deltaSeconds);
            DetachNoteView(note);
        }

        private RuntimeNote FindClosestPlayerNote(RhythmNoteType noteType, float beat)
        {
            RuntimeNote closest = null;
            float closestDistance = float.MaxValue;
            float maximumDistance = SecondsToBeats(judgement.BadSeconds);

            foreach (RuntimeNote note in activeNotes)
            {
                if (note.data_.Actor != RhythmActor.Player ||
                    note.data_.Type != noteType ||
                    note.resolved_)
                {
                    continue;
                }

                float distance = Mathf.Abs(beat - note.data_.Beat);
                if (distance <= maximumDistance && distance < closestDistance)
                {
                    closest = note;
                    closestDistance = distance;
                }
            }
            return closest;
        }

        private RuntimeNote FindActivePlayerLongNote(float beat)
        {
            float earlyWindow = SecondsToBeats(judgement.BadSeconds);
            RuntimeNote earliest = null;
            float earliestEndBeat = float.MaxValue;

            foreach (RuntimeNote note in activeNotes)
            {
                if (note.data_.Actor != RhythmActor.Player ||
                    note.data_.Type != RhythmNoteType.Long ||
                    note.resolved_ ||
                    note.holding_)
                {
                    continue;
                }

                float endBeat = note.data_.Beat + note.data_.DurationBeats + earlyWindow;
                if (beat < note.data_.Beat - earlyWindow || beat > endBeat || endBeat >= earliestEndBeat)
                {
                    continue;
                }

                earliest = note;
                earliestEndBeat = endBeat;
            }
            return earliest;
        }

        private RhythmJudgement EvaluateJudgement(float deltaSeconds)
        {
            float absoluteDelta = Mathf.Abs(deltaSeconds);
            if (absoluteDelta <= judgement.CriticalSeconds)
            {
                return RhythmJudgement.Critical;
            }
            if (absoluteDelta <= judgement.CoolSeconds)
            {
                return RhythmJudgement.Cool;
            }
            if (absoluteDelta <= judgement.GoodSeconds)
            {
                return RhythmJudgement.Good;
            }
            if (absoluteDelta <= judgement.BadSeconds)
            {
                return RhythmJudgement.Bad;
            }
            return RhythmJudgement.Miss;
        }

        private void ShowJudgement(RhythmJudgement rating, float deltaSeconds)
        {
            UpdateCombo(rating);
            onJudged.Invoke(rating, deltaSeconds);
            JudgementPerformed?.Invoke(rating, deltaSeconds);

            if (logJudgements)
            {
                Debug.Log(
                    "[Rhythm] " + rating.ToString().ToUpperInvariant() +
                    " (" + deltaSeconds.ToString("+0.000;-0.000;0.000") + "s)",
                    this);
            }

        }

        private void UpdateCombo(RhythmJudgement rating)
        {
            if (rating <= RhythmJudgement.Good)
            {
                SetCombo(combo + 1);
            }
            else if (resetComboOnBadOrMiss)
            {
                SetCombo(0);
            }
        }

        private void UpdateComboText()
        {
            if (comboText == null)
            {
                return;
            }

            try
            {
                comboText.text = string.Format(CultureInfo.InvariantCulture, comboFormat, combo);
            }
            catch (FormatException)
            {
                comboText.text = combo.ToString(CultureInfo.InvariantCulture);
            }

            comboText.gameObject.SetActive(showCombo && (!hideComboAtZero || combo > 0));
        }

        public Color GetJudgementColor(RhythmJudgement rating)
        {
            switch (rating)
            {
                case RhythmJudgement.Critical:
                    return colors.Critical;
                case RhythmJudgement.Cool:
                    return colors.Cool;
                case RhythmJudgement.Good:
                    return colors.Good;
                case RhythmJudgement.Bad:
                    return colors.Bad;
                default:
                    return colors.Miss;
            }
        }

        private void DetachNoteView(RuntimeNote note)
        {
            if (note.view_ == null)
            {
                return;
            }

            ReturnNoteView(note.view_);
            note.view_ = null;
        }

        private void StartMusicWithChart()
        {
            if (MusicClip == null || musicStarted)
            {
                return;
            }

            musicSource.time = 0f;
            if (useDspScheduling)
            {
                musicSource.PlayScheduled(dspSongStartTime);
            }
            else
            {
                musicSource.PlayDelayed(EffectiveStartDelaySeconds);
            }
            musicStarted = true;
        }

        private void TryStartMusicFromPlayerJudgement()
        {
            if (musicStartMode != RhythmMusicStartMode.FirstPlayerJudgement ||
                musicStarted ||
                MusicClip == null)
            {
                return;
            }

            musicReady = MusicClip.loadState == AudioDataLoadState.Loaded;
            if (!musicReady || musicPriming)
            {
                pendingMusicStart = true;
                if (MusicClip.loadState == AudioDataLoadState.Unloaded)
                {
                    MusicClip.LoadAudioData();
                }
                return;
            }
            PlayPreparedMusic();
        }

        private void PlayPreparedMusic()
        {
            if (musicStarted || MusicClip == null)
            {
                return;
            }

            pendingMusicStart = false;
            musicSource.Play();
            musicStarted = true;
        }

        private float TrackTravelDistance => Mathf.Max(1f, Mathf.Abs(PlayerJudgeX - PlayerSpawnX));
        private float PlayerJudgeX => playerJudgementLine.anchoredPosition.x;
        private float EnemyJudgeX => opponentJudgementLine.anchoredPosition.x;
        private float PlayerLateBadX => PlayerJudgeX +
            TrackTravelDistance * SecondsToBeats(judgement.BadSeconds) / travelBeats;
        private float ReferenceWidth => canvasScaler != null
            ? canvasScaler.referenceResolution.x
            : 1920f;
        private float PlayerSpawnX => -ReferenceWidth * 0.5f - layout.OffscreenSpawnPadding;
        private float EnemySpawnX => ReferenceWidth * 0.5f + layout.OffscreenSpawnPadding;
        private float SecondsToBeats(float seconds) => seconds * Bpm / 60f;
        private float BeatToSeconds(float beats) => beats * 60f / Bpm;
    }
}
