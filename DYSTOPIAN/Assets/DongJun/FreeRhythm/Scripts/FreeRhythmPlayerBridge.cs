using Dystopian.Combat;
using UnityEngine;

namespace Dystopian.FreeRhythm
{
    [RequireComponent(typeof(FreeRhythmSystem))]
    public sealed class FreeRhythmPlayerBridge : MonoBehaviour
    {
        [SerializeField] PlayerController player;
        [SerializeField] PlayerHealth health;
        [SerializeField, Min(0)] int perfectDamageBonus = 10;
        [SerializeField, Min(0)] int holdStrikeDamageBonus = 5;
        FreeRhythmSystem rhythm;
        int previousHealth;
        long holdStart = -1;
        int holdLane;

        void OnEnable()
        {
            rhythm = GetComponent<FreeRhythmSystem>();
            if (player == null) player = FindFirstObjectByType<PlayerController>();
            if (health == null && player != null) health = player.GetComponent<PlayerHealth>();
            if (player == null) { Debug.LogError("Free Rhythm requires a PlayerController in its test scene.", this); enabled = false; return; }
            rhythm.ActionRecognized += Execute;
            rhythm.BeatFinalized += OnBeat;
            rhythm.InputAccepted += OnBeat;
            rhythm.SequenceReset += Cancel;
            player.TargetDamaged += OnDamage;
            if (health != null) previousHealth = health.CurrentHealth;
        }
        void Update()
        {
            if (health == null) return;
            if (health.CurrentHealth < previousHealth) rhythm.ResetCombo();
            previousHealth = health.CurrentHealth;
        }
        void Execute(ActionResult result)
        {
            if (player == null) return;
            int savedBonus = player.AttackPowerBonus;
            int actionBonus = result.Action == RhythmAction.HoldStrike ? holdStrikeDamageBonus : 0;
            player.SetAttackPowerBonus(savedBonus + actionBonus + (result.Critical ? perfectDamageBonus : 0));
            try
            {
                switch (result.Action)
                {
                    case RhythmAction.ChargedAttack:
                        if (!player.IsCharging) player.BeginChargedAttack();
                        player.ReleaseChargedAttack();
                        break;
                    case RhythmAction.GuardAttack:
                        player.CancelChargedAttack();
                        player.PerformNormalAttack();
                        break;
                    default:
                        player.CancelChargedAttack();
                        player.PerformNormalAttack();
                        break;
                }
            }
            finally { player.SetAttackPowerBonus(savedBonus); }
        }
        void OnBeat(BeatInput input)
        {
            if (player == null) return;
            if (input.IsTap && (input.Held & input.Pressed) != 0) { holdStart = input.Tick; holdLane = input.Pressed; }
            if (input.Released != 0)
            {
                if (input.Released == holdLane && input.Tick - holdStart == 1 && health != null) health.EnableSoundBarrier();
                holdStart = -1; holdLane = 0;
            }
            if (input.Held != 0 && input.Pressed == 0 && !player.IsCharging) player.BeginChargedAttack();
            if (input.Held == 0 && player.IsCharging) player.CancelChargedAttack();
        }
        void OnDamage(DamageInfo damage) { rhythm.RegisterDamage(damage.Amount); }
        void Cancel() { holdStart = -1; holdLane = 0; if (player != null) { player.CancelChargedAttack(); player.CancelPendingChargedAttackRepeats(); } }
        void OnDisable()
        {
            if (rhythm != null)
            { rhythm.ActionRecognized -= Execute; rhythm.BeatFinalized -= OnBeat; rhythm.InputAccepted -= OnBeat; rhythm.SequenceReset -= Cancel; }
            if (player != null) player.TargetDamaged -= OnDamage;
            Cancel();
        }
    }
}
