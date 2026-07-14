using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Dystopian.SuEun.StageFlow
{
    public class StageFlowExternalBridge : MonoBehaviour
    {
        [FormerlySerializedAs("manager")]
        [SerializeField] private StageFlowManager manager_;

        [Header("Rhythm Requests")]
        [FormerlySerializedAs("onRhythmStartRequested")]
        [SerializeField] private BattleZoneEvent onRhythmStartRequested_;
        [FormerlySerializedAs("onRhythmStopRequested")]
        [SerializeField] private BattleZoneEvent onRhythmStopRequested_;

        [Header("HUD Requests")]
        [FormerlySerializedAs("onHudShowRequested")]
        [SerializeField] private BattleZoneEvent onHudShowRequested_;
        [FormerlySerializedAs("onHudHideRequested")]
        [SerializeField] private UnityEvent onHudHideRequested_;
        [FormerlySerializedAs("onRemainingEnemyCountRequested")]
        [SerializeField] private UnityEvent<int> onRemainingEnemyCountRequested_;
        [FormerlySerializedAs("onZoneRemainingEnemyCountRequested")]
        [SerializeField] private BattleZoneEnemyCountEvent onZoneRemainingEnemyCountRequested_;

        [Header("Music Requests")]
        [FormerlySerializedAs("onCombatMusicRequested")]
        [SerializeField] private BattleZoneEvent onCombatMusicRequested_;
        [FormerlySerializedAs("onExplorationMusicRequested")]
        [SerializeField] private BattleZoneEvent onExplorationMusicRequested_;

        [Header("Camera Requests")]
        [FormerlySerializedAs("onCameraBoundsRequested")]
        [SerializeField] private BattleZoneEvent onCameraBoundsRequested_;
        [FormerlySerializedAs("onCameraBoundsReleased")]
        [SerializeField] private BattleZoneEvent onCameraBoundsReleased_;

        private bool isBound_;

        private void Start()
        {
            if (manager_ == null)
                manager_ = StageFlowManager.Instance;

            Bind();
        }

        private void OnDisable()
        {
            Unbind();
        }

        public void Bind()
        {
            if (isBound_ || manager_ == null)
                return;

            manager_.BattleStarted += HandleBattleStarted;
            manager_.BattleCleared += HandleBattleCleared;
            manager_.RemainingEnemyCountChanged += HandleRemainingEnemyCountChanged;
            isBound_ = true;
        }

        public void Unbind()
        {
            if (!isBound_ || manager_ == null)
                return;

            manager_.BattleStarted -= HandleBattleStarted;
            manager_.BattleCleared -= HandleBattleCleared;
            manager_.RemainingEnemyCountChanged -= HandleRemainingEnemyCountChanged;
            isBound_ = false;
        }

        private void HandleBattleStarted(BattleZone zone)
        {
            onRhythmStartRequested_?.Invoke(zone);
            onHudShowRequested_?.Invoke(zone);
            onCombatMusicRequested_?.Invoke(zone);
            onCameraBoundsRequested_?.Invoke(zone);
        }

        private void HandleBattleCleared(BattleZone zone)
        {
            onRhythmStopRequested_?.Invoke(zone);
            onHudHideRequested_?.Invoke();
            onExplorationMusicRequested_?.Invoke(zone);
            onCameraBoundsReleased_?.Invoke(zone);
        }

        private void HandleRemainingEnemyCountChanged(BattleZone zone, int remainingEnemyCount)
        {
            onRemainingEnemyCountRequested_?.Invoke(remainingEnemyCount);
            onZoneRemainingEnemyCountRequested_?.Invoke(zone, remainingEnemyCount);
        }
    }
}
