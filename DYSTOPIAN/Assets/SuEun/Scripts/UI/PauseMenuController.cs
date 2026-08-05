using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Scene-independent pause overlay. It binds every active UI Button named "pause";
/// when no such button exists it supplies a top-right fallback button.
/// </summary>
public sealed class PauseMenuController : MonoBehaviour
{
    private static PauseMenuController instance;

    private GameObject overlay;
    private CanvasGroup dimGroup;
    private RectTransform pauseButton;
    private RectTransform settingsButton;
    private RectTransform resumeButton;
    private RectTransform quitButton;
    private readonly List<Button> scenePauseButtons = new List<Button>();
    private readonly List<GameObject> fallbackButtons = new List<GameObject>();
    private bool isOpen;
    private Coroutine transition;
    private Vector2 pauseFinalPosition;
    private Vector2 settingsFinalPosition;
    private Vector2 resumeFinalPosition;
    private Vector2 quitFinalPosition;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForEveryScene()
    {
        if (instance != null)
            return;

        var root = new GameObject("Pause Menu Controller");
        root.AddComponent<PauseMenuController>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        BuildOverlay();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        StartCoroutine(RebindAfterSceneLoad());
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isOpen) Close();
            else Open();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (isOpen)
        {
            isOpen = false;
            overlay.SetActive(false);
            Time.timeScale = 1f;
        }

        StartCoroutine(RebindAfterSceneLoad());
    }

    private IEnumerator RebindAfterSceneLoad()
    {
        yield return null;
        BindScenePauseButtons();
    }

    private void BindScenePauseButtons()
    {
        foreach (var button in scenePauseButtons)
        {
            if (button != null)
                button.onClick.RemoveListener(Open);
        }

        scenePauseButtons.Clear();

        foreach (var fallback in fallbackButtons)
            if (fallback != null)
                Destroy(fallback);
        fallbackButtons.Clear();

        var buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var button in buttons)
        {
            if (button == null || button.transform.IsChildOf(overlay.transform))
                continue;

            if (button.name.ToLowerInvariant().Contains("pause"))
            {
                button.onClick.RemoveListener(Open);
                button.onClick.AddListener(Open);
                scenePauseButtons.Add(button);
            }
        }

        if (scenePauseButtons.Count == 0)
            fallbackButtons.Add(CreateFallbackPauseButton());
    }

private void BuildOverlay()
    {
        var prefab = Resources.Load<GameObject>("PauseMenuLayout");
        if (prefab == null)
        {
            Debug.LogError("PauseMenuLayout prefab is missing from Assets/Resources.");
            return;
        }

        var layout = Instantiate(prefab, transform);
        layout.name = "Pause Overlay";
        overlay = layout.transform.Find("OverlayGroup").gameObject;
        var content = overlay.transform.Find("Content");
        dimGroup = overlay.transform.Find("Dim").GetComponent<CanvasGroup>();

        pauseButton = content.Find("Paused").GetComponent<RectTransform>();
        settingsButton = content.Find("Settings").GetComponent<RectTransform>();
        resumeButton = content.Find("Resume").GetComponent<RectTransform>();
        quitButton = content.Find("Quit").GetComponent<RectTransform>();

        pauseFinalPosition = pauseButton.anchoredPosition;
        settingsFinalPosition = settingsButton.anchoredPosition;
        resumeFinalPosition = resumeButton.anchoredPosition;
        quitFinalPosition = quitButton.anchoredPosition;

        pauseButton.GetComponent<Button>().onClick.AddListener(Resume);
        settingsButton.GetComponent<Button>().onClick.AddListener(OpenSettings);
        resumeButton.GetComponent<Button>().onClick.AddListener(Resume);
        quitButton.GetComponent<Button>().onClick.AddListener(QuitGame);

        overlay.SetActive(false);
    }

