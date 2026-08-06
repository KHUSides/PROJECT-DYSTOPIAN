using System.Collections;
using Dystopian.Rhythm;
using TMPro;
using UnityEngine;

namespace Dystopian.SuEun.StageFlow
{
    /// <summary>
    /// Connects sub-stage lifecycle events to the existing RhythmSystem without
    /// requiring changes to the rhythm implementation.
    /// </summary>
    public sealed class StageRhythmController : MonoBehaviour
    {
        [SerializeField] private StageFlowManager manager_;
        [SerializeField] private RhythmSystem rhythmSystem_;
        [SerializeField, Min(0.1f)] private float countdownStepSeconds_ = 1f;
        [SerializeField] private bool showCountdown_ = true;
        [SerializeField] private int countdownFontSize_ = 96;

        private Coroutine countdownCoroutine_;
        private int countdownNumber_;
        private GUIStyle countdownStyle_;
        private TextMeshProUGUI judgementText_;
        private Transform rhythmUiRoot_;

        private void Start()
        {
            if (manager_ == null)
                manager_ = StageFlowManager.Instance;

            if (rhythmSystem_ == null)
                rhythmSystem_ = FindFirstObjectByType<RhythmSystem>();

            if (manager_ == null || rhythmSystem_ == null)
            {
                Debug.LogError("[StageRhythm] StageFlowManager or RhythmSystem is missing.", this);
                enabled = false;
                return;
            }

            manager_.BattleStarted += HandleBattleStarted;
            manager_.BattleCleared += HandleBattleCleared;
            rhythmSystem_.StopChart();
            SetRhythmUiVisible(false);
            ClearJudgementText();
        }

        private void OnDisable()
        {
            if (manager_ != null)
            {
                manager_.BattleStarted -= HandleBattleStarted;
                manager_.BattleCleared -= HandleBattleCleared;
            }

            CancelCountdown();
            rhythmSystem_?.StopChart();
            SetRhythmUiVisible(false);
            ClearJudgementText();
        }

        private void HandleBattleStarted(BattleZone zone)
        {
            CancelCountdown();
            rhythmSystem_.StopChart();
            ClearJudgementText();
            SetRhythmUiVisible(true);
            countdownCoroutine_ = StartCoroutine(StartRhythmAfterCountdown());
        }

        private void HandleBattleCleared(BattleZone zone)
        {
            CancelCountdown();
            rhythmSystem_.StopChart();
            SetRhythmUiVisible(false);
            ClearJudgementText();
        }

        private IEnumerator StartRhythmAfterCountdown()
        {
            for (int number = 3; number >= 1; number--)
            {
                countdownNumber_ = number;
                yield return new WaitForSeconds(countdownStepSeconds_);
            }

            countdownNumber_ = 0;
            countdownCoroutine_ = null;
            rhythmSystem_.StartChart();
        }

        private void CancelCountdown()
        {
            if (countdownCoroutine_ != null)
            {
                StopCoroutine(countdownCoroutine_);
                countdownCoroutine_ = null;
            }

            countdownNumber_ = 0;
        }

        private void ClearJudgementText()
        {
            CacheRhythmUiReferences();

            if (judgementText_ == null)
                return;

            judgementText_.text = string.Empty;
            judgementText_.gameObject.SetActive(false);
        }

        private void SetRhythmUiVisible(bool visible)
        {
            CacheRhythmUiReferences();

            if (rhythmUiRoot_ != null)
                rhythmUiRoot_.gameObject.SetActive(visible);
        }

        private void CacheRhythmUiReferences()
        {
            if (rhythmUiRoot_ == null && rhythmSystem_ != null)
            {
                rhythmUiRoot_ = rhythmSystem_.transform.Find("Rhythm UI");
            }

            if (judgementText_ == null)
            {
                judgementText_ = rhythmUiRoot_ != null
                    ? rhythmUiRoot_.Find("Judgement Text")?.GetComponent<TextMeshProUGUI>()
                    : null;
            }
        }

        private void OnGUI()
        {
            if (!showCountdown_ || countdownNumber_ <= 0)
                return;

            if (countdownStyle_ == null)
            {
                countdownStyle_ = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = countdownFontSize_,
                    fontStyle = FontStyle.Bold
                };
            }

            GUI.Label(new Rect(0f, 0f, Screen.width, Screen.height), countdownNumber_.ToString(), countdownStyle_);
        }
    }
}
