using System;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    private const int ChargedAttackHitboxPoolSize = 6;

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
    [SerializeField] private float gravity = -45f;
    [UnityEngine.Serialization.FormerlySerializedAs("groundedStickVelocity_")]
    [SerializeField] private float groundedStickVelocity = -2f;

    [Header("Jump")]
    [UnityEngine.Serialization.FormerlySerializedAs("initialJumpVelocity_")]
    [SerializeField] private float initialJumpVelocity = 10f;
    [UnityEngine.Serialization.FormerlySerializedAs("jumpHoldAcceleration_")]
    [SerializeField] private float jumpHoldAcceleration = 20f;
    [UnityEngine.Serialization.FormerlySerializedAs("maxJumpHoldTime_")]
    [SerializeField] private float maxJumpHoldTime = 0.3f;
    [UnityEngine.Serialization.FormerlySerializedAs("maxUpwardVelocity_")]
    [SerializeField] private float maxUpwardVelocity = 17.5f;
    [UnityEngine.Serialization.FormerlySerializedAs("allowJumpWhileCharging_")]
    [SerializeField] private bool allowJumpWhileCharging = false;

    [Header("Normal Attack")]
    [UnityEngine.Serialization.FormerlySerializedAs("normalAttackColliderActiveDuration_")]
    [SerializeField] private float normalAttackColliderActiveDuration = 0.25f;
    [UnityEngine.Serialization.FormerlySerializedAs("showNormalAttackColliderDebug_")]
    [SerializeField] private bool showNormalAttackColliderDebug = true;
    [UnityEngine.Serialization.FormerlySerializedAs("normalAttackColliderDebugColor_")]
    [SerializeField] private Color normalAttackColliderDebugColor = new Color(1f, 0.1f, 0.1f, 1f);
    [UnityEngine.Serialization.FormerlySerializedAs("normalAttackColliderDebugLineWidth_")]
    [SerializeField] private float normalAttackColliderDebugLineWidth = 0.04f;

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
    [UnityEngine.Serialization.FormerlySerializedAs("showChargedAttackColliderDebug_")]
    [SerializeField] private bool showChargedAttackColliderDebug = true;
    [UnityEngine.Serialization.FormerlySerializedAs("chargedAttackColliderDebugColor_")]
    [SerializeField] private Color chargedAttackColliderDebugColor = new Color(0.2f, 0.8f, 1f, 1f);
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
    private bool isCharging;
    private float chargeTimer;
    private float normalAttackColliderDisableTime;

    public event Action NormalAttackPerformed;

    public float HorizontalSpeed => Mathf.Abs(horizontalVelocity);
    public float VerticalSpeed => verticalVelocity;
    public bool IsGrounded => controller != null && controller.isGrounded;
    public bool IsCharging => isCharging;
    public Vector2 FacingDirection => facingDirection;

    private struct ChargedAttackHitbox
    {
        private GameObject gameObject_;
        private BoxCollider collider_;
        private LineRenderer debugLine_;
        private float disableTime_;

        public GameObject GameObject => gameObject_;
        public BoxCollider Collider => collider_;
        public LineRenderer DebugLine => debugLine_;
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
            disableTime_ = 0f;
        }
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        facingDirection = Vector2.right;
        lastHorizontalFacingDirection = Vector2.right;
        normalAttackColliderDisableTime = -999f;
        chargedAttackHitboxes = new ChargedAttackHitbox[ChargedAttackHitboxPoolSize];
    }

    private void Start()
    {
        ResolveAttackColliders();
        CacheChargedAttackTemplate();
        CreateChargedAttackHitboxPool();
        SetNormalAttackCollidersEnabled(false);
        SetChargedAttackPreviewVisible(false);
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
    }

    private void LateUpdate()
    {
        UpdateChargedAttackPreview();
    }

    private Vector2 GetArrowAimDirection()
    {
        bool left = Input.GetKey(KeyCode.LeftArrow);
        bool right = Input.GetKey(KeyCode.RightArrow);
        bool up = Input.GetKey(KeyCode.UpArrow);
        bool down = Input.GetKey(KeyCode.DownArrow);
        return new Vector2(left == right ? 0f : left ? -1f : 1f, up == down ? 0f : up ? 1f : -1f);
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
        ActivateNormalAttackCollider(GetNormalAttackDirection());
        NormalAttackPerformed?.Invoke();
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
        normalAttackColliderDisableTime = Time.time + Mathf.Max(0f, normalAttackColliderActiveDuration);
    }

    private void UpdateNormalAttackColliderState()
    {
        if (activeNormalAttackCollider == null || Time.time < normalAttackColliderDisableTime)
            return;

        SetNormalAttackColliderEnabled(activeNormalAttackCollider, false);
        activeNormalAttackCollider = null;
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
        ExecuteChargedAttack();
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

    private void ExecuteChargedAttack()
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

        controller.Move(dashDirection * safeDistance);
        ActivateChargedAttackHitbox(startPosition, transform.position, dashDirection);

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

    private void ActivateChargedAttackHitbox(Vector3 startPosition, Vector3 endPosition, Vector3 dashDirection)
    {
        int hitboxIndex = GetAvailableChargedAttackHitboxIndex();
        ChargedAttackHitbox hitbox = chargedAttackHitboxes[hitboxIndex];

        hitbox.GameObject.SetActive(true);
        hitbox.Collider.enabled = true;
        hitbox.DisableTime = Time.time + Mathf.Max(0.01f, chargedAttackColliderActiveDuration);

        ApplyChargedAttackShape(hitbox.Collider, startPosition, endPosition, dashDirection);
        UpdateColliderDebugLine(hitbox.Collider, hitbox.DebugLine, chargedAttackColliderDebugColor, chargedAttackColliderDebugLineWidth);
        hitbox.DebugLine.enabled = showChargedAttackColliderDebug;
        chargedAttackHitboxes[hitboxIndex] = hitbox;
    }

    private int GetAvailableChargedAttackHitboxIndex()
    {
        for (int i = 0; i < chargedAttackHitboxes.Length; i++)
        {
            if (!chargedAttackHitboxes[i].GameObject.activeSelf)
                return i;
        }

        return 0;
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

        if (!Physics.CapsuleCast(bottom, top, radius, direction, out RaycastHit hit, requestedDistance + dashWallBuffer, dashObstacleMask, QueryTriggerInteraction.Ignore))
            return requestedDistance;

        return Mathf.Max(0f, hit.distance - dashWallBuffer);
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
        for (int i = 0; i < chargedAttackHitboxes.Length; i++)
        {
            GameObject hitboxObject = new GameObject("ChargedAttackHitbox_" + i);
            hitboxObject.layer = chargedAttackRangeCollider != null ? chargedAttackRangeCollider.gameObject.layer : gameObject.layer;

            BoxCollider hitboxCollider = hitboxObject.AddComponent<BoxCollider>();
            hitboxCollider.isTrigger = true;
            LineRenderer debugLine = CreateDebugLine(hitboxCollider, false);

            chargedAttackHitboxes[i] = new ChargedAttackHitbox(hitboxObject, hitboxCollider, debugLine);

            hitboxObject.SetActive(false);
        }
    }

    private void UpdateChargedAttackHitboxPool()
    {
        for (int i = 0; i < chargedAttackHitboxes.Length; i++)
        {
            ChargedAttackHitbox hitbox = chargedAttackHitboxes[i];

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
        hitbox.DisableTime = 0f;
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
            UpdateColliderDebugLine(targetCollider, debugLine, normalAttackColliderDebugColor, normalAttackColliderDebugLineWidth);

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
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

        lineRenderer.enabled = false;
        return lineRenderer;
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

    private void LockSidePlane()
    {
        Vector3 position = transform.position;
        position.z = lockedZ;
        transform.position = position;
    }

    private void OnDisable()
    {
        SetNormalAttackCollidersEnabled(false);
        SetChargedAttackPreviewVisible(false);

        if (chargedAttackHitboxes != null)
        {
            for (int i = 0; i < chargedAttackHitboxes.Length; i++)
                DisableChargedAttackHitbox(i);
        }

        activeNormalAttackCollider = null;
        isCharging = false;
        chargeTimer = 0f;
        normalAttackColliderDisableTime = -999f;
        horizontalVelocity = 0f;
        verticalVelocity = 0f;
        airDashFloatTimer = 0f;
        isJumping = false;
        isHoldingJump = false;
        jumpHoldTimer = 0f;
    }
}
