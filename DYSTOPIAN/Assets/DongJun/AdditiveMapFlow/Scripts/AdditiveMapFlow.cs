using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public sealed class AdditiveMapFlow : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private string[] scenes = Array.Empty<string>();
    private readonly HashSet<int> clearedBosses = new HashSet<int>();
    private bool streaming;
    private AdditiveMapSection activeSection;
    private AdditiveMapSection departedSection;
    private bool teleporting;

    public PlayerController Player => player;
    public int CurrentIndex { get; private set; }
    public bool IsReady { get; private set; }
    public bool IsBusy => streaming || teleporting;
    public bool HasClearedBoss(int stage) => clearedBosses.Contains(stage);
    public void ClearBoss(int stage) { clearedBosses.Add(stage); RefreshGates(); }

    private IEnumerator Start()
    {
        if (player == null || scenes.Length == 0) yield break;
        bool wasEnabled = player.enabled;
        player.enabled = false;
        foreach (string path in scenes)
        {
            if (SceneUtility.GetBuildIndexByScenePath(path) >= 0) continue;
            Debug.LogError("[AdditiveMapFlow] Scene is not enabled in Build Settings: " + path, this);
            yield break;
        }
        SceneManager.SetActiveScene(gameObject.scene);
        yield return StreamScenes();
        ActivateSection(GetSection(CurrentIndex));
        var camera = FindFirstObjectByType<SideViewCameraFollow>();
        if (camera != null) camera.SnapToTarget();
        IsReady = true;
        player.enabled = wasEnabled;
    }

    public void EnterSection(int index, AdditiveMapSection entered = null)
    {
        if (!IsReady || teleporting || index < 0 || index >= scenes.Length) return;
        if (entered == null) entered = GetSection(index);
        if (entered != null && entered == departedSection && activeSection != null) return;
        if (activeSection != null && entered != null && activeSection.activationPriority > entered.activationPriority) return;
        if (index != CurrentIndex) departedSection = activeSection;
        if (activeSection != entered) ActivateSection(entered);
        if (index == CurrentIndex) return;
        CurrentIndex = index;
        Debug.Log("[AdditiveMapFlow] Entered " + scenes[index], this);
        if (!streaming) StartCoroutine(StreamScenes());
    }

    public void TeleportTo(int index, Vector3 position)
    {
        if (!IsReady || teleporting || index < 0 || index >= scenes.Length) return;
        StartCoroutine(Teleport(index, position));
    }

    private IEnumerator Teleport(int index, Vector3 position)
    {
        teleporting = true;
        departedSection = null;
        bool wasEnabled = player.enabled;
        player.enabled = false;
        while (streaming) yield return null;
        CurrentIndex = index;
        yield return StreamScenes();
        var character = player.GetComponent<CharacterController>();
        character.enabled = false;
        player.transform.position = position;
        character.enabled = true;
        Physics.SyncTransforms();
        ActivateSection(GetSection(CurrentIndex));
        var camera = FindFirstObjectByType<SideViewCameraFollow>();
        if (camera != null) camera.SnapToTarget();
        player.enabled = wasEnabled;
        teleporting = false;
        Debug.Log("[AdditiveMapFlow] Teleported to " + scenes[index], this);
    }

    public void ReleaseSection(AdditiveMapSection section)
    {
        if (departedSection == section) departedSection = null;
        if (activeSection == section) activeSection = null;
    }

    private void ActivateSection(AdditiveMapSection section)
    {
        activeSection = section;
        if (section != null) section.Activate(player);
    }
    private IEnumerator StreamScenes()
    {
        streaming = true;
        var loadingPriority = Application.backgroundLoadingPriority;
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
                var keep = new HashSet<int> { center, section.previousIndex, section.nextIndex };
                foreach (int extra in section.additionalNeighbors) keep.Add(extra);
                foreach (int neighbor in keep) yield return LoadScene(neighbor);
                RefreshGates();
                if (center != CurrentIndex) continue;
                for (int i = 0; i < scenes.Length; i++)
                {
                    if (keep.Contains(i) || !IsLoaded(i)) continue;
                    yield return SceneManager.UnloadSceneAsync(scenes[i]);
                    RefreshGates();
                    if (center != CurrentIndex) break;
                }
            } while (center != CurrentIndex);
        }
        finally { streaming = false; Application.backgroundLoadingPriority = loadingPriority; }
    }

    private IEnumerator LoadScene(int index)
    {
        if (index < 0 || index >= scenes.Length || IsLoaded(index)) yield break;
        yield return SceneManager.LoadSceneAsync(scenes[index], LoadSceneMode.Additive);
        RefreshGates();
    }

    private bool IsLoaded(int index)
    {
        return index >= 0 && index < scenes.Length && SceneManager.GetSceneByPath(scenes[index]).isLoaded;
    }

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
}


