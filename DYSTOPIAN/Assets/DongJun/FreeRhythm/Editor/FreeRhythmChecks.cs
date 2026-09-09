#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Dystopian.FreeRhythm;

namespace Dystopian.FreeRhythm.Editor
{
    // Invoked through Unity MCP. Throws on the first regression; never edits scenes.
    public static class FreeRhythmChecks
    {
        [UnityEditor.MenuItem("Dystopian/Free Rhythm/Open Test Scene")]
        static void OpenScene()
        {
            if (UnityEditor.EditorApplication.isPlaying) return;
            if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/DongJun/FreeRhythm/FreeRhythmTest.unity");
        }
        [UnityEditor.MenuItem("Dystopian/Free Rhythm/Run Timing Checks")]
        static void RunFromMenu() { UnityEngine.Debug.Log(Run()); }

        public static string Run()
        {
            int checks = 0;
            Action<bool, string> check = (ok, name) => { if (!ok) throw new Exception(name); checks++; };
            var e = new FreeRhythmEngine(120);
            long tick; double error;
            check(e.Interval == .25, "120 BPM eighth interval");
            check(e.Judge(0, out tick, out error) == BeatGrade.Perfect && tick == 0, "downbeat");
            check(e.Judge(.25, out tick, out error) == BeatGrade.Perfect && tick == 1, "offbeat is equally valid");
            check(e.Judge(-.02, out tick, out error) == BeatGrade.Perfect && tick == 0, "early first input");
            check(e.Judge(-.2, out tick, out error) == BeatGrade.Miss, "negative tick rejected");
            check(e.Judge(.09, out tick, out error) == BeatGrade.Good, "inclusive late window");
            check(e.Judge(.091, out tick, out error) == BeatGrade.Miss, "outside window");
            check(e.Judge(.125, out tick, out error) == BeatGrade.Miss, "between eighths");
            check(e.Judge(7200.25, out tick, out error) == BeatGrade.Perfect && tick == 28801, "two-hour clock precision");
            var fast = new FreeRhythmEngine(300);
            check(fast.GoodWindow < fast.Interval / 2, "high BPM windows do not overlap");
            foreach (double bpm in new[] { 60d, 120d, 180d, 300d })
            {
                var timing = new FreeRhythmEngine(bpm);
                foreach (long target in new[] { 0L, 1L, 7L, 8L, 100000L })
                {
                    double center = target * timing.Interval;
                    foreach (double offset in new[] { -timing.GoodWindow, -.01, 0, .01, timing.GoodWindow })
                    {
                        check(timing.Judge(center + offset, out tick, out error) != BeatGrade.Miss && tick == target,
                            "symmetric beat window at " + bpm + " BPM tick " + target);
                    }
                    check(!timing.IsWindowClosed(target, center + timing.GoodWindow), "inclusive boundary not finalized early");
                    check(timing.IsWindowClosed(target, center + timing.GoodWindow + .001), "late window expires");
                }
                check(timing.IsTapRelease(0, timing.Interval * .5 + .001), "tap release between beats does not reset sequence");
                check(!timing.IsTapRelease(0, timing.Interval - timing.GoodWindow), "early next beat release is a hold gesture");
            }
            var rectangle = new UnityEngine.Rect(10, 20, 220, 220);
            for (int i = -8; i <= 16; i++)
            {
                var point = FreeRhythmHUD.PerimeterPoint(rectangle, i);
                check((point - FreeRhythmHUD.PerimeterPoint(rectangle, i + 8)).sqrMagnitude < .001f, "perimeter wraps across bars");
                check(UnityEngine.Mathf.Approximately(point.x, rectangle.xMin) || UnityEngine.Mathf.Approximately(point.x, rectangle.xMax) ||
                    UnityEngine.Mathf.Approximately(point.y, rectangle.yMin) || UnityEngine.Mathf.Approximately(point.y, rectangle.yMax), "marker stays on border");
            }
            check(FreeRhythmHUD.PerimeterPoint(rectangle, 0) == new UnityEngine.Vector2(120, 20), "downbeat is top center");
            check(FreeRhythmHUD.PerimeterPoint(rectangle, 2) == new UnityEngine.Vector2(230, 130), "second beat is right center");
            bool invalid = false;
            try { new FreeRhythmEngine(0); } catch (ArgumentOutOfRangeException) { invalid = true; }
            check(invalid, "invalid BPM");
            var actions = new List<ActionResult>(); e.ActionRecognized += actions.Add;
            Action reset = () => { e.Reset(); actions.Clear(); };
            for (int i = 0; i < 64; i++) e.Push(Frame(i));
            check(actions.Count == 0 && e.HistoryCount <= 8, "idle has no misses/actions and bounded history");
            reset(); e.Push(Frame(6, 1)); e.Push(Frame(7)); e.Push(Frame(8, 2));
            check(actions.Count == 1 && actions[0].Action == RhythmAction.Swing && actions[0].Critical, "cross-bar mixed-lane swing");
            reset(); e.Push(Frame(0, 2, 0, 0, BeatGrade.Good)); e.Push(Frame(1)); e.Push(Frame(2, 1));
            check(actions.Count == 1 && !actions[0].Critical, "good input removes critical");
            reset(); e.Push(Frame(0, 1)); e.Push(Frame(1, 2)); e.Push(Frame(2, 1));
            check(actions.Count == 1 && actions[0].Tick == 2, "double slash first strike");
            e.Push(Frame(3, 2));
            check(actions.Count == 2 && actions[1].Action == RhythmAction.DoubleSlash && actions[1].Tick == 3, "double slash second strike requires input");
            reset(); e.Push(Frame(0, 1)); e.Push(Frame(1, 1)); e.Push(Frame(2, 1)); e.Push(Frame(3));
            check(actions.Count == 1, "no automatic second slash when resting");
            reset(); e.Push(Frame(0, 2, 2));
            for (int i = 1; i <= 3; i++) e.Push(Frame(i, 0, 2));
            e.Push(Frame(4, 0, 0, 2));
            check(actions.Count == 1 && actions[0].Action == RhythmAction.ChargedAttack, "two-beat charge");
            reset(); e.Push(Frame(0, 1, 1)); e.Push(Frame(1, 0, 0, 1)); e.Push(Frame(2, 2));
            check(actions.Count == 1 && actions[0].Action == RhythmAction.GuardAttack, "half-beat guard then attack");
            reset(); e.Push(Frame(0, 3));
            check(actions.Count == 0, "chords are unsupported");
            reset(); e.Push(Frame(0, 1, 1)); e.Push(Frame(1, 2, 3));
            check(actions.Count == 1 && actions[0].Action == RhythmAction.HoldStrike, "other lane during hold");
            reset(); e.Push(Frame(0, 1)); e.Push(Frame(1, 0, 0, 0, BeatGrade.Miss)); e.Push(Frame(2, 1));
            check(actions.Count == 0, "miss invalidates sequence");
            reset(); e.Push(Frame(0, 1)); e.Push(Frame(2)); e.Push(Frame(3, 1));
            check(actions.Count == 0, "time gap invalidates sequence");
            reset(); e.Push(Frame(0, 1, 1)); e.Push(Frame(1, 0, 1)); e.Push(Frame(2, 0, 0, 1));
            check(actions.Count == 0, "short charge does not cast");
            reset(); e.Commit(Frame(0, 1)); e.Commit(Frame(1)); e.Push(Frame(2, 2));
            check(actions.Count == 1, "action fires on input before slot commit");
            e.Commit(Frame(2, 2)); e.Push(Frame(2, 2));
            check(actions.Count == 1, "commit and repeated input cannot replay consumed action");
            e.Commit(Frame(3)); e.Push(Frame(4, 1));
            check(actions.Count == 1, "consumed last tap cannot start a phantom swing");
            reset(); e.Push(Frame(0, 1)); e.Commit(Frame(0, 1)); e.Push(Frame(1, 2));
            e.Commit(Frame(1, 2)); e.Push(Frame(2, 1)); e.Commit(Frame(2, 1)); e.Push(Frame(3, 2));
            check(actions.Count == 2, "double slash survives immediate input plus commits");
            return checks + " Free Rhythm checks passed";
        }

