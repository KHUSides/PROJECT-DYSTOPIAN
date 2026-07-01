using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SideViewCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;

    [Header("Follow")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -10f);
    [SerializeField] private float smoothTime = 0.12f;
    [SerializeField] private bool followY = true;

    [Header("Camera")]
    [SerializeField] private bool forceOrthographic = true;
    [SerializeField] private float orthographicSize = 5.5f;

    private Vector3 velocity;

    private void Awake()
    {
        Camera cam = GetComponent<Camera>();

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

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref velocity,
            smoothTime
        );

        // Unity 기본 카메라는 Rotation (0,0,0)일 때 +Z 방향을 바라본다.
        // 즉, Z = -10 위치에서 Z = 0 쪽의 플레이어를 본다.
        transform.rotation = Quaternion.Euler(0f, 0f, 0f);
    }
}