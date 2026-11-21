using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public sealed class AdditiveMapFlow : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private string[] scenes = Array.Empty<string>();
    private bool streaming;

    public PlayerController Player => player;
    public int CurrentIndex { get; private set; }
    public bool IsReady { get; private set; }

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
        // Keep the existing player's independently created effect pools in Core.
        SceneManager.SetActiveScene(gameObject.scene);
        yield return StreamScenes();
        IsReady = true;
        player.enabled = wasEnabled;
    }

    public void EnterSection(int index)
    {
        if (!IsReady || index == CurrentIndex || index < 0 || index >= scenes.Length) return;
        CurrentIndex = index;
        Debug.Log("[AdditiveMapFlow] Entered " + scenes[index], this);
        if (!streaming) StartCoroutine(StreamScenes());
    }

    private IEnumerator StreamScenes()
    {
        streaming = true;
        try
        {
            int center;
            do
            {
                center = CurrentIndex;
                for (int i = Mathf.Max(0, center - 1); i <= Mathf.Min(scenes.Length - 1, center + 1); i++)
                {
                    if (!IsLoaded(i))
                        yield return SceneManager.LoadSceneAsync(scenes[i], LoadSceneMode.Additive);
                    RefreshGates();
                }
                for (int i = 0; i < scenes.Length; i++)
                {
                    if (Mathf.Abs(i - CurrentIndex) <= 1 || !IsLoaded(i)) continue;
                    yield return SceneManager.UnloadSceneAsync(scenes[i]);
                    RefreshGates();
                }
            } while (center != CurrentIndex);
        }
        finally { streaming = false; }
    }

    private bool IsLoaded(int index)
    {
        return index >= 0 && index < scenes.Length && SceneManager.GetSceneByPath(scenes[index]).isLoaded;
    }

    private void RefreshGates()
    {
        foreach (var section in FindObjectsByType<AdditiveMapSection>(FindObjectsSortMode.None))
            section.SetNeighborsReady(IsLoaded(section.routeIndex - 1), IsLoaded(section.routeIndex + 1));
    }
}
