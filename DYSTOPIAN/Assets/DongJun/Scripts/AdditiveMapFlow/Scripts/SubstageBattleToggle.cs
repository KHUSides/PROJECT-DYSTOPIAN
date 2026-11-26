using UnityEngine;

// Temporary inspector control. Removing this component leaves SubstageBattle enabled.
[DisallowMultipleComponent]
[RequireComponent(typeof(SubstageBattle))]
public sealed class SubstageBattleToggle : MonoBehaviour
{
    [SerializeField, Tooltip("Temporary testing option. Disable to walk through this substage without a battle.")]
    private bool battleEnabled = true;
    private SubstageBattle battle;

    private void Awake() => battle = GetComponent<SubstageBattle>();
    private void OnEnable() => Apply();
    private void Update()
    {
        Apply();
        if (battle != null && battle.IsRunning && Input.GetKeyDown(KeyCode.T))
            battle.CompleteBattle();
    }

    private void Apply()
    {
        if (battle != null && battle.enabled != battleEnabled) battle.enabled = battleEnabled;
    }

    private void OnDisable()
    {
        if (battle != null) battle.enabled = true;
    }
}

