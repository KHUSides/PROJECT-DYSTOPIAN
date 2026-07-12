using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform visualRoot;

    [Header("Side View Plane")]
    [SerializeField] private float lockedZ = 0f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float chargeMoveSpeed = 1.5f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -24f;
    [SerializeField] private float groundedStickVelocity = -2f;

    [Header("Jump")]
    [SerializeField] private float initialJumpVelocity = 5.5f;
    [SerializeField] private float jumpHoldAcceleration = 18f;
    [SerializeField] private float maxJumpHoldTime = 0.22f;
    [SerializeField] private float maxUpwardVelocity = 9.5f;

    [Tooltip("꺼두면 차징 중 위 방향키는 점프가 아니라 조준으로만 사용된다.")]
    [SerializeField] private bool allowJumpWhileCharging = false;

    [Header("Normal Attack")]
    [SerializeField] private float normalAttackCooldown = 0.25f;
    [SerializeField] private float normalAttackColliderActiveDuration = 0.25f;
    [SerializeField] private Transform attackColliderRoot;
    [SerializeField] private Collider normalAttackColliderLeft;
    [SerializeField] private Collider normalAttackColliderRight;
    [SerializeField] private Collider normalAttackColliderUp;
    [SerializeField] private Collider normalAttackColliderDown;
    [SerializeField] private bool showNormalAttackColliderDebug = true;
    [SerializeField] private Color normalAttackColliderDebugColor = new Color(1f, 0.1f, 0.1f, 1f);
    [SerializeField] private float normalAttackColliderDebugLineWidth = 0.04f;

    [Header("Charged Attack")]
    [SerializeField] private LayerMask dashObstacleMask;
    [SerializeField] private float maxChargeTime = 1.2f;
    [SerializeField] private float dashMinDistance = 2f;
    [SerializeField] private float dashMaxDistance = 6f;
    [SerializeField] private float dashWallBuffer = 0.08f;
    [SerializeField] private bool requireGroundedForChargeAttack = false;
    [SerializeField] private float airDashFloatDuration = 0.18f;
    [SerializeField] private float airDashGravityMultiplier = 0.35f;
    [SerializeField] private float maxFallSpeedAfterAirDash = -2f;
    [SerializeField] private Collider chargedAttackRangeCollider;
    [SerializeField] private float chargedAttackColliderActiveDuration = 0.25f;
    [SerializeField] private float chargedAttackRangeWidth = 1.2f;
    [SerializeField] private bool showChargedAttackColliderDebug = true;
    [SerializeField] private Color chargedAttackColliderDebugColor = new Color(0.2f, 0.8f, 1f, 1f);
    [SerializeField] private float chargedAttackColliderDebugLineWidth = 0.05f;

    private float airDashFloatTimer;

    private CharacterController controller;

    private float horizontalVelocity;
    private float verticalVelocity;

    private bool isJumping;
    private bool isHoldingJump;
    private float jumpHoldTimer;

    // 이제 바라보는 방향은 좌우 float가 아니라 8방향 Vector2다.
    private Vector2 facingDirection = Vector2.right;
    private Vector2 lastHorizontalFacingDirection = Vector2.right;
    private Vector2 previousFacingDirection = Vector2.right;

    private float lastNormalAttackTime = -999f;
    private float normalAttackColliderDisableTime = -999f;
    private Collider activeNormalAttackCollider;
    private LineRenderer normalAttackDebugLeft;
    private LineRenderer normalAttackDebugRight;
    private LineRenderer normalAttackDebugUp;
    private LineRenderer normalAttackDebugDown;
    private LineRenderer chargedAttackDebugLine;
    private Vector3 chargedAttackRangeLocalPositionOffset;
    private Vector3 chargedAttackRangeColliderCenterOffset;

    private bool isCharging;
    private float chargeTimer;

private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (visualRoot == null && transform.childCount > 0)
            visualRoot = transform.GetChild(0);

        ResolveNormalAttackColliders();
        ResolveNormalAttackColliderDebugVisuals();
        ResolveChargedAttackColliderDebugVisual();
        CacheChargedAttackRangeOffsets();
        SetNormalAttackCollidersEnabled(false);
