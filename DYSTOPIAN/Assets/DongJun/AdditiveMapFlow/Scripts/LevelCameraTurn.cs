using UnityEngine;

// A camera shot sampled from route distance. Waiting or changing speed never advances it.
[RequireComponent(typeof(AdditiveMapSection))]
public sealed class LevelCameraTurn : MonoBehaviour
{
    public Transform[] routePoints;
    public float startYaw;
    public float endYaw;
    public bool followPathHeading;
    public bool smoothRotation = true;
    public bool extendBeforeStart;
    public bool extendAfterEnd;
    public float startSize = -1f;
    public float endSize = -1f;
    public Transform entryCameraPosition;
    public Transform exitCameraPosition;
    [Min(0.1f)] public float maxRouteDistance = 6f;
    [Min(1f)] public float zoomOut = 1.3f;
    [Range(0f, 45f)] public float lookDown = 15f;
    public float focusHeight = 2f;
    [Range(0.01f, 0.5f)] public float fadeFraction = 0.2f;
    [Range(0f, 1f)] public float startStrength;
    [Range(0f, 1f)] public float endStrength;

    public bool Sample(Vector3 position, out Quaternion rotation, out float zoom, out float progress, out float strength)
    {
        rotation = Quaternion.identity;
        zoom = 1f;
        progress = strength = 0f;
        if (!isActiveAndEnabled || routePoints == null || routePoints.Length < 2) return false;
        float best = float.PositiveInfinity, distance = 0f, travelled = 0f;
        for (int i = 0; i < routePoints.Length - 1; i++)
        {
            if (routePoints[i] == null || routePoints[i + 1] == null) return false;
            Vector3 a = routePoints[i].position, delta = routePoints[i + 1].position - a;
            float length = delta.magnitude;
            if (length < 0.001f) continue;
            float t = Vector3.Dot(position - a, delta) / (length * length);
            if (i > 0) t = Mathf.Max(0f, t);
            if (i < routePoints.Length - 2) t = Mathf.Min(1f, t);
            float square = (position - a - delta * t).sqrMagnitude;
            if (square < best) { best = square; distance = travelled + t * length; }
            travelled += length;
        }
        if (travelled < 0.001f || best > maxRouteDistance * maxRouteDistance || (!extendBeforeStart && distance < -0.001f) || (!extendAfterEnd && distance > travelled + 0.001f)) return false;
        progress = Mathf.Clamp01(distance / travelled);
        float fade = Mathf.Max(0.01f, fadeFraction);
        strength = Mathf.Lerp(startStrength, 1f, Mathf.SmoothStep(0f, 1f, progress / fade))
                 * Mathf.Lerp(endStrength, 1f, Mathf.SmoothStep(0f, 1f, (1f - progress) / fade));
        float yaw = Mathf.LerpAngle(startYaw, endYaw, smoothRotation ? Mathf.SmoothStep(0f, 1f, progress) : progress);
        if (followPathHeading)
        {
            Vector3 direction = PointAt(distance + 0.8f) - PointAt(distance - 0.8f);
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f) yaw = Quaternion.LookRotation(Vector3.Cross(direction.normalized, Vector3.up)).eulerAngles.y;
        }
        rotation = Quaternion.Euler(lookDown * strength, yaw, 0f);
        zoom = Mathf.Lerp(1f, Mathf.Max(1f, zoomOut), strength);
        return true;
    }

    private Vector3 PointAt(float distance)
    {
        for (int i = 0; i < routePoints.Length - 1; i++)
        {
            Vector3 a = routePoints[i].position, b = routePoints[i + 1].position;
            float length = Vector3.Distance(a, b);
            if (length < 0.001f) continue;
            if (distance <= length || i == routePoints.Length - 2) return Vector3.LerpUnclamped(a, b, distance / length);
            distance -= length;
        }
        return routePoints[0].position;
    }
}
