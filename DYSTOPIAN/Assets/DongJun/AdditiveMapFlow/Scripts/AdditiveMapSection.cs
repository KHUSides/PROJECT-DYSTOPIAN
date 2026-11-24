using System;
using UnityEngine;

[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
public sealed class AdditiveMapSection : MonoBehaviour
{
    public int routeIndex;
    public int previousIndex = -1;
    public int nextIndex = -1;
    public int[] additionalNeighbors = Array.Empty<int>();
    public int requiredBossForNext = -1;
    public int activationPriority;
    public bool requireGrounded;
    public BoxCollider previousGate;
    public BoxCollider nextGate;
    public Transform movementPlane;
    public Transform[] pathPoints = Array.Empty<Transform>();
    public LevelCameraMode cameraMode;
    public Transform cameraView;
    public Vector2 viewSize;
    public Vector2 roomSize;
    public BoxCollider cameraFrame;
    public bool vertical;
    public bool clampCameraBottom = true;
    public bool clampCameraTop;
    public bool clampCameraLeft;
    public bool clampCameraRight;
    public int cameraGroup = -1;
    public LevelCameraTurn[] CameraTurns { get; private set; }
    private AdditiveMapFlow flow;
    private SideViewCameraFollow cameraFollow;
    private LevelMovementPlane driver;

    private void Awake()
    {
        CameraTurns = GetComponents<LevelCameraTurn>();
        flow = FindFirstObjectByType<AdditiveMapFlow>();
        cameraFollow = FindFirstObjectByType<SideViewCameraFollow>();
        if (flow != null) driver = flow.Player.GetComponent<LevelMovementPlane>();
        GetComponent<BoxCollider>().isTrigger = true;
        var body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryActivate(other);
    }

    private void OnTriggerStay(Collider other) => TryActivate(other);

    private void OnTriggerExit(Collider other)
    {
        if (flow != null && other.GetComponent<PlayerController>() == flow.Player) flow.ReleaseSection(this);
    }

    private void TryActivate(Collider other)
    {
        if (flow == null || other.GetComponent<PlayerController>() != flow.Player) return;
        if (requireGrounded)
        {
            if (pathPoints.Length > 0 && flow.Player.transform.position.y < pathPoints[0].position.y - 0.2f)
            {
                flow.ReleaseSection(this);
                return;
            }
            if (!flow.Player.IsGrounded) return;
        }
        var box = GetComponent<BoxCollider>();
        if (!requireGrounded && box.enabled)
        {
            Vector3 feet = other.bounds.center;
            feet.y = other.bounds.min.y + 0.05f;
            Vector3 local = box.transform.InverseTransformPoint(feet);
            Vector3 min = box.center - box.size * 0.5f, max = box.center + box.size * 0.5f;
            if (local.x < min.x - 0.01f || local.x > max.x + 0.01f ||
                local.y < min.y || local.y > max.y ||
                local.z < min.z - 0.01f || local.z > max.z + 0.01f) return;
        }
        flow.EnterSection(routeIndex, this);
    }

    public void Activate(PlayerController player)
    {
        if (driver != null) driver.SetSection(movementPlane, pathPoints);
        if (cameraFollow != null) cameraFollow.SetSectionView(this);
    }

    public void SetNeighborsReady(bool previousReady, bool nextReady)
    {
        SetGate(previousGate, !previousReady);
        bool unlocked = requiredBossForNext < 0 || flow != null && flow.HasClearedBoss(requiredBossForNext);
        SetGate(nextGate, !nextReady || !unlocked);
    }

    private static void SetGate(BoxCollider gate, bool closed)
    {
        if (gate == null) return;
        gate.enabled = closed;
        var renderer = gate.GetComponent<Renderer>();
        if (renderer != null) renderer.enabled = closed;
    }
}

