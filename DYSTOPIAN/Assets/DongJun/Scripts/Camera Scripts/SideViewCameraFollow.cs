using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SideViewCameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Follow")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -10f);
    [SerializeField] private float smoothTime = 0.12f;
    [SerializeField] private bool followY = true;

    [Header("Camera")]
    [SerializeField] private bool forceOrthographic = true;
    [SerializeField] private float orthographicSize = 10f;

    [Header("Bounds")]
    [SerializeField] private CameraBounds2D cameraBounds;
    [SerializeField] private bool clampToBounds = true;

    [Header("Debug")]
    [SerializeField] private bool drawCameraViewGizmo = true;
    [SerializeField] private bool drawCameraCenterLimitGizmo = true;

    private Camera cam;
    private Vector3 velocity;

    private void Awake()
    {
        cam = GetComponent<Camera>();

        if (forceOrthographic)
        {
            cam.orthographic = true;
            cam.orthographicSize = orthographicSize;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredPosition = target.position + offset;

        if (!followY)
            desiredPosition.y = transform.position.y;

        desiredPosition.z = offset.z;

        if (clampToBounds && cameraBounds != null && cam.orthographic)
        {
            desiredPosition = ClampCameraPositionToBounds(desiredPosition);
        }

        Vector3 smoothedPosition = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref velocity,
            smoothTime
        );

        // 중요:
        // SmoothDamp 결과도 다시 Clamp한다.
        // 그래야 시작 위치가 바깥이거나 경계 근처에서 애매할 때도
        // 화면이 바운더리 밖으로 새지 않는다.
        if (clampToBounds && cameraBounds != null && cam.orthographic)
        {
            smoothedPosition = ClampCameraPositionToBounds(smoothedPosition);
        }

        transform.position = smoothedPosition;
        transform.rotation = Quaternion.Euler(0f, 0f, 0f);
    }

    private Vector3 ClampCameraPositionToBounds(Vector3 position)
    {
        GetCameraHalfExtents(out float halfWidth, out float halfHeight);

        Vector2 min = cameraBounds.Min;
        Vector2 max = cameraBounds.Max;

        float minCameraX = min.x + halfWidth;
        float maxCameraX = max.x - halfWidth;

        float minCameraY = min.y + halfHeight;
        float maxCameraY = max.y - halfHeight;

        position.x = ClampEvenIfBoundsAreTooSmall(
            position.x,
            minCameraX,
            maxCameraX,
            min.x,
            max.x
        );

        position.y = ClampEvenIfBoundsAreTooSmall(
            position.y,
            minCameraY,
            maxCameraY,
            min.y,
            max.y
        );

        return position;
    }

    private void GetCameraHalfExtents(out float halfWidth, out float halfHeight)
    {
        halfHeight = cam.orthographicSize;

        float aspect = GetActualCameraAspect();
        halfWidth = halfHeight * aspect;
    }

    private float GetActualCameraAspect()
    {
        // 픽셀 렌더링을 켠 경우,
        // Main Camera는 화면이 아니라 RenderTexture에 렌더링한다.
        // 이때는 targetTexture의 비율을 기준으로 계산하는 편이 안전하다.
        if (cam.targetTexture != null && cam.targetTexture.height > 0)
        {
            return (float)cam.targetTexture.width / cam.targetTexture.height;
        }

        return cam.aspect;
    }

    private float ClampEvenIfBoundsAreTooSmall(
        float value,
        float minCameraPosition,
        float maxCameraPosition,
        float rawMin,
        float rawMax
    )
    {
        // 카메라 화면 크기가 보더보다 더 큰 경우,
        // 카메라 중심을 보더 중앙에 고정한다.
        if (minCameraPosition > maxCameraPosition)
        {
            return (rawMin + rawMax) * 0.5f;
        }

        return Mathf.Clamp(value, minCameraPosition, maxCameraPosition);
    }

    public void SetBounds(CameraBounds2D newBounds)
    {
        cameraBounds = newBounds;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawCameraViewGizmo && !drawCameraCenterLimitGizmo)
            return;

        if (cam == null)
            cam = GetComponent<Camera>();

        if (cam == null || !cam.orthographic)
            return;

        GetCameraHalfExtents(out float halfWidth, out float halfHeight);

        if (drawCameraViewGizmo)
        {
            Gizmos.color = Color.yellow;

            Vector3 cameraViewSize = new Vector3(
                halfWidth * 2f,
                halfHeight * 2f,
                0f
            );

            Gizmos.DrawWireCube(
                new Vector3(transform.position.x, transform.position.y, 0f),
                cameraViewSize
            );
        }

        if (drawCameraCenterLimitGizmo && cameraBounds != null)
        {
            Vector2 min = cameraBounds.Min;
            Vector2 max = cameraBounds.Max;

            float minCameraX = min.x + halfWidth;
            float maxCameraX = max.x - halfWidth;
            float minCameraY = min.y + halfHeight;
            float maxCameraY = max.y - halfHeight;

            if (minCameraX <= maxCameraX && minCameraY <= maxCameraY)
            {
                Gizmos.color = Color.green;

                Vector3 center = new Vector3(
                    (minCameraX + maxCameraX) * 0.5f,
                    (minCameraY + maxCameraY) * 0.5f,
                    0f
                );

                Vector3 size = new Vector3(
                    maxCameraX - minCameraX,
                    maxCameraY - minCameraY,
                    0f
                );

                Gizmos.DrawWireCube(center, size);
            }
        }
    }
}