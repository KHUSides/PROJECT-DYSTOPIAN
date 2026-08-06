#if UNITY_EDITOR
using Dystopian.EnemyTest;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class NpcTestAssetBuilder
{
    private const string NpcPrefabPath = "Assets/JiSeon/Prefabs/NPCs/TutorialNPC.prefab";
    private const string ManagerPrefabPath = "Assets/JiSeon/Prefabs/NPCs/NpcDialogueManager.prefab";
    private const string ScenePath = "Assets/JiSeon/Scenes/NPC_Test.unity";
    private const string BossReferenceScenePath = "Assets/JiSeon/Scenes/MerlinBoss_Test.unity";
    private const string NpcVisualSourcePath = "Assets/Synty/PolygonDungeon/Prefabs/Characters/SM_Chr_Hero_Knight_Female_01.prefab";
    private const string DummyPlayerPath = "Assets/JiSeon/Prefabs/DummyPlayer.prefab";

    [MenuItem("Dystopian/NPC/Rebuild NPC Test Assets")]
    public static void RebuildNpcTestAssets()
    {
        EnsureFolder("Assets/JiSeon/Prefabs/NPCs");
        EnsureFolder("Assets/JiSeon/Prefabs/UI");
        EnsureFolder("Assets/JiSeon/Scenes");

        CreateDialogueManagerPrefab();
        CreateTutorialNpcPrefab();
        CreateNpcTestScene();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[NPC Builder] Rebuilt NPC prefabs and NPC_Test scene.");
    }

    public static void BuildForCodex()
    {
        RebuildNpcTestAssets();
    }

    private static void CreateDialogueManagerPrefab()
    {
        GameObject root = new GameObject("NpcDialogueManager");
        root.AddComponent<NpcPlayerControlLock>();
        root.AddComponent<NpcDialogueManager>();

        SavePrefabAndDestroy(root, ManagerPrefabPath);
    }

    private static void CreateTutorialNpcPrefab()
    {
        GameObject root = new GameObject("TutorialNPC");
        root.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

        CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, 1f, 0f);
        collider.radius = 0.35f;
        collider.height = 2f;

        NpcDialogueEventBridge eventBridge = root.AddComponent<NpcDialogueEventBridge>();
        NpcInteractable interactable = root.AddComponent<NpcInteractable>();

        GameObject visualRoot = new GameObject("VisualRoot");
        visualRoot.transform.SetParent(root.transform, false);

        GameObject visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NpcVisualSourcePath);
        if (visualPrefab != null)
        {
            GameObject visual = PrefabUtility.InstantiatePrefab(visualPrefab) as GameObject;
            if (visual != null)
            {
                visual.name = "NPCVisual";
                visual.transform.SetParent(visualRoot.transform, false);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one;
            }
        }
        else
        {
            GameObject fallbackVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            fallbackVisual.name = "NPCVisual_Fallback";
            fallbackVisual.transform.SetParent(visualRoot.transform, false);
            fallbackVisual.transform.localPosition = new Vector3(0f, 1f, 0f);
            fallbackVisual.transform.localScale = new Vector3(0.6f, 1f, 0.6f);
        }

        CreatePrompt(interactable.transform, out GameObject promptRoot, out Text promptText);
        ApplyTutorialNpcSerializedValues(interactable, promptRoot, promptText);

        // Keep the event bridge in the prefab so designers can wire UnityEvents in branches later.
        SerializedObject eventBridgeSerialized = new SerializedObject(eventBridge);
        eventBridgeSerialized.ApplyModifiedPropertiesWithoutUndo();

        SavePrefabAndDestroy(root, NpcPrefabPath);
    }

    private static void CreatePrompt(Transform parent, out GameObject promptRoot, out Text promptText)
    {
        promptRoot = new GameObject("PromptCanvas");
        promptRoot.transform.SetParent(parent, false);
        promptRoot.transform.localPosition = new Vector3(0f, 2.35f, 0f);
        promptRoot.transform.localRotation = Quaternion.identity;
        promptRoot.transform.localScale = Vector3.one * 0.01f;

        Canvas canvas = promptRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 50;
        promptRoot.AddComponent<CanvasGroup>();

        GameObject backgroundObject = new GameObject("PromptBackground");
        backgroundObject.transform.SetParent(promptRoot.transform, false);
        Image background = backgroundObject.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.72f);
        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.sizeDelta = new Vector2(180f, 46f);

        GameObject textObject = new GameObject("PromptText");
        textObject.transform.SetParent(promptRoot.transform, false);
        promptText = textObject.AddComponent<Text>();
        promptText.font = GetDefaultFont();
        promptText.text = "E : 대화";
        promptText.fontSize = 24;
        promptText.fontStyle = FontStyle.Bold;
        promptText.color = Color.white;
        promptText.alignment = TextAnchor.MiddleCenter;
        promptText.rectTransform.sizeDelta = new Vector2(180f, 46f);

        promptRoot.SetActive(false);
    }

    private static void ApplyTutorialNpcSerializedValues(
        NpcInteractable interactable,
        GameObject promptRoot,
        Text promptText)
    {
        SerializedObject serialized = new SerializedObject(interactable);
        serialized.FindProperty("npcDisplayName").stringValue = "튜토리얼 NPC";
        serialized.FindProperty("interactionRadius").floatValue = 2.6f;
        serialized.FindProperty("verticalRange").floatValue = 2.3f;
        serialized.FindProperty("promptRoot").objectReferenceValue = promptRoot;
        serialized.FindProperty("promptText").objectReferenceValue = promptText;
        serialized.FindProperty("promptFormat").stringValue = "E : 대화";

        SerializedProperty branches = serialized.FindProperty("dialogueBranches");
        branches.arraySize = 3;

        ConfigureBranch(
            branches.GetArrayElementAtIndex(0),
            "tutorial_step_2_repeat",
            string.Empty,
            true,
            2,
            -1,
            string.Empty,
            true,
            -1,
            false,
            "npc_tutorial_repeat",
            new[]
            {
                "지금 NPC 시스템은 독립 테스트용으로 동작 중이에요.",
                "다른 파트에서는 이 NPC 프리팹과 DialogueManager를 씬에 두고, 필요한 이벤트만 UnityEvent로 연결하면 됩니다."
            });

        ConfigureBranch(
            branches.GetArrayElementAtIndex(1),
            "tutorial_step_1_rhythm_hint",
            "tutorial_npc_met",
            true,
            1,
            1,
            string.Empty,
            true,
            -1,
            true,
            "npc_tutorial_step_1_completed",
            new[]
            {
                "좋아요. 다음 단계는 조건별 대화예요.",
                "이 대화는 첫 대화를 끝낸 뒤에만 나오고, 완료되면 튜토리얼 진행도가 2로 올라갑니다."
            });

        ConfigureBranch(
            branches.GetArrayElementAtIndex(2),
            "first_talk",
            string.Empty,
            true,
            -1,
            0,
            "tutorial_npc_met",
            true,
            1,
            false,
            "npc_first_talk_completed",
            new[]
            {
                "안녕하세요. 저는 NPC 대화 테스트용 캐릭터예요.",
                "가까이 오면 E 표시가 뜨고, E를 누르면 대화가 시작됩니다.",
                "대화 중에는 플레이어 조작이 잠기고, Space / Enter / E로 다음 문장으로 넘길 수 있어요."
            });

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureBranch(
        SerializedProperty branch,
        string branchId,
        string requiredFlagKey,
        bool requiredFlagValue,
        int minimumTutorialStep,
        int maximumTutorialStep,
        string flagKeyToSetOnComplete,
        bool flagValueOnComplete,
        int tutorialStepToSetOnComplete,
        bool advanceTutorialStepOnComplete,
        string debugEventNameOnComplete,
        string[] lineTexts)
    {
        branch.FindPropertyRelative("branchId").stringValue = branchId;
        branch.FindPropertyRelative("requiredFlagKey").stringValue = requiredFlagKey;
        branch.FindPropertyRelative("requiredFlagValue").boolValue = requiredFlagValue;
        branch.FindPropertyRelative("minimumTutorialStep").intValue = minimumTutorialStep;
        branch.FindPropertyRelative("maximumTutorialStep").intValue = maximumTutorialStep;
        branch.FindPropertyRelative("flagKeyToSetOnComplete").stringValue = flagKeyToSetOnComplete;
        branch.FindPropertyRelative("flagValueOnComplete").boolValue = flagValueOnComplete;
        branch.FindPropertyRelative("tutorialStepToSetOnComplete").intValue = tutorialStepToSetOnComplete;
        branch.FindPropertyRelative("advanceTutorialStepOnComplete").boolValue = advanceTutorialStepOnComplete;
        branch.FindPropertyRelative("debugEventNameOnComplete").stringValue = debugEventNameOnComplete;

        SerializedProperty lines = branch.FindPropertyRelative("lines");
        lines.arraySize = lineTexts.Length;
        for (int i = 0; i < lineTexts.Length; i++)
        {
            SerializedProperty line = lines.GetArrayElementAtIndex(i);
            line.FindPropertyRelative("speakerName").stringValue = "튜토리얼 NPC";
            line.FindPropertyRelative("text").stringValue = lineTexts[i];
        }
    }

    private static void CreateNpcTestScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "NPC_Test";

        CreateCamera();
        CreateLight();
        CreateSideViewMap();
        CreateEventSystem();

        InstantiatePrefabIntoScene(ManagerPrefabPath, Vector3.zero, Quaternion.identity);
        InstantiatePrefabIntoScene(NpcPrefabPath, new Vector3(1.25f, 0f, 0f), Quaternion.Euler(0f, -90f, 0f));

        if (!CopyBossTestPlayerSetup(scene))
            CreateFallbackDummyPlayer();

        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    private static bool CopyBossTestPlayerSetup(Scene targetScene)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BossReferenceScenePath) == null)
            return false;

        Scene bossScene = EditorSceneManager.OpenScene(BossReferenceScenePath, OpenSceneMode.Additive);
        try
        {
            GameObject rhythm = CopyRootObjectFromScene(bossScene, targetScene, "Rhythm System");
            GameObject player = CopyRootObjectFromScene(bossScene, targetScene, "Player");

            if (player == null)
                return false;

            player.name = "Player";
            player.transform.position = new Vector3(-4f, 0.05f, 0f);
            player.transform.rotation = Quaternion.identity;
            TryAssignPlayerTagAndLayer(player);

            if (rhythm != null)
                rhythm.name = "Rhythm System";

            return true;
        }
        finally
        {
            EditorSceneManager.CloseScene(bossScene, true);
        }
    }

    private static GameObject CopyRootObjectFromScene(Scene sourceScene, Scene targetScene, string rootName)
    {
        GameObject source = null;
        GameObject[] roots = sourceScene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == rootName)
            {
                source = roots[i];
                break;
            }
        }

        if (source == null)
            return null;

        GameObject clone = Object.Instantiate(source);
        clone.name = rootName;
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(clone, targetScene);
        return clone;
    }

    private static void CreateFallbackDummyPlayer()
    {
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DummyPlayerPath);
        if (playerPrefab != null)
        {
            GameObject player = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;
            if (player != null)
            {
                player.name = "DummyPlayer";
                player.transform.position = new Vector3(-4f, 0.05f, 0f);
                player.transform.rotation = Quaternion.identity;
                TryAssignPlayerTagAndLayer(player);
            }
        }
        else
        {
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "DummyPlayer_Fallback";
            player.transform.position = new Vector3(-4f, 1f, 0f);
            TryAssignPlayerTagAndLayer(player);
        }
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.2f;
        camera.backgroundColor = new Color(0.48f, 0.5f, 0.52f, 1f);
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 2.8f, -12f);
        cameraObject.transform.rotation = Quaternion.identity;
    }

    private static void CreateLight()
    {
        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    private static void CreateSideViewMap()
    {
        CreatePlatform("Ground", new Vector3(0f, -0.25f, 0f), new Vector3(18f, 0.5f, 4f), new Color(0.34f, 0.37f, 0.42f));
        CreatePlatform("LeftWall", new Vector3(-8.9f, 1.5f, 0f), new Vector3(0.35f, 3.5f, 4f), new Color(0.26f, 0.29f, 0.34f));
        CreatePlatform("RightWall", new Vector3(8.9f, 1.5f, 0f), new Vector3(0.35f, 3.5f, 4f), new Color(0.26f, 0.29f, 0.34f));
        CreatePlatform("SmallPlatform", new Vector3(4f, 1.25f, 0f), new Vector3(3f, 0.35f, 3.5f), new Color(0.4f, 0.44f, 0.52f));
    }

    private static void CreatePlatform(string name, Vector3 position, Vector3 scale, Color color)
    {
        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.name = name;
        platform.transform.position = position;
        platform.transform.localScale = scale;
        Renderer renderer = platform.GetComponent<Renderer>();
        if (renderer != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            if (shader != null)
            {
                Material material = new Material(shader);
                material.color = color;
                renderer.sharedMaterial = material;
            }
        }
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private static GameObject InstantiatePrefabIntoScene(string prefabPath, Vector3 position, Quaternion rotation)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
            return null;

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
            return null;

        instance.transform.position = position;
        instance.transform.rotation = rotation;
        return instance;
    }

    private static void TryAssignPlayerTagAndLayer(GameObject player)
    {
        if (player == null)
            return;

        try
        {
            player.tag = "Player";
        }
        catch (UnityException)
        {
            // The project normally has Player tag. If it does not, NPC auto-detection still finds the controller type.
        }

        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0)
            SetLayerRecursively(player, playerLayer);
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        for (int i = 0; i < root.transform.childCount; i++)
            SetLayerRecursively(root.transform.GetChild(i).gameObject, layer);
    }

    private static void SavePrefabAndDestroy(GameObject root, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static Font GetDefaultFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
            return font;

        try
        {
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        catch (System.ArgumentException)
        {
            return null;
        }
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        string parent = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
        string leaf = System.IO.Path.GetFileName(folder);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent, leaf);
    }
}
#endif
