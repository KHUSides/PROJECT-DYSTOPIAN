using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public sealed class AdditiveMapFlow : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private LevelMovementPlane movementPlane;
    [SerializeField] private SideViewCameraFollow cameraController;
    [SerializeField] private string[] scenes = Array.Empty<string>();


    private readonly HashSet<int> clearedSections = new HashSet<int>();
    private readonly HashSet<int> clearedBosses = new HashSet<int>();
    private CharacterController character;
    private AdditiveMapSection activeSection;
    private AdditiveMapSection departedSection;
    private bool streaming;
    private bool teleporting;
    private bool initializationFailed;


    public AdditiveMapSection CurrentSection => activeSection;
    public PlayerController Player => player;
    public LevelMovementPlane MovementPlane => movementPlane;
    public int CurrentIndex { get; private set; }
    public bool IsReady { get; private set; }
    public bool IsBusy => streaming || teleporting;

    private void Awake()
    {
        if (cameraController == null) cameraController = FindFirstObjectByType<SideViewCameraFollow>();
        if (movementPlane == null) movementPlane = GetComponent<LevelMovementPlane>();
        if (player == null) return;
        character = player.GetComponent<CharacterController>();
        if (movementPlane == null) return;
        movementPlane.SetTarget(player.transform);
        player.SetMovementPlane(movementPlane);
    }

    private void OnDestroy()
    {
        if (player != null && player.MovementPlane == movementPlane) player.SetMovementPlane(null);
    }

    private IEnumerator Start()
    {
        if (!ValidateSetup())
        {
            initializationFailed = true;
            yield break;
        }
        bool wasEnabled = player.enabled;
        player.enabled = false;
        try
        {
            SceneManager.SetActiveScene(gameObject.scene);
            yield return StreamScenes();
            ActivateSection(GetSection(CurrentIndex));
            if (cameraController != null) cameraController.SnapToTarget();
            IsReady = true;
        }
        finally
        {
            player.enabled = wasEnabled;
            initializationFailed = !IsReady;
        }
    }

    // Game-flow blocks: yield return these from the director's IEnumerator.
    public IEnumerator WaitUntilReady()
    {
        while (!IsReady)
        {
            if (initializationFailed) throw new InvalidOperationException("AdditiveMapFlow initialization failed. Check the Console.");
            yield return null;
        }
    }

    public IEnumerator WaitForSection(int index)
    {
        ValidateIndex(index);
        if (!IsReady) yield return WaitUntilReady();
        while (CurrentIndex != index || IsBusy) yield return null;
    }

    public IEnumerator WaitForBossClear(int stage)
    {
        ValidateStage(stage);
        if (!IsReady) yield return WaitUntilReady();
        while (!HasClearedBoss(stage)) yield return null;
    }

    public bool HasClearedBoss(int stage) => clearedBosses.Contains(stage);

    public bool HasClearedSection(int index) => clearedSections.Contains(index);

    public void ClearSection(int index)
    {
        ValidateIndex(index);
        clearedSections.Add(index);
    }


    public void ClearBoss(int stage)
    {
        ValidateStage(stage);
        if (clearedBosses.Add(stage)) RefreshGates();
    }

    public Coroutine TeleportTo(int index, Vector3 worldPosition)
    {
        ValidateIndex(index);
        return StartCoroutine(Teleport(index, worldPosition));
    }

    private IEnumerator Teleport(int index, Vector3 worldPosition)
    {
        if (!IsReady) yield return WaitUntilReady();
        while (IsBusy) yield return null;
        if (SubstageBattle.Active != null) SubstageBattle.Active.CancelBattle();
        teleporting = true;
        bool wasEnabled = player.enabled;
        player.enabled = false;
        try
        {
            departedSection = null;
            CurrentIndex = index;
            yield return StreamScenes();
            character.enabled = false;
            player.transform.position = worldPosition;
            character.enabled = true;
            Physics.SyncTransforms();
            ActivateSection(GetSection(CurrentIndex));
            if (cameraController != null) cameraController.SnapToTarget();
        }
        finally
        {
            player.enabled = wasEnabled;
            teleporting = false;
        }
    }

    // Trigger notifications change the active section, not player position.
    public void EnterSection(int index, AdditiveMapSection entered = null)
    {
        if (!IsReady || teleporting || index < 0 || index >= scenes.Length) return;
        if (entered == null) entered = GetSection(index);
        if (entered == null) return;
        if (SubstageBattle.Active != null && entered != SubstageBattle.Active.Section) return;
        if (entered == departedSection && activeSection != null) return;
        if (activeSection != null && activeSection.activationPriority > entered.activationPriority) return;
        if (index != CurrentIndex) departedSection = activeSection;
        if (activeSection != entered) ActivateSection(entered);
        if (index == CurrentIndex) return;
        CurrentIndex = index;
        if (!streaming) StartCoroutine(StreamScenes());
    }

    public void ReleaseSection(AdditiveMapSection section)
    {
        if (departedSection == section) departedSection = null;
        if (activeSection == section) activeSection = null;
    }

    private void ActivateSection(AdditiveMapSection section)
    {
        activeSection = section;
        if (section != null) section.Activate();
    }

    private IEnumerator StreamScenes()
    {
        streaming = true;
        var originalPriority = Application.backgroundLoadingPriority;
        Application.backgroundLoadingPriority = ThreadPriority.Low;
        try
        {
            int center;
            do
            {
                center = CurrentIndex;
                yield return LoadScene(center);
                var section = GetSection(center);
                if (section == null) throw new InvalidOperationException("Missing section trigger: " + scenes[center]);
                var required = new HashSet<int> { center, section.previousIndex, section.nextIndex };
                foreach (int neighbor in section.additionalNeighbors) required.Add(neighbor);
                foreach (int index in required) yield return LoadScene(index);
                RefreshGates();
                if (center == CurrentIndex) yield return UnloadUnusedScenes(required, center);
            } while (center != CurrentIndex);
        }
        finally
        {
            streaming = false;
            Application.backgroundLoadingPriority = originalPriority;
        }
    }

    private IEnumerator UnloadUnusedScenes(HashSet<int> required, int center)
    {
        for (int i = 0; i < scenes.Length; i++)
        {
            if (required.Contains(i) || !IsLoaded(i)) continue;
            yield return SceneManager.UnloadSceneAsync(scenes[i]);
            RefreshGates();
            if (center != CurrentIndex) yield break;
        }
    }

    private IEnumerator LoadScene(int index)
    {
        if (index < 0 || index >= scenes.Length || IsLoaded(index)) yield break;
        yield return SceneManager.LoadSceneAsync(scenes[index], LoadSceneMode.Additive);
        RefreshGates();
    }

    private bool IsLoaded(int index)
        => index >= 0 && index < scenes.Length && SceneManager.GetSceneByPath(scenes[index]).isLoaded;

    private AdditiveMapSection GetSection(int index)
    {
        if (!IsLoaded(index)) return null;
        foreach (var root in SceneManager.GetSceneByPath(scenes[index]).GetRootGameObjects())
        {
            var section = root.GetComponentInChildren<AdditiveMapSection>();
            if (section != null) return section;
        }
        return null;
    }

    private void RefreshGates()
    {
        foreach (var section in FindObjectsByType<AdditiveMapSection>(FindObjectsSortMode.None))
            section.SetNeighborsReady(IsLoaded(section.previousIndex), IsLoaded(section.nextIndex));
    }

    private bool ValidateSetup()
    {
        if (player == null || character == null || scenes.Length == 0)
        {
            Debug.LogError("[AdditiveMapFlow] Assign Player and map scenes.", this);
            return false;
        }
        foreach (string path in scenes)
        {
            if (SceneUtility.GetBuildIndexByScenePath(path) >= 0) continue;
            Debug.LogError("[AdditiveMapFlow] Scene is not enabled in Build Settings: " + path, this);
            return false;
        }
        return true;
    }

    private void ValidateIndex(int index)
    {
        if (index < 0 || index >= scenes.Length) throw new ArgumentOutOfRangeException(nameof(index));
    }

    private static void ValidateStage(int stage)
    {
        if (stage <= 0) throw new ArgumentOutOfRangeException(nameof(stage), "Boss stage must be positive.");
    }
}
