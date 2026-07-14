using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Dystopian.Rhythm
{
    internal sealed class ParsedBmsChart
    {
        public List<RhythmChartNote> Notes { get; }
        public float? Bpm { get; }

        public ParsedBmsChart(List<RhythmChartNote> notes, float? bpm)
        {
            Notes = notes;
            Bpm = bpm;
        }
    }

    internal static class BmsChartParser
    {
        public static ParsedBmsChart Parse(
            string playerSource,
            string opponentSource,
            RhythmActor opponentActor,
            float beatsPerMeasure)
        {
            List<RhythmChartNote> notes = new List<RhythmChartNote>();
            ParseNotes(playerSource, RhythmActor.Player, beatsPerMeasure, notes);
            ParseNotes(opponentSource, opponentActor, beatsPerMeasure, notes);
            notes.Sort((left, right) => left.Beat.CompareTo(right.Beat));

            return new ParsedBmsChart(notes, TryReadBpm(playerSource, out float bpm) ? bpm : null);
        }

        private static void ParseNotes(
            string source,
            RhythmActor actor,
            float beatsPerMeasure,
            List<RhythmChartNote> output)
        {
            Dictionary<int, List<float>> longMarkers = new Dictionary<int, List<float>>();
            string[] lines = source.Replace("\r", string.Empty).Split('\n');

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                int colonIndex = line.IndexOf(':');
                if (!line.StartsWith("#", StringComparison.Ordinal) || colonIndex != 6)
                {
                    continue;
                }

                if (!int.TryParse(line.Substring(1, 3), out int measure) ||
                    !int.TryParse(line.Substring(4, 2), out int channel))
                {
                    continue;
                }

                string data = line.Substring(colonIndex + 1).Trim();
                if (data.Length < 2 || data.Length % 2 != 0)
                {
                    continue;
                }

                bool isSingleChannel = channel >= 11 && channel <= 19;
                bool isLongChannel = channel >= 51 && channel <= 59;
                if (!isSingleChannel && !isLongChannel)
                {
                    continue;
                }

                int slotCount = data.Length / 2;
                for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
                {
                    if (data.Substring(slotIndex * 2, 2) == "00")
                    {
                        continue;
                    }

                    float beat = measure * beatsPerMeasure + slotIndex * beatsPerMeasure / slotCount;
                    if (isSingleChannel)
                    {
                        output.Add(new RhythmChartNote(actor, RhythmNoteType.Single, beat));
                        continue;
                    }

                    if (!longMarkers.TryGetValue(channel, out List<float> markers))
                    {
                        markers = new List<float>();
                        longMarkers.Add(channel, markers);
                    }
                    markers.Add(beat);
                }
            }

            foreach (List<float> markers in longMarkers.Values)
            {
                markers.Sort();
                for (int index = 0; index + 1 < markers.Count; index += 2)
                {
                    float startBeat = markers[index];
                    float durationBeats = Mathf.Max(0.01f, markers[index + 1] - startBeat);
                    output.Add(new RhythmChartNote(actor, RhythmNoteType.Long, startBeat, durationBeats));
                }
            }
        }

        private static bool TryReadBpm(string source, out float bpm)
        {
            string[] lines = source.Replace("\r", string.Empty).Split('\n');
            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (!line.StartsWith("#BPM ", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return float.TryParse(
                    line.Substring(5).Trim(),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out bpm);
            }

            bpm = 0f;
            return false;
        }
    }
}