private GameObject CreateFallbackPauseButton()
    {
        var rect = CreateIconButton("pause", "Ⅱ", new Vector2(-70f, -70f), Open);
        rect.SetParent(overlay.transform.parent, false);
        rect.SetAsLastSibling();

        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(-70f, -70f);
        return rect.gameObject;
    }

    private RectTransform CreateIconButton(string objectName, string icon, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        var buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(RawImage), typeof(Button));
        buttonObject.transform.SetParent(overlay.transform, false);

        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(124f, 124f);
        rect.anchoredPosition = position;

        var background = buttonObject.GetComponent<RawImage>();
        background.texture = CreateCircleTexture();
        background.color = Color.white;
        background.raycastTarget = true;

        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(action);

        var labelObject = new GameObject("Icon", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(buttonObject.transform, false);
        Stretch(labelObject.GetComponent<RectTransform>());

        var label = labelObject.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = icon;
        label.alignment = TextAnchor.MiddleCenter;
        label.fontSize = 56;
        label.color = new Color(0.26f, 0.28f, 0.31f, 1f);
        label.raycastTarget = false;

        return rect;
    }

    private void Open()
    {
        if (isOpen)
            return;

        if (transition != null)
            StopCoroutine(transition);

        isOpen = true;
        foreach (var button in scenePauseButtons)
            if (button != null) button.gameObject.SetActive(false);
        foreach (var button in fallbackButtons)
            if (button != null) button.SetActive(false);

        Time.timeScale = 0f;
        transition = StartCoroutine(Animate(true));
    }

    private void Close()
    {
        if (!isOpen)
            return;

        if (transition != null)
            StopCoroutine(transition);

        transition = StartCoroutine(Animate(false));
    }

    private void Resume()
    {
        Close();
    }

    private void OpenSettings()
    {
        Debug.Log("PauseMenuController: settings button clicked. Connect this event to the project's settings panel when it is available.");
    }

    private void QuitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();
    }

private IEnumerator Animate(bool opening)
    {
        var pauseStart = pauseFinalPosition + Vector2.up * 270f;
        var collapsed = pauseFinalPosition;

        if (opening)
        {
            overlay.SetActive(true);
            dimGroup.alpha = 0f;
            SetButton(pauseButton, pauseStart, 0f);
            SetButton(settingsButton, collapsed, 0f);
            SetButton(resumeButton, collapsed, 0f);
            SetButton(quitButton, collapsed, 0f);

            yield return MoveButton(pauseButton, pauseStart, pauseFinalPosition, 0.20f, true);

            var settings = MoveButton(settingsButton, collapsed, settingsFinalPosition, 0.16f, true);
            var resume = MoveButton(resumeButton, collapsed, resumeFinalPosition, 0.16f, true);
            var quit = MoveButton(quitButton, collapsed, quitFinalPosition, 0.16f, true);
            while (settings.MoveNext() | resume.MoveNext() | quit.MoveNext())
                yield return null;
        }
        else
        {
            var settings = MoveButton(settingsButton, settingsFinalPosition, collapsed, 0.14f, false);
            var resume = MoveButton(resumeButton, resumeFinalPosition, collapsed, 0.14f, false);
            var quit = MoveButton(quitButton, quitFinalPosition, collapsed, 0.14f, false);
            while (settings.MoveNext() | resume.MoveNext() | quit.MoveNext())
                yield return null;

            yield return MoveButton(pauseButton, pauseFinalPosition, pauseStart, 0.18f, false);

            overlay.SetActive(false);
            isOpen = false;
            Time.timeScale = 1f;
            foreach (var button in scenePauseButtons)
                if (button != null) button.gameObject.SetActive(true);
            foreach (var button in fallbackButtons)
                if (button != null) button.SetActive(true);
        }

        transition = null;
    }

private IEnumerator MoveButton(RectTransform button, Vector2 from, Vector2 to, float duration, bool fadeIn)
    {
        float elapsed = 0f;
        var graphic = button.GetComponent<Graphic>();

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = 1f - Mathf.Pow(1f - t, 3f);
            button.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
            button.localScale = Vector3.one * Mathf.Lerp(fadeIn ? 0.72f : 1f, fadeIn ? 1f : 0.72f, t);
            if (graphic != null)
                graphic.color = new Color(1f, 1f, 1f, fadeIn ? t : 1f - t);
            dimGroup.alpha = fadeIn ? Mathf.Lerp(0f, 1f, t) : Mathf.Lerp(1f, 0f, t);
            yield return null;
        }
    }

private static void SetButton(RectTransform button, Vector2 position, float alpha)
    {
        button.anchoredPosition = position;
        button.localScale = Vector3.one * 0.72f;

        var graphic = button.GetComponent<Graphic>();
        if (graphic != null)
            graphic.color = new Color(1f, 1f, 1f, alpha);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static Texture2D CreateCircleTexture()
    {
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        float radius = size * 0.5f - 1f;
        var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float distance = Vector2.Distance(new Vector2(x, y), center);
            float alpha = Mathf.Clamp01(radius - distance);
            texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
        }

        texture.Apply();
        return texture;
    }
}