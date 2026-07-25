using UnityEngine;

[DisallowMultipleComponent]
public class CameraBounds2D : MonoBehaviour
{
    [Header("Manual Bounds")]
    [SerializeField] private Vector2 size = new Vector2(81f, 22f);

    [Header("Auto Fit")]
    [SerializeField] private bool autoFitOnValidate = false;
    [SerializeField] private Collider[] fitColliders;
    [SerializeField] private Vector2 padding = Vector2.zero;
    [SerializeField] private float overrideTopY = 21f;
    [SerializeField] private bool useOverrideTopY = true;

    [Header("Debug")]
    [SerializeField] private Color fillColor = new Color(0f, 1f, 1f, 0.12f);
    [SerializeField] private Color lineColor = new Color(0f, 1f, 1f, 1f);

    public Vector2 Size => size;
    public Vector2 Center => new Vector2(transform.position.x, transform.position.y);
    public Vector2 Min => Center - size * 0.5f;
    public Vector2 Max => Center + size * 0.5f;

    public void SetFromMinMax(Vector2 min, Vector2 max)
    {
        Vector2 center = (min + max) * 0.5f;
        Vector2 newSize = max - min;
        transform.position = new Vector3(center.x, center.y, transform.position.z);
        size = new Vector2(
            Mathf.Max(0.01f, newSize.x),
            Mathf.Max(0.01f, newSize.y));
    }

    [ContextMenu("Fit Bounds To Colliders")]
    public void FitBoundsToColliders()
    {
        if (fitColliders == null || fitColliders.Length == 0)
        {
            Debug.LogWarning("[CameraBounds2D] Fit Colliders is empty.", this);
            return;
        }

        bool hasBounds = false;
        Bounds combinedBounds = new Bounds();

        foreach (Collider targetCollider in fitColliders)
        {
            if (targetCollider == null)
                continue;

            if (!hasBounds)
            {
                combinedBounds = targetCollider.bounds;
                hasBounds = true;
                continue;
            }

            combinedBounds.Encapsulate(targetCollider.bounds);
        }

        if (!hasBounds)
        {
            Debug.LogWarning("[CameraBounds2D] No valid colliders were found.", this);
            return;
        }

        Vector2 min = new Vector2(combinedBounds.min.x, combinedBounds.min.y);
        Vector2 max = new Vector2(combinedBounds.max.x, combinedBounds.max.y);
        if (useOverrideTopY)
            max.y = overrideTopY;

        SetFromMinMax(min - padding, max + padding);
        Debug.Log(
            $"[CameraBounds2D] Fitted Bounds - Min: {Min}, Max: {Max}, Size: {Size}",
            this);
    }

    private void OnValidate()
    {
        if (autoFitOnValidate)
            FitBoundsToColliders();
    }

    private void OnDrawGizmos()
    {
        Vector3 center = new Vector3(Center.x, Center.y, 0f);
        Vector3 drawSize = new Vector3(size.x, size.y, 0f);

        Gizmos.color = fillColor;
        Gizmos.DrawCube(center, drawSize);
        Gizmos.color = lineColor;
        Gizmos.DrawWireCube(center, drawSize);

#if UNITY_EDITOR
        UnityEditor.Handles.color = lineColor;
        Vector2 min = Min;
        Vector2 max = Max;
        UnityEditor.Handles.Label(
            new Vector3(min.x, max.y, 0f),
            $"Min X: {min.x:0.##}\nMax Y: {max.y:0.##}");
        UnityEditor.Handles.Label(
            new Vector3(max.x, min.y, 0f),
            $"Max X: {max.x:0.##}\nMin Y: {min.y:0.##}");
#endif
    }
}