        [UnityEditor.MenuItem("Dystopian/Free Rhythm/Run Immediate Input Checks (Play Mode)")]
        static void RunImmediateFromMenu() { UnityEngine.Debug.Log(RunImmediateInputChecks()); }

        public static string RunImmediateInputChecks()
        {
            if (!UnityEngine.Application.isPlaying) throw new InvalidOperationException("Enter FreeRhythmTest Play Mode first.");
            var r = UnityEngine.Object.FindFirstObjectByType<FreeRhythmSystem>();
            if (r == null) throw new InvalidOperationException("FreeRhythmSystem required.");
            var finalize = typeof(FreeRhythmSystem).GetMethod("FinalizeThrough", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Action<double> advance = time => finalize.Invoke(r, new object[] { time });
            double step = r.Interval, good = r.Window * .9, quick = Math.Min(step * .1, r.Window * .2);
            int checks = 0;
            Action<bool, string> check = (ok, name) => { if (!ok) throw new Exception(name); checks++; };
            var actions = new List<ActionResult>();
            Action reset = () => { r.Restart(); actions.Clear(); };
            r.ActionRecognized += actions.Add;
            try
            {
                reset();
                check(r.Submit(0, true, 0), "first press accepted");
                check(r.HasRecent(0) && r.Recent(0).Pressed == 1, "HUD records input immediately");
                check(!r.Submit(1, true, 0) && !r.Submit(1, true, good), "second key on same eighth rejected");
                check(actions.Count == 0 && r.Recent(0).Pressed == 1, "no chord or hold strike from simultaneous keys");
                r.Submit(0, false, quick);
                check(!r.Submit(0, true, quick * 2), "same-slot repeat rejected after releasing");
                r.Submit(1, true, 2 * step);
                check(actions.Count == 1 && actions[0].Action == RhythmAction.Swing, "swing fires inside Submit without waiting 90ms");
                check(actions[0].Critical, "perfect immediate swing");
                r.Submit(1, false, 2 * step + quick); advance(2 * step + r.Window + .01);
                check(actions.Count == 1, "slot expiry cannot repeat damage");

                reset(); r.Submit(1, true, 0); r.Submit(1, false, quick); r.Submit(0, true, 2 * step - good);
                check(actions.Count == 1 && !actions[0].Critical, "early Good input fires before target beat");
                reset(); r.Submit(0, true, 0); r.Submit(0, false, quick); r.Submit(1, true, 2 * step + good);
                check(actions.Count == 1 && !actions[0].Critical, "late Good input fires immediately");

                reset();
                for (int i = 0; i < 3; i++) { r.Submit(i % 2, true, i * step); r.Submit(i % 2, false, i * step + quick); }
                check(actions.Count == 1 && actions[0].Action == RhythmAction.DoubleSlash, "third press attacks immediately");
                r.Submit(1, true, 3 * step);
                check(actions.Count == 2, "fourth press attacks immediately");
                advance(3 * step + r.Window + .01); check(actions.Count == 2, "double slash not duplicated by commit");

                reset(); r.Submit(0, true, 0); r.Submit(0, false, 4 * step);
                check(actions.Count == 1 && actions[0].Action == RhythmAction.ChargedAttack, "hold release attacks immediately");
                advance(4 * step + r.Window + .01); check(actions.Count == 1, "charge release not duplicated");
                reset(); r.Submit(0, true, 0); r.Submit(0, false, step); r.Submit(1, true, 2 * step);
                check(actions.Count == 1 && actions[0].Action == RhythmAction.GuardAttack, "guard followup remains immediate");
                reset(); r.Submit(0, true, good); r.Submit(1, true, step);
                check(actions.Count == 1 && actions[0].Action == RhythmAction.HoldStrike && !actions[0].Critical, "earlier hold retains grade during other-key attack");
                reset(); check(!r.Submit(0, true, step * .5) && actions.Count == 0, "off-beat cannot attack");
                advance(7 * step + r.Window + .01); check(actions.Count == 0 && r.Combo == 0, "rest cannot attack or build combo");
                r.SetPaused(true); check(!r.Submit(0, true, 8 * step), "pause rejects inputs");
                r.SetPaused(false);

                foreach (double offset in new[] { -r.Window, 0, r.Window })
                {
                    reset();
                    check(r.Submit(0, true, offset), "first beat centered input accepted");
                    r.Submit(0, false, r.Window + quick);
                    check(r.Submit(1, true, 2 * step + offset), "next quarter beat centered input accepted");
                    check(actions.Count == 1 && actions[0].Action == RhythmAction.Swing, "early/exact/late swing recognized");
                }

                reset(); r.Submit(0, true, 0);
                check(r.Submit(0, false, step * .6), "tap release outside judgement windows is ignored");
                r.Submit(1, true, 2 * step);
                check(actions.Count == 1 && actions[0].Action == RhythmAction.Swing, "relaxed tap release preserves sequence");

                reset(); r.Submit(0, true, 0); r.Submit(0, false, quick);
                var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var systemType = typeof(FreeRhythmSystem);
                var inputType = systemType.GetNestedType("TimedKeyInput", System.Reflection.BindingFlags.NonPublic);
                object queuedInput = Activator.CreateInstance(inputType);
                double origin = (double)systemType.GetField("origin", flags).GetValue(r);
                inputType.GetField("Lane_").SetValue(queuedInput, 1);
                inputType.GetField("Press_").SetValue(queuedInput, true);
                inputType.GetField("Time_").SetValue(queuedInput, origin + r.AudioLatencySeconds + 2 * step);
                object queue = systemType.GetField("queuedInputs", flags).GetValue(r);
                queue.GetType().GetMethod("Enqueue").Invoke(queue, new[] { queuedInput });
                systemType.GetMethod("ProcessQueuedInputs", flags).Invoke(r, new object[] { 0d });
                advance(2 * step + r.Window + .05);
                check(actions.Count == 1 && actions[0].Critical, "delayed frame uses original key timestamp before finalization");
            }
            finally { r.ActionRecognized -= actions.Add; r.Restart(); }
            return checks + " immediate input checks passed";
        }
        [UnityEditor.MenuItem("Dystopian/Free Rhythm/Run Keyboard Timestamp Checks (Play Mode)")]
        static void RunKeyboardFromMenu() { UnityEngine.Debug.Log(RunKeyboardTimestampChecks()); }

        public static string RunKeyboardTimestampChecks()
        {
            if (!UnityEngine.Application.isPlaying) throw new InvalidOperationException("Enter FreeRhythmTest Play Mode first.");
            var r = UnityEngine.Object.FindFirstObjectByType<FreeRhythmSystem>();
            if (r == null) throw new InvalidOperationException("FreeRhythmSystem required.");
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var type = typeof(FreeRhythmSystem);
            int accepted = 0;
            Action<BeatInput> onInput = input => { if (input.Pressed != 0) accepted++; };
            r.InputAccepted += onInput;
            try
            {
                foreach (double offset in new[] { -r.Window * .9, 0, r.Window * .9 })
                {
                    r.Restart();
                    var keyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
                    try
                    {
                        int previous = accepted;
                        double dsp = UnityEngine.AudioSettings.dspTime;
                        double realtime = UnityEngine.Time.realtimeSinceStartupAsDouble;
                        type.GetField("origin", flags).SetValue(r,
                            dsp - (2 * r.Interval + offset + r.AudioLatencySeconds));
                        UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,
                            new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.A), realtime);
                        UnityEngine.InputSystem.InputSystem.Update();
                        type.GetMethod("ProcessQueuedInputs", flags).Invoke(r, new object[] { dsp - realtime });
                        if (accepted != previous + 1) throw new Exception("Keyboard timestamp rejected: " + offset);
                    }
                    finally { UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard); }
                }
            }
            finally { r.InputAccepted -= onInput; r.Restart(); }
            return accepted + " keyboard timestamp checks passed";
        }

        static BeatInput Frame(long tick, int pressed = 0, int held = 0, int released = 0, BeatGrade grade = BeatGrade.Perfect)
        { return new BeatInput { Tick = tick, Pressed = pressed, Held = held, Released = released, Grade = grade }; }
    }
}
#endif
