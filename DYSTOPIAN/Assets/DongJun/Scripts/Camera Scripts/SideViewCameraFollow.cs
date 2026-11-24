using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum LevelCameraMode { Follow, Fixed, Vertical }

[DisallowMultipleComponent, RequireComponent(typeof(Camera))]
public class SideViewCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -10f);
    [SerializeField] private float smoothTime = 0.12f;
    [SerializeField, Min(1f)] private float rotationSpeed = 60f;
    [SerializeField] private bool followY = true;
    [SerializeField] private bool forceOrthographic = true;
    [SerializeField] private float orthographicSize = 6f;
    [SerializeField] private CameraBounds2D cameraBounds;
    [SerializeField] private bool clampToBounds = true;
    [SerializeField] private bool drawCameraViewGizmo = true;
    [SerializeField] private bool drawCameraCenterLimitGizmo = true;
    private readonly List<LevelCameraZone> zones = new List<LevelCameraZone>();
    private readonly List<Renderer> hiddenForeground = new List<Renderer>();
    private readonly RaycastHit[] foregroundHits = new RaycastHit[32];
    private AdditiveMapSection section;
    private bool transitioning;
    private bool enteringSection, entryPending;
    private Vector3 entryPosition;
    private Quaternion entryRotation;
    private float entrySize;
    private bool manualOverride, hasZoomOverride;
    private Vector3 manualPosition;
    private Quaternion manualRotation;
    private float manualSize, zoomOverride;
    private Coroutine manualMotion;
    private Camera cam;
    private float normalNear, sizeVelocity;
    private Vector3 velocity, followPosition, shakeOffset;
    public LevelCameraMode ActiveMode { get; private set; }
    public bool IsFollowing => !manualOverride && !enteringSection && ActiveMode != LevelCameraMode.Fixed;
    public bool IsTransitioning => manualOverride ? manualMotion != null : transitioning || enteringSection;
    public bool HasManualOverride => manualOverride;
    public float RotationSpeed => rotationSpeed;
    public float TurnProgress { get; private set; }
    public float TurnStrength { get; private set; }

    private void Awake()
    {
        cam = GetComponent<Camera>();
        normalNear = cam.nearClipPlane;
        followPosition = transform.position;
        if (forceOrthographic) { cam.orthographic = true; cam.orthographicSize = orthographicSize; }
    }

    public void SetSectionView(AdditiveMapSection value)
    {
        if (section != value)
        {
            transitioning = section != null;
            enteringSection = section != null && value != null && section.cameraGroup != value.cameraGroup;
            entryPending = enteringSection;
        }
        section = value;
        zones.RemoveAll(z => z == null || !z.isActiveAndEnabled);
    }

    public void EnterZone(LevelCameraZone zone)
    {
        transitioning = true;
        zones.Remove(zone);
        zones.Add(zone);
    }

    public void ExitZone(LevelCameraZone zone)
    {
        if (zones.Remove(zone)) transitioning = true;
    }

    private Vector3 Desired(out Quaternion rotation, out float size)
    {
        LevelCameraZone active = null;
        for (int i = zones.Count - 1; i >= 0; i--)
        {
            var zone = zones[i];
            if (zone == null || !zone.isActiveAndEnabled || !zone.Contains(target)) { zones.RemoveAt(i); continue; }
            if (active == null || zone.priority > active.priority) active = zone;
        }
        rotation = target != null ? target.rotation : transform.rotation;
        Vector3 desired = target != null ? target.position + rotation * offset : followPosition;
        size = orthographicSize;
        ActiveMode = section != null ? section.cameraMode : LevelCameraMode.Follow;
        if (section != null && section.cameraFrame != null)
        {
            var frame = section.cameraFrame;
            size = section.vertical ? frame.size.x / (2f * Aspect()) : frame.size.y * 0.5f;
            Vector3 local = frame.transform.InverseTransformPoint(desired);
            if (!hasZoomOverride)
            {
                if (section.vertical) local.x = frame.center.x;
                else local.y = frame.center.y;
            }
            desired = frame.transform.TransformPoint(local);
            if (ActiveMode == LevelCameraMode.Fixed && section.cameraView != null)
            {
                desired = section.cameraView.position;
                rotation = section.cameraView.rotation;
                size = Mathf.Min(frame.size.y * 0.5f, frame.size.x / (2f * Aspect()));
            }
        }
        else if (!followY) desired.y = followPosition.y;
        if (active != null && active.viewPoint != null)
        {
            ActiveMode = LevelCameraMode.Fixed;
            desired = active.viewPoint.position;
            rotation = active.viewPoint.rotation;
            size = section != null && section.cameraFrame != null
                ? (section.vertical ? section.cameraFrame.size.x / (2f * Aspect()) : section.cameraFrame.size.y * 0.5f)
                : Mathf.Min(active.viewSize.y * 0.5f, active.viewSize.x / (2f * Aspect()));
        }
        return desired;
    }

    private Vector3 Confine(Vector3 position, float size)
    {
        if (section != null && section.cameraFrame != null)
        {
            var frame = section.cameraFrame;
            Vector3 local = frame.transform.InverseTransformPoint(position);
            Vector3 min = frame.center - frame.size * 0.5f, max = frame.center + frame.size * 0.5f;
            // Constrain the non-scrolling axis to the authored wall or floor/ceiling bounds.
            if (section.vertical) local.x = Clamp(local.x, min.x, max.x, size * Aspect());
            else local.y = Clamp(local.y, min.y, max.y, size);
            if (section.vertical && section.clampCameraBottom) local.y = Mathf.Max(local.y, min.y + size);
            if (section.vertical && section.clampCameraTop) local.y = Mathf.Min(local.y, max.y - size);
            if (!section.vertical && section.pathPoints.Length < 2)
            {
                if (section.clampCameraLeft) local.x = Mathf.Max(local.x, min.x + size * Aspect());
                if (section.clampCameraRight) local.x = Mathf.Min(local.x, max.x - size * Aspect());
            }
            return frame.transform.TransformPoint(local);
        }
        if (clampToBounds && cameraBounds != null)
        {
            position.x = Clamp(position.x, cameraBounds.Min.x, cameraBounds.Max.x, size * Aspect());
            position.y = Clamp(position.y, cameraBounds.Min.y, cameraBounds.Max.y, size);
        }
        return position;
    }

    private void LateUpdate()
    {
        RestoreForeground();
        if (manualOverride) { ApplyManualPose(); return; }
        if (target == null) return;
        Vector3 desired = Desired(out Quaternion rotation, out float size);
        if (hasZoomOverride) size = Mathf.Min(size, zoomOverride);
        desired = Confine(desired, size);
        if (ApplyTraversalView(desired, rotation, size)) return;
        if (entryPending)
        {
            entryPosition = desired;
            entryRotation = rotation;
            entrySize = size;
            entryPending = false;
        }
        if (enteringSection) { desired = entryPosition; rotation = entryRotation; size = entrySize; }
        Quaternion actualRotation = Quaternion.RotateTowards(transform.rotation, rotation, rotationSpeed * Time.deltaTime);
        bool rotating = Quaternion.Angle(actualRotation, rotation) > 0.01f;
        // Orbit around the same focus while turning; slower rotation must not leave the player behind.
        desired += actualRotation * offset - rotation * offset;
        if (rotating) transitioning = true;
        float actualSize = Mathf.SmoothDamp(cam.orthographicSize, size, ref sizeVelocity, smoothTime);
        if (Mathf.Abs(actualSize - size) < 0.001f) actualSize = size;
        followPosition = Vector3.SmoothDamp(followPosition, desired, ref velocity, smoothTime);
        Vector3 confined = Confine(followPosition, actualSize);
        if (!rotating && (!transitioning || ((confined - followPosition).sqrMagnitude < 0.000001f && Mathf.Abs(actualSize - size) < 0.001f)))
        {
            followPosition = confined;
            transitioning = false;
        }
        if (!rotating && ActiveMode == LevelCameraMode.Fixed && (followPosition - desired).sqrMagnitude < 0.000001f)
            followPosition = Confine(desired, actualSize);
        if (enteringSection && !rotating && (followPosition - entryPosition).sqrMagnitude < 0.0004f && Mathf.Abs(actualSize - entrySize) < 0.01f)
            enteringSection = false;
        transform.SetPositionAndRotation(followPosition + actualRotation * shakeOffset, actualRotation);
        if (forceOrthographic) cam.orthographicSize = actualSize;
        UpdateNearClip();
    }

    public void SnapToTarget()
    {
        RestoreForeground();
        if (manualOverride) { ApplyManualPose(); return; }
        if (target == null) return;
        if (cam == null) cam = GetComponent<Camera>();
        followPosition = Desired(out Quaternion rotation, out float size);
        if (hasZoomOverride) size = Mathf.Min(size, zoomOverride);
        followPosition = Confine(followPosition, size);
        if (ApplyTraversalView(followPosition, rotation, size)) return;
        velocity = Vector3.zero;
        sizeVelocity = 0f;
        transitioning = false;
        enteringSection = entryPending = false;
        transform.SetPositionAndRotation(Confine(followPosition + rotation * shakeOffset, size), rotation);
        if (forceOrthographic) cam.orthographicSize = size;
        UpdateNearClip();
    }

    private bool ApplyTraversalView(Vector3 desired, Quaternion baseRotation, float size)
    {
        TurnProgress = TurnStrength = 0f;
        if (ActiveMode == LevelCameraMode.Fixed || section == null || section.CameraTurns == null) return false;
        foreach (var turn in section.CameraTurns)
        {
            if (turn == null || !turn.Sample(target.position, out Quaternion rotation, out float zoom, out float progress, out float strength)) continue;
            // Orbit the framed focus, rather than pitching in place and losing sight of the player.
            float distance = Mathf.Max(1f, Mathf.Abs(offset.z));
            Vector3 focus = desired + baseRotation * Vector3.forward * distance;
            focus = Vector3.Lerp(focus, target.position + Vector3.up * turn.focusHeight, strength);
            if (!hasZoomOverride && turn.startSize > 0f && turn.endSize > 0f)
                size = Mathf.Lerp(turn.startSize, turn.endSize, Mathf.SmoothStep(0f, 1f, progress));
            followPosition = focus - rotation * Vector3.forward * (distance * zoom);
            var edgePosition = progress < 0.5f ? turn.entryCameraPosition : turn.exitCameraPosition;
            if (edgePosition != null) followPosition = Vector3.Lerp(edgePosition.position, followPosition, strength);
            velocity = Vector3.zero;
            sizeVelocity = 0f;
            enteringSection = entryPending = transitioning = false;
            TurnProgress = progress;
            TurnStrength = strength;
            transform.SetPositionAndRotation(followPosition + rotation * shakeOffset, rotation);
            if (forceOrthographic) cam.orthographicSize = size * zoom;
            UpdateNearClip();
            HideForeground();
            return true;
        }
        return false;
    }

    private void HideForeground()
    {
        // Cut away complete foreground objects, without slicing every surface with the near plane.
        for (int ray = 0; ray < 2; ray++)
        {
            Vector3 delta = target.position + Vector3.up * (ray == 0 ? 0.9f : 1.6f) - transform.position;
            if (delta.magnitude <= 0.2f) continue;
            int count = Physics.RaycastNonAlloc(transform.position, delta.normalized, foregroundHits, delta.magnitude - 0.2f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var hit = foregroundHits[i];
                if (hit.transform.IsChildOf(target) || hit.transform.root.name != "PrototypeMap") continue;
                var renderer = hit.collider.GetComponent<Renderer>();
                if (renderer == null || !renderer.enabled || renderer.forceRenderingOff) continue;
                if (Vector3.Dot(hit.point - transform.position, transform.forward) <= cam.nearClipPlane) continue;
                renderer.forceRenderingOff = true;
                hiddenForeground.Add(renderer);
            }
        }
    }

    private void RestoreForeground()
    {
        foreach (var renderer in hiddenForeground) if (renderer != null) renderer.forceRenderingOff = false;
        hiddenForeground.Clear();
    }

    private void OnDisable() => RestoreForeground();

    private void UpdateNearClip()
    {
        var plane = target != null ? target.GetComponent<LevelMovementPlane>() : null;
        if (plane == null || !plane.IsActive) { cam.nearClipPlane = normalNear; return; }
        float actorDepth = Vector3.Dot(target.position - transform.position, transform.forward);
        if (TurnStrength > 0f) { cam.nearClipPlane = normalNear; return; }
        cam.nearClipPlane = Mathf.Max(0.1f, Mathf.Min(Mathf.Abs(offset.z) - 3f, actorDepth - 1.5f));
    }

    public void SetBounds(CameraBounds2D bounds) => cameraBounds = bounds;
    public void SetTarget(Transform value) => target = value;
    public void SetShakeOffset(Vector3 value) => shakeOffset = value;
    public void SetRotationSpeed(float degreesPerSecond) => rotationSpeed = Mathf.Max(1f, degreesPerSecond);
    public void SetSmoothing(float seconds) => smoothTime = Mathf.Max(0.01f, seconds);

    public void StopFollow()
    {
        RestoreForeground();
        CancelManualMotion();
        manualPosition = transform.position - transform.rotation * shakeOffset;
        manualRotation = transform.rotation;
        manualSize = GetComponent<Camera>().orthographicSize;
        manualOverride = true;
        hasZoomOverride = false;
        enteringSection = entryPending = false;
        transitioning = false;
        ActiveMode = LevelCameraMode.Fixed;
        TurnProgress = TurnStrength = 0f;
    }

    public void ResumeFollow(bool snap = false)
    {
        CancelManualMotion();
        manualOverride = false;
        followPosition = transform.position - transform.rotation * shakeOffset;
        velocity = Vector3.zero;
        sizeVelocity = 0f;
        enteringSection = entryPending = false;
        transitioning = true;
        if (snap) SnapToTarget();
    }

    public void FixAt(Transform view, float size = -1f, bool instant = false)
    {
        if (view == null) throw new System.ArgumentNullException(nameof(view));
        FixAt(view.position, view.rotation, size, instant);
    }

    public void FixAt(Vector3 position, Quaternion rotation, float size = -1f, bool instant = false)
    {
        MoveTo(position, rotation, instant ? 0f : 0.6f, size);
    }

    public Coroutine MoveTo(Transform view, float duration = 0.8f, float size = -1f, bool resumeAfter = false)
    {
        if (view == null) throw new System.ArgumentNullException(nameof(view));
        return MoveTo(view.position, view.rotation, duration, size, resumeAfter);
    }

    public Coroutine MoveTo(Vector3 position, Quaternion rotation, float duration = 0.8f, float size = -1f, bool resumeAfter = false)
    {
        StopFollow();
        if (duration <= 0f)
        {
            manualPosition = position; manualRotation = rotation;
            if (size > 0f) manualSize = size;
            ApplyManualPose();
            if (resumeAfter) ResumeFollow();
            return null;
        }
        manualMotion = StartCoroutine(MoveRoutine(position, rotation, size > 0f ? size : manualSize, duration, resumeAfter));
        return manualMotion;
    }

    private IEnumerator MoveRoutine(Vector3 position, Quaternion rotation, float size, float duration, bool resumeAfter)
    {
        Vector3 from = manualPosition;
        Quaternion fromRotation = manualRotation;
        float fromSize = manualSize;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            manualPosition = Vector3.Lerp(from, position, t);
            manualRotation = Quaternion.Slerp(fromRotation, rotation, t);
            manualSize = Mathf.Lerp(fromSize, size, t);
            yield return null;
        }
        manualPosition = position; manualRotation = rotation; manualSize = size;
        manualMotion = null;
        ApplyManualPose();
        if (resumeAfter) ResumeFollow();
    }

    private void CancelManualMotion()
    {
        if (manualMotion != null) StopCoroutine(manualMotion);
        manualMotion = null;
    }

    private void ApplyManualPose()
    {
        if (cam == null) { cam = GetComponent<Camera>(); normalNear = cam.nearClipPlane; }
        followPosition = manualPosition;
        transform.SetPositionAndRotation(manualPosition + manualRotation * shakeOffset, manualRotation);
        cam.orthographicSize = hasZoomOverride ? Mathf.SmoothDamp(cam.orthographicSize, zoomOverride, ref sizeVelocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime) : manualSize;
        cam.nearClipPlane = normalNear;
        ActiveMode = LevelCameraMode.Fixed;
    }

    public void SetZoom(float size, bool instant = false)
    {
        zoomOverride = Mathf.Max(0.1f, size);
        hasZoomOverride = true;
        enteringSection = entryPending = false;
        transitioning = true;
        if (!instant) return;
        if (cam == null) cam = GetComponent<Camera>();
        float allowedSize = zoomOverride;
        if (!manualOverride && target != null) { Desired(out Quaternion unused, out float fit); allowedSize = Mathf.Min(fit, zoomOverride); }
        cam.orthographicSize = allowedSize;
        sizeVelocity = 0f;
    }

    public void ResetZoom() { hasZoomOverride = false; transitioning = true; }
    private float Aspect() => cam != null && cam.aspect > 0f ? cam.aspect : 16f / 9f;
    private static float Clamp(float value, float min, float max, float halfSize)
        => max - min < halfSize * 2f ? (min + max) * 0.5f : Mathf.Clamp(value, min + halfSize, max - halfSize);

    private void OnDrawGizmosSelected()
    {
        var camera = GetComponent<Camera>();
        if (!drawCameraViewGizmo || camera == null || !camera.orthographic) return;
        Gizmos.color = Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.forward, new Vector3(camera.orthographicSize * 2f * camera.aspect, camera.orthographicSize * 2f, 0f));
        Gizmos.matrix = Matrix4x4.identity;
        if (drawCameraCenterLimitGizmo && cameraBounds != null)
            Gizmos.DrawWireCube(new Vector3(cameraBounds.Center.x, cameraBounds.Center.y, 0f), new Vector3(cameraBounds.Size.x, cameraBounds.Size.y, .01f));
    }
}
