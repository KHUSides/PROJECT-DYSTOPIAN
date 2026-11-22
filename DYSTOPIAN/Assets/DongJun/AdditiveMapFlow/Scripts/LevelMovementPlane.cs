using UnityEngine;

public sealed class LevelMovementPlane : MonoBehaviour
{
    private Transform plane;
    private Transform[] points;
    public bool IsActive => plane != null;

    public void SetSection(Transform movementPlane, Transform[] path)
    {
        plane = movementPlane;
        points = path;
        Prepare(0f);
    }

    public void Prepare(float input)
    {
        if (plane == null) return;
        Vector3 right = plane.right;
        if (points != null && points.Length > 1)
        {
            float distance = NearestDistance(transform.position);
            Vector3 a = PointAt(distance - 0.8f);
            Vector3 b = PointAt(distance + 0.8f);
            right = (b - a).normalized;
        }
        right.y = 0f;
        if (right.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(Vector3.Cross(right.normalized, Vector3.up), Vector3.up);
    }

    public Vector3 Displacement(float horizontal, float vertical)
    {
        if (points == null || points.Length < 2)
            return transform.right * horizontal + Vector3.up * vertical;
        Vector3 next = PointAt(NearestDistance(transform.position) + horizontal);
        return new Vector3(next.x - transform.position.x, vertical, next.z - transform.position.z);
    }

    public Vector3 Project(Vector3 position, float depth = 0f)
    {
        if (plane == null) return position;
        Vector3 result;
        if (points != null && points.Length > 1)
        {
            result = PointAt(NearestDistance(position));
            result.y = position.y;
        }
        else
        {
            Vector3 local = plane.InverseTransformPoint(position);
            local.z = 0f;
            result = plane.TransformPoint(local);
        }
        return result + transform.forward * depth;
    }

    private float NearestDistance(Vector3 position)
    {
        position.y = 0f;
        float best = float.PositiveInfinity, result = 0f, travelled = 0f;
        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector3 a = points[i].position, b = points[i + 1].position;
            a.y = b.y = 0f;
            Vector3 delta = b - a;
            float length = delta.magnitude;
            if (length < 0.001f) continue;
            float t = Vector3.Dot(position - a, delta) / (length * length);
            if (i > 0) t = Mathf.Max(0f, t);
            if (i < points.Length - 2) t = Mathf.Min(1f, t);
            float square = (position - (a + delta * t)).sqrMagnitude;
            if (square < best) { best = square; result = travelled + t * length; }
            travelled += length;
        }
        return result;
    }

    private Vector3 PointAt(float distance)
    {
        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector3 a = points[i].position, b = points[i + 1].position;
            a.y = b.y = 0f;
            float length = Vector3.Distance(a, b);
            if (length < 0.001f) continue;
            if (distance <= length || i == points.Length - 2) return Vector3.LerpUnclamped(a, b, distance / length);
            distance -= length;
        }
        return points[0].position;
    }
}
