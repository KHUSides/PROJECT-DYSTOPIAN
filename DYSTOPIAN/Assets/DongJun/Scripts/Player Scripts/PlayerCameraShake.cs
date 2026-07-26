using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController))]
public sealed class PlayerCameraShake : MonoBehaviour
{
    [Header("Enemy Defeated Shake")]
    [SerializeField, Min(0f)] private float defeatedDuration = 0.18f;
    [SerializeField] private Vector2 defeatedStrength = new Vector2(0.24f, 0.16f);
    [SerializeField, Min(1)] private int defeatedVibrato = 14;
    [SerializeField, Range(0f, 180f)] private float defeatedRandomness = 90f;
    [SerializeField] private bool defeatedFadeOut = true;

    [Header("Reinforced Attack Shake")]
    [SerializeField, Min(0f)] private float reinforcedDuration = 0.1f;
    [SerializeField] private Vector2 reinforcedStrength = new Vector2(0.08f, 0.05f);
    [SerializeField, Min(1)] private int reinforcedVibrato = 8;
    [SerializeField, Range(0f, 180f)] private float reinforcedRandomness = 70f;
    [SerializeField] private bool reinforcedFadeOut = true;

    private PlayerController playerController;
    private SideViewCameraFollow cameraFollow;
    private Tween activeShake;
    private Vector3 shakeOffset;
    private int shakeVersion;
    private bool isDefeatedShakeActive;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        playerController.TargetDefeated += HandleTargetDefeated;
        playerController.ReinforcedAttackPerformed += HandleReinforcedAttack;
    }

    private void Start()
    {
        cameraFollow = FindFirstObjectByType<SideViewCameraFollow>();
        if (cameraFollow == null)
        {
            Debug.LogError("[PlayerCameraShake] SideViewCameraFollow is required in the scene.", this);
            enabled = false;
        }
    }

    private void OnDisable()
    {
        playerController.TargetDefeated -= HandleTargetDefeated;
        playerController.ReinforcedAttackPerformed -= HandleReinforcedAttack;
        StopShake();
    }

    private void HandleTargetDefeated()
    {
        PlayShake(
            defeatedDuration,
            defeatedStrength,
            defeatedVibrato,
            defeatedRandomness,
            defeatedFadeOut,
            true);
    }

    private void HandleReinforcedAttack()
    {
        if (isDefeatedShakeActive)
            return;

        PlayShake(
            reinforcedDuration,
            reinforcedStrength,
            reinforcedVibrato,
            reinforcedRandomness,
            reinforcedFadeOut,
            false);
    }

    private void PlayShake(
        float duration,
        Vector2 strength,
        int vibrato,
        float randomness,
        bool fadeOut,
        bool isDefeatedShake)
    {
        if (cameraFollow == null || duration <= 0f)
            return;

        int version = ++shakeVersion;
        activeShake?.Kill(false);
        isDefeatedShakeActive = isDefeatedShake;
        shakeOffset = Vector3.zero;
        cameraFollow.SetShakeOffset(Vector3.zero);

        activeShake = DOTween.Shake(
                () => shakeOffset,
                value =>
                {
                    shakeOffset = value;
                    cameraFollow.SetShakeOffset(value);
                },
                duration,
                new Vector3(strength.x, strength.y, 0f),
                Mathf.Max(1, vibrato),
                randomness,
                fadeOut,
                ShakeRandomnessMode.Full)
            .OnComplete(() => FinishShake(version))
            .OnKill(() => FinishShake(version));
    }

    private void StopShake()
    {
        ++shakeVersion;
        activeShake?.Kill(false);
        activeShake = null;
        isDefeatedShakeActive = false;
        shakeOffset = Vector3.zero;

        if (cameraFollow != null)
            cameraFollow.SetShakeOffset(Vector3.zero);
    }

    private void FinishShake(int version)
    {
        if (version != shakeVersion)
            return;

        activeShake = null;
        isDefeatedShakeActive = false;
        shakeOffset = Vector3.zero;
        if (cameraFollow != null)
            cameraFollow.SetShakeOffset(Vector3.zero);
    }
}
