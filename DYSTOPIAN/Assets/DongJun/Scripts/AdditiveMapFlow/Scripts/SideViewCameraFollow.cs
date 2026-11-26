using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum LevelCameraMode { Follow, Fixed, Vertical }

[DisallowMultipleComponent]
public class SideViewCameraFollow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera controlledCamera;
    [SerializeField] private Transform target;
    [Header("Follow")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -10f);
    [SerializeField] private float smoothTime = 0.12f;
    [SerializeField, Min(1f)] private float rotationSpeed = 60f;
    [SerializeField] private bool followY = true;
    [SerializeField] private bool forceOrthographic = true;
    [SerializeField] private float orthographicSize = 6f;
    [SerializeField] private CameraBounds2D cameraBounds;
    [SerializeField] private bool clampToBounds = true;
    [Header("Debug")]
    [SerializeField] private bool drawCameraViewGizmo = true;
    [SerializeField] private bool drawCameraCenterLimitGizmo = true;
    private readonly List<LevelCameraZone> zones = new List<LevelCameraZone>();
    private readonly List<Renderer> hiddenForeground = new List<Renderer>();
    private readonly RaycastHit[] foregroundHits = new RaycastHit[32];


    private Vector3 confinementTargetPosition;
    private Quaternion confinementTargetRotation;
    private float confinementTargetSize;

    private bool usingRouteView;

    private LevelCameraTurn activeRouteTurn;
    private Vector3 routeEntryOffset;
    private float routeEntryProgress;
    private Vector3 routeConfinementOffset, routeConfinementVelocity;
    private bool confinementPending;
    private BoxCollider confinement;
    public BoxCollider Confinement => confinement;
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
    private int moveVersion;
    private float normalNear, sizeVelocity;
    private Vector3 velocity, followPosition, shakeOffset;
    private Transform CameraTransform => controlledCamera.transform;
    public Camera ControlledCamera => controlledCamera;
    public LevelCameraMode ActiveMode { get; private set; }
    public bool IsFollowing => !manualOverride && !enteringSection && ActiveMode != LevelCameraMode.Fixed;
    public bool IsTransitioning => confinementPending || (manualOverride ? manualMotion != null : transitioning || enteringSection);
    public bool HasManualOverride => manualOverride;
    public float RotationSpeed => rotationSpeed;
    public float TurnProgress { get; private set; }
    public float TurnStrength { get; private set; }

    private void Awake()
    {
        if (controlledCamera == null) controlledCamera = GetComponent<Camera>() ?? Camera.main;
        if (controlledCamera == null)
        {
            Debug.LogError("[SideViewCameraFollow] Assign the camera to control.", this);
            enabled = false;
            return;
        }
        normalNear = controlledCamera.nearClipPlane;
        followPosition = CameraTransform.position;
        if (forceOrthographic) { controlledCamera.orthographic = true; controlledCamera.orthographicSize = orthographicSize; }
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

    private LevelCameraZone FindActiveZone()
    {
        LevelCameraZone active = null;
        for (int i = zones.Count - 1; i >= 0; i--)
        {
            var zone = zones[i];
            if (zone == null || !zone.isActiveAndEnabled || !zone.Contains(target)) { zones.RemoveAt(i); continue; }
            if (active == null || zone.priority > active.priority) active = zone;
        }
        return active;
    }

    private Vector3 CalculateView(out Quaternion rotation, out float size)
    {
        var active = FindActiveZone();
        var frame = confinement != null ? confinement : section != null ? section.cameraFrame : null;
        bool vertical = section != null && section.vertical;
        rotation = target != null ? target.rotation : CameraTransform.rotation;
        Vector3 desired = target != null ? target.position + rotation * offset : followPosition;
        size = orthographicSize;
        ActiveMode = section != null ? section.cameraMode : LevelCameraMode.Follow;
        if (frame != null)
        {
            size = vertical ? frame.size.x / (2f * Aspect()) : frame.size.y * 0.5f;
            Vector3 local = frame.transform.InverseTransformPoint(desired);
            if (!hasZoomOverride)
            {
                if (vertical) local.x = frame.center.x;
                else local.y = frame.center.y;
            }
            desired = frame.transform.TransformPoint(local);
            if (ActiveMode == LevelCameraMode.Fixed && section != null && section.cameraView != null)
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
            size = frame != null
                ? (vertical ? frame.size.x / (2f * Aspect()) : frame.size.y * 0.5f)
                : Mathf.Min(active.viewSize.y * 0.5f, active.viewSize.x / (2f * Aspect()));
        }
        return desired;
    }

    private Vector3 ClampToFrame(Vector3 position, float size)
    {
        // The combat frame replaces level bounds and is solved with the target rotation.
        if (confinement != null) return position;
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
        usingRouteView = false;
        RestoreForeground();
        if (manualOverride) { ApplyManualPose(); return; }
        if (target == null || controlledCamera == null) { ConstrainView(); return; }
        Vector3 desired = CalculateView(out Quaternion rotation, out float size);
        if (hasZoomOverride) size = Mathf.Min(size, zoomOverride);
        desired = ClampToFrame(desired, size);
        if (TryApplyTurnView(desired, rotation, size)) return;
        routeConfinementOffset = routeConfinementVelocity = Vector3.zero;
        activeRouteTurn = null;
        routeEntryOffset = Vector3.zero;
        ConstrainTarget(ref desired, ref rotation, ref size);
        UpdateFollowView(desired, rotation, size);
        ConstrainView();
    }

    private void UpdateFollowView(Vector3 desired, Quaternion rotation, float size)
    {
        if (entryPending)
        {
            entryPosition = desired;
            entryRotation = rotation;
            entrySize = size;
            entryPending = false;
        }
        if (enteringSection) { desired = entryPosition; rotation = entryRotation; size = entrySize; }
        Quaternion actualRotation = Quaternion.RotateTowards(CameraTransform.rotation, rotation, rotationSpeed * Time.deltaTime);
        bool rotating = Quaternion.Angle(actualRotation, rotation) > 0.01f;
        // Orbit around the same focus while turning; slower rotation must not leave the player behind.
        desired += actualRotation * offset - rotation * offset;
        if (rotating) transitioning = true;
        float actualSize = Mathf.SmoothDamp(controlledCamera.orthographicSize, size, ref sizeVelocity, smoothTime);
        if (Mathf.Abs(actualSize - size) < 0.001f) actualSize = size;
        followPosition = Vector3.SmoothDamp(followPosition, desired, ref velocity, smoothTime);
        Vector3 confined = ClampToFrame(followPosition, actualSize);
        bool frameAligned = (confined - followPosition).sqrMagnitude < 0.000001f;
        bool zoomFinished = Mathf.Abs(actualSize - size) < 0.001f;
        if (!rotating && (!transitioning || (frameAligned && zoomFinished)))
        {
            followPosition = confined;
            transitioning = false;
        }
        if (!rotating && ActiveMode == LevelCameraMode.Fixed && (followPosition - desired).sqrMagnitude < 0.000001f)
            followPosition = ClampToFrame(desired, actualSize);
        if (enteringSection && !rotating && (followPosition - entryPosition).sqrMagnitude < 0.0004f && Mathf.Abs(actualSize - entrySize) < 0.01f)
            enteringSection = false;
        CameraTransform.SetPositionAndRotation(followPosition + actualRotation * shakeOffset, actualRotation);
        if (forceOrthographic) controlledCamera.orthographicSize = actualSize;
        UpdateNearClip();
    }

    public void SnapToTarget()
    {
        usingRouteView = false;
        confinementPending = false;
        RestoreForeground();
        if (manualOverride) { ApplyManualPose(); return; }
        if (target == null || controlledCamera == null) { ConstrainView(); return; }
        followPosition = CalculateView(out Quaternion rotation, out float size);
        if (hasZoomOverride) size = Mathf.Min(size, zoomOverride);
        followPosition = ClampToFrame(followPosition, size);
        if (TryApplyTurnView(followPosition, rotation, size, true)) return;
        routeConfinementOffset = routeConfinementVelocity = Vector3.zero;
        activeRouteTurn = null;
        routeEntryOffset = Vector3.zero;
        ConstrainTarget(ref followPosition, ref rotation, ref size);
        velocity = Vector3.zero;
        sizeVelocity = 0f;
        transitioning = false;
        enteringSection = entryPending = false;
        CameraTransform.SetPositionAndRotation(ClampToFrame(followPosition + rotation * shakeOffset, size), rotation);
        if (forceOrthographic) controlledCamera.orthographicSize = size;
        UpdateNearClip();
        ConstrainView();
    }

    private bool TryApplyTurnView(Vector3 desired, Quaternion baseRotation, float size, bool snap = false)
    {
        TurnProgress = TurnStrength = 0f;
        if (ActiveMode == LevelCameraMode.Fixed || section == null || section.CameraTurns == null) return false;
        foreach (var turn in section.CameraTurns)
        {
            if (turn == null || !turn.Sample(target.position, out Quaternion rotation, out float zoom, out float progress, out float strength)) continue;
            float distance = Mathf.Max(1f, Mathf.Abs(offset.z));
            Vector3 focus = desired + baseRotation * Vector3.forward * distance;
            focus = Vector3.Lerp(focus, target.position + Vector3.up * turn.focusHeight, strength);
            if (!hasZoomOverride && turn.startSize > 0f && turn.endSize > 0f)
                size = Mathf.Lerp(turn.startSize, turn.endSize, Mathf.SmoothStep(0f, 1f, progress));
            float turnSize = size * zoom;
            usingRouteView = section.pathPoints != null && section.pathPoints.Length > 1;
            if (usingRouteView)
            {
                if (activeRouteTurn != turn)
                {
                    routeEntryOffset = activeRouteTurn == null && !snap
                        ? followPosition - (focus - rotation * Vector3.forward * (distance * zoom)) - routeConfinementOffset
                        : Vector3.zero;
                    routeEntryOffset.y = 0f;
                    routeEntryProgress = progress;
                    activeRouteTurn = turn;
                }
                if (snap) routeEntryOffset = Vector3.zero;
                float entryBlend = Mathf.SmoothStep(0f, 1f, Mathf.Abs(progress - routeEntryProgress) / Mathf.Max(0.01f, turn.fadeFraction));
                focus += routeEntryOffset * (1f - entryBlend);
                Vector3 boundedFocus = confinement != null ? turn.ConstrainFocus(focus, rotation, Aspect(), ref turnSize) : focus;
                Vector3 wantedOffset = boundedFocus - focus;
                if (snap)
                {
                    routeConfinementOffset = wantedOffset;
                    routeConfinementVelocity = Vector3.zero;
                    confinementPending = false;
                }
                else if (Time.deltaTime > 0f)
                    routeConfinementOffset = Vector3.SmoothDamp(routeConfinementOffset, wantedOffset, ref routeConfinementVelocity, smoothTime);
                Vector3 shifted = focus + routeConfinementOffset;
                if (confinement != null)
                {
                    float allowedSize = turnSize;
                    Vector3 allowed = turn.ConstrainFocus(shifted, rotation, Aspect(), ref allowedSize);
                    if (!confinementPending || (allowed - shifted).sqrMagnitude < 0.0004f)
                    {
                        confinementPending = false;
                        shifted = allowed;
                        routeConfinementOffset = shifted - focus;
                        turnSize = allowedSize;
                    }
                }
                if (confinement == null && routeConfinementOffset.sqrMagnitude < 0.000001f)
                    routeConfinementOffset = routeConfinementVelocity = Vector3.zero;
                focus = shifted;
            }
            else
            {
                activeRouteTurn = null;
                routeEntryOffset = Vector3.zero;
                routeConfinementOffset = routeConfinementVelocity = Vector3.zero;
            }

            Vector3 turnPosition = focus - rotation * Vector3.forward * (distance * zoom);
            var edgePosition = progress < 0.5f ? turn.entryCameraPosition : turn.exitCameraPosition;
            if (edgePosition != null) turnPosition = Vector3.Lerp(edgePosition.position, turnPosition, strength);
            if (!usingRouteView) ConstrainTarget(ref turnPosition, ref rotation, ref turnSize);
            enteringSection = entryPending = false;
            TurnProgress = progress;
            TurnStrength = strength;
            if (!usingRouteView && confinementPending)
                UpdateFollowView(turnPosition, rotation, turnSize);
            else
            {
                followPosition = turnPosition;
                velocity = Vector3.zero;
                sizeVelocity = 0f;
                transitioning = false;
                CameraTransform.SetPositionAndRotation(followPosition + rotation * shakeOffset, rotation);
                if (forceOrthographic) controlledCamera.orthographicSize = turnSize;
                UpdateNearClip();
            }
            ConstrainView();
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
            Vector3 delta = target.position + Vector3.up * (ray == 0 ? 0.9f : 1.6f) - CameraTransform.position;
            if (delta.magnitude <= 0.2f) continue;
            int count = Physics.RaycastNonAlloc(CameraTransform.position, delta.normalized, foregroundHits, delta.magnitude - 0.2f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var hit = foregroundHits[i];
                if (hit.transform.IsChildOf(target) || hit.transform.root.name != "PrototypeMap") continue;
                var renderer = hit.collider.GetComponent<Renderer>();
                if (renderer == null || !renderer.enabled || renderer.forceRenderingOff) continue;
                if (Vector3.Dot(hit.point - CameraTransform.position, CameraTransform.forward) <= controlledCamera.nearClipPlane) continue;
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

    private void OnDisable()
    {
        RestoreForeground();
        CancelManualMotion();
    }

    private void UpdateNearClip()
    {
        var player = target != null ? target.GetComponent<PlayerController>() : null;
        var plane = player != null ? player.MovementPlane : null;
        if (plane == null || !plane.IsActive) { controlledCamera.nearClipPlane = normalNear; return; }
        float actorDepth = Vector3.Dot(target.position - CameraTransform.position, CameraTransform.forward);
        if (TurnStrength > 0f) { controlledCamera.nearClipPlane = normalNear; return; }
        controlledCamera.nearClipPlane = Mathf.Max(0.1f, Mathf.Min(Mathf.Abs(offset.z) - 3f, actorDepth - 1.5f));
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
        manualPosition = CameraTransform.position - CameraTransform.rotation * shakeOffset;
        manualRotation = CameraTransform.rotation;
        manualSize = controlledCamera.orthographicSize;
        confinementPending = false;
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
        followPosition = CameraTransform.position - CameraTransform.rotation * shakeOffset;
        velocity = Vector3.zero;
        sizeVelocity = 0f;
        enteringSection = entryPending = false;
        transitioning = true;
        if (snap) SnapToTarget();
    }

    public Coroutine FixAt(Transform view, float size = -1f, bool instant = false)
    {
        if (view == null) throw new System.ArgumentNullException(nameof(view));
        return FixAt(view.position, view.rotation, size, instant);
    }

    public Coroutine FixAt(Vector3 position, Quaternion rotation, float size = -1f, bool instant = false)
    {
        return MoveTo(position, rotation, instant ? 0f : 0.6f, size);
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
            confinementPending = false;
            manualPosition = position; manualRotation = rotation;
            if (size > 0f) manualSize = size;
            ApplyManualPose();
            if (resumeAfter) ResumeFollow();
            return null;
        }
        manualMotion = StartCoroutine(MoveRoutine(position, rotation, size > 0f ? size : manualSize, duration, resumeAfter, moveVersion));
        return manualMotion;
    }

    private IEnumerator MoveRoutine(Vector3 position, Quaternion rotation, float size, float duration, bool resumeAfter, int version)
    {
        Vector3 from = manualPosition;
        Quaternion fromRotation = manualRotation;
        float fromSize = manualSize;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            if (version != moveVersion) yield break;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            manualPosition = Vector3.Lerp(from, position, t);
            manualRotation = Quaternion.Slerp(fromRotation, rotation, t);
            manualSize = Mathf.Lerp(fromSize, size, t);
            yield return null;
        }
        if (version != moveVersion) yield break;
        manualPosition = position; manualRotation = rotation; manualSize = size;
        manualMotion = null;
        ApplyManualPose();
        if (resumeAfter) ResumeFollow();
    }

    private void CancelManualMotion()
    {
        // Let the old coroutine finish naturally so callers waiting on it can continue.
        moveVersion++;
        manualMotion = null;
    }

    private void ApplyManualPose()
    {
        followPosition = manualPosition;
        CameraTransform.SetPositionAndRotation(manualPosition + manualRotation * shakeOffset, manualRotation);
        controlledCamera.orthographicSize = hasZoomOverride ? Mathf.SmoothDamp(controlledCamera.orthographicSize, zoomOverride, ref sizeVelocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime) : manualSize;
        controlledCamera.nearClipPlane = normalNear;
        ActiveMode = LevelCameraMode.Fixed;
        ConstrainView();
    }

    public void SetZoom(float size, bool instant = false)
    {
        zoomOverride = Mathf.Max(0.1f, size);
        hasZoomOverride = true;
        enteringSection = entryPending = false;
        transitioning = true;
        if (!instant) return;
        float allowedSize = zoomOverride;
        if (!manualOverride && target != null) { CalculateView(out _, out float fit); allowedSize = Mathf.Min(fit, zoomOverride); }
        controlledCamera.orthographicSize = allowedSize;
        sizeVelocity = 0f;
        confinementPending = false;
        ConstrainView();
    }

    public void ResetZoom() { hasZoomOverride = false; transitioning = true; }
    private float Aspect() => controlledCamera != null && controlledCamera.aspect > 0f ? controlledCamera.aspect : 16f / 9f;
    private static float Clamp(float value, float min, float max, float halfSize)
        => max - min < halfSize * 2f ? (min + max) * 0.5f : Mathf.Clamp(value, min + halfSize, max - halfSize);

    private void OnDrawGizmosSelected()
    {
        var camera = controlledCamera != null ? controlledCamera : GetComponent<Camera>();
        if (!drawCameraViewGizmo || camera == null || !camera.orthographic) return;
        Gizmos.color = Color.yellow;
        Gizmos.matrix = camera.transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.forward, new Vector3(camera.orthographicSize * 2f * camera.aspect, camera.orthographicSize * 2f, 0f));
        Gizmos.matrix = Matrix4x4.identity;
        if (drawCameraCenterLimitGizmo && cameraBounds != null)
            Gizmos.DrawWireCube(new Vector3(cameraBounds.Center.x, cameraBounds.Center.y, 0f), new Vector3(cameraBounds.Size.x, cameraBounds.Size.y, .01f));
    }


    public void SetConfinement(BoxCollider bounds)
    {
        if (confinement == bounds) return;
        confinement = bounds;
        confinementPending = bounds != null && !manualOverride && target != null;
        // Retarget the entry already in flight without restarting its motion.
        if (enteringSection) entryPending = true;
    }

    public void ClearConfinement(BoxCollider bounds)
    {
        if (confinement == bounds) SetConfinement(null);
    }

    private void ConstrainView()
    {
        if (usingRouteView && !manualOverride) return;
        if (confinement == null || controlledCamera == null || !controlledCamera.orthographic) return;
        if (confinementPending && !manualOverride && target != null)
        {
            Vector3 unshaken = CameraTransform.position - CameraTransform.rotation * shakeOffset;
            bool arrived = (unshaken - confinementTargetPosition).sqrMagnitude < 0.0004f
                && Quaternion.Angle(CameraTransform.rotation, confinementTargetRotation) < 0.1f
                && Mathf.Abs(controlledCamera.orthographicSize - confinementTargetSize) < 0.01f;
            // The single follow interpolation brings us inside; do not run a second tween.
            if (!arrived) return;
        }
        confinementPending = false;
        Vector3 position = CameraTransform.position;
        Quaternion rotation = CameraTransform.rotation;
        float size = controlledCamera.orthographicSize;
        Vector3 before = position;
        GetConfinedPose(ref position, ref rotation, ref size);
        CameraTransform.SetPositionAndRotation(position, rotation);
        if (size < controlledCamera.orthographicSize) sizeVelocity = 0f;
        controlledCamera.orthographicSize = size;
        followPosition = position - rotation * shakeOffset;
        var frame = confinement.transform;
        Vector3 correction = frame.InverseTransformVector(position - before);
        Vector3 localVelocity = frame.InverseTransformVector(velocity);
        if (Mathf.Abs(correction.x) > 0.0001f) localVelocity.x = 0f;
        if (Mathf.Abs(correction.y) > 0.0001f) localVelocity.y = 0f;
        velocity = frame.TransformVector(localVelocity);
    }

    private void ConstrainTarget(ref Vector3 position, ref Quaternion rotation, ref float size)
    {
        GetConfinedPose(ref position, ref rotation, ref size);
        confinementTargetPosition = position;
        confinementTargetRotation = rotation;
        confinementTargetSize = size;
    }

    private void GetConfinedPose(ref Vector3 position, ref Quaternion rotation, ref float size)
    {
        if (confinement == null || controlledCamera == null || !controlledCamera.orthographic) return;
        var frame = confinement.transform;
        Vector3 forward = frame.InverseTransformVector(rotation * Vector3.forward);
        if (Mathf.Abs(forward.z) < 0.05f)
        {
            rotation = frame.rotation;
            forward = frame.InverseTransformVector(rotation * Vector3.forward);
        }
        Vector3 right = frame.InverseTransformVector(rotation * Vector3.right);
        Vector3 up = frame.InverseTransformVector(rotation * Vector3.up);
        right -= forward * (right.z / forward.z);
        up -= forward * (up.z / forward.z);
        float widthPerSize = Mathf.Abs(right.x) * Aspect() + Mathf.Abs(up.x);
        float heightPerSize = Mathf.Abs(right.y) * Aspect() + Mathf.Abs(up.y);
        float fit = Mathf.Min(confinement.size.x / (2f * Mathf.Max(0.0001f, widthPerSize)),
                              confinement.size.y / (2f * Mathf.Max(0.0001f, heightPerSize)));
        size = Mathf.Min(size, Mathf.Max(0.01f, fit));
        Vector3 local = frame.InverseTransformPoint(position);
        Vector3 focus = local + forward * ((confinement.center.z - local.z) / forward.z);
        Vector3 min = confinement.center - confinement.size * 0.5f;
        Vector3 max = confinement.center + confinement.size * 0.5f;
        Vector3 clamped = focus;
        clamped.x = Clamp(focus.x, min.x, max.x, size * widthPerSize);
        clamped.y = Clamp(focus.y, min.y, max.y, size * heightPerSize);
        position += frame.TransformVector(clamped - focus);
    }

}


