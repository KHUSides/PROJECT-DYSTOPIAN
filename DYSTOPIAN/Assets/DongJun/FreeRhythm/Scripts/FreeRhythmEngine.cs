using System;
using System.Collections.Generic;

namespace Dystopian.FreeRhythm
{
    public enum BeatGrade { Perfect, Good, Miss }
    public enum RhythmAction { Swing = 0, DoubleSlash = 1, ChargedAttack = 2, GuardAttack = 3, HoldStrike = 5 }

    public struct BeatInput
    {
        public long Tick;
        public int Pressed, Held, Released;
        public BeatGrade Grade;
        public bool IsRest => (Pressed | Held | Released) == 0;
        public bool IsTap => Pressed == 1 || Pressed == 2;
    }

    public struct ActionResult
    {
        public RhythmAction Action;
        public long Tick;
        public bool Critical;
        public ActionResult(RhythmAction action, long tick, bool critical)
        { Action = action; Tick = tick; Critical = critical; }
    }

    // Pure timing and recognition: no dependency on the legacy chart or scene.
    public sealed class FreeRhythmEngine
    {
        public const int EighthsPerBar = 8;
        readonly List<BeatInput> history = new List<BeatInput>(8);
        long consumedThrough = -1, lastActionTick = -1;
        public event Action<ActionResult> ActionRecognized;
        public double Interval { get; }
        public double GoodWindow { get; }
        public double PerfectWindow { get; }
        public int HistoryCount => history.Count;

        public FreeRhythmEngine(double bpm, double goodWindow = .09, double perfectWindow = .035)
        {
            if (double.IsNaN(bpm) || double.IsInfinity(bpm) || bpm < 30 || bpm > 300)
                throw new ArgumentOutOfRangeException(nameof(bpm));
            Interval = 30.0 / bpm;
            GoodWindow = Math.Min(Math.Max(.001, goodWindow), Interval * .45);
            PerfectWindow = Math.Min(Math.Max(0, perfectWindow), GoodWindow);
        }

        public BeatGrade Judge(double time, out long tick, out double error)
        {
            // Each window straddles its nearest eighth-note timestamp.
            tick = (long)Math.Floor(time / Interval + .5);
            error = time - tick * Interval;
            if (tick < 0 || Math.Abs(error) > GoodWindow + 1e-9) return BeatGrade.Miss;
            return Math.Abs(error) <= PerfectWindow + 1e-9 ? BeatGrade.Perfect : BeatGrade.Good;
        }

        public bool IsWindowClosed(long tick, double time)
        {
            return time > tick * Interval + GoodWindow + 1e-9;
        }

        public bool IsTapRelease(long startTick, double time)
        {
            return time < (startTick + 1) * Interval - GoodWindow - 1e-9;
        }

        public void Reset() { history.Clear(); consumedThrough = lastActionTick = -1; }

        // Expired slots establish rests/holds, but never replay an input's action.
        public void Commit(BeatInput input) { Store(input); }

        bool Store(BeatInput input)
        {
            if (input.Grade == BeatGrade.Miss) { Reset(); return false; }
            if (input.Tick <= consumedThrough || input.Pressed == 3) return false;
            if (history.Count > 0)
            {
                long previousTick = history[history.Count - 1].Tick;
                if (input.Tick < previousTick) return false;
                if (input.Tick == previousTick) { history[history.Count - 1] = input; return true; }
                if (input.Tick != previousTick + 1) history.Clear();
            }
            history.Add(input);
            if (history.Count > 8) history.RemoveAt(0);
            return true;
        }

        public void Push(BeatInput input)
        {
            if (!Store(input) || input.Tick == lastActionTick) return;
            int n = history.Count;

            // A hold from an earlier tick can still be combined with a new tap.
            if (input.IsTap && (input.Held & ~input.Pressed) != 0)
            { Emit(RhythmAction.HoldStrike, 1); return; }

            // Hold for two quarter beats, then release on the fifth eighth slot.
            // No tap-rest-tap prefix: a prepared charge must not also cast a swing.
            if (n >= 5 && At(4).IsTap &&
                Hold(3, At(4).Pressed) && Hold(2, At(4).Pressed) && Hold(1, At(4).Pressed) &&
                input.Pressed == 0 && input.Released == At(4).Pressed)
            { Emit(RhythmAction.ChargedAttack, 5); return; }

            // Half-beat hold, release, then a tap. Either lane can start/finish.
            if (n >= 3 && At(2).IsTap && At(1).Pressed == 0 &&
                At(1).Released == At(2).Pressed && input.IsTap)
            { Emit(RhythmAction.GuardAttack, 3); return; }

            // First hit at the third eighth; second hit requires the fourth input.
            if (n >= 4 && TapOnly(3) && TapOnly(2) && TapOnly(1) && TapOnly(0))
            { Emit(RhythmAction.DoubleSlash, 4); return; }
            if (n >= 3 && TapOnly(2) && TapOnly(1) && TapOnly(0))
            { Emit(RhythmAction.DoubleSlash, 3, false); return; }
            if (n >= 3 && TapOnly(2) && At(1).IsRest && TapOnly(0))
            { Emit(RhythmAction.Swing, 3); }
        }

        BeatInput At(int back) => history[history.Count - 1 - back];
        bool TapOnly(int back) => At(back).IsTap && (At(back).Held & ~At(back).Pressed) == 0;
        bool Hold(int back, int lane) => At(back).Pressed == 0 && At(back).Released == 0 && At(back).Held == lane;
        void Emit(RhythmAction action, int length, bool consume = true)
        {
            bool critical = true;
            for (int i = 0; i < length; i++)
                if (!At(i).IsRest && At(i).Grade != BeatGrade.Perfect) critical = false;
            var result = new ActionResult(action, At(0).Tick, critical);
            lastActionTick = result.Tick;
            if (consume) { history.Clear(); consumedThrough = result.Tick; }
            ActionRecognized?.Invoke(result);
        }
    }
}
