using UnityEngine;
using UnityEngine.UI;

namespace Dystopian.Rhythm
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class ComboFireUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RhythmSystem rhythmSystem;
        [SerializeField] private Image fireImage;

        [Header("Animation Frames")]
        [SerializeField] private Sprite[] smallFrames;
        [SerializeField] private Sprite[] mediumFrames;
        [SerializeField] private Sprite[] bigFrames;
        [SerializeField, Min(1f)] private float framesPerSecond = 12f;

        [Header("Combo Thresholds")]
        [SerializeField, Min(0)] private int smallCombo = 10;
        [SerializeField, Min(0)] private int mediumCombo = 20;
        [SerializeField, Min(0)] private int bigCombo = 30;

        private Sprite[] activeFrames;
        private int frameIndex;
        private int lastCombo = int.MinValue;
        private float frameAccumulator;

        private void OnValidate()
        {
            framesPerSecond = Mathf.Max(1f, framesPerSecond);
            smallCombo = Mathf.Max(0, smallCombo);
            mediumCombo = Mathf.Max(smallCombo + 1, mediumCombo);
            bigCombo = Mathf.Max(mediumCombo + 1, bigCombo);
        }

        private void Awake()
        {
            ResolveReferences();
            RefreshForCurrentCombo(true);
        }

        private void OnEnable()
        {
            ResolveReferences();
            RefreshForCurrentCombo(true);
        }

        private void Update()
        {
            ResolveReferences();
            RefreshForCurrentCombo(false);

            if (fireImage == null || !fireImage.enabled || activeFrames == null || activeFrames.Length == 0)
            {
                return;
            }

            frameAccumulator += Time.unscaledDeltaTime * framesPerSecond;
            if (frameAccumulator < 1f)
            {
                return;
            }

            int elapsedFrames = Mathf.FloorToInt(frameAccumulator);
            frameAccumulator -= elapsedFrames;
            frameIndex = (frameIndex + elapsedFrames) % activeFrames.Length;
            ApplyCurrentFrame();
        }

        private void ResolveReferences()
        {
            if (fireImage == null)
            {
                fireImage = GetComponent<Image>();
            }

            if (rhythmSystem == null)
            {
                rhythmSystem = GetComponentInParent<RhythmSystem>(true);
            }
        }

        private void RefreshForCurrentCombo(bool force)
        {
            int combo = rhythmSystem != null ? rhythmSystem.CurrentCombo : 0;
            if (!force && combo == lastCombo)
            {
                return;
            }

            lastCombo = combo;
            Sprite[] nextFrames = SelectFrames(combo);
            if (!force && ReferenceEquals(activeFrames, nextFrames))
            {
                return;
            }

            activeFrames = nextFrames;
            frameIndex = 0;
            frameAccumulator = 0f;

            bool shouldShow = fireImage != null && activeFrames != null && activeFrames.Length > 0;
            if (fireImage != null)
            {
                fireImage.enabled = shouldShow;
                if (shouldShow)
                {
                    ApplyCurrentFrame();
                }
                else
                {
                    fireImage.sprite = null;
                }
            }
        }

        private Sprite[] SelectFrames(int combo)
        {
            if (combo < smallCombo)
            {
                return null;
            }

            if (combo < mediumCombo)
            {
                return smallFrames;
            }

            if (combo < bigCombo)
            {
                return mediumFrames;
            }

            return bigFrames;
        }

        private void ApplyCurrentFrame()
        {
            if (fireImage == null || activeFrames == null || activeFrames.Length == 0)
            {
                return;
            }

            for (int offset = 0; offset < activeFrames.Length; offset++)
            {
                int index = (frameIndex + offset) % activeFrames.Length;
                Sprite frame = activeFrames[index];
                if (frame == null)
                {
                    continue;
                }

                frameIndex = index;
                fireImage.sprite = frame;
                return;
            }

            fireImage.enabled = false;
            fireImage.sprite = null;
        }

#if UNITY_EDITOR
        public bool MatchesEditorConfiguration(
            RhythmSystem expectedRhythmSystem,
            Image expectedImage,
            Sprite[] expectedSmallFrames,
            Sprite[] expectedMediumFrames,
            Sprite[] expectedBigFrames)
        {
            return rhythmSystem == expectedRhythmSystem &&
                fireImage == expectedImage &&
                FramesMatch(smallFrames, expectedSmallFrames) &&
                FramesMatch(mediumFrames, expectedMediumFrames) &&
                FramesMatch(bigFrames, expectedBigFrames) &&
                Mathf.Approximately(framesPerSecond, 12f) &&
                smallCombo == 10 &&
                mediumCombo == 20 &&
                bigCombo == 30;
        }

        public void ConfigureInEditor(
            RhythmSystem targetRhythmSystem,
            Image targetImage,
            Sprite[] targetSmallFrames,
            Sprite[] targetMediumFrames,
            Sprite[] targetBigFrames)
        {
            rhythmSystem = targetRhythmSystem;
            fireImage = targetImage;
            smallFrames = targetSmallFrames;
            mediumFrames = targetMediumFrames;
            bigFrames = targetBigFrames;
            framesPerSecond = 12f;
            smallCombo = 10;
            mediumCombo = 20;
            bigCombo = 30;
            activeFrames = null;
            lastCombo = int.MinValue;
            frameIndex = 0;
            frameAccumulator = 0f;
        }

        private static bool FramesMatch(Sprite[] left, Sprite[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }
#endif
    }
}
