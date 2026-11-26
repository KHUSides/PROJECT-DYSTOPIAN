using UnityEngine;

[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
public sealed class LevelCameraZone : MonoBehaviour
{
    private BoxCollider trigger;

    public bool Contains(Transform player)
    {
        var actor = player != null ? player.GetComponent<CharacterController>() : null;
        return actor != null && trigger.bounds.Intersects(actor.bounds);
    }
    public Transform viewPoint;
    public Vector2 viewSize = new Vector2(13.24f, 7.46f);
    public int priority;
    private SideViewCameraFollow cameraFollow;

    private void Awake()
    {
        cameraFollow = FindFirstObjectByType<SideViewCameraFollow>();
        trigger = GetComponent<BoxCollider>();
        trigger.isTrigger = true;
        var body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (cameraFollow != null && other.GetComponent<PlayerController>() != null) cameraFollow.EnterZone(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (cameraFollow != null && other.GetComponent<PlayerController>() != null) cameraFollow.ExitZone(this);
    }

    private void OnDisable()
    {
        if (cameraFollow != null) cameraFollow.ExitZone(this);
    }
}
