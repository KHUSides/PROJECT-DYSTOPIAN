using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dystopian.FreeRhythm
{
    [DisallowMultipleComponent]
    public sealed class FreeRhythmSystem : MonoBehaviour
    {
        [Header("Constant tempo / 4 quarter beats / 8 eighth notes")]
        [SerializeField, Range(30, 300)] float bpm = 120;
        [SerializeField, Range(.01f, .15f)] float goodWindow = .09f;
        [SerializeField, Range(.005f, .08f)] float perfectWindow = .035f;
        [SerializeField, Range(-.25f, .25f)] float inputOffsetSeconds;
        [Header("Audio / visual sync")]
        [SerializeField] bool estimateAudioLatencyFromDspBuffer = true;
        [SerializeField, Range(-.25f, .5f)] float audioLatencyAdjustmentSeconds;
        [SerializeField] KeyCode firstKey = KeyCode.A;
        [SerializeField] KeyCode secondKey = KeyCode.D;
        [SerializeField] bool metronome = true;
        [SerializeField, Range(0, 1)] float clickVolume = .25f;
        [Tooltip("Optional constant-tempo music. BPM must match; starts at bar one.")]
        [SerializeField] AudioClip music;

        readonly SortedDictionary<long, BeatInput> pending = new SortedDictionary<long, BeatInput>();
        readonly long[] downTick = { -1, -1 };
        readonly BeatGrade[] holdGrades = new BeatGrade[2];
        readonly Queue<TimedKeyInput> queuedInputs = new Queue<TimedKeyInput>();
        InputAction firstInput, secondInput;
        AudioSource[] clicks;
        AudioSource musicSource;
        AudioClip[] clickClips;
        FreeRhythmEngine engine;
        double origin, pauseStart, estimatedAudioLatency;
        long nextFinalize, nextClick;
        long recentBar = -1;
        int held;
        bool running, paused, externalPause, focusPause;
        readonly BeatInput[] recent = new BeatInput[8];
        readonly bool[] hasRecent = new bool[8];
        public event Action<ActionResult> ActionRecognized;
        public event Action<BeatInput> BeatFinalized;
        public event Action<BeatInput> InputAccepted;
        public event Action SequenceReset;
        public float Bpm => (float)(60 / (Interval * 2));
        public double Interval => engine == null ? 30.0 / bpm : engine.Interval;
        public double SongTime => RawSongTime - AudioLatencySeconds;
        public long CurrentTick => (long)Math.Floor(SongTime / Interval);
        public bool IsPaused => paused;
        public string Feedback { get; private set; } = "READY";
        public string LastAction { get; private set; } = "Choose your rhythm";
        public int Combo { get; private set; }
        public double Window => engine == null ? goodWindow : engine.GoodWindow;
        public double PerfectWindow => engine == null ? perfectWindow : engine.PerfectWindow;
        public double AudioLatencySeconds => Math.Max(0,
            (estimateAudioLatencyFromDspBuffer ? estimatedAudioLatency : 0) + audioLatencyAdjustmentSeconds);
        public BeatGrade CurrentGrade => engine == null ? BeatGrade.Miss : engine.Judge(SongTime, out _, out _);
        public BeatInput Recent(int i) => recent[i];
        public bool HasRecent(int i) => hasRecent[i];
        double RawSongTime => (paused ? pauseStart : AudioSettings.dspTime) - origin;

        private struct TimedKeyInput
        {
            public int Lane_;
            public bool Press_;
            public double Time_;
        }

        void OnEnable() { Restart(); }
        public void Restart()
        {
            StopAudio();
            UpdateEstimatedAudioLatency();
            engine = new FreeRhythmEngine(Mathf.Clamp(bpm, 30, 300), goodWindow, perfectWindow);
            engine.ActionRecognized += OnAction;
            pending.Clear(); held = 0; downTick[0] = downTick[1] = -1;
            queuedInputs.Clear();
            Array.Clear(hasRecent, 0, hasRecent.Length);
            recentBar = -1;
            // One complete bar of count-in. Input begins on bar one's downbeat.
            origin = AudioSettings.dspTime + 8 * Interval + .2;
            nextFinalize = 0; nextClick = -8; running = true; paused = false; externalPause = false;
            Combo = 0; Feedback = "COUNT IN"; LastAction = "Choose your rhythm";
            SequenceReset?.Invoke();
            BuildAudio();
            ConfigureInputs();
            if (music != null) { musicSource.clip = music; musicSource.loop = true; musicSource.PlayScheduled(origin); }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.R)) { Restart(); return; }
            // Cooperate with the project's global pause overlay (including its resume button).
            if (Time.timeScale == 0) { externalPause = true; SetPaused(true); return; }
            if (externalPause) { externalPause = false; SetPaused(false); }
            if (Input.GetKeyDown(KeyCode.Escape) && FindFirstObjectByType<PauseMenuController>() == null) SetPaused(!paused);
            if (!running || paused) return;
            double now = SongTime;
            if (now >= 0 && Feedback == "COUNT IN") Feedback = "READY";
            ScheduleClicks(RawSongTime);
            // Hardware timestamps arrive before Update; judge them before closing windows.
            ProcessQueuedInputs(AudioSettings.dspTime - Time.realtimeSinceStartupAsDouble);
            FinalizeThrough(now + inputOffsetSeconds);
        }

        void ConfigureInputs()
        {
            firstInput?.Dispose(); secondInput?.Dispose();
            firstInput = CreateInput(firstKey, 0);
            secondInput = CreateInput(secondKey, 1);
        }

        InputAction CreateInput(KeyCode key, int lane)
        {
            string control = key.ToString().ToLowerInvariant();
            if (control.StartsWith("alpha")) control = control.Substring(5);
            if (key == KeyCode.Return) control = "enter";
            var action = new InputAction(type: InputActionType.PassThrough, binding: "<Keyboard>/" + control);
            action.performed += context =>
            {
                if (running && !paused)
                    queuedInputs.Enqueue(new TimedKeyInput
                    { Lane_ = lane, Press_ = context.ReadValue<float>() > .5f, Time_ = context.time });
            };
            action.Enable();
            return action;
        }

        void ProcessQueuedInputs(double dspMinusRealtime)
        {
            while (queuedInputs.Count > 0)
            {
                TimedKeyInput input = queuedInputs.Dequeue();
                Submit(input.Lane_, input.Press_,
                    input.Time_ + dspMinusRealtime - origin - AudioLatencySeconds + inputOffsetSeconds);
            }
        }

        public void SetAudioLatencyAdjustment(float seconds)
        {
            audioLatencyAdjustmentSeconds = Mathf.Clamp(seconds, -.25f, .5f);
        }

        public void SetAutomaticAudioLatency(bool enabled)
        {
            estimateAudioLatencyFromDspBuffer = enabled;
            UpdateEstimatedAudioLatency();
        }

        void UpdateEstimatedAudioLatency()
        {
            AudioSettings.GetDSPBufferSize(out int bufferLength, out int bufferCount);
            estimatedAudioLatency = AudioSettings.outputSampleRate > 0
                ? (double)bufferLength * bufferCount / AudioSettings.outputSampleRate
                : 0;
        }

        public bool Submit(int lane, bool press, double songTime)
        {
            if (!running || paused || lane < 0 || lane > 1) return false;
            // Fill only elapsed slots before judging this input; never wait on its slot.
            FinalizeThrough(songTime);
            long tick; double error;
            BeatGrade grade = engine.Judge(songTime, out tick, out error);
            int mask = 1 << lane;
            if (!press)
            {
                held &= ~mask;
                long start = downTick[lane]; downTick[lane] = -1;
                // A quick tap's release is not a separate rhythm gesture.
                if (start < 0 || engine.IsTapRelease(start, songTime)) return true;
            }
            if (grade == BeatGrade.Miss || tick < nextFinalize)
            {
                // Count-in input has no effect on gameplay.
                if (songTime < -Window) return false;
                Feedback = "OFF BEAT";
                pending.Clear(); engine.Reset(); held = 0; downTick[0] = downTick[1] = -1; SequenceReset?.Invoke();
                return false;
            }
            BeatInput frame;
            if (!pending.TryGetValue(tick, out frame)) frame = new BeatInput { Tick = tick, Grade = BeatGrade.Perfect };
            if (press)
            {
                // One press per eighth slot. A second key cannot turn it into a chord.
                if (frame.Pressed != 0 || (held & mask) != 0) return false;
                frame.Pressed |= mask; held |= mask; downTick[lane] = tick;
                holdGrades[lane] = grade;
            }
            else frame.Released |= mask;
            frame.Grade = (BeatGrade)Math.Max((int)frame.Grade, (int)grade);
            SetHeldState(ref frame);
            pending[tick] = frame;
            Feedback = grade.ToString().ToUpperInvariant() + "  " + (error * 1000).ToString("+0;-0;0") + " ms";
            Record(frame);
            engine.Push(frame);
            InputAccepted?.Invoke(frame);
            return true;
        }

        void SetHeldState(ref BeatInput frame)
        {
            frame.Held = held;
            for (int lane = 0; lane < 2; lane++)
                if ((held & (1 << lane)) != 0)
                    frame.Grade = (BeatGrade)Math.Max((int)frame.Grade, (int)holdGrades[lane]);
        }

        void Record(BeatInput frame)
        {
            long bar = frame.Tick / 8;
            if (bar != recentBar) { Array.Clear(hasRecent, 0, hasRecent.Length); recentBar = bar; }
            int index = (int)(frame.Tick % 8);
            recent[index] = frame; hasRecent[index] = true;
        }

        void FinalizeThrough(double now)
        {
            // Recover from long stalls without replaying old attacks in a burst.
            if (now - nextFinalize * Interval > 8 * Interval)
            {
                pending.Clear(); engine.Reset(); held = 0; downTick[0] = downTick[1] = -1;
                nextFinalize = Math.Max(0, (long)Math.Floor((now - Window) / Interval));
                SequenceReset?.Invoke();
            }
            while (engine.IsWindowClosed(nextFinalize, now))
            {
                BeatInput frame;
                if (!pending.TryGetValue(nextFinalize, out frame))
                    frame = new BeatInput { Tick = nextFinalize, Grade = BeatGrade.Perfect };
                SetHeldState(ref frame);
                pending.Remove(nextFinalize);
                Record(frame);
                engine.Commit(frame); BeatFinalized?.Invoke(frame);
                nextFinalize++;
            }
        }

        void OnAction(ActionResult result)
        {
            LastAction = result.Action + (result.Critical ? "  / CRITICAL" : "  / GOOD");
            ActionRecognized?.Invoke(result);
        }
        public void RegisterDamage(int amount) { if (amount > 0) Combo++; }
        public void ResetCombo() { Combo = 0; }
        public void SetPaused(bool value)
        {
            if (paused == value) return;
            if (value)
            {
                pauseStart = AudioSettings.dspTime; paused = true;
                foreach (var source in clicks) source.Stop();
                musicSource.Pause();
                pending.Clear(); engine.Reset(); held = 0; downTick[0] = downTick[1] = -1;
                queuedInputs.Clear();
                SequenceReset?.Invoke(); Feedback = "PAUSED - ESC TO RESUME";
            }
            else
            {
                origin += AudioSettings.dspTime - pauseStart;
                paused = false;
                nextClick = (long)Math.Floor(RawSongTime / Interval) + 1;
                if (music != null)
                {
                    musicSource.Stop(); musicSource.clip = music;
                    if (RawSongTime < 0) musicSource.PlayScheduled(origin);
                    else { musicSource.time = (float)(RawSongTime % music.length); musicSource.Play(); }
                }
                Feedback = "RESUMED";
            }
        }
        void OnApplicationFocus(bool focus)
        {
            if (!running) return;
            if (!focus) { focusPause = !paused; SetPaused(true); }
            else if (focusPause) { focusPause = false; if (Time.timeScale > 0) SetPaused(false); }
        }
        void OnApplicationPause(bool value) { OnApplicationFocus(!value); }

        void BuildAudio()
        {
            if (clicks != null) return;
            clicks = new AudioSource[8]; clickClips = new AudioClip[3];
            for (int i = 0; i < 3; i++)
            {
                const int rate = 44100;
                var samples = new float[2205];
                for (int s = 0; s < samples.Length; s++)
                    samples[s] = (float)(Math.Sin(2 * Math.PI * (i == 0 ? 1400 : i == 1 ? 1000 : 700) * s / rate) * Math.Exp(-s / 300.0));
                clickClips[i] = AudioClip.Create("FreeRhythm_Click_" + i, samples.Length, 1, rate, false);
                clickClips[i].SetData(samples, 0);
            }
            for (int i = 0; i < clicks.Length; i++)
            { clicks[i] = gameObject.AddComponent<AudioSource>(); clicks[i].playOnAwake = false; clicks[i].spatialBlend = 0; }
            musicSource = gameObject.AddComponent<AudioSource>(); musicSource.playOnAwake = false;
        }
        void ScheduleClicks(double now)
        {
            if (nextClick * Interval < now - Interval) nextClick = (long)Math.Ceiling(now / Interval);
            while (nextClick * Interval < now + .15)
            {
                long tick = nextClick++;
                if (!metronome || origin + tick * Interval <= AudioSettings.dspTime) continue;
                var source = clicks[(int)((tick % 8 + 8) % 8)];
                source.clip = clickClips[tick % 8 == 0 ? 0 : tick % 2 == 0 ? 1 : 2];
                source.volume = clickVolume; source.PlayScheduled(origin + tick * Interval);
            }
        }
        void StopAudio()
        {
            if (clicks != null) foreach (var source in clicks) if (source != null) source.Stop();
            if (musicSource != null) musicSource.Stop();
        }
        void OnDisable()
        {
            running = false; firstInput?.Disable(); secondInput?.Disable(); queuedInputs.Clear();
            StopAudio(); SequenceReset?.Invoke();
        }
        void OnDestroy()
        {
            firstInput?.Dispose(); secondInput?.Dispose();
            if (clickClips != null) foreach (var clip in clickClips) Destroy(clip);
        }
    }
}
