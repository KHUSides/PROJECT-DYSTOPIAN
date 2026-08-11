using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dystopian.Rhythm
{
    internal static class CmChartParser
    {
        private const string SupportedFormat = "chartmaker-project";

        public static ParsedRhythmChart Parse(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                throw new FormatException("CMChart source is empty.");
            }

            CmChartDocument document = JsonUtility.FromJson<CmChartDocument>(source);
            if (document == null || !string.Equals(
                    document.format,
                    SupportedFormat,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new FormatException("Unsupported CMChart format.");
            }

            Dictionary<string, LaneMapping> laneMappings = BuildLaneMappings(document.lanes);
            MeasureMap measureMap = new MeasureMap(document.timing?.timeSignatures, document.notes);
            List<RhythmChartNote> notes = ParseNotes(document.notes, laneMappings, measureMap);
            notes.Sort((left, right) => left.Beat.CompareTo(right.Beat));

            float? bpm = ReadInitialBpm(document.timing?.tempoEvents, measureMap);
            return new ParsedRhythmChart(notes, bpm);
        }

        private static Dictionary<string, LaneMapping> BuildLaneMappings(CmLane[] lanes)
        {
            Dictionary<string, LaneMapping> mappings =
                new Dictionary<string, LaneMapping>(StringComparer.OrdinalIgnoreCase);
            if (lanes == null)
            {
                return mappings;
            }

            foreach (CmLane lane in lanes)
            {
                if (lane == null || string.IsNullOrWhiteSpace(lane.id) ||
                    !TryMapLaneTag(lane.tag, out LaneMapping mapping))
                {
                    continue;
                }

                mappings[lane.id] = mapping;
            }

            return mappings;
        }

        private static bool TryMapLaneTag(string tag, out LaneMapping mapping)
        {
            string normalized = (tag ?? string.Empty).Trim().Replace('-', '_').ToLowerInvariant();
            switch (normalized)
            {
                case "player":
                    mapping = new LaneMapping(RhythmActor.Player, null);
                    return true;
                case "enemy":
                    mapping = new LaneMapping(RhythmActor.Enemy, null);
                    return true;
                case "player_ghost":
                    mapping = new LaneMapping(RhythmActor.Player, RhythmNoteType.Ghost);
                    return true;
                case "enemy_ghost":
                    mapping = new LaneMapping(RhythmActor.Enemy, RhythmNoteType.Ghost);
                    return true;
                case "attack":
                    mapping = new LaneMapping(RhythmActor.Player, RhythmNoteType.AttackState);
                    return true;
                default:
                    mapping = default;
                    return false;
            }
        }

        private static List<RhythmChartNote> ParseNotes(
            CmNote[] sourceNotes,
            Dictionary<string, LaneMapping> laneMappings,
            MeasureMap measureMap)
        {
            List<RhythmChartNote> notes = new List<RhythmChartNote>();
            if (sourceNotes == null)
            {
                return notes;
            }

            foreach (CmNote sourceNote in sourceNotes)
            {
                if (sourceNote == null || sourceNote.start == null ||
                    !laneMappings.TryGetValue(sourceNote.laneId ?? string.Empty, out LaneMapping lane))
                {
                    continue;
                }

                float startBeat = measureMap.ToAbsoluteBeat(sourceNote.start);
                RhythmNoteType noteType = ResolveNoteType(sourceNote.type, lane.TypeOverride);
                if (noteType == RhythmNoteType.Ghost)
                {
                    notes.Add(new RhythmChartNote(
                        lane.Actor,
                        noteType,
                        startBeat,
                        sourceId: sourceNote.id));
                    continue;
                }

                if (noteType == RhythmNoteType.AttackState && sourceNote.end == null)
                {
                    continue;
                }

                bool requiresDuration = noteType == RhythmNoteType.Long ||
                    noteType == RhythmNoteType.AttackState;
                float durationBeats = 0f;
                if (requiresDuration)
                {
                    float endBeat = sourceNote.end != null
                        ? measureMap.ToAbsoluteBeat(sourceNote.end)
                        : startBeat;
                    durationBeats = Mathf.Max(0.01f, endBeat - startBeat);
                }

                notes.Add(new RhythmChartNote(
                    lane.Actor,
                    noteType,
                    startBeat,
                    durationBeats,
                    sourceNote.id));
            }

            return notes;
        }

        private static RhythmNoteType ResolveNoteType(string sourceType, RhythmNoteType? typeOverride)
        {
            if (typeOverride.HasValue)
            {
                return typeOverride.Value;
            }

            return string.Equals(sourceType, "hold", StringComparison.OrdinalIgnoreCase)
                ? RhythmNoteType.Long
                : RhythmNoteType.Single;
        }

        private static float? ReadInitialBpm(CmTempoEvent[] events, MeasureMap measureMap)
        {
            if (events == null || events.Length == 0)
            {
                return null;
            }

            CmTempoEvent earliest = null;
            float earliestBeat = float.PositiveInfinity;
            foreach (CmTempoEvent tempoEvent in events)
            {
                if (tempoEvent == null || tempoEvent.position == null || tempoEvent.bpm <= 0f)
                {
                    continue;
                }

                float beat = measureMap.ToAbsoluteBeat(tempoEvent.position);
                if (beat < earliestBeat)
                {
                    earliest = tempoEvent;
                    earliestBeat = beat;
                }
            }

            return earliest != null ? (float?)earliest.bpm : null;
        }

        private readonly struct LaneMapping
        {
            public RhythmActor Actor { get; }
            public RhythmNoteType? TypeOverride { get; }

            public LaneMapping(RhythmActor actor, RhythmNoteType? typeOverride)
            {
                Actor = actor;
                TypeOverride = typeOverride;
            }
        }

        private sealed class MeasureMap
        {
            private readonly float[] measureStartBeats_;

            public MeasureMap(CmTimeSignature[] signatures, CmNote[] notes)
            {
                int maxMeasure = FindMaxMeasure(signatures, notes);
                measureStartBeats_ = new float[maxMeasure + 2];

                Dictionary<int, CmTimeSignature> changes = new Dictionary<int, CmTimeSignature>();
                if (signatures != null)
                {
                    foreach (CmTimeSignature signature in signatures)
                    {
                        if (signature != null && signature.measure >= 0 &&
                            signature.numerator > 0 && signature.denominator > 0)
                        {
                            changes[signature.measure] = signature;
                        }
                    }
                }

                int numerator = 4;
                int denominator = 4;
                for (int measure = 0; measure <= maxMeasure; measure++)
                {
                    if (changes.TryGetValue(measure, out CmTimeSignature signature))
                    {
                        numerator = signature.numerator;
                        denominator = signature.denominator;
                    }

                    float measureBeats = numerator * 4f / denominator;
                    measureStartBeats_[measure + 1] =
                        measureStartBeats_[measure] + measureBeats;
                }
            }

            public float ToAbsoluteBeat(CmPosition position)
            {
                int measure = Mathf.Clamp(position.measure, 0, measureStartBeats_.Length - 2);
                return measureStartBeats_[measure] + ReadFraction(position.offset);
            }

            private static float ReadFraction(CmFraction fraction)
            {
                if (fraction == null || fraction.denominator == 0)
                {
                    return 0f;
                }

                return (float)fraction.numerator / fraction.denominator;
            }

            private static int FindMaxMeasure(CmTimeSignature[] signatures, CmNote[] notes)
            {
                int maxMeasure = 0;
                if (signatures != null)
                {
                    foreach (CmTimeSignature signature in signatures)
                    {
                        if (signature != null)
                        {
                            maxMeasure = Mathf.Max(maxMeasure, signature.measure);
                        }
                    }
                }

                if (notes != null)
                {
                    foreach (CmNote note in notes)
                    {
                        if (note?.start != null)
                        {
                            maxMeasure = Mathf.Max(maxMeasure, note.start.measure);
                        }
                        if (note?.end != null)
                        {
                            maxMeasure = Mathf.Max(maxMeasure, note.end.measure);
                        }
                    }
                }

                return maxMeasure;
            }
        }

        #pragma warning disable 0649

        [Serializable]
        private sealed class CmChartDocument
        {
            public string format;
            public int schemaVersion;
            public CmTiming timing;
            public CmLane[] lanes;
            public CmNote[] notes;
        }

        [Serializable]
        private sealed class CmTiming
        {
            public CmTempoEvent[] tempoEvents;
            public CmTimeSignature[] timeSignatures;
        }

        [Serializable]
        private sealed class CmTempoEvent
        {
            public CmPosition position;
            public float bpm;
        }

        [Serializable]
        private sealed class CmTimeSignature
        {
            public int measure;
            public int numerator;
            public int denominator;
        }

        [Serializable]
        private sealed class CmLane
        {
            public string id;
            public string tag;
        }

        [Serializable]
        private sealed class CmNote
        {
            public string id;
            public string laneId;
            public string type;
            public CmPosition start;
            public CmPosition end;
        }

        [Serializable]
        private sealed class CmPosition
        {
            public int measure;
            public CmFraction offset;
        }

        [Serializable]
        private sealed class CmFraction
        {
            public int numerator;
            public int denominator = 1;
        }

        #pragma warning restore 0649
    }
}
