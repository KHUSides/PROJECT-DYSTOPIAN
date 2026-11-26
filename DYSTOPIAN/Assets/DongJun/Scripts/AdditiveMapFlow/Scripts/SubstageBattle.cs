using System;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
[RequireComponent(typeof(AdditiveMapSection))]
public sealed class SubstageBattle : MonoBehaviour
{
    [SerializeField] private BoxCollider[] barriers = Array.Empty<BoxCollider>();
    [SerializeField] private BoxCollider cameraFrame;
    [SerializeField] private UnityEvent onBattleStarted = new UnityEvent();
    [SerializeField] private UnityEvent onBattleCleared = new UnityEvent();

    private AdditiveMapSection section;
    private AdditiveMapFlow flow;
    private SideViewCameraFollow cameraController;

    public static SubstageBattle Active { get; private set; }
    public AdditiveMapSection Section => section;
    public bool IsRunning { get; private set; }
    public bool IsCleared => flow != null && section != null && flow.HasClearedSection(section.routeIndex);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetActive() => Active = null;

    private void Awake()
    {
        section = GetComponent<AdditiveMapSection>();
        flow = FindFirstObjectByType<AdditiveMapFlow>();
        cameraController = FindFirstObjectByType<SideViewCameraFollow>();
        SetBarriersClosed(false);
    }

    private void OnTriggerEnter(Collider other) => TryBeginBattle(other);
    private void OnTriggerStay(Collider other) => TryBeginBattle(other);

    private void TryBeginBattle(Collider other)
    {
        if (!isActiveAndEnabled || IsRunning || IsCleared || flow == null || !flow.IsReady) return;
        if (flow.CurrentSection != section || other.GetComponent<PlayerController>() != flow.Player) return;
        // Wait until the complete player body is past both door planes before sealing them.
        if (HasFullyEntered(other.bounds)) BeginBattle();
    }

    private void Update()
    {
        // A director can deliberately teleport out of an encounter without leaving a stale lock.
        if (IsRunning && (flow == null || flow.CurrentIndex != section.routeIndex)) CancelBattle();
    }

    [ContextMenu("Begin Battle")]
    public void BeginBattle()
    {
        if (!Application.isPlaying || !isActiveAndEnabled || IsRunning || IsCleared) return;
        if (flow == null || !flow.IsReady || flow.CurrentSection != section || Active != null) return;
        if (cameraFrame == null || cameraController == null || barriers.Length < 2)
        {
            Debug.LogError("[SubstageBattle] Assign the camera frame and both battle barriers.", this);
            return;
        }
        var playerBody = flow.Player.GetComponent<CharacterController>();
        if (playerBody == null || !HasFullyEntered(playerBody.bounds)) return;
        Active = this;
        IsRunning = true;
        SetBarriersClosed(true);
        cameraController.SetConfinement(cameraFrame);
        onBattleStarted.Invoke();
    }

    [ContextMenu("Complete Battle")]
    public void CompleteBattle()
    {
        if (!IsRunning) return;
        flow.ClearSection(section.routeIndex);
        ReleaseLocks();
        onBattleCleared.Invoke();
    }

    [ContextMenu("Cancel Battle")]
    public void CancelBattle()
    {
        if (IsRunning) ReleaseLocks();
    }

    private void OnDisable() => CancelBattle();

    private void ReleaseLocks()
    {
        IsRunning = false;
        if (Active == this) Active = null;
        SetBarriersClosed(false);
        if (cameraController != null) cameraController.ClearConfinement(cameraFrame);
    }

    private void SetBarriersClosed(bool closed)
    {
        foreach (var barrier in barriers)
        {
            if (barrier == null) continue;
            barrier.enabled = closed;
            var visual = barrier.GetComponent<Renderer>();
            if (visual != null) visual.enabled = closed;
        }
    }

    private bool HasFullyEntered(Bounds player)
    {
        if (cameraFrame == null) return false;
        Vector3 roomCenter = cameraFrame.transform.TransformPoint(cameraFrame.center);
        foreach (var barrier in barriers)
        {
            if (barrier == null) continue;
            Vector3 size = Vector3.Scale(barrier.size, barrier.transform.lossyScale);
            Vector3 normal = barrier.transform.right;
            float thickness = Mathf.Abs(size.x);
            if (Mathf.Abs(size.y) < thickness) { normal = barrier.transform.up; thickness = Mathf.Abs(size.y); }
            if (Mathf.Abs(size.z) < thickness) { normal = barrier.transform.forward; thickness = Mathf.Abs(size.z); }
            Vector3 point = barrier.transform.TransformPoint(barrier.center);
            if (Vector3.Dot(roomCenter - point, normal) < 0f) normal = -normal;
            float radius = Vector3.Dot(player.extents, new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z)));
            if (Vector3.Dot(player.center - point, normal) < radius + thickness * 0.5f + 0.05f) return false;
        }
        return true;
    }
}

