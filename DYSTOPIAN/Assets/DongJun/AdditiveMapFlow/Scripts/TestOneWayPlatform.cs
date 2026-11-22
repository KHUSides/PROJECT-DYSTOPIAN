using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public sealed class TestOneWayPlatform : MonoBehaviour
{
    private Collider platform;
    private CharacterController player;
    private bool ignored;

    private void Awake()
    {
        platform = GetComponent<Collider>();
        var flow = FindFirstObjectByType<AdditiveMapFlow>();
        if (flow != null) player = flow.Player.GetComponent<CharacterController>();
        UpdateCollision();
    }

    private void FixedUpdate() => UpdateCollision();

    private void UpdateCollision()
    {
        if (player == null) return;
        bool passThrough = player.bounds.min.y < platform.bounds.max.y - 0.06f;
        if (passThrough == ignored) return;
        Physics.IgnoreCollision(player, platform, passThrough);
        ignored = passThrough;
    }

    private void OnDisable()
    {
        if (player != null && platform != null) Physics.IgnoreCollision(player, platform, false);
    }
}
