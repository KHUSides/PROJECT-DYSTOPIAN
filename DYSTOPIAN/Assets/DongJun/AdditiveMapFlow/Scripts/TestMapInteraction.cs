using UnityEngine;

[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
public sealed class TestMapInteraction : MonoBehaviour
{
    public string prompt;
    public bool automatic;
    public int requiredBoss = -1;
    public int clearsBoss = -1;
    public int destinationIndex = -1;
    public Vector3 destination;
    public Collider blocker;
    public GameObject bossVisual;
    private AdditiveMapFlow flow;
    private bool inside;
    private bool used;
    private bool PlayerInside => inside && flow != null && GetComponent<BoxCollider>().bounds.Intersects(flow.Player.GetComponent<CharacterController>().bounds);

    private void Awake()
    {
        flow = FindFirstObjectByType<AdditiveMapFlow>();
        GetComponent<BoxCollider>().isTrigger = true;
        var body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
    }

    private void Start()
    {
        if (clearsBoss > 0 && flow.HasClearedBoss(clearsBoss)) Complete();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerController>() == flow.Player) inside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerController>() == flow.Player) inside = false;
    }

    private void Update()
    {
        if (automatic && !used && requiredBoss > 0 && flow.HasClearedBoss(requiredBoss) && blocker != null && blocker.gameObject.activeSelf)
            blocker.gameObject.SetActive(false);
        if (PlayerInside && (automatic || Input.GetKeyDown(KeyCode.E))) TryInteract();
    }

    public bool TryInteract()
    {
        if (!PlayerInside || used || flow.IsBusy || (requiredBoss > 0 && !flow.HasClearedBoss(requiredBoss))) return false;
        if (clearsBoss > 0) flow.ClearBoss(clearsBoss);
        if (blocker != null) blocker.gameObject.SetActive(false);
        if (destinationIndex >= 0) flow.TeleportTo(destinationIndex, destination);
        else Complete();
        Debug.Log("[TestMapInteraction] " + prompt, this);
        return true;
    }

    private void Complete()
    {
        used = true;
        if (blocker != null) blocker.gameObject.SetActive(false);
        if (bossVisual != null) bossVisual.SetActive(false);
        var label = GetComponentInChildren<TextMesh>();
        if (label != null) label.text = clearsBoss == 7 ? "TEST COMPLETE" : clearsBoss > 0 ? "BOSS CLEARED" : "DOOR OPEN";
    }

    private void OnGUI()
    {
        if (!PlayerInside || used || automatic) return;
        string text = requiredBoss > 0 && !flow.HasClearedBoss(requiredBoss)
            ? "Clear Stage " + requiredBoss + " boss first" : "[E] " + prompt;
        GUI.Box(new Rect((Screen.width - 660) / 2, Screen.height - 64, 660, 36), text);
    }
}

