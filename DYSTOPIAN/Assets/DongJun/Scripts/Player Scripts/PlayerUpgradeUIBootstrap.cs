using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController), typeof(PlayerHealth))]
public sealed class PlayerUpgradeUIBootstrap : MonoBehaviour
{
    [SerializeField] private GameObject upgradeCanvasPrefab;

    private GameObject upgradeCanvasInstance;

    private void Start()
    {
        if (upgradeCanvasPrefab == null)
        {
            Debug.LogError("[PlayerUpgradeUIBootstrap] Upgrade Canvas prefab is required.", this);
            return;
        }

        upgradeCanvasInstance = Instantiate(upgradeCanvasPrefab);
        upgradeCanvasInstance.name = upgradeCanvasPrefab.name;
        SceneManager.MoveGameObjectToScene(upgradeCanvasInstance, gameObject.scene);

        PlayerUpgradePanel panel = upgradeCanvasInstance.GetComponentInChildren<PlayerUpgradePanel>(true);
        if (panel == null)
        {
            Debug.LogError("[PlayerUpgradeUIBootstrap] PlayerUpgradePanel was not found in the Upgrade Canvas prefab.", this);
            Destroy(upgradeCanvasInstance);
            upgradeCanvasInstance = null;
            return;
        }

        panel.Initialize(
            GetComponent<PlayerController>(),
            GetComponent<PlayerHealth>(),
            FindFirstObjectByType<PlayerRhythmAttackBridge>());

        if (EventSystem.current == null)
            Debug.LogWarning("[PlayerUpgradeUIBootstrap] An EventSystem is required to use the upgrade buttons.", this);
    }

    private void OnEnable()
    {
        if (upgradeCanvasInstance != null)
            upgradeCanvasInstance.SetActive(true);
    }

    private void OnDisable()
    {
        if (upgradeCanvasInstance != null)
            upgradeCanvasInstance.SetActive(false);
    }

    private void OnDestroy()
    {
        if (upgradeCanvasInstance != null)
            Destroy(upgradeCanvasInstance);
    }
}