ResolveChargedAttackColliderDebugVisual();
        SetNormalAttackCollidersEnabled(false);
        SetChargedAttackPreviewEnabled(false);

        LockSidePlane();
    }

private void Update()
    {
        UpdateChargeTimer();

        UpdateFacingDirectionFromArrowKeys();

        HandleArrowKeyMovement();
        HandleJumpInput();
        HandleNormalAttackInput();
        UpdateNormalAttackColliderState();
        HandleChargeAttackInput();
        UpdateChargedAttackPreview();

        ApplyMovement();

        LockSidePlane();
    }

    private void UpdateChargeTimer()
    {
        if (!isCharging)
            return;

        chargeTimer += Time.deltaTime;
        chargeTimer = Mathf.Min(chargeTimer, maxChargeTime);
    }

    private void UpdateFacingDirectionFromArrowKeys()
    {
        Vector2 inputDirection = GetArrowAimDirection();

        if (inputDirection.x < 0f)
            lastHorizontalFacingDirection = Vector2.left;
        else if (inputDirection.x > 0f)
            lastHorizontalFacingDirection = Vector2.right;

        Vector2 nextFacingDirection;

        if (Mathf.Abs(inputDirection.y) > 0f)
            nextFacingDirection = inputDirection.normalized;
        else
            nextFacingDirection = lastHorizontalFacingDirection;

        facingDirection = nextFacingDirection;

        if (facingDirection == previousFacingDirection)
            return;

        previousFacingDirection = facingDirection;

        Debug.Log($"[AnimLog] Face {DirectionToText(facingDirection)}");

        // 지금은 애니메이션 파일이 없으니 로그만 출력한다.
        // 나중에 2D 스프라이트나 3D 모델을 붙이면 여기서 회전/플립 처리하면 된다.
        //
        // 예시:
        // if (visualRoot != null && Mathf.Abs(facingDirection.x) > 0.01f)
        // {
        //     visualRoot.localRotation = Quaternion.Euler(
        //         0f,
        //         facingDirection.x > 0f ? 90f : -90f,
        //         0f
        //     );
        // }
    }

    private Vector2 GetArrowAimDirection()
    {
        bool left = Input.GetKey(KeyCode.LeftArrow);
        bool right = Input.GetKey(KeyCode.RightArrow);
        bool up = Input.GetKey(KeyCode.UpArrow);
        bool down = Input.GetKey(KeyCode.DownArrow);

        float x = 0f;
        float y = 0f;

        // 반대 방향 동시 입력은 제외한다.
        // Left + Right를 동시에 누르면 좌우 조준 없음.
        if (left && !right)
            x = -1f;
        else if (right && !left)
            x = 1f;

        // Up + Down을 동시에 누르면 상하 조준 없음.
        if (up && !down)
            y = 1f;
        else if (down && !up)
            y = -1f;

        return new Vector2(x, y);
    }

    private float GetHorizontalMoveInput()
    {
        bool left = Input.GetKey(KeyCode.LeftArrow);
        bool right = Input.GetKey(KeyCode.RightArrow);

        if (left && !right)
            return -1f;

        if (right && !left)
            return 1f;

        return 0f;
    }

    private void HandleArrowKeyMovement()
    {
        float moveInput = GetHorizontalMoveInput();

        float currentMoveSpeed = isCharging ? chargeMoveSpeed : moveSpeed;

        horizontalVelocity = moveInput * currentMoveSpeed;
    }

    private void HandleJumpInput()
    {
        // 차징 중에는 위 방향키를 "점프"보다 "위 조준"으로 쓰는 쪽이 조작감이 낫다.
        // allowJumpWhileCharging을 켜면 차징 중에도 점프 가능하다.
        if (isCharging && !allowJumpWhileCharging)
            return;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            TryStartJump();
        }

        if (Input.GetKey(KeyCode.UpArrow))
        {
            ContinueVariableJump();
        }

        if (Input.GetKeyUp(KeyCode.UpArrow))
        {
            EndJumpEarly("Up arrow released");
        }
    }

    private void TryStartJump()
    {
        if (!controller.isGrounded)
            return;

        verticalVelocity = initialJumpVelocity;
        isJumping = true;
        isHoldingJump = true;
        jumpHoldTimer = 0f;

        Debug.Log("[AnimLog] Jump Start");
    }

    private void ContinueVariableJump()
    {
        if (!isJumping)
            return;

        if (!isHoldingJump)
            return;

        jumpHoldTimer += Time.deltaTime;

        if (jumpHoldTimer > maxJumpHoldTime)
        {
            isHoldingJump = false;
            return;
        }

        verticalVelocity += jumpHoldAcceleration * Time.deltaTime;
        verticalVelocity = Mathf.Min(verticalVelocity, maxUpwardVelocity);
    }

    private void EndJumpEarly(string reason)
    {
        if (!isJumping)
            return;

        if (!isHoldingJump)
            return;

        isHoldingJump = false;

        // 버튼을 빨리 떼면 상승 속도를 즉시 끊는다.
        // 다음 프레임부터 중력 때문에 떨어지기 시작한다.
        if (verticalVelocity > 0f)
            verticalVelocity = 0f;

        Debug.Log($"[AnimLog] Jump End Early - {reason}");
    }

