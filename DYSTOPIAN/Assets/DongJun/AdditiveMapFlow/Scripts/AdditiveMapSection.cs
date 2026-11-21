using UnityEngine;

[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
public sealed class AdditiveMapSection : MonoBehaviour
{
    public int routeIndex;
    public BoxCollider leftLoadingGate;
    public BoxCollider rightLoadingGate;
    private AdditiveMapFlow flow;

    private void Awake()
    {
        flow = FindFirstObjectByType<AdditiveMapFlow>();
        GetComponent<BoxCollider>().isTrigger = true;
        var body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (flow != null && other.GetComponent<PlayerController>() == flow.Player)
            flow.EnterSection(routeIndex);
    }

    public void SetNeighborsReady(bool leftReady, bool rightReady)
    {
        if (leftLoadingGate != null) leftLoadingGate.enabled = !leftReady;
        if (rightLoadingGate != null) rightLoadingGate.enabled = !rightReady;
    }
}
