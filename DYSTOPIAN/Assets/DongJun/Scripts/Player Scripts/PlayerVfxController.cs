using System;
using Dystopian.Combat;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController), typeof(CharacterController))]
public sealed class PlayerVfxController : MonoBehaviour
{
    private const int NormalAttackPoolSize = 4;
    private const int ChargedPathPoolSize = 12;
    private const int ReinforcedSlashPoolSize = 16;
    private const int HitPoolSize = 8;
    private const int GroundSmokePoolSize = 6;
    private const string VfxLayerName = "PlayerVFX";
    private const int ForegroundSortingOrder = 1000;


    [Header("VFX Prefabs")]
    [SerializeField] private GameObject normalAttackVfx1;
    [SerializeField] private GameObject normalAttackVfx2;
    [SerializeField] private GameObject chargingVfx;
    [SerializeField] private GameObject chargedPathVfx;
    [SerializeField] private GameObject reinforcedChargedAttackVfx;
    [SerializeField] private GameObject hitVfx;
    [SerializeField] private GameObject groundSmokeVfx;
    [SerializeField] private GameObject landingVfx;


    [Header("Normal Attack")]
    [SerializeField] private Vector3 normalAttackScale = new Vector3(1.2f, 1.2f, 1.2f);
    [SerializeField] private Vector3 reinforcedNormalAttackScale = new Vector3(2f, 1.4f, 1.2f);
    [SerializeField, Min(0.01f)] private float normalAttackDuration = 0.35f;
    [SerializeField] private float normalAttackRotationOffset = 180f;

    [Header("Charged Attack")]
    [SerializeField] private Vector3 fallbackHandOffset = new Vector3(0.55f, 1.25f, -0.25f);
    [SerializeField] private Vector3 chargingScale = new Vector3(0.55f, 0.55f, 0.55f);

    [SerializeField] private float chargedHandHorizontalRotationOffset;
    [SerializeField] private float chargedHandVerticalRotationOffset = 180f;
    [SerializeField, Min(0f)] private float chargedHandDirectionalOffset = 0.45f;
    [SerializeField] private Vector3 chargedHandSlashScale = new Vector3(1.25f, 1.25f, 1.25f);
    [SerializeField] private Color chargedHandSlashColor = new Color(0.15f, 0.65f, 1f, 1f);
    [SerializeField] private Vector3 chargedPathScale = new Vector3(0.35f, 0.35f, 0.35f);
    [SerializeField, Min(0.1f)] private float chargedPathSpacing = 0.75f;
    [SerializeField, Min(0.01f)] private float chargedAttackDuration = 0.45f;
    [SerializeField, Range(1, 16)] private int reinforcedSlashCount = 7;
    [SerializeField] private Vector3 reinforcedSlashScale = Vector3.one;

    [SerializeField, Min(0.01f)] private float reinforcedAttackDuration = 0.12f;
    [SerializeField, Min(0f)] private float reinforcedAttackEmissionDuration = 0.01f;
    [SerializeField, Min(0.01f)] private float reinforcedAttackSpeedMultiplier = 4f;
    [SerializeField, Range(0f, 180f)] private float reinforcedSlashRandomAngle = 35f;

    [Header("Hit")]
    [SerializeField] private Vector3 hitScale = new Vector3(1.6f, 1.6f, 1.6f);
    [SerializeField, Min(0.01f)] private float hitDuration = 0.4f;

    [Header("Ground Effects")]
    [SerializeField] private Vector3 jumpSmokeScale = new Vector3(0.5f, 0.5f, 0.5f);
    [SerializeField, Min(0f)] private float jumpSmokeEmissionDuration = 0.05f;
    [SerializeField, Min(0.01f)] private float jumpSmokeSpeedMultiplier = 2f;

    [SerializeField, Min(0.01f)] private float landingSmokeSpeedMultiplier = 4f;
    [SerializeField] private Vector3 landingSmokeScale = new Vector3(0.75f, 0.5f, 0.75f);
    [SerializeField, Min(0.01f)] private float jumpSmokeDuration = 0.65f;
    [SerializeField, Min(0.01f)] private float landingSmokeDuration = 0.22f;
    [SerializeField, Min(0f)] private float landingEmissionDuration = 0.02f;

    [Header("World Placement")]
    [SerializeField] private float effectZ = -0.5f;
    [SerializeField] private float footHeightOffset = 0.12f;

