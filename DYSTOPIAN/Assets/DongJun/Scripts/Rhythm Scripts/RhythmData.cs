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

    public enum RhythmChartSource
    {
        CmChart,
        Bms
    }

    [Serializable]
    public sealed class RhythmChartNote
    {
        public RhythmActor Actor { get; }
        public RhythmNoteType Type { get; }
        public float Beat { get; }
        public float DurationBeats { get; }
        public string SourceId { get; }

        public RhythmChartNote(
            RhythmActor actor,
            RhythmNoteType type,
            float beat,
            float durationBeats = 0f,
            string sourceId = null)
        {
            Actor = actor;
            Type = type;
            Beat = Mathf.Max(0f, beat);
            DurationBeats = Mathf.Max(0f, durationBeats);
            SourceId = sourceId;
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
        private float noteHeight_ = 90f;

        [SerializeField, FormerlySerializedAs("minimumLongWidth"), Min(0f)]
        private float minimumLongWidth_ = 42f;

        [SerializeField, Min(0.1f)]
        private float attackNoteHeightMultiplier_ = 1.35f;

        public float OffscreenSpawnPadding => offscreenSpawnPadding_;
        public float SingleNoteWidth => singleNoteWidth_;
        public float NoteHeight => noteHeight_;
        public float MinimumLongWidth => minimumLongWidth_;
        public float AttackNoteHeightMultiplier => attackNoteHeightMultiplier_;
    }

    [Serializable]
    public sealed class RhythmColors
    {
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

        public Color AttackState => attackState_;
        public Color Critical => critical_;
        public Color Cool => cool_;
        public Color Good => good_;
        public Color Bad => bad_;
        public Color Miss => miss_;
    }

    [Serializable]
    public sealed class RhythmComboAnimationSettings
    {
        [SerializeField] private bool enabled_ = true;

        [SerializeField, FormerlySerializedAs("rollDuration_"), Min(0.01f)]
        private float increaseDuration_ = 0.12f;

        [SerializeField, FormerlySerializedAs("rollVerticalPunch_"), Min(0f)]
        private float increaseDistance_ = 12f;

        [SerializeField] private bool smearEnabled_ = true;
        [SerializeField, Range(1, 5)] private int smearCopies_ = 5;
        [SerializeField, Min(0f)] private float smearDistance_ = 32f;
        [SerializeField, Min(1f)] private float smearStretch_ = 1.35f;
        [SerializeField, Range(0f, 1f)] private float smearAlpha_ = 0.55f;

        [Header("Continuous Shake")]
        [SerializeField, Min(0f)] private float shakeStrength10_ = 1.5f;
        [SerializeField, Min(0f)] private float shakeStrength20_ = 2.75f;
        [SerializeField, Min(0f)] private float shakeStrength30_ = 4.5f;
        [SerializeField, Min(0.01f)] private float shakeCycleDuration_ = 0.14f;
        [SerializeField, Min(1)] private int shakeVibrato_ = 8;
        [SerializeField, Range(0f, 180f)] private float shakeRandomness_ = 75f;

        [Header("Change Blur")]
        [SerializeField, Min(0.01f)] private float blurDuration_ = 0.1f;
        [SerializeField, Range(0f, 1f)] private float blurSoftness_ = 0.45f;
        [SerializeField, Range(-1f, 1f)] private float blurFaceDilate_ = -0.08f;

        [SerializeField, Min(0.01f)] private float breakDuration_ = 0.22f;
        [SerializeField, Min(0f)] private float breakStrength_ = 18f;
        [SerializeField, Min(1)] private int breakVibrato_ = 24;
        [SerializeField, Range(0f, 180f)] private float breakRandomness_ = 60f;
        [SerializeField] private bool useUnscaledTime_ = true;

        public bool Enabled => enabled_;
        public float IncreaseDuration => Mathf.Max(0.01f, increaseDuration_);
        public float IncreaseDistance => Mathf.Max(0f, increaseDistance_);
        public bool SmearEnabled => smearEnabled_;
        public int SmearCopies => Mathf.Clamp(smearCopies_, 1, 5);
        public float SmearDistance => Mathf.Max(0f, smearDistance_);
        public float SmearStretch => Mathf.Max(1f, smearStretch_);
        public float SmearAlpha => Mathf.Clamp01(smearAlpha_);
        public float ShakeStrength10 => Mathf.Max(0f, shakeStrength10_);
        public float ShakeStrength20 => Mathf.Max(0f, shakeStrength20_);
        public float ShakeStrength30 => Mathf.Max(0f, shakeStrength30_);
        public float ShakeCycleDuration => Mathf.Max(0.01f, shakeCycleDuration_);
        public int ShakeVibrato => Mathf.Max(1, shakeVibrato_);
        public float ShakeRandomness => Mathf.Clamp(shakeRandomness_, 0f, 180f);
        public float BlurDuration => Mathf.Max(0.01f, blurDuration_);
        public float BlurSoftness => Mathf.Clamp01(blurSoftness_);
        public float BlurFaceDilate => Mathf.Clamp(blurFaceDilate_, -1f, 1f);
        public float BreakDuration => Mathf.Max(0.01f, breakDuration_);
        public float BreakStrength => Mathf.Max(0f, breakStrength_);
        public int BreakVibrato => Mathf.Max(1, breakVibrato_);
        public float BreakRandomness => Mathf.Clamp(breakRandomness_, 0f, 180f);
        public bool UseUnscaledTime => useUnscaledTime_;
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
