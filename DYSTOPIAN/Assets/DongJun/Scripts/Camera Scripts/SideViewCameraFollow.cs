using UnityEngine;

[DisallowMultipleComponent]
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
    private Vector3 followPosition;
    private Vector3 shakeOffset;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        followPosition = transform.position;

        if (!forceOrthographic)
            return;

        cam.orthographic = true;
        cam.orthographicSize = orthographicSize;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredPosition = target.position + offset;
        if (!followY)
            desiredPosition.y = followPosition.y;

        desiredPosition.z = offset.z;
        if (ShouldClampToBounds())
            desiredPosition = ClampCameraPositionToBounds(desiredPosition);

        Vector3 smoothedPosition = Vector3.SmoothDamp(
            followPosition,
            desiredPosition,
            ref velocity,
            smoothTime);

        // Smoothing can cross the boundary near its edge.
        if (ShouldClampToBounds())
            smoothedPosition = ClampCameraPositionToBounds(smoothedPosition);

        followPosition = smoothedPosition;
        transform.position = followPosition + shakeOffset;
    }

    public void SetBounds(CameraBounds2D newBounds)
    {
        cameraBounds = newBounds;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SetShakeOffset(Vector3 newShakeOffset)
    {
        shakeOffset = newShakeOffset;
    }

    private bool ShouldClampToBounds()
    {
        return clampToBounds && cameraBounds != null && cam.orthographic;
    }

    private Vector3 ClampCameraPositionToBounds(Vector3 position)
    {
        GetCameraHalfExtents(out float halfWidth, out float halfHeight);

        Vector2 min = cameraBounds.Min;
        Vector2 max = cameraBounds.Max;
        position.x = ClampEvenIfBoundsAreTooSmall(
            position.x,
            min.x + halfWidth,
            max.x - halfWidth,
            min.x,
            max.x);
        position.y = ClampEvenIfBoundsAreTooSmall(
            position.y,
            min.y + halfHeight,
            max.y - halfHeight,
            min.y,
            max.y);
        return position;
    }

    private void GetCameraHalfExtents(out float halfWidth, out float halfHeight)
    {
        halfHeight = cam.orthographicSize;
        halfWidth = halfHeight * GetActualCameraAspect();
    }

    private float GetActualCameraAspect()
    {
        if (cam.targetTexture != null && cam.targetTexture.height > 0)
            return (float)cam.targetTexture.width / cam.targetTexture.height;

        return cam.aspect;
    }

    private static float ClampEvenIfBoundsAreTooSmall(
        float value,
        float minCameraPosition,
        float maxCameraPosition,
        float rawMin,
        float rawMax)
    {
        if (minCameraPosition > maxCameraPosition)
            return (rawMin + rawMax) * 0.5f;

        return Mathf.Clamp(value, minCameraPosition, maxCameraPosition);
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
            Gizmos.DrawWireCube(
                new Vector3(transform.position.x, transform.position.y, 0f),
                new Vector3(halfWidth * 2f, halfHeight * 2f, 0f));
        }

        if (!drawCameraCenterLimitGizmo || cameraBounds == null)
            return;

        Vector2 min = cameraBounds.Min;
        Vector2 max = cameraBounds.Max;
        float minCameraX = min.x + halfWidth;
        float maxCameraX = max.x - halfWidth;
        float minCameraY = min.y + halfHeight;
        float maxCameraY = max.y - halfHeight;

        if (minCameraX > maxCameraX || minCameraY > maxCameraY)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(
            new Vector3(
                (minCameraX + maxCameraX) * 0.5f,
                (minCameraY + maxCameraY) * 0.5f,
                0f),
            new Vector3(
                maxCameraX - minCameraX,
                maxCameraY - minCameraY,
                0f));
    }
}
