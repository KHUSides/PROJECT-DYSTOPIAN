using UnityEngine;

[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
public sealed class LevelCameraZone : MonoBehaviour
{
    public bool Contains(Transform player)
    {
        var actor = player != null ? player.GetComponent<CharacterController>() : null;
        return actor != null && GetComponent<BoxCollider>().bounds.Intersects(actor.bounds);
    }
    public Transform viewPoint;
    public Vector2 viewSize = new Vector2(13.24f, 7.46f);
    public int priority;
    private SideViewCameraFollow cameraFollow;

    private void Awake()
    {
        cameraFollow = FindFirstObjectByType<SideViewCameraFollow>();
        GetComponent<BoxCollider>().isTrigger = true;
        var body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null) cameraFollow.EnterZone(this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() != null) cameraFollow.ExitZone(this);
    }

    private void OnDisable()
    {
        if (cameraFollow != null) cameraFollow.ExitZone(this);
    }
}
