using System;
using UnityEngine;

namespace Dystopian.FreeRhythm
{
    [RequireComponent(typeof(FreeRhythmSystem))]
    public sealed class FreeRhythmHUD : MonoBehaviour
    {
        [Header("Beat perimeter (1280 x 720 reference)")]
        [SerializeField] Vector2 perimeterCenter = new Vector2(190, 550);
        [SerializeField] Vector2 perimeterSize = new Vector2(220, 220);
        [SerializeField, Min(1)] float borderWidth = 3;
        [SerializeField, Min(2)] float markerSize = 10;
        [SerializeField, Min(2)] float indicatorSize = 16;
        [SerializeField] bool showTimingWindows = true;
        [SerializeField] Color borderColor = new Color(.35f, .38f, .38f);
        [SerializeField] Color beatColor = Color.white;
        [SerializeField] Color halfBeatColor = new Color(.65f, .68f, .68f);
        [SerializeField] Color perfectColor = new Color(.3f, .92f, .72f);
        [SerializeField] Color goodColor = new Color(1f, .74f, .25f);
        [SerializeField] Color indicatorColor = Color.white;

        FreeRhythmSystem rhythm;
        GUIStyle title, label, centered, small;

        void Start() { rhythm = GetComponent<FreeRhythmSystem>(); }

        void OnGUI()
        {
            if (rhythm == null) return;
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold };
                label = new GUIStyle(GUI.skin.label) { fontSize = 16 };
                centered = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter };
                small = new GUIStyle(centered) { fontSize = 13 };
                title.normal.textColor = label.normal.textColor = centered.normal.textColor = small.normal.textColor = Color.white;
            }

            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(
                new Vector3((Screen.width - 1280 * scale) / 2, (Screen.height - 720 * scale) / 2),
                Quaternion.identity, Vector3.one * scale);
            GUI.color = Color.white;
            try
            {
                Panel(new Rect(24, 20, 620, 104), new Color(.035f, .04f, .04f, .92f));
                GUI.Label(new Rect(42, 30, 590, 32), "FREE RHYTHM   /   " + rhythm.Bpm.ToString("0") + " BPM   /   4/4", title);
                GUI.Label(new Rect(42, 66, 590, 26), rhythm.LastAction, label);
                GUI.Label(new Rect(42, 92, 590, 24), rhythm.Feedback + "      HIT COMBO  " + rhythm.Combo, label);
                DrawPerimeter();
                if (rhythm.IsPaused) GUI.Label(new Rect(400, 300, 480, 50), "PAUSED", title);
            }
            finally { GUI.matrix = oldMatrix; GUI.color = oldColor; }
        }

        void DrawPerimeter()
        {
            Vector2 size = new Vector2(Mathf.Max(80, perimeterSize.x), Mathf.Max(80, perimeterSize.y));
            Rect track = new Rect(perimeterCenter - size * .5f, size);
            Panel(track, new Color(.035f, .04f, .04f, .82f));
            DrawRange(track, 0, 8, borderColor, borderWidth);
            for (int i = 0; i < 8; i++)
            {
                if (showTimingWindows)
                {
                    double good = rhythm.Window / rhythm.Interval;
                    double perfect = rhythm.PerfectWindow / rhythm.Interval;
                    DrawRange(track, i - good, i + good, goodColor, borderWidth + 3);
                    DrawRange(track, i - perfect, i + perfect, perfectColor, borderWidth + 3);
                }
                Vector2 point = PerimeterPoint(track, i);
                Square(point, markerSize, i % 2 == 0 ? beatColor : halfBeatColor);
                Vector2 outward = (point - track.center).normalized;
                Vector2 textPoint = point + outward * 25;
                GUI.Label(new Rect(textPoint.x - 20, textPoint.y - 12, 40, 24),
                    i % 2 == 0 ? (i / 2 + 1).ToString() : "&", centered);
                if (rhythm.HasRecent(i))
                {
                    BeatInput input = rhythm.Recent(i);
                    string mark = input.Released != 0 ? "UP" : input.Pressed == 1 ? "A" :
                        input.Pressed == 2 ? "D" : input.Held != 0 ? "HOLD" : "-";
                    Vector2 inputPoint = point - outward * 25;
                    GUI.Label(new Rect(inputPoint.x - 23, inputPoint.y - 10, 46, 20), mark, small);
                }
            }

            // Absolute DSP phase: a marker crossing and its judged beat share one timestamp.
            double phase = rhythm.SongTime / rhythm.Interval;
            Vector2 indicator = PerimeterPoint(track, phase);
            Color color = rhythm.CurrentGrade == BeatGrade.Perfect ? perfectColor :
                rhythm.CurrentGrade == BeatGrade.Good ? goodColor : indicatorColor;
            Square(indicator, indicatorSize + 4, new Color(.035f, .04f, .04f));
            Square(indicator, indicatorSize, color);
            string bar = rhythm.SongTime < 0 ? "COUNT IN" : "BAR " + (rhythm.CurrentTick / 8 + 1);
            GUI.Label(new Rect(track.center.x - 75, track.center.y - 26, 150, 26), bar, centered);
            GUI.Label(new Rect(track.center.x - 75, track.center.y + 2, 150, 24), "1  &  2  &  3  &  4  &", small);
        }

        public static Vector2 PerimeterPoint(Rect rect, double eighthPosition)
        {
            double phase = eighthPosition / FreeRhythmEngine.EighthsPerBar;
            phase -= Math.Floor(phase);
            double perimeter = 2 * (rect.width + rect.height);
            double distance = (phase * perimeter + rect.width * .5) % perimeter;
            if (distance <= rect.width) return new Vector2(rect.xMin + (float)distance, rect.yMin);
            distance -= rect.width;
            if (distance <= rect.height) return new Vector2(rect.xMax, rect.yMin + (float)distance);
            distance -= rect.height;
            if (distance <= rect.width) return new Vector2(rect.xMax - (float)distance, rect.yMax);
            return new Vector2(rect.xMin, rect.yMax - (float)(distance - rect.width));
        }

        static void DrawRange(Rect rect, double start, double end, Color color, float width)
        {
            // Split at every corner so timing bands stay on the border, even after resizing.
            double perimeter = 2 * (rect.width + rect.height);
            double position = start / 8 * perimeter + rect.width * .5;
            double finish = position + (end - start) / 8 * perimeter;
            for (int segment = 0; position < finish - 1e-8 && segment < 12; segment++)
            {
                double cycle = Math.Floor(position / perimeter);
                double within = position - cycle * perimeter;
                double corner = within < rect.width - 1e-8 ? rect.width :
                    within < rect.width + rect.height - 1e-8 ? rect.width + rect.height :
                    within < 2 * rect.width + rect.height - 1e-8 ? 2 * rect.width + rect.height : perimeter;
                double next = Math.Min(finish, cycle * perimeter + corner);
                Vector2 from = PerimeterPoint(rect, (position - rect.width * .5) / perimeter * 8);
                Vector2 to = PerimeterPoint(rect, (next - rect.width * .5) / perimeter * 8);
                Panel(Rect.MinMaxRect(Mathf.Min(from.x, to.x) - width / 2, Mathf.Min(from.y, to.y) - width / 2,
                    Mathf.Max(from.x, to.x) + width / 2, Mathf.Max(from.y, to.y) + width / 2), color);
                position = next;
            }
        }

        static void Square(Vector2 center, float size, Color color)
        { Panel(new Rect(center - Vector2.one * size / 2, Vector2.one * size), color); }

        static void Panel(Rect rect, Color color)
        { Color previous = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = previous; }
    }
}
