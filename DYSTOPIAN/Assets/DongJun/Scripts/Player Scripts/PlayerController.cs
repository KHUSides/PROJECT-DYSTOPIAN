using System;
using System.Collections.Generic;
using Dystopian.Combat;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    private const int ChargedAttackHitboxPoolSize = 12;

    [Header("Side View Plane")]
    [UnityEngine.Serialization.FormerlySerializedAs("lockedZ_")]
    [SerializeField] private float lockedZ = 0f;

    [Header("Movement")]
    [UnityEngine.Serialization.FormerlySerializedAs("moveSpeed_")]
    [SerializeField] private float moveSpeed = 8f;
    [UnityEngine.Serialization.FormerlySerializedAs("chargeMoveSpeed_")]
    [SerializeField] private float chargeMoveSpeed = 1.5f;

    [Header("Gravity")]
    [UnityEngine.Serialization.FormerlySerializedAs("gravity_")]
    [SerializeField] private float gravity = -24f;
    [UnityEngine.Serialization.FormerlySerializedAs("groundedStickVelocity_")]
    [SerializeField] private float groundedStickVelocity = -2f;

    [Header("Jump")]
    [UnityEngine.Serialization.FormerlySerializedAs("initialJumpVelocity_")]
    [SerializeField] private float initialJumpVelocity = 5.5f;
    [UnityEngine.Serialization.FormerlySerializedAs("jumpHoldAcceleration_")]
    [SerializeField] private float jumpHoldAcceleration = 18f;
    [UnityEngine.Serialization.FormerlySerializedAs("maxJumpHoldTime_")]
    [SerializeField] private float maxJumpHoldTime = 0.22f;
    [UnityEngine.Serialization.FormerlySerializedAs("maxUpwardVelocity_")]
    [SerializeField] private float maxUpwardVelocity = 9.5f;
    [UnityEngine.Serialization.FormerlySerializedAs("allowJumpWhileCharging_")]
    [SerializeField] private bool allowJumpWhileCharging = false;

    [Header("Normal Attack")]
    [UnityEngine.Serialization.FormerlySerializedAs("normalAttackColliderActiveDuration_")]
    [SerializeField] private float normalAttackColliderActiveDuration = 0.25f;
    [SerializeField, Min(1)] private int normalAttackDamage = 10;
    [SerializeField] private LayerMask normalAttackDamageMask = ~0;
    [UnityEngine.Serialization.FormerlySerializedAs("showNormalAttackColliderDebug_")]
    [SerializeField] private bool showNormalAttackColliderDebug = true;
    [UnityEngine.Serialization.FormerlySerializedAs("normalAttackColliderDebugColor_")]
    [SerializeField] private Color normalAttackColliderDebugColor = new Color(1f, 0.1f, 0.1f, 1f);
    [UnityEngine.Serialization.FormerlySerializedAs("normalAttackColliderDebugLineWidth_")]
    [SerializeField] private float normalAttackColliderDebugLineWidth = 0.04f;

    [Header("Attack Reinforce")]
    [SerializeField] private bool enableDistanceChargeUpgrade;
    [SerializeField, Min(0.01f)] private float distanceForFullCharge = 20f;
    [SerializeField, Min(1)] private int empoweredAttackAdditionalDamage = 10;
    [SerializeField] private Color empoweredNormalAttackDebugColor = new Color(1f, 0.85f, 0.05f, 1f);
    [SerializeField] private bool showDistanceChargeReadyDebug = true;

    [Header("Charged Attack")]
    [UnityEngine.Serialization.FormerlySerializedAs("dashObstacleMask_")]
    [SerializeField] private LayerMask dashObstacleMask;
    [UnityEngine.Serialization.FormerlySerializedAs("maxChargeTime_")]
    [SerializeField] private float maxChargeTime = 1.2f;
    [UnityEngine.Serialization.FormerlySerializedAs("dashMinDistance_")]
    [SerializeField] private float dashMinDistance = 2f;
    [UnityEngine.Serialization.FormerlySerializedAs("dashMaxDistance_")]
    [SerializeField] private float dashMaxDistance = 6f;
    [UnityEngine.Serialization.FormerlySerializedAs("dashWallBuffer_")]
    [SerializeField] private float dashWallBuffer = 0.08f;
    [UnityEngine.Serialization.FormerlySerializedAs("requireGroundedForChargeAttack_")]
    [SerializeField] private bool requireGroundedForChargeAttack = false;
    [UnityEngine.Serialization.FormerlySerializedAs("airDashFloatDuration_")]
    [SerializeField] private float airDashFloatDuration = 0.18f;
    [UnityEngine.Serialization.FormerlySerializedAs("airDashGravityMultiplier_")]
    [SerializeField] private float airDashGravityMultiplier = 0.35f;
    [UnityEngine.Serialization.FormerlySerializedAs("maxFallSpeedAfterAirDash_")]
    [SerializeField] private float maxFallSpeedAfterAirDash = -2f;
    [UnityEngine.Serialization.FormerlySerializedAs("chargedAttackColliderActiveDuration_")]
    [SerializeField] private float chargedAttackColliderActiveDuration = 0.25f;
    [SerializeField, Min(1)] private int chargedAttackDamage = 20;
    [SerializeField] private LayerMask chargedAttackDamageMask = ~0;
    [UnityEngine.Serialization.FormerlySerializedAs("showChargedAttackColliderDebug_")]
    [SerializeField] private bool showChargedAttackColliderDebug = true;
    [UnityEngine.Serialization.FormerlySerializedAs("chargedAttackColliderDebugColor_")]
    [SerializeField] private Color chargedAttackColliderDebugColor = new Color(0.2f, 0.8f, 1f, 1f);
    [SerializeField] private Color chargedAttackRepeatColliderDebugColor = new Color(1f, 0.15f, 0.65f, 1f);
    [UnityEngine.Serialization.FormerlySerializedAs("chargedAttackColliderDebugLineWidth_")]
    [SerializeField] private float chargedAttackColliderDebugLineWidth = 0.05f;

    private CharacterController controller;
    private Collider normalAttackColliderLeft;
    private Collider normalAttackColliderRight;
    private Collider normalAttackColliderUp;
    private Collider normalAttackColliderDown;
    private Collider activeNormalAttackCollider;
    private LineRenderer normalAttackDebugLeft;
    private LineRenderer normalAttackDebugRight;
    private LineRenderer normalAttackDebugUp;
    private LineRenderer normalAttackDebugDown;
    private LineRenderer distanceChargeReadyDebugLine;
    private Collider[] attackHitResults;
    private Collider[] dashOverlapResults;
    private Collider[] ignoredDashColliders;
    private RaycastHit[] dashHitResults;
    private HashSet<IDamageable> damagedNormalAttackTargets;
    private HashSet<IDamageable> damagedChargedAttackTargets;
    private Collider chargedAttackRangeCollider;
    private LineRenderer chargedAttackDebugLine;
    private ChargedAttackHitbox[] chargedAttackHitboxes;
    private Vector3 chargedAttackLocalOffset;
    private float chargedAttackWidth = 1.2f;
    private float chargedAttackDepth = 1f;
    private float horizontalVelocity;
    private float verticalVelocity;
    private float airDashFloatTimer;
    private bool isJumping;
    private bool isHoldingJump;
    private float jumpHoldTimer;
    private Vector2 facingDirection;
    private Vector2 lastHorizontalFacingDirection;
    private Vector2 arrowInputDirection;
    private int arrowInputFrame = -1;
    private bool isCharging;
    private float chargeTimer;
    private float normalAttackColliderDisableTime;
    private float distanceChargePercent;
    private Vector3 lastDistanceChargePosition;
    private int activeNormalAttackDamage;
    private int attackPowerBonus;
    private Color activeNormalAttackDebugColor;
    private Material runtimeDebugMaterial;
    private GameObject chargedAttackHitboxPoolRoot;


    public event Action NormalAttackPerformed;
    public event Action ChargedAttackPerformed;
    public event Action ReinforcedAttackPerformed;
    public event Action TargetDefeated;
    public event Action<DamageInfo> TargetDamaged;
    public event Action<ChargedAttackArea> ChargedAttackAreaActivated;

    public event Action JumpStarted;
    public event Action Landed;
    public float HorizontalSpeed => Mathf.Abs(horizontalVelocity);
    public float VerticalSpeed => verticalVelocity;
    public bool IsGrounded => controller != null && controller.isGrounded;
    public bool IsCharging => isCharging;
    public bool IsAttackReinforceEnabled => enableDistanceChargeUpgrade;
    public int AttackPowerBonus => attackPowerBonus;
    public float AttackReinforceChargePercent => distanceChargePercent;
    public Vector2 FacingDirection => facingDirection;
    public bool LastNormalAttackWasEmpowered { get; private set; }
    public Vector2 LastNormalAttackDirection { get; private set; }
    public Vector3 LastNormalAttackCenter { get; private set; }


    public readonly struct ChargedAttackArea
    {
        public Vector3 Center { get; }
        public Quaternion Rotation { get; }
        public Vector3 Size { get; }
        public bool IsReinforced { get; }

        public ChargedAttackArea(Vector3 center, Quaternion rotation, Vector3 size, bool isReinforced)
        {
            Center = center;
            Rotation = rotation;
            Size = size;
            IsReinforced = isReinforced;
        }
    }


    private struct ChargedAttackHitbox
    {
        private GameObject gameObject_;
        private BoxCollider collider_;
        private LineRenderer debugLine_;
        private float activationTime_;
        private float disableTime_;
        private bool isReinforced_;

        public GameObject GameObject => gameObject_;
        public BoxCollider Collider => collider_;
        public LineRenderer DebugLine => debugLine_;
        public bool IsPending => activationTime_ > 0f;
        public bool IsReinforced
        {
            get => isReinforced_;
            set => isReinforced_ = value;
        }
        public float ActivationTime
        {
            get => activationTime_;
            set => activationTime_ = value;
        }
        public float DisableTime
        {
            get => disableTime_;
            set => disableTime_ = value;
        }

        public ChargedAttackHitbox(GameObject gameObject, BoxCollider collider, LineRenderer debugLine)
        {
            gameObject_ = gameObject;
            collider_ = collider;
            debugLine_ = debugLine;
            activationTime_ = 0f;
            disableTime_ = 0f;
            isReinforced_ = false;
        }
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        facingDirection = Vector2.right;
        lastHorizontalFacingDirection = Vector2.right;
        normalAttackColliderDisableTime = 0f;
        attackHitResults = new Collider[32];
        dashOverlapResults = new Collider[32];
        ignoredDashColliders = new Collider[32];
        dashHitResults = new RaycastHit[32];
        damagedNormalAttackTargets = new HashSet<IDamageable>();
        damagedChargedAttackTargets = new HashSet<IDamageable>();
        chargedAttackHitboxes = new ChargedAttackHitbox[ChargedAttackHitboxPoolSize];
        lastDistanceChargePosition = transform.position;
        activeNormalAttackDebugColor = normalAttackColliderDebugColor;
    }

    private void Start()
    {
        ResolveAttackColliders();
        CacheChargedAttackTemplate();
        CreateChargedAttackHitboxPool();
        SetNormalAttackCollidersEnabled(false);
        SetChargedAttackPreviewVisible(false);
        CreateDistanceChargeReadyDebugLine();
        LockSidePlane();
    }

    private void Update()
    {
        UpdateChargeTimer();
        UpdateFacingDirection();
        UpdateHorizontalMovement();
        HandleJumpInput();
        UpdateNormalAttackColliderState();

        ApplyMovement();
        UpdateChargedAttackHitboxPool();
        LockSidePlane();
        UpdateDistanceCharge();
    }

    private void LateUpdate()
    {
        UpdateChargedAttackPreview();
    }

    private Vector2 GetArrowAimDirection()
    {
        if (arrowInputFrame == Time.frameCount)
            return arrowInputDirection;

        arrowInputFrame = Time.frameCount;
        bool left = Input.GetKey(KeyCode.LeftArrow);
        bool right = Input.GetKey(KeyCode.RightArrow);
        bool up = Input.GetKey(KeyCode.UpArrow);
        bool down = Input.GetKey(KeyCode.DownArrow);
        bool leftPressed = Input.GetKeyDown(KeyCode.LeftArrow);
        bool rightPressed = Input.GetKeyDown(KeyCode.RightArrow);

        if (leftPressed != rightPressed)
            lastHorizontalFacingDirection = leftPressed ? Vector2.left : Vector2.right;

        float horizontal = left && right
            ? lastHorizontalFacingDirection.x
            : left ? -1f : right ? 1f : 0f;
        float vertical = up == down ? 0f : up ? 1f : -1f;
        arrowInputDirection = new Vector2(horizontal, vertical);
        return arrowInputDirection;
    }

    private void UpdateFacingDirection()
    {
        Vector2 inputDirection = GetArrowAimDirection();

        if (inputDirection.x < 0f)
            lastHorizontalFacingDirection = Vector2.left;
        else if (inputDirection.x > 0f)
            lastHorizontalFacingDirection = Vector2.right;

        facingDirection = Mathf.Abs(inputDirection.y) > 0f ? inputDirection.normalized : lastHorizontalFacingDirection;
    }

    private void UpdateHorizontalMovement()
    {
        horizontalVelocity = GetArrowAimDirection().x * (isCharging ? chargeMoveSpeed : moveSpeed);
    }

    private void HandleJumpInput()
    {
        if (isCharging && !allowJumpWhileCharging)
            return;

        if (Input.GetKeyDown(KeyCode.UpArrow) && controller.isGrounded)
        {
            verticalVelocity = initialJumpVelocity;
            isJumping = true;
            isHoldingJump = true;
            jumpHoldTimer = 0f;
            JumpStarted?.Invoke();
        }

        if (Input.GetKey(KeyCode.UpArrow) && isJumping && isHoldingJump)
        {
            jumpHoldTimer += Time.deltaTime;

            if (jumpHoldTimer > maxJumpHoldTime)
            {
                isHoldingJump = false;
                return;
            }

            verticalVelocity = Mathf.Min(verticalVelocity + jumpHoldAcceleration * Time.deltaTime, maxUpwardVelocity);
        }

        if (Input.GetKeyUp(KeyCode.UpArrow) && isJumping && isHoldingJump)
        {
            isHoldingJump = false;

            if (verticalVelocity > 0f)
                verticalVelocity = 0f;
        }
    }

    // Attack input providers call this API after validating their own input rules.
    public void PerformNormalAttack()
    {
        bool isEmpowered = enableDistanceChargeUpgrade && distanceChargePercent >= 100f;
        Vector2 attackDirection = GetNormalAttackDirection();
        activeNormalAttackDamage = Mathf.Max(
            1,
            normalAttackDamage + attackPowerBonus + (isEmpowered ? empoweredAttackAdditionalDamage : 0));
        activeNormalAttackDebugColor = isEmpowered
            ? empoweredNormalAttackDebugColor
            : normalAttackColliderDebugColor;
        damagedNormalAttackTargets.Clear();
        LastNormalAttackWasEmpowered = isEmpowered;
        LastNormalAttackDirection = attackDirection;
        LastNormalAttackCenter = transform.position + Vector3.up * controller.center.y;
        ActivateNormalAttackCollider(attackDirection);
        if (isEmpowered)
        {
            distanceChargePercent = 0f;
            UpdateDistanceChargeReadyDebug();
            ReinforcedAttackPerformed?.Invoke();
        }

        NormalAttackPerformed?.Invoke();
    }

    public void SetAttackReinforceEnabled(bool enabled)
    {
        enableDistanceChargeUpgrade = enabled;
        distanceChargePercent = 0f;
        lastDistanceChargePosition = transform.position;
        UpdateDistanceChargeReadyDebug();
    }

    public void ToggleAttackReinforce()
    {
        SetAttackReinforceEnabled(!enableDistanceChargeUpgrade);
    }

    public void SetAttackPowerBonus(int bonus)
    {
        attackPowerBonus = Mathf.Max(0, bonus);
    }

    private Vector2 GetNormalAttackDirection()
    {
        Vector2 inputDirection = GetArrowAimDirection();

        if (inputDirection.x < 0f)
            lastHorizontalFacingDirection = Vector2.left;
        else if (inputDirection.x > 0f)
            lastHorizontalFacingDirection = Vector2.right;

        if (inputDirection.y > 0f)
            return Vector2.up;
        if (inputDirection.y < 0f)
            return Vector2.down;
        if (inputDirection.x < 0f)
            return Vector2.left;
        if (inputDirection.x > 0f)
            return Vector2.right;

        return lastHorizontalFacingDirection;
    }

    private void ActivateNormalAttackCollider(Vector2 direction)
    {
        Collider targetCollider = GetNormalAttackCollider(direction);

        if (targetCollider == null)
            return;

        if (activeNormalAttackCollider != targetCollider)
        {
            SetNormalAttackColliderEnabled(activeNormalAttackCollider, false);
            activeNormalAttackCollider = targetCollider;
        }

        SetNormalAttackColliderEnabled(activeNormalAttackCollider, true);
        Bounds attackBounds = activeNormalAttackCollider.bounds;
        LastNormalAttackCenter = attackBounds.center + new Vector3(
            direction.x * attackBounds.extents.x,
            direction.y * attackBounds.extents.y,
            0f);

        normalAttackColliderDisableTime = Time.time + Mathf.Max(0f, normalAttackColliderActiveDuration);
        DamageTargetsInActiveNormalAttack();
    }

    private void UpdateNormalAttackColliderState()
    {
        if (activeNormalAttackCollider == null)
            return;

        if (Time.time < normalAttackColliderDisableTime)
            return;

        SetNormalAttackColliderEnabled(activeNormalAttackCollider, false);
        activeNormalAttackCollider = null;
        damagedNormalAttackTargets.Clear();
    }

    private void DamageTargetsInActiveNormalAttack()
    {
        BoxCollider attackCollider = activeNormalAttackCollider as BoxCollider;
        if (attackCollider == null || attackHitResults == null)
            return;

        Vector3 scale = attackCollider.transform.lossyScale;
        Vector3 halfExtents = Vector3.Scale(
            attackCollider.size * 0.5f,
            new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
        int hitCount = Physics.OverlapBoxNonAlloc(
            attackCollider.transform.TransformPoint(attackCollider.center),
            halfExtents,
            attackHitResults,
            attackCollider.transform.rotation,
            normalAttackDamageMask,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = attackHitResults[i];
            attackHitResults[i] = null;
            if (hit == null)
                continue;

            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target == null || !target.IsAlive || !damagedNormalAttackTargets.Add(target))
                continue;

            Vector3 hitPoint = hit.ClosestPoint(activeNormalAttackCollider.bounds.center);
            ApplyDamage(target, new DamageInfo(
                activeNormalAttackDamage,
                hitPoint,
                gameObject));
        }
    }

    private void DamageTargetsInChargedAttack(BoxCollider attackCollider)
    {
        if (attackCollider == null || attackHitResults == null)
            return;

        damagedChargedAttackTargets.Clear();
        Vector3 scale = attackCollider.transform.lossyScale;
        Vector3 halfExtents = Vector3.Scale(
            attackCollider.size * 0.5f,
            new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
        int hitCount = Physics.OverlapBoxNonAlloc(
            attackCollider.transform.TransformPoint(attackCollider.center),
            halfExtents,
            attackHitResults,
            attackCollider.transform.rotation,
            chargedAttackDamageMask,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = attackHitResults[i];
            attackHitResults[i] = null;
            if (hit == null)
                continue;

            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target == null || !target.IsAlive || !damagedChargedAttackTargets.Add(target))
                continue;

            ApplyDamage(target, new DamageInfo(
                Mathf.Max(1, chargedAttackDamage + attackPowerBonus),
                hit.ClosestPoint(attackCollider.bounds.center),
                gameObject));
        }
    }

    private void ApplyDamage(IDamageable target, DamageInfo damageInfo)
    {
        bool wasAlive = target.IsAlive;
        target.TakeDamage(damageInfo);
        TargetDamaged?.Invoke(damageInfo);

        if (wasAlive && !target.IsAlive)
            TargetDefeated?.Invoke();
    }

    private void UpdateDistanceCharge()
    {
        Vector3 currentPosition = transform.position;
        Vector2 previousPlanarPosition = new Vector2(lastDistanceChargePosition.x, lastDistanceChargePosition.y);
        Vector2 currentPlanarPosition = new Vector2(currentPosition.x, currentPosition.y);
        lastDistanceChargePosition = currentPosition;

        if (!enableDistanceChargeUpgrade || distanceChargePercent >= 100f)
            return;

        float previousChargePercent = distanceChargePercent;
        float movedDistance = Vector2.Distance(previousPlanarPosition, currentPlanarPosition);
        distanceChargePercent = Mathf.Min(
            100f,
            distanceChargePercent + movedDistance / Mathf.Max(0.01f, distanceForFullCharge) * 100f);

        if (previousChargePercent < 100f && distanceChargePercent >= 100f)
            UpdateDistanceChargeReadyDebug();
    }

    private Collider GetNormalAttackCollider(Vector2 direction)
    {
        if (direction == Vector2.left)
            return normalAttackColliderLeft;
        if (direction == Vector2.right)
            return normalAttackColliderRight;
        if (direction == Vector2.up)
            return normalAttackColliderUp;
        if (direction == Vector2.down)
            return normalAttackColliderDown;

        return normalAttackColliderRight;
    }

    public void BeginChargedAttack()
    {
        if (isCharging)
            return;

        isCharging = true;
        chargeTimer = 0f;
    }

    public void ReleaseChargedAttack()
    {
        ExecuteChargedAttack(-1f);
    }

    public void ReleaseChargedAttackWithRepeat(float repeatDelaySeconds)
    {
        ExecuteChargedAttack(Mathf.Max(0f, repeatDelaySeconds));
    }

    public void CancelPendingChargedAttackRepeats()
    {
        if (chargedAttackHitboxes == null)
            return;

        for (int i = 0; i < chargedAttackHitboxes.Length; i++)
        {
            if (chargedAttackHitboxes[i].IsPending)
                DisableChargedAttackHitbox(i);
        }
    }

    public void CancelChargedAttack()
    {
        isCharging = false;
        chargeTimer = 0f;
        SetChargedAttackPreviewVisible(false);
    }

    private void UpdateChargeTimer()
    {
        if (isCharging)
            chargeTimer = Mathf.Min(chargeTimer + Time.deltaTime, maxChargeTime);
    }

    private void ExecuteChargedAttack(float repeatDelaySeconds)
    {
        if (!isCharging)
            return;

        isCharging = false;
        SetChargedAttackPreviewVisible(false);

        if (requireGroundedForChargeAttack && !controller.isGrounded)
            return;

        Vector2 releaseDirectionInput = GetArrowAimDirection();
        if (releaseDirectionInput != Vector2.zero)
            facingDirection = releaseDirectionInput.normalized;

        Vector3 dashDirection = new Vector3(facingDirection.x, facingDirection.y, 0f).normalized;
        float requestedDistance = Mathf.Lerp(dashMinDistance, dashMaxDistance, Mathf.Clamp01(chargeTimer / maxChargeTime));
        float safeDistance = GetSafeDashDistance(dashDirection, requestedDistance);
        bool wasAirborne = !controller.isGrounded;
        Vector3 startPosition = transform.position;

        MoveThroughDamageables(dashDirection, safeDistance);
        int hitboxIndex = ActivateChargedAttackHitbox(startPosition, transform.position, dashDirection);
        if (hitboxIndex >= 0 && repeatDelaySeconds >= 0f)
            ScheduleChargedAttackRepeat(hitboxIndex, repeatDelaySeconds);

        ChargedAttackPerformed?.Invoke();

        if (wasAirborne || dashDirection.y > 0.01f)
            ApplyAirDashFallCorrection(dashDirection);

        chargeTimer = 0f;
    }

    private void UpdateChargedAttackPreview()
    {
        if (!isCharging || chargedAttackRangeCollider == null)
            return;

        Vector3 dashDirection = new Vector3(facingDirection.x, facingDirection.y, 0f).normalized;
        float requestedDistance = Mathf.Lerp(dashMinDistance, dashMaxDistance, Mathf.Clamp01(chargeTimer / maxChargeTime));
        float safeDistance = GetSafeDashDistance(dashDirection, requestedDistance);

        ApplyChargedAttackShape(chargedAttackRangeCollider, transform.position, transform.position + dashDirection * safeDistance, dashDirection);
        UpdateColliderDebugLine(chargedAttackRangeCollider, chargedAttackDebugLine, chargedAttackColliderDebugColor, chargedAttackColliderDebugLineWidth);
        SetChargedAttackPreviewVisible(true);
    }

    private int ActivateChargedAttackHitbox(Vector3 startPosition, Vector3 endPosition, Vector3 dashDirection)
    {
        int hitboxIndex = GetAvailableChargedAttackHitboxIndex();
        if (hitboxIndex < 0)
            return -1;

        ChargedAttackHitbox hitbox = chargedAttackHitboxes[hitboxIndex];

        hitbox.GameObject.SetActive(true);
        hitbox.Collider.enabled = true;
        hitbox.ActivationTime = 0f;
        hitbox.DisableTime = Time.time + Mathf.Max(0.01f, chargedAttackColliderActiveDuration);
        hitbox.IsReinforced = false;

        ApplyChargedAttackShape(hitbox.Collider, startPosition, endPosition, dashDirection);
        DamageTargetsInChargedAttack(hitbox.Collider);
        UpdateColliderDebugLine(hitbox.Collider, hitbox.DebugLine, chargedAttackColliderDebugColor, chargedAttackColliderDebugLineWidth);
        hitbox.DebugLine.enabled = showChargedAttackColliderDebug;
        chargedAttackHitboxes[hitboxIndex] = hitbox;
        NotifyChargedAttackArea(hitbox);
        return hitboxIndex;
    }

    private void ScheduleChargedAttackRepeat(int sourceIndex, float delaySeconds)
    {
        int repeatIndex = GetAvailableChargedAttackHitboxIndex();
        if (repeatIndex < 0)
            return;

        ChargedAttackHitbox source = chargedAttackHitboxes[sourceIndex];
        ChargedAttackHitbox repeat = chargedAttackHitboxes[repeatIndex];

        repeat.GameObject.transform.SetPositionAndRotation(
            source.GameObject.transform.position,
            source.GameObject.transform.rotation);
        repeat.GameObject.transform.localScale = source.GameObject.transform.localScale;
        repeat.Collider.center = source.Collider.center;
        repeat.Collider.size = source.Collider.size;
        repeat.Collider.enabled = false;
        repeat.DebugLine.enabled = false;
        repeat.ActivationTime = Time.unscaledTime + delaySeconds;
        repeat.DisableTime = 0f;
        repeat.IsReinforced = true;

        UpdateColliderDebugLine(
            repeat.Collider,
            repeat.DebugLine,
            chargedAttackRepeatColliderDebugColor,
            chargedAttackColliderDebugLineWidth);
        chargedAttackHitboxes[repeatIndex] = repeat;
    }

    private int GetAvailableChargedAttackHitboxIndex()
    {
        for (int i = 0; i < chargedAttackHitboxes.Length; i++)
        {
            GameObject hitboxObject = chargedAttackHitboxes[i].GameObject;
            if (hitboxObject != null &&
                !hitboxObject.activeSelf &&
                !chargedAttackHitboxes[i].IsPending)
                return i;
        }

        return -1;
    }

    private void ApplyChargedAttackShape(Collider targetCollider, Vector3 startPosition, Vector3 endPosition, Vector3 dashDirection)
    {
        BoxCollider boxCollider = targetCollider as BoxCollider;
        if (boxCollider == null)
            return;

        Vector3 direction = dashDirection.sqrMagnitude > 0.0001f ? dashDirection.normalized : Vector3.right;
        float pathLength = Mathf.Max(0.01f, (endPosition - startPosition).magnitude);
        Vector3 center = startPosition + direction * (pathLength * 0.5f) + transform.TransformVector(chargedAttackLocalOffset);

        boxCollider.transform.position = center;
        boxCollider.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        boxCollider.transform.localScale = Vector3.one;
        boxCollider.center = Vector3.zero;
        boxCollider.size = new Vector3(pathLength, chargedAttackWidth, chargedAttackDepth);
    }

    private float GetSafeDashDistance(Vector3 direction, float requestedDistance)
    {
        direction.Normalize();
        GetControllerCapsuleWorldPoints(out Vector3 bottom, out Vector3 top, out float radius);
        int hitCount = Physics.CapsuleCastNonAlloc(
            bottom,
            top,
            radius,
            direction,
            dashHitResults,
            requestedDistance + dashWallBuffer,
            dashObstacleMask,
            QueryTriggerInteraction.Ignore);
        float nearestObstacleDistance = float.PositiveInfinity;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = dashHitResults[i];
            dashHitResults[i] = default;
            if (hit.collider == null || IsDamageableCollider(hit.collider))
                continue;

            nearestObstacleDistance = Mathf.Min(nearestObstacleDistance, hit.distance);
        }

        return float.IsPositiveInfinity(nearestObstacleDistance)
            ? requestedDistance
            : Mathf.Max(0f, nearestObstacleDistance - dashWallBuffer);
    }

    private void MoveThroughDamageables(Vector3 direction, float distance)
    {
        int ignoredCount = IgnoreDamageableCollisionsAlongDash(direction, distance);
        try
        {
            controller.Move(direction * distance);
        }
        finally
        {
            for (int i = 0; i < ignoredCount; i++)
            {
                Collider ignoredCollider = ignoredDashColliders[i];
                ignoredDashColliders[i] = null;
                if (ignoredCollider != null)
                    Physics.IgnoreCollision(controller, ignoredCollider, false);
            }
        }
    }

    private int IgnoreDamageableCollisionsAlongDash(Vector3 direction, float distance)
    {
        GetControllerCapsuleWorldPoints(out Vector3 bottom, out Vector3 top, out float radius);
        int ignoredCount = 0;
        int overlapCount = Physics.OverlapCapsuleNonAlloc(
            bottom,
            top,
            radius,
            dashOverlapResults,
            ~0,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < overlapCount; i++)
        {
            Collider overlap = dashOverlapResults[i];
            dashOverlapResults[i] = null;
            ignoredCount = TryIgnoreDamageableCollision(overlap, ignoredCount);
        }

        int hitCount = Physics.CapsuleCastNonAlloc(
            bottom,
            top,
            radius,
            direction,
            dashHitResults,
            distance + controller.skinWidth,
            ~0,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = dashHitResults[i].collider;
            dashHitResults[i] = default;
            ignoredCount = TryIgnoreDamageableCollision(hitCollider, ignoredCount);
        }

        return ignoredCount;
    }

    private int TryIgnoreDamageableCollision(Collider targetCollider, int ignoredCount)
    {
        if (targetCollider == null || targetCollider == controller || !IsDamageableCollider(targetCollider))
            return ignoredCount;

        for (int i = 0; i < ignoredCount; i++)
        {
            if (ignoredDashColliders[i] == targetCollider)
                return ignoredCount;
        }

        if (ignoredCount >= ignoredDashColliders.Length)
            return ignoredCount;

        Physics.IgnoreCollision(controller, targetCollider, true);
        ignoredDashColliders[ignoredCount] = targetCollider;
        return ignoredCount + 1;
    }

    private static bool IsDamageableCollider(Collider targetCollider)
    {
        return targetCollider.GetComponentInParent<IDamageable>() != null;
    }

    private void ApplyAirDashFallCorrection(Vector3 dashDirection)
    {
        airDashFloatTimer = airDashFloatDuration;

        if (verticalVelocity < maxFallSpeedAfterAirDash)
            verticalVelocity = maxFallSpeedAfterAirDash;

        if (dashDirection.y > 0.01f && verticalVelocity < 0f)
            verticalVelocity = 0f;
    }

    private void ApplyMovement()
    {
        bool wasGrounded = controller.isGrounded;

        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedStickVelocity;
            isJumping = false;
            isHoldingJump = false;
        }

        float gravityMultiplier = airDashFloatTimer > 0f && !controller.isGrounded ? airDashGravityMultiplier : 1f;
        airDashFloatTimer = Mathf.Max(0f, airDashFloatTimer - Time.deltaTime);
        verticalVelocity += gravity * gravityMultiplier * Time.deltaTime;

        CollisionFlags flags = controller.Move(new Vector3(horizontalVelocity * Time.deltaTime, verticalVelocity * Time.deltaTime, 0f));

        if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
        {
            verticalVelocity = 0f;
            isHoldingJump = false;
        }

        if ((flags & CollisionFlags.Below) != 0 && verticalVelocity <= 0f)
        {
            isJumping = false;
            isHoldingJump = false;
        }

        if (!wasGrounded && (flags & CollisionFlags.Below) != 0)
            Landed?.Invoke();
    }

    private void ResolveAttackColliders()
    {
        Transform attackColliderRoot = transform.Find("AttackColliders");
        Transform searchRoot = attackColliderRoot != null ? attackColliderRoot : transform;

        normalAttackColliderLeft = FindCollider(searchRoot, "NormalAttackCollider_Left");
        normalAttackColliderRight = FindCollider(searchRoot, "NormalAttackCollider_Right");
        normalAttackColliderUp = FindCollider(searchRoot, "NormalAttackCollider_Up");
        normalAttackColliderDown = FindCollider(searchRoot, "NormalAttackCollider_Down");
        chargedAttackRangeCollider = FindCollider(searchRoot, "ChargedAttackRangeCollider");

        normalAttackDebugLeft = CreateDebugLine(normalAttackColliderLeft, true);
        normalAttackDebugRight = CreateDebugLine(normalAttackColliderRight, true);
        normalAttackDebugUp = CreateDebugLine(normalAttackColliderUp, true);
        normalAttackDebugDown = CreateDebugLine(normalAttackColliderDown, true);
        chargedAttackDebugLine = CreateDebugLine(chargedAttackRangeCollider, false);
    }

    private Collider FindCollider(Transform searchRoot, string objectName)
    {
        Transform found = searchRoot != null ? searchRoot.Find(objectName) : null;
        return found != null ? found.GetComponent<Collider>() : null;
    }

    private void CacheChargedAttackTemplate()
    {
        BoxCollider boxCollider = chargedAttackRangeCollider as BoxCollider;
        if (boxCollider == null)
            return;

        chargedAttackRangeCollider.isTrigger = true;
        chargedAttackRangeCollider.enabled = false;
        chargedAttackLocalOffset = chargedAttackRangeCollider.transform.localPosition + boxCollider.center;
        chargedAttackWidth = Mathf.Max(0.01f, boxCollider.size.y);
        chargedAttackDepth = Mathf.Max(0.01f, boxCollider.size.z);
    }

    private void CreateChargedAttackHitboxPool()
    {
        if (chargedAttackRangeCollider == null)
            return;

        chargedAttackHitboxPoolRoot = new GameObject("ChargedAttackHitboxPool");

        for (int i = 0; i < chargedAttackHitboxes.Length; i++)
        {
            GameObject hitboxObject = new GameObject("ChargedAttackHitbox_" + i);
            hitboxObject.layer = chargedAttackRangeCollider.gameObject.layer;
            hitboxObject.transform.SetParent(chargedAttackHitboxPoolRoot.transform, false);

            BoxCollider hitboxCollider = hitboxObject.AddComponent<BoxCollider>();
            hitboxCollider.isTrigger = true;
            LineRenderer debugLine = CreateDebugLine(hitboxCollider, false);

            chargedAttackHitboxes[i] = new ChargedAttackHitbox(hitboxObject, hitboxCollider, debugLine);

            hitboxObject.SetActive(false);
        }
    }

    private void UpdateChargedAttackHitboxPool()
    {
        if (chargedAttackHitboxes == null)
            return;

        for (int i = 0; i < chargedAttackHitboxes.Length; i++)
        {
            ChargedAttackHitbox hitbox = chargedAttackHitboxes[i];

            if (hitbox.GameObject == null)
                continue;

            if (hitbox.IsPending)
            {
                if (Time.unscaledTime < hitbox.ActivationTime)
                    continue;

                hitbox.GameObject.SetActive(true);
                hitbox.Collider.enabled = true;
                hitbox.DebugLine.enabled = showChargedAttackColliderDebug;
                hitbox.ActivationTime = 0f;
                hitbox.DisableTime = Time.time + Mathf.Max(0.01f, chargedAttackColliderActiveDuration);
                DamageTargetsInChargedAttack(hitbox.Collider);
                chargedAttackHitboxes[i] = hitbox;
                NotifyChargedAttackArea(hitbox);
                if (hitbox.IsReinforced)
                    ReinforcedAttackPerformed?.Invoke();
                continue;
            }

            if (hitbox.GameObject == null || !hitbox.GameObject.activeSelf || Time.time < hitbox.DisableTime)
                continue;

            DisableChargedAttackHitbox(i);
        }
    }

    private void DisableChargedAttackHitbox(int index)
    {
        ChargedAttackHitbox hitbox = chargedAttackHitboxes[index];

        if (hitbox.GameObject == null)
            return;

        if (hitbox.Collider != null)
            hitbox.Collider.enabled = false;

        if (hitbox.DebugLine != null)
            hitbox.DebugLine.enabled = false;

        hitbox.GameObject.SetActive(false);
        hitbox.ActivationTime = 0f;
        hitbox.DisableTime = 0f;
        hitbox.IsReinforced = false;
        chargedAttackHitboxes[index] = hitbox;
    }

    private void SetNormalAttackCollidersEnabled(bool enabled)
    {
        SetNormalAttackColliderEnabled(normalAttackColliderLeft, enabled);
        SetNormalAttackColliderEnabled(normalAttackColliderRight, enabled);
        SetNormalAttackColliderEnabled(normalAttackColliderUp, enabled);
        SetNormalAttackColliderEnabled(normalAttackColliderDown, enabled);
    }

    private void SetNormalAttackColliderEnabled(Collider targetCollider, bool enabled)
    {
        if (targetCollider != null)
            targetCollider.enabled = enabled;

        LineRenderer debugLine = GetNormalAttackDebugLine(targetCollider);
        if (debugLine == null)
            return;

        if (enabled)
            UpdateColliderDebugLine(targetCollider, debugLine, activeNormalAttackDebugColor, normalAttackColliderDebugLineWidth);

        debugLine.enabled = enabled && showNormalAttackColliderDebug;
    }

    private void SetChargedAttackPreviewVisible(bool visible)
    {
        if (chargedAttackRangeCollider != null)
            chargedAttackRangeCollider.enabled = false;

        if (chargedAttackDebugLine != null)
            chargedAttackDebugLine.enabled = visible && showChargedAttackColliderDebug;
    }

    private LineRenderer GetNormalAttackDebugLine(Collider targetCollider)
    {
        if (targetCollider == normalAttackColliderLeft)
            return normalAttackDebugLeft;
        if (targetCollider == normalAttackColliderRight)
            return normalAttackDebugRight;
        if (targetCollider == normalAttackColliderUp)
            return normalAttackDebugUp;
        if (targetCollider == normalAttackColliderDown)
            return normalAttackDebugDown;

        return null;
    }

    private LineRenderer CreateDebugLine(Collider targetCollider, bool createChild)
    {
        if (targetCollider == null)
            return null;

        GameObject lineObject = targetCollider.gameObject;
        if (createChild)
        {
            Transform existing = targetCollider.transform.Find("DebugVisibleBounds");
            lineObject = existing != null ? existing.gameObject : new GameObject("DebugVisibleBounds");
            lineObject.transform.SetParent(targetCollider.transform, false);
        }

        LineRenderer lineRenderer = lineObject.GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = lineObject.AddComponent<LineRenderer>();

        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
        lineRenderer.positionCount = 4;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;

        if (lineRenderer.sharedMaterial == null)
            lineRenderer.sharedMaterial = GetRuntimeDebugMaterial();

        lineRenderer.enabled = false;
        return lineRenderer;
    }

    private void CreateDistanceChargeReadyDebugLine()
    {
        Transform existing = transform.Find("DistanceChargeReadyDebug");
        GameObject lineObject = existing != null
            ? existing.gameObject
            : new GameObject("DistanceChargeReadyDebug");
        lineObject.transform.SetParent(transform, false);

        distanceChargeReadyDebugLine = lineObject.GetComponent<LineRenderer>();
        if (distanceChargeReadyDebugLine == null)
            distanceChargeReadyDebugLine = lineObject.AddComponent<LineRenderer>();

        distanceChargeReadyDebugLine.useWorldSpace = false;
        distanceChargeReadyDebugLine.loop = true;
        distanceChargeReadyDebugLine.positionCount = 4;
        distanceChargeReadyDebugLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        distanceChargeReadyDebugLine.receiveShadows = false;

        if (distanceChargeReadyDebugLine.sharedMaterial == null)
            distanceChargeReadyDebugLine.sharedMaterial = GetRuntimeDebugMaterial();

        UpdateDistanceChargeReadyDebug();
    }

    private void UpdateDistanceChargeReadyDebug()
    {
        if (distanceChargeReadyDebugLine == null || controller == null)
            return;

        bool isReady = showDistanceChargeReadyDebug &&
            enableDistanceChargeUpgrade &&
            distanceChargePercent >= 100f;
        distanceChargeReadyDebugLine.enabled = isReady;
        if (!isReady)
            return;

        Vector3 center = controller.center;
        float halfWidth = controller.radius + 0.15f;
        float halfHeight = controller.height * 0.5f + 0.15f;
        float debugZ = center.z - controller.radius - 0.03f;

        distanceChargeReadyDebugLine.SetPosition(0, new Vector3(center.x - halfWidth, center.y - halfHeight, debugZ));
        distanceChargeReadyDebugLine.SetPosition(1, new Vector3(center.x - halfWidth, center.y + halfHeight, debugZ));
        distanceChargeReadyDebugLine.SetPosition(2, new Vector3(center.x + halfWidth, center.y + halfHeight, debugZ));
        distanceChargeReadyDebugLine.SetPosition(3, new Vector3(center.x + halfWidth, center.y - halfHeight, debugZ));
        distanceChargeReadyDebugLine.startColor = empoweredNormalAttackDebugColor;
        distanceChargeReadyDebugLine.endColor = empoweredNormalAttackDebugColor;
        distanceChargeReadyDebugLine.startWidth = normalAttackColliderDebugLineWidth;
        distanceChargeReadyDebugLine.endWidth = normalAttackColliderDebugLineWidth;
    }

    private void UpdateColliderDebugLine(Collider targetCollider, LineRenderer lineRenderer, Color color, float width)
    {
        BoxCollider boxCollider = targetCollider as BoxCollider;
        if (boxCollider == null || lineRenderer == null)
            return;

        Vector3 center = boxCollider.center;
        Vector3 halfSize = boxCollider.size * 0.5f;
        float debugZ = center.z - halfSize.z - 0.01f;

        lineRenderer.SetPosition(0, new Vector3(center.x - halfSize.x, center.y - halfSize.y, debugZ));
        lineRenderer.SetPosition(1, new Vector3(center.x - halfSize.x, center.y + halfSize.y, debugZ));
        lineRenderer.SetPosition(2, new Vector3(center.x + halfSize.x, center.y + halfSize.y, debugZ));
        lineRenderer.SetPosition(3, new Vector3(center.x + halfSize.x, center.y - halfSize.y, debugZ));
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
    }

    private void GetControllerCapsuleWorldPoints(out Vector3 bottom, out Vector3 top, out float radius)
    {
        Vector3 center = transform.TransformPoint(controller.center);
        float scaleX = Mathf.Abs(transform.lossyScale.x);
        float scaleY = Mathf.Abs(transform.lossyScale.y);
        float scaleZ = Mathf.Abs(transform.lossyScale.z);

        radius = controller.radius * Mathf.Max(scaleX, scaleZ);
        float height = Mathf.Max(controller.height * scaleY, radius * 2f);
        float halfHeightWithoutCaps = Mathf.Max(0f, height * 0.5f - radius);

        top = center + transform.up * halfHeightWithoutCaps;
        bottom = center - transform.up * halfHeightWithoutCaps;
    }

    private Material GetRuntimeDebugMaterial()
    {
        if (runtimeDebugMaterial == null)
            runtimeDebugMaterial = new Material(Shader.Find("Sprites/Default"));

        return runtimeDebugMaterial;
    }

    private void LockSidePlane()
    {
        Vector3 position = transform.position;
        if (Mathf.Approximately(position.z, lockedZ))
            return;

        position.z = lockedZ;
        transform.position = position;
    }

    private void OnDisable()
    {
        SetNormalAttackCollidersEnabled(false);
        SetChargedAttackPreviewVisible(false);

        if (distanceChargeReadyDebugLine != null)
            distanceChargeReadyDebugLine.enabled = false;

        if (chargedAttackHitboxes != null)
        {
            for (int i = 0; i < chargedAttackHitboxes.Length; i++)
                DisableChargedAttackHitbox(i);
        }

        activeNormalAttackCollider = null;
        damagedNormalAttackTargets?.Clear();
        activeNormalAttackDamage = 0;
        activeNormalAttackDebugColor = normalAttackColliderDebugColor;
        distanceChargePercent = 0f;
        lastDistanceChargePosition = transform.position;
        isCharging = false;
        chargeTimer = 0f;
        normalAttackColliderDisableTime = 0f;
        horizontalVelocity = 0f;
        verticalVelocity = 0f;
        airDashFloatTimer = 0f;
        isJumping = false;
        isHoldingJump = false;
        jumpHoldTimer = 0f;
    }

    private void OnDestroy()
    {
        if (chargedAttackHitboxPoolRoot != null)
            Destroy(chargedAttackHitboxPoolRoot);

        if (runtimeDebugMaterial != null)
            Destroy(runtimeDebugMaterial);
    }


    private void NotifyChargedAttackArea(ChargedAttackHitbox hitbox)
    {
        BoxCollider attackCollider = hitbox.Collider;
        Vector3 scale = attackCollider.transform.lossyScale;
        Vector3 worldSize = Vector3.Scale(
            attackCollider.size,
            new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));

        ChargedAttackAreaActivated?.Invoke(new ChargedAttackArea(
            attackCollider.transform.TransformPoint(attackCollider.center),
            attackCollider.transform.rotation,
            worldSize,
            hitbox.IsReinforced));
    }
}
