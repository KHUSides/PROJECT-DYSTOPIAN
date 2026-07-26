using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyRespawnerDummy : MonoBehaviour
{
    [Header("Respawn")]
    [SerializeField] private KeyCode respawnKey = KeyCode.Alpha4;

    private EnemyHealthDummy targetDummy;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    private void Start()
    {
        targetDummy = FindFirstObjectByType<EnemyHealthDummy>(FindObjectsInactive.Include);
        if (targetDummy == null)
        {
            Debug.LogError("[EnemyRespawnerDummy] EnemyHealthDummy is required in the scene.", this);
            enabled = false;
            return;
        }

        spawnPosition = targetDummy.transform.position;
        spawnRotation = targetDummy.transform.rotation;
    }

    private void Update()
    {
        if (Input.GetKeyDown(respawnKey))
            targetDummy.Respawn(spawnPosition, spawnRotation);
    }
}