private void HandleNormalAttackInput()
    {
        if (!Input.GetKeyDown(KeyCode.A))
            return;

        if (Time.time < lastNormalAttackTime + normalAttackCooldown)
            return;

        lastNormalAttackTime = Time.time;

        Vector2 normalAttackDirection = GetNormalAttackDirectionFromInput();
        ActivateNormalAttackCollider(normalAttackDirection);

        Debug.Log($"[AnimLog] Normal Attack - Direction: {DirectionToText(normalAttackDirection)}");
    }

private Vector2 GetNormalAttackDirectionFromInput()
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


private void ResolveNormalAttackColliders()
    {
        if (attackColliderRoot == null)
            attackColliderRoot = transform.Find("AttackColliders");

        if (normalAttackColliderLeft == null)
            normalAttackColliderLeft = FindNormalAttackCollider("NormalAttackCollider_Left");

        if (normalAttackColliderRight == null)
            normalAttackColliderRight = FindNormalAttackCollider("NormalAttackCollider_Right");

        if (normalAttackColliderUp == null)
            normalAttackColliderUp = FindNormalAttackCollider("NormalAttackCollider_Up");

        if (normalAttackColliderDown == null)
            normalAttackColliderDown = FindNormalAttackCollider("NormalAttackCollider_Down");
    }

    private Collider FindNormalAttackCollider(string colliderName)
    {
        Transform searchRoot = attackColliderRoot != null ? attackColliderRoot : transform;
        Transform found = searchRoot.Find(colliderName);

        if (found != null && found.TryGetComponent(out Collider foundCollider))
            return foundCollider;

        Collider[] colliders = transform.GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].name == colliderName)
                return colliders[i];
        }

        Debug.LogWarning($"[AnimLog] Missing normal attack collider: {colliderName}", this);
        return null;
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

        Debug.Log($"[AnimLog] Normal Attack Collider On - {targetCollider.name}");
    }

    private void UpdateNormalAttackColliderState()
    {
        if (activeNormalAttackCollider == null)
            return;

        if (Time.time < normalAttackColliderDisableTime)
            return;

        SetNormalAttackColliderEnabled(activeNormalAttackCollider, false);
        activeNormalAttackCollider = null;
    }

    private Collider GetNormalAttackCollider(Vector2 direction)
    {
        Vector2 fourWayDirection = GetFourWayDirection(direction);

        if (fourWayDirection == Vector2.left)
            return normalAttackColliderLeft;

        if (fourWayDirection == Vector2.right)
            return normalAttackColliderRight;

        if (fourWayDirection == Vector2.up)
            return normalAttackColliderUp;

        if (fourWayDirection == Vector2.down)
            return normalAttackColliderDown;

        return null;
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

        SetNormalAttackColliderDebugEnabled(targetCollider, enabled);
    }

