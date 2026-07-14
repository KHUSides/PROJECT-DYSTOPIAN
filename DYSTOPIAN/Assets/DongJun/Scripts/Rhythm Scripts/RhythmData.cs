using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Dystopian.Rhythm
{
    public enum RhythmActor
    {
        Player,
        Enemy,
        Boss
    }

    public enum RhythmNoteType
    {
        Single,
        Long,
        AttackState,
        Ghost
    }

    public enum RhythmJudgement
    {
        Critical,
        Cool,
        Good,
        Bad,
        Miss
    }

    public enum RhythmMusicStartMode
    {
        ChartStart,
        FirstPlayerJudgement
    }

    [Serializable]
    public sealed class RhythmChartNote
    {
        public RhythmActor Actor { get; }
        public RhythmNoteType Type { get; }
        public float Beat { get; }
        public float DurationBeats { get; }

        public RhythmChartNote(RhythmActor actor, RhythmNoteType type, float beat, float durationBeats = 0f)
        {
            Actor = actor;
            Type = type;
            Beat = Mathf.Max(0f, beat);
            DurationBeats = Mathf.Max(0f, durationBeats);
        }
    }

    [Serializable]
    public sealed class RhythmJudgementWindows
    {
        [SerializeField, FormerlySerializedAs("criticalSeconds"), Min(0.001f)]
        private float criticalSeconds_ = 0.045f;

        [SerializeField, FormerlySerializedAs("coolSeconds"), Min(0.001f)]
        private float coolSeconds_ = 0.09f;

        [SerializeField, FormerlySerializedAs("goodSeconds"), Min(0.001f)]
        private float goodSeconds_ = 0.14f;

        [SerializeField, FormerlySerializedAs("badSeconds"), Min(0.001f)]
        private float badSeconds_ = 0.22f;

        public float CriticalSeconds => criticalSeconds_;
        public float CoolSeconds => coolSeconds_;
        public float GoodSeconds => goodSeconds_;
        public float BadSeconds => badSeconds_;

        public void ClampValues()
        {
            criticalSeconds_ = Mathf.Max(0.001f, criticalSeconds_);
            coolSeconds_ = Mathf.Max(criticalSeconds_, coolSeconds_);
            goodSeconds_ = Mathf.Max(coolSeconds_, goodSeconds_);
            badSeconds_ = Mathf.Max(goodSeconds_, badSeconds_);
        }
    }

    [Serializable]
    public sealed class RhythmLayout
    {
        [SerializeField, FormerlySerializedAs("offscreenSpawnPadding"), Min(0f)]
        private float offscreenSpawnPadding_ = 80f;

        [SerializeField, FormerlySerializedAs("singleNoteWidth"), Min(1f)]
        private float singleNoteWidth_ = 20f;

        [SerializeField, FormerlySerializedAs("noteHeight"), Min(1f)]
        private float noteHeight_ = 72f;

        [SerializeField, FormerlySerializedAs("minimumLongWidth"), Min(0f)]
        private float minimumLongWidth_ = 42f;

        public float OffscreenSpawnPadding => offscreenSpawnPadding_;
        public float SingleNoteWidth => singleNoteWidth_;
        public float NoteHeight => noteHeight_;
        public float MinimumLongWidth => minimumLongWidth_;
    }

    [Serializable]
    public sealed class RhythmColors
    {
        [SerializeField, FormerlySerializedAs("playerNote")]
        private Color playerNote_ = new Color(0.1f, 0.85f, 1f, 1f);

        [SerializeField, FormerlySerializedAs("enemyNote")]
        private Color enemyNote_ = new Color(1f, 0.2f, 0.26f, 1f);

        [SerializeField, FormerlySerializedAs("bossNote")]
        private Color bossNote_ = new Color(1f, 0.72f, 0.1f, 1f);

        [SerializeField, FormerlySerializedAs("attackState")]
        private Color attackState_ = new Color(1f, 0.25f, 0.82f, 0.9f);

        [SerializeField, FormerlySerializedAs("critical")]
        private Color critical_ = new Color(1f, 0.9f, 0.25f, 1f);

        [SerializeField, FormerlySerializedAs("cool")]
        private Color cool_ = new Color(0.2f, 0.9f, 1f, 1f);

        [SerializeField, FormerlySerializedAs("good")]
        private Color good_ = new Color(0.25f, 1f, 0.48f, 1f);

        [SerializeField, FormerlySerializedAs("bad")]
        private Color bad_ = new Color(1f, 0.45f, 0.18f, 1f);

        [SerializeField, FormerlySerializedAs("miss")]
        private Color miss_ = new Color(1f, 0.15f, 0.2f, 1f);

        public Color PlayerNote => playerNote_;
        public Color EnemyNote => enemyNote_;
        public Color BossNote => bossNote_;
        public Color AttackState => attackState_;
        public Color Critical => critical_;
        public Color Cool => cool_;
        public Color Good => good_;
        public Color Bad => bad_;
        public Color Miss => miss_;
    }

    [Serializable]
    public sealed class NoteUnityEvent : UnityEvent<RhythmChartNote>
    {
    }

    [Serializable]
    public sealed class JudgementUnityEvent : UnityEvent<RhythmJudgement, float>
    {
    }

    [Serializable]
    public sealed class IntUnityEvent : UnityEvent<int>
    {
    }
}
