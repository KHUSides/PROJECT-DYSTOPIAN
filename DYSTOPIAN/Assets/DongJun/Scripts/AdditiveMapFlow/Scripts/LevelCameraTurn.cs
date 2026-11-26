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
        if (!TryGetRouteDistance(position, out float distance, out float length)) return false;

        progress = Mathf.Clamp01(distance / length);
        strength = CalculateStrength(progress);
        rotation = Quaternion.Euler(lookDown * strength, CalculateYaw(distance, progress), 0f);
        zoom = Mathf.Lerp(1f, Mathf.Max(1f, zoomOut), strength);
        return true;
    }

    public Vector3 ConstrainFocus(Vector3 focus, Quaternion rotation, float aspect, ref float size)
    {
        Vector3 result;
        Vector3 right = rotation * Vector3.right;
        if (TryConstrainFocus(focus, right, size * aspect, out result)) return result;
        // Reduce zoom only if the view cannot fit between the two door planes at all.
        float low = 0f, high = size;
        Vector3 best = focus;
        for (int i = 0; i < 12; i++)
        {
            float candidate = (low + high) * 0.5f;
            if (TryConstrainFocus(focus, right, candidate * aspect, out result))
            {
                low = candidate;
                best = result;
            }
            else high = candidate;
        }
        if (low > 0.01f) size = low;
        return best;
    }

    private bool TryConstrainFocus(Vector3 focus, Vector3 right, float halfWidth, out Vector3 result)
    {
        result = focus;
        if (routePoints == null || routePoints.Length < 2) return true;
        int last = routePoints.Length - 1;
        Vector3 entry = routePoints[0].position;
        Vector3 exit = routePoints[last].position;
        Vector3 entryNormal = routePoints[1].position - entry;
        Vector3 exitNormal = routePoints[last - 1].position - exit;
        entryNormal.y = exitNormal.y = 0f;
        if (entryNormal.sqrMagnitude < 0.000001f || exitNormal.sqrMagnitude < 0.000001f) return true;
        entryNormal.Normalize();
        exitNormal.Normalize();
        float entryMargin = Mathf.Abs(Vector3.Dot(entryNormal, right)) * halfWidth;
        float exitMargin = Mathf.Abs(Vector3.Dot(exitNormal, right)) * halfWidth;
        if (Vector3.Dot(focus - entry, entryNormal) >= entryMargin
            && Vector3.Dot(focus - exit, exitNormal) >= exitMargin) return true;

        float nearest = float.PositiveInfinity;
        bool found = false;
        for (int i = 0; i < last; i++)
        {
            Vector3 a = routePoints[i].position, b = routePoints[i + 1].position;
            a.y = b.y = focus.y;
            Vector3 delta = b - a;
            if (delta.sqrMagnitude < 0.000001f) continue;
            float min = 0f, max = 1f;
            if (!ClipFocusInterval(Vector3.Dot(a - entry, entryNormal) - entryMargin, Vector3.Dot(delta, entryNormal), ref min, ref max)
                || !ClipFocusInterval(Vector3.Dot(a - exit, exitNormal) - exitMargin, Vector3.Dot(delta, exitNormal), ref min, ref max)) continue;
            float t = Mathf.Clamp(Vector3.Dot(focus - a, delta) / delta.sqrMagnitude, min, max);
            Vector3 point = a + delta * t;
            float squared = (point - focus).sqrMagnitude;
            if (squared >= nearest) continue;
            nearest = squared;
            result = point;
            found = true;
        }
        return found;
    }

    private static bool ClipFocusInterval(float value, float slope, ref float min, ref float max)
    {
        if (Mathf.Abs(slope) < 0.000001f) return value >= -0.00001f;
        float t = -value / slope;
        if (slope > 0f) min = Mathf.Max(min, t);
        else max = Mathf.Min(max, t);
        return min <= max;
    }


    private bool TryGetRouteDistance(Vector3 position, out float distance, out float totalLength)
    {
        distance = totalLength = 0f;
        if (!isActiveAndEnabled || routePoints == null || routePoints.Length < 2) return false;
        float nearestSquaredDistance = float.PositiveInfinity;
        for (int i = 0; i < routePoints.Length - 1; i++)
        {
            if (routePoints[i] == null || routePoints[i + 1] == null) return false;
            Vector3 start = routePoints[i].position;
            Vector3 delta = routePoints[i + 1].position - start;
            float segmentLength = delta.magnitude;
            if (segmentLength < 0.001f) continue;

            float t = Vector3.Dot(position - start, delta) / (segmentLength * segmentLength);
            if (i > 0) t = Mathf.Max(0f, t);
            if (i < routePoints.Length - 2) t = Mathf.Min(1f, t);
            float squaredDistance = (position - start - delta * t).sqrMagnitude;
            if (squaredDistance < nearestSquaredDistance)
            {
                nearestSquaredDistance = squaredDistance;
                distance = totalLength + t * segmentLength;
            }
            totalLength += segmentLength;
        }
        if (totalLength < 0.001f || nearestSquaredDistance > maxRouteDistance * maxRouteDistance) return false;
        if (!extendBeforeStart && distance < -0.001f) return false;
        if (!extendAfterEnd && distance > totalLength + 0.001f) return false;
        return true;
    }

    private float CalculateStrength(float progress)
    {
        float fade = Mathf.Max(0.01f, fadeFraction);
        float entering = Mathf.Lerp(startStrength, 1f, Mathf.SmoothStep(0f, 1f, progress / fade));
        float leaving = Mathf.Lerp(endStrength, 1f, Mathf.SmoothStep(0f, 1f, (1f - progress) / fade));
        return entering * leaving;
    }

    private float CalculateYaw(float distance, float progress)
    {
        float t = smoothRotation ? Mathf.SmoothStep(0f, 1f, progress) : progress;
        float yaw = Mathf.LerpAngle(startYaw, endYaw, t);
        if (!followPathHeading) return yaw;

        Vector3 direction = PointAt(distance + 0.8f) - PointAt(distance - 0.8f);
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f) return yaw;
        return Quaternion.LookRotation(Vector3.Cross(direction.normalized, Vector3.up)).eulerAngles.y;
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

