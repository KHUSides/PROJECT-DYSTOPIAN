using UnityEngine;

/// <summary>
/// CharacterController 플레이어용 한 방향 발판입니다.
/// 발판 자체는 항상 충돌하므로 적은 위에 유지되고,
/// 플레이어와의 충돌만 아래 통과 규칙에 따라 제어합니다.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public sealed class OneWayPlatform : MonoBehaviour
{
    [SerializeField, Min(0f)] private float topSurfaceTolerance = 0.03f;

    private BoxCollider platformCollider;
    private PlayerController player;
    private CharacterController playerController;
    private bool hasLandedOnPlatform;

    private void Awake()
    {
        platformCollider = GetComponent<BoxCollider>();
        // 적을 포함한 다른 물체가 게임 시작부터 발판 위에 설 수 있게 항상 활성화합니다.
        platformCollider.enabled = true;
        FindPlayer();
        UpdatePlayerCollision();
    }

    private void LateUpdate()
    {
        if (playerController == null)
            FindPlayer();

        UpdatePlayerCollision();
    }

    private void FindPlayer()
    {
        player = FindFirstObjectByType<PlayerController>();
        playerController = player != null ? player.GetComponent<CharacterController>() : null;
    }

    private void UpdatePlayerCollision()
    {
        if (platformCollider == null || player == null || playerController == null)
            return;

        GetWorldBounds(out float left, out float right, out float top);
        Bounds playerBounds = playerController.bounds;
        bool isOverPlatform = playerBounds.max.x > left && playerBounds.min.x < right;

        // 발판의 좌우를 벗어날 때만 다시 아래로 내려갈 수 있습니다.
        if (!isOverPlatform)
        {
            hasLandedOnPlatform = false;
        }
        // 상승 중에는 절대 충돌시키지 않아 발판 밑을 통과하게 합니다.
        else if (player.VerticalSpeed <= 0f &&
                 playerBounds.min.y >= top - topSurfaceTolerance)
        {
            hasLandedOnPlatform = true;
        }

        // 발판 콜라이더를 끄지 않아 적과 다른 물체는 항상 발판 위에 남습니다.
        Physics.IgnoreCollision(playerController, platformCollider, !hasLandedOnPlatform);
    }

    private void OnDestroy()
    {
        if (playerController != null && platformCollider != null)
            Physics.IgnoreCollision(playerController, platformCollider, false);
    }

    // Collider.bounds는 비활성화 시 비어 있으므로, 비활성 상태에서도 쓸 수 있는 월드 좌표를 계산합니다.
    private void GetWorldBounds(out float left, out float right, out float top)
    {
        Vector3 scale = transform.lossyScale;
        Vector3 center = transform.TransformPoint(platformCollider.center);
        Vector3 halfSize = Vector3.Scale(platformCollider.size, new Vector3(
            Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z))) * 0.5f;

        left = center.x - halfSize.x;
        right = center.x + halfSize.x;
        top = center.y + halfSize.y;
    }
}