    private PlayerController playerController;
    private CharacterController characterController;
    private Transform handTransform;
    private GameObject poolRoot;
    private VfxPool normalAttackPool1;
    private VfxPool normalAttackPool2;
    private VfxPool chargingPool;
    private VfxPool chargedPathPool;
    private VfxPool reinforcedSlashPool;
    private VfxPool hitPool;
    private VfxPool groundSmokePool;
    private VfxPool landingPool;

    private VfxPool[] pools;
    private PooledVfx activeChargingVfx;
    private bool started;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        characterController = GetComponent<CharacterController>();
    }

    private void Start()
    {
        ResolveHandTransform();
        CreatePools();
        Subscribe();
        started = true;
    }

    private void OnEnable()
    {
        if (started)
            Subscribe();
    }

    private void Update()
    {
        UpdatePools();
        UpdateChargingVfx();
    }

    private void LateUpdate()
    {
        if (activeChargingVfx != null && activeChargingVfx.IsActive)
        {
            Vector2 direction = GetFacingDirection();
            activeChargingVfx.SetTransform(
                GetHandPosition(direction),
                GetDirectionRotation(direction),
                chargingScale);
        }

        if (pools == null)
            return;

        for (int i = 0; i < pools.Length; i++)
            pools[i]?.UpdateFollowTransform();
    }

    private void OnDisable()
    {
        Unsubscribe();
        activeChargingVfx = null;

        if (pools == null)
            return;

        for (int i = 0; i < pools.Length; i++)
            pools[i]?.StopAll();
    }

    private void OnDestroy()
    {
        if (poolRoot != null)
            Destroy(poolRoot);
    }

    private void CreatePools()
    {
        int vfxLayer = LayerMask.NameToLayer(VfxLayerName);
        if (vfxLayer < 0)
            vfxLayer = 0;

        poolRoot = new GameObject($"{name} VFX Pool");
        poolRoot.layer = vfxLayer;
        poolRoot.SetActive(false);
        Transform root = poolRoot.transform;

        normalAttackPool1 = new VfxPool(
            normalAttackVfx1,
            NormalAttackPoolSize,
            root,
            vfxLayer);
        normalAttackPool2 = new VfxPool(
            normalAttackVfx2,
            NormalAttackPoolSize,
            root,
            vfxLayer);
        chargingPool = new VfxPool(chargingVfx, 1, root, vfxLayer);
        chargedPathPool = new VfxPool(
            chargedPathVfx,
            ChargedPathPoolSize,
            root,
            vfxLayer);
        reinforcedSlashPool = new VfxPool(
            reinforcedChargedAttackVfx,
            ReinforcedSlashPoolSize,
            root,
            vfxLayer);
        hitPool = new VfxPool(hitVfx, HitPoolSize, root, vfxLayer);
        groundSmokePool = new VfxPool(
            groundSmokeVfx,
            GroundSmokePoolSize,
            root,
            vfxLayer);
        landingPool = new VfxPool(
            landingVfx,
            GroundSmokePoolSize,
            root,
            vfxLayer);

        pools = new[]
        {
            normalAttackPool1,
            normalAttackPool2,
            chargingPool,
            chargedPathPool,
            reinforcedSlashPool,
            hitPool,
            groundSmokePool,
            landingPool
        };

        poolRoot.SetActive(true);
    }

    private void Subscribe()
    {
        if (playerController == null)
            return;

        Unsubscribe();
        playerController.NormalAttackPerformed += HandleNormalAttack;
        playerController.ChargedAttackPerformed += HandleChargedAttack;
        playerController.ChargedAttackAreaActivated += HandleChargedAttackArea;
        playerController.TargetDamaged += HandleTargetDamaged;
        playerController.JumpStarted += HandleJumpStarted;
        playerController.Landed += HandleLanded;
    }

    private void Unsubscribe()
    {
        if (playerController == null)
            return;

        playerController.NormalAttackPerformed -= HandleNormalAttack;
        playerController.ChargedAttackPerformed -= HandleChargedAttack;
        playerController.ChargedAttackAreaActivated -= HandleChargedAttackArea;
        playerController.TargetDamaged -= HandleTargetDamaged;
        playerController.JumpStarted -= HandleJumpStarted;
        playerController.Landed -= HandleLanded;
    }

    private void HandleNormalAttack()
    {
        Vector2 direction = playerController.LastNormalAttackDirection;
        Vector3 position = playerController.LastNormalAttackCenter;
        position.z = effectZ;
        Vector3 scale = playerController.LastNormalAttackWasEmpowered
            ? reinforcedNormalAttackScale
            : normalAttackScale;

        bool isHorizontal =
            Mathf.Abs(direction.x) >= Mathf.Abs(direction.y);
        bool useFirstVfx = isHorizontal
            ? direction.x < 0f
            : direction.y >= 0f;
        Vector2 rotationAxis = isHorizontal
            ? Vector2.right
            : Vector2.up;
        VfxPool pool = useFirstVfx
            ? normalAttackPool1
            : normalAttackPool2;
        float angle = GetDirectionAngle(
            rotationAxis,
            normalAttackRotationOffset);

        PooledVfx effect = pool.Play(
            position,
            Quaternion.Euler(0f, 0f, angle),
            scale,
            normalAttackDuration,
            particleRotationOffsetDegrees: angle);
        effect?.Follow(transform, position - transform.position);
    }

    private void HandleChargedAttack()
    {
        Vector2 direction = GetFacingDirection();
        bool isVertical =
            Mathf.Abs(direction.y) > Mathf.Abs(direction.x);
        Vector3 position = GetHandPosition(direction);
        position += new Vector3(direction.x, direction.y, 0f) *
            chargedHandDirectionalOffset;

        float rotationOffset = isVertical
            ? chargedHandVerticalRotationOffset
            : chargedHandHorizontalRotationOffset;
        float angle = GetDirectionAngle(direction, rotationOffset);
        chargingPool.StopAll();
        activeChargingVfx = null;

        PooledVfx effect = normalAttackPool1.Play(
            position,
            Quaternion.Euler(0f, 0f, angle),
            chargedHandSlashScale,
            chargedAttackDuration,
            chargedHandSlashColor,
            particleRotationOffsetDegrees: angle);
        FollowHand(effect, position);
    }

    private void HandleChargedAttackArea(PlayerController.ChargedAttackArea area)
    {
        if (area.IsReinforced)
        {
            PlayReinforcedChargedAttack(area);
            return;
        }

        PlayChargedPath(area);
    }

    private void PlayReinforcedChargedAttack(
        PlayerController.ChargedAttackArea area)
    {
        Vector3 halfSize = area.Size * 0.5f;
        float baseAngle = area.Rotation.eulerAngles.z;

        for (int i = 0; i < reinforcedSlashCount; i++)
        {
            Vector3 localPosition = new Vector3(
                UnityEngine.Random.Range(-halfSize.x, halfSize.x),
                UnityEngine.Random.Range(-halfSize.y, halfSize.y),
                0f);
            Vector3 position = area.Center + area.Rotation * localPosition;
            position.z = effectZ;
            float angle = baseAngle + UnityEngine.Random.Range(
                -reinforcedSlashRandomAngle,
                reinforcedSlashRandomAngle);

            reinforcedSlashPool.Play(
                position,
                Quaternion.Euler(0f, 0f, angle),
                reinforcedSlashScale,
                reinforcedAttackDuration,
                null,
                reinforcedAttackEmissionDuration,
                reinforcedAttackSpeedMultiplier,
                angle);
        }
    }

    private void HandleTargetDamaged(DamageInfo damageInfo)
    {
        Vector3 position = damageInfo.HitPoint;
        position.z = effectZ;
        hitPool.Play(
            position,
            Quaternion.identity,
            hitScale,
            hitDuration);
    }

    private void HandleJumpStarted()
    {
        groundSmokePool.Play(
            GetFootPosition(),
            Quaternion.identity,
            jumpSmokeScale,
            jumpSmokeDuration,
            null,
            jumpSmokeEmissionDuration,
            jumpSmokeSpeedMultiplier);
    }

    private void HandleLanded()
    {
        landingPool.Play(
            GetFootPosition(),
            Quaternion.identity,
            landingSmokeScale,
            landingSmokeDuration,
            null,
            landingEmissionDuration,
            landingSmokeSpeedMultiplier);
    }

    private void UpdateChargingVfx()
    {
        if (playerController.IsCharging)
        {
            if (activeChargingVfx == null || !activeChargingVfx.IsActive)
            {
                Vector2 direction = GetFacingDirection();
                activeChargingVfx = chargingPool.Play(
                    GetHandPosition(direction),
                    GetDirectionRotation(direction),
                    chargingScale,
                    float.PositiveInfinity);
            }

            return;
        }

        if (activeChargingVfx == null)
            return;

        activeChargingVfx.Stop();
        activeChargingVfx = null;
    }



    private void UpdatePools()
    {
        if (pools == null)
            return;

        float currentTime = Time.time;
        for (int i = 0; i < pools.Length; i++)
            pools[i]?.Update(currentTime);
    }

    private void ResolveHandTransform()
    {
        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null && animator.isHuman)
        {
            handTransform = animator.GetBoneTransform(
                HumanBodyBones.RightRingProximal);
            if (handTransform == null)
            {
                handTransform = animator.GetBoneTransform(
                    HumanBodyBones.RightHand);
            }
        }

        if (handTransform != null)
            return;

        Transform fallbackHand = null;
        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            string childName = children[i].name;
            if (childName.EndsWith(
                "R_HandRing1",
                StringComparison.OrdinalIgnoreCase))
            {
                handTransform = children[i];
                return;
            }

            if (childName.EndsWith(
                    "RightHand",
                    StringComparison.OrdinalIgnoreCase) ||
                childName.EndsWith(
                    "R_Hand",
                    StringComparison.OrdinalIgnoreCase))
            {
                fallbackHand = children[i];
            }
        }

        handTransform = fallbackHand;
    }

    private Vector3 GetHandPosition(Vector2 direction)
    {
        Vector3 position;
        if (handTransform != null)
        {
            position = handTransform.position;
        }
        else
        {
            Vector3 offset = fallbackHandOffset;
            offset.x *= direction.x < 0f ? -1f : 1f;
            position = transform.TransformPoint(offset);
        }

        position.z = effectZ;
        return position;
    }

    private void FollowHand(PooledVfx effect, Vector3 position)
    {
        if (effect == null)
            return;

        Transform target = handTransform != null ? handTransform : transform;
        effect.Follow(target, position - target.position);
    }


    private Vector3 GetFootPosition()
    {
        Bounds bounds = characterController.bounds;
        Vector3 position = new Vector3(bounds.center.x, bounds.min.y + footHeightOffset, effectZ);
        return position;
    }

    private Vector2 GetFacingDirection()
    {
        Vector2 direction = playerController.FacingDirection;
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
    }

    private static Quaternion GetDirectionRotation(
        Vector2 direction,
        float offset = 0f)
    {
        return Quaternion.Euler(
            0f,
            0f,
            GetDirectionAngle(direction, offset));
    }

    private sealed class VfxPool
    {
        private readonly PooledVfx[] items_;
        private int nextIndex_;

        public VfxPool(
            GameObject prefab,
            int size,
            Transform root,
            int layer)
        {
            if (prefab == null || size <= 0)
            {
                items_ = Array.Empty<PooledVfx>();
                return;
            }

            items_ = new PooledVfx[size];
            for (int i = 0; i < size; i++)
            {
                GameObject instance =
                    UnityEngine.Object.Instantiate(prefab, root);
                instance.name = $"{prefab.name} [{i}]";
                items_[i] = new PooledVfx(instance, layer);
            }
        }

        public PooledVfx Play(
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            float duration,
            Color? tint = null,
            float emissionDuration = -1f,
            float simulationSpeedMultiplier = 1f,
            float particleRotationOffsetDegrees = 0f)
        {
            if (items_.Length == 0)
                return null;

            PooledVfx item = items_[nextIndex_];
            nextIndex_ = (nextIndex_ + 1) % items_.Length;
            item.Play(
                position,
                rotation,
                scale,
                duration,
                tint,
                emissionDuration,
                simulationSpeedMultiplier,
                particleRotationOffsetDegrees);
            return item;
        }

        public void Update(float currentTime)
        {
            for (int i = 0; i < items_.Length; i++)
                items_[i].Update(currentTime);
        }

        public void UpdateFollowTransform()
        {
            for (int i = 0; i < items_.Length; i++)
                items_[i].UpdateFollowTransform();
        }


        public void StopAll()
        {
            for (int i = 0; i < items_.Length; i++)
                items_[i].Stop();
        }
    }

    private sealed class PooledVfx
    {
        private readonly GameObject gameObject_;
        private readonly Transform transform_;
        private readonly ParticleSystem[] particleSystems_;
        private readonly ParticleSystem.MinMaxCurve[] originalStartRotations_;
        private readonly ParticleSystem.MinMaxGradient[] originalStartColors_;
        private readonly float[] originalSimulationSpeeds_;
        private Transform followTarget_;
        private Vector3 followOffset_;
        private float stopEmissionTime_;
        private float disableTime_;
        private bool emissionStopped_;

        public bool IsActive => gameObject_.activeSelf;

        public PooledVfx(
            GameObject gameObject,
            int layer)
        {
            gameObject_ = gameObject;
            transform_ = gameObject.transform;
            SetLayerRecursively(transform_, layer);
            particleSystems_ =
                gameObject.GetComponentsInChildren<ParticleSystem>(true);

            AudioSource[] audioSources =
                gameObject.GetComponentsInChildren<AudioSource>(true);
            for (int i = 0; i < audioSources.Length; i++)
            {
                audioSources[i].Stop();
                audioSources[i].enabled = false;
            }

            ParticleSystemRenderer[] renderers =
                gameObject.GetComponentsInChildren<ParticleSystemRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].sortingOrder = ForegroundSortingOrder;

            originalStartColors_ =
                new ParticleSystem.MinMaxGradient[particleSystems_.Length];
            originalStartRotations_ =
                new ParticleSystem.MinMaxCurve[particleSystems_.Length];
            originalSimulationSpeeds_ = new float[particleSystems_.Length];

            for (int i = 0; i < particleSystems_.Length; i++)
            {
                ParticleSystem.MainModule main = particleSystems_[i].main;
                originalStartColors_[i] = main.startColor;
                originalStartRotations_[i] = main.startRotation;
                originalSimulationSpeeds_[i] = main.simulationSpeed;
            }

            gameObject_.SetActive(false);
        }

        public void Play(
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            float duration,
            Color? tint,
            float emissionDuration,
            float simulationSpeedMultiplier,
            float particleRotationOffsetDegrees)
        {
            gameObject_.SetActive(false);
            followTarget_ = null;
            followOffset_ = Vector3.zero;
            SetTransform(position, rotation, scale);
            gameObject_.SetActive(true);

            float speedMultiplier = Mathf.Max(0.01f, simulationSpeedMultiplier);
            float rotationOffset =
                particleRotationOffsetDegrees * Mathf.Deg2Rad;
            for (int i = 0; i < particleSystems_.Length; i++)
            {
                ParticleSystem particleSystem = particleSystems_[i];
                particleSystem.Stop(
                    false,
                    ParticleSystemStopBehavior.StopEmittingAndClear);
                ParticleSystem.MainModule main = particleSystem.main;
                main.startColor = tint.HasValue
                    ? tint.Value
                    : originalStartColors_[i];
                main.startRotation = OffsetRotation(
                    originalStartRotations_[i],
                    rotationOffset);
                main.simulationSpeed =
                    originalSimulationSpeeds_[i] * speedMultiplier;
                particleSystem.Play(false);
            }

            float currentTime = Time.time;
            stopEmissionTime_ = emissionDuration >= 0f
                ? currentTime + emissionDuration
                : float.PositiveInfinity;
            disableTime_ = float.IsPositiveInfinity(duration)
                ? float.PositiveInfinity
                : currentTime + Mathf.Max(0.01f, duration);
            emissionStopped_ = false;
        }

        public void SetTransform(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            transform_.SetPositionAndRotation(position, rotation);
            transform_.localScale = scale;
        }

        public void Follow(Transform target, Vector3 worldOffset)
        {
            followTarget_ = target;
            followOffset_ = worldOffset;
        }

        public void UpdateFollowTransform()
        {
            if (!gameObject_.activeSelf || followTarget_ == null)
                return;

            Vector3 position = followTarget_.position + followOffset_;
            position.z = transform_.position.z;
            transform_.position = position;
        }


        public void Update(float currentTime)
        {
            if (!gameObject_.activeSelf)
                return;

            if (!emissionStopped_ && currentTime >= stopEmissionTime_)
            {
                for (int i = 0; i < particleSystems_.Length; i++)
                    particleSystems_[i].Stop(false, ParticleSystemStopBehavior.StopEmitting);

                emissionStopped_ = true;
            }

            if (currentTime >= disableTime_)
                Stop();
        }

        public void Stop()
        {
            if (gameObject_ == null)
                return;

            if (!gameObject_.activeSelf)
                return;

            for (int i = 0; i < particleSystems_.Length; i++)
            {
                particleSystems_[i].Stop(
                    false,
                    ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            followTarget_ = null;
            followOffset_ = Vector3.zero;
            gameObject_.SetActive(false);
            stopEmissionTime_ = 0f;
            disableTime_ = 0f;
            emissionStopped_ = false;
        }

        private static ParticleSystem.MinMaxCurve OffsetRotation(
            ParticleSystem.MinMaxCurve source,
            float offset)
        {
            switch (source.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    return new ParticleSystem.MinMaxCurve(
                        source.constant + offset);

                case ParticleSystemCurveMode.TwoConstants:
                    return new ParticleSystem.MinMaxCurve(
                        source.constantMin + offset,
                        source.constantMax + offset);

                case ParticleSystemCurveMode.Curve:
                    if (TryGetConstantCurve(source.curve, out float value))
                    {
                        return new ParticleSystem.MinMaxCurve(
                            value * source.curveMultiplier + offset);
                    }

                    return new ParticleSystem.MinMaxCurve(
                        1f,
                        CreateOffsetCurve(
                            source.curve,
                            source.curveMultiplier,
                            offset));

                case ParticleSystemCurveMode.TwoCurves:
                    bool constantMin =
                        TryGetConstantCurve(source.curveMin, out float min);
                    bool constantMax =
                        TryGetConstantCurve(source.curveMax, out float max);
                    if (constantMin && constantMax)
                    {
                        return new ParticleSystem.MinMaxCurve(
                            min * source.curveMultiplier + offset,
                            max * source.curveMultiplier + offset);
                    }

                    return new ParticleSystem.MinMaxCurve(
                        1f,
                        CreateOffsetCurve(
                            source.curveMin,
                            source.curveMultiplier,
                            offset),
                        CreateOffsetCurve(
                            source.curveMax,
                            source.curveMultiplier,
                            offset));

                default:
                    return source;
            }
        }

        private static bool TryGetConstantCurve(
            AnimationCurve curve,
            out float value)
        {
            value = 0f;
            if (curve == null || curve.length == 0)
                return true;

            value = curve[0].value;
            for (int i = 1; i < curve.length; i++)
            {
                if (!Mathf.Approximately(curve[i].value, value))
                    return false;
            }

            return true;
        }

        private static AnimationCurve CreateOffsetCurve(
            AnimationCurve source,
            float multiplier,
            float offset)
        {
            if (source == null || source.length == 0)
                return AnimationCurve.Constant(0f, 1f, offset);

            Keyframe[] keys = source.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                Keyframe key = keys[i];
                key.value = key.value * multiplier + offset;
                key.inTangent *= multiplier;
                key.outTangent *= multiplier;
                keys[i] = key;
            }

            AnimationCurve result = new AnimationCurve(keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
            return result;
        }

    }

    private void PlayChargedPath(PlayerController.ChargedAttackArea area)
    {
        int effectCount = Mathf.Clamp(
            Mathf.CeilToInt(area.Size.x / chargedPathSpacing) + 1,
            2,
            ChargedPathPoolSize);
        float halfLength = area.Size.x * 0.5f;

        for (int i = 0; i < effectCount; i++)
        {
            float progress = effectCount > 1 ? i / (float)(effectCount - 1) : 0.5f;
            Vector3 localPosition = new Vector3(Mathf.Lerp(-halfLength, halfLength, progress), 0f, 0f);
            Vector3 position = area.Center + area.Rotation * localPosition;
            position.z = effectZ;
            chargedPathPool.Play(
                position,
                area.Rotation,
                chargedPathScale,
                chargedAttackDuration);
        }
    }





    private static void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        for (int i = 0; i < root.childCount; i++)
            SetLayerRecursively(root.GetChild(i), layer);
    }


    private static float GetDirectionAngle(
        Vector2 direction,
        float offset = 0f)
    {
        return Mathf.Atan2(direction.y, direction.x) *
            Mathf.Rad2Deg + offset;
    }
}

