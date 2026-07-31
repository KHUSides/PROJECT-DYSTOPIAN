using UnityEngine;

namespace Dystopian.EnemyTest
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BossController))]
    [RequireComponent(typeof(BossHealth))]
    [RequireComponent(typeof(MerlinBossCombat))]
    public sealed class BossAnimationController : MonoBehaviour
    {
        private const string IdleStateName = "Idle";
        private const string IntroStateName = "Intro";
        private const string ThrustStateName = "SpearThrust";
        private const string SwingStateName = "SpearSwing";
        private const string ChargeStateName = "SpearCharge";
        private const string ThrowStateName = "SpearThrow";
        private const string HitStateName = "Hit";
        private const string DeathStateName = "Death";

        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private BossController bossController;
        [SerializeField] private BossHealth bossHealth;
        [SerializeField] private MerlinBossCombat merlinCombat;

        [Header("Playback")]
        [SerializeField, Min(0f)] private float crossFadeSeconds = 0.04f;
        [SerializeField, Min(0.01f)] private float normalSpeed = 1f;
        [SerializeField, Min(0.01f)] private float attackSpeed = 1.5f;
        [SerializeField, Min(0.01f)] private float hitSpeed = 1.2f;
        [SerializeField, Range(0f, 0.95f)] private float thrustStartNormalizedTime = 0.18f;
        [SerializeField, Range(0f, 0.95f)] private float swingStartNormalizedTime = 0.22f;
        [SerializeField, Range(0f, 0.95f)] private float chargeHoldNormalizedTime = 0.2f;
        [SerializeField, Range(0f, 0.95f)] private float throwStartNormalizedTime = 0.14f;

        [Header("Death Grounding")]
        [SerializeField] private bool alignDeathPoseToGround = true;
        [SerializeField] private Transform deathVisualRoot;
        [SerializeField] private LayerMask deathGroundMask;
        [SerializeField, Min(0.1f)] private float deathGroundRayStartHeight = 4f;
        [SerializeField, Min(0.1f)] private float deathGroundRayDistance = 8f;
        [SerializeField] private float deathGroundOffset;
        [SerializeField, Min(0.01f)] private float maxDeathGroundCorrection = 1.5f;
        [SerializeField] private bool useDeathSupportBones = true;

        private bool subscribed;
        private string currentStateName;

        private void Reset()
        {
            CacheReferences();
        }

        private void Awake()
        {
            CacheReferences();
        }

        private void OnEnable()
        {
            CacheReferences();
            Subscribe();
        }

        private void Start()
        {
            CacheReferences();
            Subscribe();
            ConfigureAnimator();
            PlayStateImmediately(IntroStateName, 0f, normalSpeed);
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void LateUpdate()
        {
            if (alignDeathPoseToGround && bossHealth != null && !bossHealth.IsAlive)
            {
                AlignDeathPoseToGround();
            }
        }

        private void HandleStateChanged(BossState state)
        {
            if (animator == null)
            {
                return;
            }

            switch (state)
            {
                case BossState.Intro:
                    PlayState(IntroStateName, normalSpeed);
                    break;
                case BossState.Idle:
                    PlayState(IdleStateName, normalSpeed);
                    break;
                case BossState.Dead:
                    PlayStateImmediately(DeathStateName, 0f, normalSpeed);
                    break;
            }
        }

        private void HandleThrustStarted()
        {
            PlayStateImmediately(ThrustStateName, thrustStartNormalizedTime, attackSpeed);
        }

        private void HandleSwingStarted()
        {
            PlayStateImmediately(SwingStateName, swingStartNormalizedTime, attackSpeed);
        }

        private void HandleLongChargeStarted()
        {
            PlayStateImmediately(ChargeStateName, chargeHoldNormalizedTime, 0f);
        }

        private void HandleLongChargeReleased()
        {
            PlayStateImmediately(ThrowStateName, throwStartNormalizedTime, attackSpeed);
        }

        private void HandleLongChargeCancelled()
        {
            PlayState(IdleStateName, normalSpeed);
        }

        private void HandleDamaged(BossHealth _)
        {
            if (bossHealth == null || !bossHealth.IsAlive)
            {
                return;
            }

            PlayStateImmediately(HitStateName, 0f, hitSpeed);
        }

        private void HandleDied(BossHealth _)
        {
            PlayStateImmediately(DeathStateName, 0f, normalSpeed);
        }

        private void ConfigureAnimator()
        {
            if (animator == null)
            {
                return;
            }

            animator.applyRootMotion = false;
            animator.speed = normalSpeed;
            currentStateName = string.Empty;
            InitializeDeathGroundMaskIfNeeded();
        }

        private bool PlayState(string stateName, float speed)
        {
            if (animator == null || string.IsNullOrEmpty(stateName) || currentStateName == stateName)
            {
                return false;
            }

            if (!TryGetStateHash(stateName, out int stateHash))
            {
                return false;
            }

            animator.speed = speed;
            animator.CrossFadeInFixedTime(stateHash, crossFadeSeconds, 0, 0f);
            currentStateName = stateName;
            return true;
        }

        private bool PlayStateImmediately(string stateName, float normalizedTime, float speed)
        {
            if (animator == null || string.IsNullOrEmpty(stateName))
            {
                return false;
            }

            if (!TryGetStateHash(stateName, out int stateHash))
            {
                return false;
            }

            animator.speed = speed;
            animator.Play(stateHash, 0, Mathf.Clamp01(normalizedTime));
            animator.Update(0f);
            currentStateName = stateName;
            return true;
        }

        private bool TryGetStateHash(string stateName, out int stateHash)
        {
            stateHash = Animator.StringToHash(stateName);
            if (animator.HasState(0, stateHash))
            {
                return true;
            }

            stateHash = Animator.StringToHash("Base Layer." + stateName);
            return animator.HasState(0, stateHash);
        }

        private void CacheReferences()
        {
            if (bossController == null)
            {
                bossController = GetComponent<BossController>();
            }

            if (bossHealth == null)
            {
                bossHealth = GetComponent<BossHealth>();
            }

            if (merlinCombat == null)
            {
                merlinCombat = GetComponent<MerlinBossCombat>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }

            if (deathVisualRoot == null && animator != null)
            {
                deathVisualRoot = animator.transform.parent != null ? animator.transform.parent : animator.transform;
            }

            InitializeDeathGroundMaskIfNeeded();
        }

        private void InitializeDeathGroundMaskIfNeeded()
        {
            if (deathGroundMask.value != 0)
            {
                return;
            }

            int environmentLayer = LayerMask.NameToLayer("Environment");
            if (environmentLayer >= 0)
            {
                deathGroundMask = 1 << environmentLayer;
            }
        }

        private void AlignDeathPoseToGround()
        {
            if (deathVisualRoot == null)
            {
                return;
            }

            if (!TryGetDeathSupportMinY(out float supportMinY))
            {
                return;
            }

            float groundY = GetGroundY();
            float desiredMinY = groundY + deathGroundOffset;
            float correction = desiredMinY - supportMinY;
            correction = Mathf.Clamp(correction, -maxDeathGroundCorrection, maxDeathGroundCorrection);

            if (Mathf.Abs(correction) <= 0.002f)
            {
                return;
            }

            deathVisualRoot.position += Vector3.up * correction;
        }

        private bool TryGetDeathSupportMinY(out float minY)
        {
            if (useDeathSupportBones && animator != null)
            {
                bool hasBone = false;
                minY = float.MaxValue;
                AddSupportBoneMinY(HumanBodyBones.LeftFoot, ref minY, ref hasBone);
                AddSupportBoneMinY(HumanBodyBones.RightFoot, ref minY, ref hasBone);
                AddSupportBoneMinY(HumanBodyBones.LeftLowerLeg, ref minY, ref hasBone);
                AddSupportBoneMinY(HumanBodyBones.RightLowerLeg, ref minY, ref hasBone);

                if (hasBone)
                {
                    return true;
                }
            }

            return TryGetVisualBoundsMinY(out minY);
        }

        private void AddSupportBoneMinY(HumanBodyBones bone, ref float minY, ref bool hasBone)
        {
            if (animator == null)
            {
                return;
            }

            Transform boneTransform = animator.GetBoneTransform(bone);
            if (boneTransform == null)
            {
                return;
            }

            minY = Mathf.Min(minY, boneTransform.position.y);
            hasBone = true;
        }

        private bool TryGetVisualBoundsMinY(out float minY)
        {
            Renderer[] renderers = deathVisualRoot.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            Bounds bounds = default;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer candidate = renderers[i];
                if (candidate == null || IsWeaponRenderer(candidate))
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = candidate.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(candidate.bounds);
                }
            }

            minY = hasBounds ? bounds.min.y : 0f;
            return hasBounds;
        }

        private static bool IsWeaponRenderer(Renderer renderer)
        {
            Transform current = renderer.transform;
            while (current != null)
            {
                if (current.name.Contains("Spear") || current.name.Contains("Weapon"))
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private float GetGroundY()
        {
            if (deathGroundMask.value == 0)
            {
                return transform.position.y;
            }

            Vector3 origin = transform.position + Vector3.up * deathGroundRayStartHeight;
            float distance = deathGroundRayStartHeight + deathGroundRayDistance;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, distance, deathGroundMask, QueryTriggerInteraction.Ignore))
            {
                return hit.point.y;
            }

            return transform.position.y;
        }

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            if (bossController != null)
            {
                bossController.StateChanged += HandleStateChanged;
            }

            if (merlinCombat != null)
            {
                merlinCombat.ThrustStarted += HandleThrustStarted;
                merlinCombat.SwingStarted += HandleSwingStarted;
                merlinCombat.LongChargeStarted += HandleLongChargeStarted;
                merlinCombat.LongChargeReleased += HandleLongChargeReleased;
                merlinCombat.LongChargeCancelled += HandleLongChargeCancelled;
            }

            if (bossHealth != null)
            {
                bossHealth.Damaged += HandleDamaged;
                bossHealth.Died += HandleDied;
            }

            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed)
            {
                return;
            }

            if (bossController != null)
            {
                bossController.StateChanged -= HandleStateChanged;
            }

            if (merlinCombat != null)
            {
                merlinCombat.ThrustStarted -= HandleThrustStarted;
                merlinCombat.SwingStarted -= HandleSwingStarted;
                merlinCombat.LongChargeStarted -= HandleLongChargeStarted;
                merlinCombat.LongChargeReleased -= HandleLongChargeReleased;
                merlinCombat.LongChargeCancelled -= HandleLongChargeCancelled;
            }

            if (bossHealth != null)
            {
                bossHealth.Damaged -= HandleDamaged;
                bossHealth.Died -= HandleDied;
            }

            subscribed = false;
        }

        private void OnValidate()
        {
            crossFadeSeconds = Mathf.Max(0f, crossFadeSeconds);
            normalSpeed = Mathf.Max(0.01f, normalSpeed);
            attackSpeed = Mathf.Max(0.01f, attackSpeed);
            hitSpeed = Mathf.Max(0.01f, hitSpeed);
            thrustStartNormalizedTime = Mathf.Clamp01(thrustStartNormalizedTime);
            swingStartNormalizedTime = Mathf.Clamp01(swingStartNormalizedTime);
            chargeHoldNormalizedTime = Mathf.Clamp01(chargeHoldNormalizedTime);
            throwStartNormalizedTime = Mathf.Clamp01(throwStartNormalizedTime);
            deathGroundRayStartHeight = Mathf.Max(0.1f, deathGroundRayStartHeight);
            deathGroundRayDistance = Mathf.Max(0.1f, deathGroundRayDistance);
            maxDeathGroundCorrection = Mathf.Max(0.01f, maxDeathGroundCorrection);
            CacheReferences();
        }
    }
}