private void ResolveNormalAttackColliderDebugVisuals()
    {
        normalAttackDebugLeft = CreateNormalAttackColliderDebugVisual(normalAttackColliderLeft, normalAttackDebugLeft);
        normalAttackDebugRight = CreateNormalAttackColliderDebugVisual(normalAttackColliderRight, normalAttackDebugRight);
        normalAttackDebugUp = CreateNormalAttackColliderDebugVisual(normalAttackColliderUp, normalAttackDebugUp);
        normalAttackDebugDown = CreateNormalAttackColliderDebugVisual(normalAttackColliderDown, normalAttackDebugDown);
    }

    private LineRenderer CreateNormalAttackColliderDebugVisual(Collider targetCollider, LineRenderer existingLine)
    {
        if (targetCollider == null)
            return existingLine;

        if (existingLine != null)
        {
            UpdateNormalAttackColliderDebugVisual(targetCollider, existingLine);
            existingLine.enabled = false;
            return existingLine;
        }

        Transform existingChild = targetCollider.transform.Find("DebugVisibleBounds");
        LineRenderer lineRenderer;

        if (existingChild != null && existingChild.TryGetComponent(out lineRenderer))
        {
            UpdateNormalAttackColliderDebugVisual(targetCollider, lineRenderer);
            lineRenderer.enabled = false;
            return lineRenderer;
        }

        GameObject debugObject = new GameObject("DebugVisibleBounds");
        debugObject.transform.SetParent(targetCollider.transform, false);

        lineRenderer = debugObject.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
        lineRenderer.positionCount = 4;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

        UpdateNormalAttackColliderDebugVisual(targetCollider, lineRenderer);
        lineRenderer.enabled = false;
        return lineRenderer;
    }

    private void UpdateNormalAttackColliderDebugVisual(Collider targetCollider, LineRenderer lineRenderer)
    {
        if (targetCollider == null || lineRenderer == null)
            return;

        BoxCollider boxCollider = targetCollider as BoxCollider;

        if (boxCollider == null)
            return;

        Vector3 center = boxCollider.center;
        Vector3 halfSize = boxCollider.size * 0.5f;
        float debugZ = center.z - halfSize.z - 0.01f;

        lineRenderer.SetPosition(0, new Vector3(center.x - halfSize.x, center.y - halfSize.y, debugZ));
        lineRenderer.SetPosition(1, new Vector3(center.x - halfSize.x, center.y + halfSize.y, debugZ));
        lineRenderer.SetPosition(2, new Vector3(center.x + halfSize.x, center.y + halfSize.y, debugZ));
        lineRenderer.SetPosition(3, new Vector3(center.x + halfSize.x, center.y - halfSize.y, debugZ));

        lineRenderer.startColor = normalAttackColliderDebugColor;
        lineRenderer.endColor = normalAttackColliderDebugColor;
        lineRenderer.startWidth = normalAttackColliderDebugLineWidth;
        lineRenderer.endWidth = normalAttackColliderDebugLineWidth;
    }

    private void SetNormalAttackColliderDebugEnabled(Collider targetCollider, bool enabled)
    {
        LineRenderer lineRenderer = GetNormalAttackColliderDebugVisual(targetCollider);

        if (lineRenderer == null)
            return;

        if (enabled)
            UpdateNormalAttackColliderDebugVisual(targetCollider, lineRenderer);

        lineRenderer.enabled = showNormalAttackColliderDebug && enabled;
    }

    private LineRenderer GetNormalAttackColliderDebugVisual(Collider targetCollider)
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


    private void OnDisable()
    {
        SetNormalAttackCollidersEnabled(false);
        SetChargedAttackPreviewEnabled(false);
        activeNormalAttackCollider = null;
    }


    private Vector2 GetFourWayDirection(Vector2 direction)
    {
        if (direction == Vector2.zero)
            return Vector2.right;

        Vector2 normalized = direction.normalized;

        if (Mathf.Abs(normalized.x) >= Mathf.Abs(normalized.y))
            return normalized.x >= 0f ? Vector2.right : Vector2.left;

        return normalized.y >= 0f ? Vector2.up : Vector2.down;
    }


    private void HandleChargeAttackInput()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            StartCharging();
        }

        if (Input.GetKeyUp(KeyCode.D))
        {
            ReleaseChargedAttack();
        }
    }

    private void StartCharging()
    {
        if (isCharging)
            return;

        isCharging = true;
        chargeTimer = 0f;

        Debug.Log("[AnimLog] Charge Start");
    }

    private void ReleaseChargedAttack()
    {
        if (!isCharging)
            return;

        isCharging = false;
        SetChargedAttackPreviewEnabled(false);

        if (requireGroundedForChargeAttack && !controller.isGrounded)
        {
            Debug.Log("[AnimLog] Charged Attack Failed - Not grounded");
            return;
        }

        // D를 뗀 바로 그 순간, 현재 입력 중인 방향키 조합을 다시 읽는다.
        // 입력이 있다면 그 방향으로 갱신하고, 입력이 없다면 마지막으로 바라보던 방향을 사용한다.
        Vector2 releaseDirectionInput = GetArrowAimDirection();

        if (releaseDirectionInput != Vector2.zero)
        {
            facingDirection = releaseDirectionInput.normalized;
            previousFacingDirection = facingDirection;
        }

        Vector3 dashDirection = new Vector3(
            facingDirection.x,
            facingDirection.y,
            0f
        ).normalized;

        float chargeRate = Mathf.Clamp01(chargeTimer / maxChargeTime);
        float requestedDashDistance = Mathf.Lerp(dashMinDistance, dashMaxDistance, chargeRate);

        float safeDashDistance = GetSafeDashDistance(dashDirection, requestedDashDistance);

        bool wasAirborneBeforeDash = !controller.isGrounded;
        Vector3 attackStartPosition = transform.position;

        controller.Move(dashDirection * safeDashDistance);

        Vector3 attackEndPosition = transform.position;
        CreateChargedAttackHitbox(attackStartPosition, attackEndPosition, dashDirection);

        // 공중 대시, 위 대시, 대각선 위 대시 이후에 낙하가 너무 빠른 문제를 완화한다.
        if (wasAirborneBeforeDash || dashDirection.y > 0.01f)
        {
            ApplyAirDashFallCorrection(dashDirection);
        }

        Debug.Log(
            $"[AnimLog] Charged Attack Release - Direction: {DirectionToText(facingDirection)}, " +
            $"Charge: {chargeTimer:0.00}s, " +
            $"Requested Dash: {requestedDashDistance:0.00}, " +
            $"Actual Dash: {safeDashDistance:0.00}"
        );

        chargeTimer = 0f;
    }

    private void ResolveChargedAttackColliderDebugVisual()
    {
        if (attackColliderRoot == null)
            attackColliderRoot = transform.Find("AttackColliders");

        if (chargedAttackRangeCollider == null)
        {
            Transform searchRoot = attackColliderRoot != null ? attackColliderRoot : transform;
            Transform found = searchRoot.Find("ChargedAttackRangeCollider");

            if (found != null)
                chargedAttackRangeCollider = found.GetComponent<Collider>();
        }

        if (chargedAttackRangeCollider == null)
        {
            GameObject colliderObject = new GameObject("ChargedAttackRangeCollider");
            colliderObject.transform.SetParent(attackColliderRoot != null ? attackColliderRoot : transform, false);
            chargedAttackRangeCollider = colliderObject.AddComponent<BoxCollider>();
        }

        chargedAttackRangeCollider.isTrigger = true;
        chargedAttackRangeCollider.enabled = false;
        chargedAttackDebugLine = CreateChargedAttackDebugVisual(chargedAttackRangeCollider, chargedAttackDebugLine, false);
    }

    private void CacheChargedAttackRangeOffsets()
    {
        chargedAttackRangeLocalPositionOffset = Vector3.zero;
        chargedAttackRangeColliderCenterOffset = Vector3.zero;

        if (chargedAttackRangeCollider == null)
            return;

        chargedAttackRangeLocalPositionOffset = chargedAttackRangeCollider.transform.localPosition;

        BoxCollider boxCollider = chargedAttackRangeCollider as BoxCollider;

        if (boxCollider != null)
            chargedAttackRangeColliderCenterOffset = boxCollider.center;
    }


    private LineRenderer CreateChargedAttackDebugVisual(Collider targetCollider, LineRenderer existingLine, bool worldSpace)
    {
        if (targetCollider == null)
            return existingLine;

        if (existingLine == null)
            existingLine = targetCollider.GetComponent<LineRenderer>();

        if (existingLine == null)
            existingLine = targetCollider.gameObject.AddComponent<LineRenderer>();

        existingLine.useWorldSpace = worldSpace;
        existingLine.loop = true;
        existingLine.positionCount = 4;
        existingLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        existingLine.receiveShadows = false;

        if (existingLine.sharedMaterial == null)
            existingLine.material = new Material(Shader.Find("Sprites/Default"));

        existingLine.enabled = false;
        return existingLine;
    }

    private void UpdateChargedAttackPreview()
    {
        if (!isCharging)
            return;

        ResolveChargedAttackColliderDebugVisual();

        Vector3 dashDirection = new Vector3(facingDirection.x, facingDirection.y, 0f).normalized;
        float chargeRate = Mathf.Clamp01(chargeTimer / maxChargeTime);
        float requestedDashDistance = Mathf.Lerp(dashMinDistance, dashMaxDistance, chargeRate);
        float safeDashDistance = GetSafeDashDistance(dashDirection, requestedDashDistance);

        UpdateChargedAttackColliderShape(chargedAttackRangeCollider, transform.position, transform.position + dashDirection * safeDashDistance, dashDirection);
        UpdateChargedAttackDebugVisual(chargedAttackRangeCollider, chargedAttackDebugLine);
        SetChargedAttackPreviewEnabled(true);
    }

    private void SetChargedAttackPreviewEnabled(bool enabled)
    {
        if (chargedAttackRangeCollider != null)
            chargedAttackRangeCollider.enabled = false;

        if (chargedAttackDebugLine != null)
            chargedAttackDebugLine.enabled = showChargedAttackColliderDebug && enabled;
    }

    private void CreateChargedAttackHitbox(Vector3 startPosition, Vector3 endPosition, Vector3 dashDirection)
    {
        ResolveChargedAttackColliderDebugVisual();

        GameObject hitboxObject = new GameObject("ChargedAttackHitbox_Runtime");
        hitboxObject.layer = chargedAttackRangeCollider != null ? chargedAttackRangeCollider.gameObject.layer : gameObject.layer;

        BoxCollider hitboxCollider = hitboxObject.AddComponent<BoxCollider>();
        hitboxCollider.isTrigger = true;
        hitboxCollider.enabled = true;

        LineRenderer hitboxLine = CreateChargedAttackDebugVisual(hitboxCollider, null, false);

        UpdateChargedAttackColliderShape(hitboxCollider, startPosition, endPosition, dashDirection);
        UpdateChargedAttackDebugVisual(hitboxCollider, hitboxLine);
        hitboxLine.enabled = showChargedAttackColliderDebug;

        Destroy(hitboxObject, Mathf.Max(0.01f, chargedAttackColliderActiveDuration));
    }

    private void UpdateChargedAttackColliderShape(Collider targetCollider, Vector3 startPosition, Vector3 endPosition, Vector3 dashDirection)
    {
        BoxCollider boxCollider = targetCollider as BoxCollider;

        if (boxCollider == null)
            return;

        Vector3 path = endPosition - startPosition;
        float pathLength = Mathf.Max(0.01f, path.magnitude);
        Vector3 direction = dashDirection.sqrMagnitude > 0.0001f ? dashDirection.normalized : Vector3.right;
        Vector3 localOffset = targetCollider == chargedAttackRangeCollider
            ? chargedAttackRangeLocalPositionOffset + chargedAttackRangeColliderCenterOffset
            : chargedAttackRangeLocalPositionOffset + chargedAttackRangeColliderCenterOffset;
        Vector3 worldOffset = transform.TransformVector(localOffset);
        Vector3 center = startPosition + direction * (pathLength * 0.5f) + worldOffset;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        boxCollider.transform.position = center;
        boxCollider.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        boxCollider.transform.localScale = Vector3.one;
        boxCollider.center = Vector3.zero;
        boxCollider.size = new Vector3(pathLength, chargedAttackRangeWidth, 1f);
    }

    private void UpdateChargedAttackDebugVisual(Collider targetCollider, LineRenderer lineRenderer)
    {
        if (targetCollider == null || lineRenderer == null)
            return;

        BoxCollider boxCollider = targetCollider as BoxCollider;

        if (boxCollider == null)
            return;

        Vector3 center = boxCollider.center;
        Vector3 halfSize = boxCollider.size * 0.5f;
        float debugZ = center.z - halfSize.z - 0.01f;

        lineRenderer.SetPosition(0, new Vector3(center.x - halfSize.x, center.y - halfSize.y, debugZ));
        lineRenderer.SetPosition(1, new Vector3(center.x - halfSize.x, center.y + halfSize.y, debugZ));
        lineRenderer.SetPosition(2, new Vector3(center.x + halfSize.x, center.y + halfSize.y, debugZ));
        lineRenderer.SetPosition(3, new Vector3(center.x + halfSize.x, center.y - halfSize.y, debugZ));

        lineRenderer.startColor = chargedAttackColliderDebugColor;
        lineRenderer.endColor = chargedAttackColliderDebugColor;
        lineRenderer.startWidth = chargedAttackColliderDebugLineWidth;
        lineRenderer.endWidth = chargedAttackColliderDebugLineWidth;
    }

    private void ApplyAirDashFallCorrection(Vector3 dashDirection)
    {
        airDashFloatTimer = airDashFloatDuration;

        // 핵심:
        // 대시 직후 기존 낙하 속도가 너무 크면 완화한다.
        // 예를 들어 verticalVelocity가 -18이었다면 -2 정도로 줄인다.
        if (verticalVelocity < maxFallSpeedAfterAirDash)
        {
            verticalVelocity = maxFallSpeedAfterAirDash;
        }

        // 위쪽 성분이 있는 대시라면 하강 속도를 더 강하게 끊어준다.
        // Up / Up-Right / Up-Left 대시가 시원하게 느껴진다.
        if (dashDirection.y > 0.01f && verticalVelocity < 0f)
        {
            verticalVelocity = 0f;
        }

        Debug.Log("[AnimLog] Air Dash Fall Correction");
    }


    private float GetSafeDashDistance(Vector3 direction, float requestedDistance)
    {
        direction.Normalize();

        GetControllerCapsuleWorldPoints(out Vector3 bottom, out Vector3 top, out float radius);

        bool hitSomething = Physics.CapsuleCast(
            bottom,
            top,
            radius,
            direction,
            out RaycastHit hit,
            requestedDistance + dashWallBuffer,
            dashObstacleMask,
            QueryTriggerInteraction.Ignore
        );

        if (!hitSomething)
            return requestedDistance;

        float safeDistance = hit.distance - dashWallBuffer;
        return Mathf.Max(0f, safeDistance);
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

    private void ApplyMovement()
    {
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedStickVelocity;

            if (isJumping)
            {
                isJumping = false;
                isHoldingJump = false;
                Debug.Log("[AnimLog] Land");
            }
        }

        float gravityMultiplier = 1f;

        if (airDashFloatTimer > 0f && !controller.isGrounded)
        {
            airDashFloatTimer -= Time.deltaTime;
            gravityMultiplier = airDashGravityMultiplier;
        }

        verticalVelocity += gravity * gravityMultiplier * Time.deltaTime;

        Vector3 moveDelta = new Vector3(
            horizontalVelocity * Time.deltaTime,
            verticalVelocity * Time.deltaTime,
            0f
        );

        CollisionFlags flags = controller.Move(moveDelta);

        if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
        {
            verticalVelocity = 0f;
            isHoldingJump = false;

            Debug.Log("[AnimLog] Jump End Early - Hit Ceiling");
        }

        if ((flags & CollisionFlags.Below) != 0 && verticalVelocity <= 0f)
        {
            if (isJumping)
            {
                isJumping = false;
                isHoldingJump = false;

                Debug.Log("[AnimLog] Land");
            }
        }
    }

    private void LockSidePlane()
    {
        Vector3 position = transform.position;
        position.z = lockedZ;
        transform.position = position;
    }

    private string DirectionToText(Vector2 direction)
    {
        Vector2 normalized = direction.normalized;

        float x = normalized.x;
        float y = normalized.y;

        bool right = x > 0.5f;
        bool left = x < -0.5f;
        bool up = y > 0.5f;
        bool down = y < -0.5f;

        if (up && right)
            return "Up-Right";

        if (up && left)
            return "Up-Left";

        if (down && right)
            return "Down-Right";

        if (down && left)
            return "Down-Left";

        if (right)
            return "Right";

        if (left)
            return "Left";

        if (up)
            return "Up";

        if (down)
            return "Down";

        return "None";
    }
}