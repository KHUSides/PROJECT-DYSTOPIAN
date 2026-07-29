#if UNITY_EDITOR
using Dystopian.ObjectPixel;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dystopian.ObjectPixel.Editor
{
    [InitializeOnLoad]
    internal static class ObjectPixelRendererTestSceneBuilder
    {
        private const string ScenePath =
            "Assets/DongJun/Scenes/Test/ObjectPixelRendererTest.unity";
        private const string MaterialFolder =
            "Assets/DongJun/ObjectPixelRenderer/Materials";
        private const string BuildSessionKey =
            "DYSTOPIAN.ObjectPixelRendererTestSceneBuilder.v1";

        static ObjectPixelRendererTestSceneBuilder()
        {
            if (!SessionState.GetBool(BuildSessionKey, false))
                EditorApplication.delayCall += BuildOnce;
        }

        [MenuItem("DYSTOPIAN/Pixel Art/Rebuild Object Pixel Test Scene")]
        private static void RebuildFromMenu()
        {
            BuildScene(true);
        }

        private static void BuildOnce()
        {
            SessionState.SetBool(BuildSessionKey, true);

            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                BuildScene(false);
        }

        private static void BuildScene(bool overwrite)
        {
            if (!overwrite &&
                AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                return;
            }

            EnsureFolder("Assets/DongJun/ObjectPixelRenderer");
            EnsureFolder(MaterialFolder);
            EnsureFolder("Assets/DongJun/Scenes");
            EnsureFolder("Assets/DongJun/Scenes/Test");

            Scene previousScene = SceneManager.GetActiveScene();
            bool rebuildingOpenScene = previousScene.IsValid() &&
                                       previousScene.path == ScenePath;
            Scene scene;

            if (rebuildingOpenScene)
            {
                scene = previousScene;
                foreach (GameObject root in scene.GetRootGameObjects())
                    Object.DestroyImmediate(root);
            }
            else
            {
                scene = EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Additive);
                scene.name = "ObjectPixelRendererTest";
            }

            SceneManager.SetActiveScene(scene);

            BuildCamera();
            BuildLighting();
            BuildEnvironment();
            BuildPixelCharacters(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            if (!rebuildingOpenScene &&
                previousScene.IsValid() && previousScene.isLoaded)
            {
                SceneManager.SetActiveScene(previousScene);
            }

            if (!rebuildingOpenScene)
                EditorSceneManager.CloseScene(scene, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[ObjectPixelRenderer] Created a separate test scene at " +
                ScenePath + ". The previously open scene was not modified.");
        }

        private static void BuildCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 3.3f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.11f, 0.13f, 0.17f, 1f);
            camera.transform.position = new Vector3(0f, 1.8f, -12f);
            camera.transform.rotation = Quaternion.identity;

            IsolatedObjectPixelRenderer renderer =
                cameraObject.AddComponent<IsolatedObjectPixelRenderer>();
            renderer.pixelObjectLayer = 30;
            renderer.compositeLayer = 29;
            renderer.pixelHeight = 180;
            renderer.colorSteps = 6;
            renderer.ditherStrength = 0.85f;
            renderer.outlinePixels = 2;
            renderer.outlineColor = new Color(0.02f, 0.015f, 0.02f, 1f);
            renderer.silhouetteThreshold = 0.45f;
        }

        private static void BuildLighting()
        {
            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.91f, 0.78f);
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation =
                Quaternion.Euler(38f, -32f, 0f);
        }

        private static void BuildEnvironment()
        {
            Material floorMaterial = GetOrCreateMaterial(
                "ObjectPixelTest_Floor",
                new Color(0.16f, 0.18f, 0.22f));
            Material backgroundMaterial = GetOrCreateMaterial(
                "ObjectPixelTest_Background",
                new Color(0.23f, 0.27f, 0.34f));
            Material accentMaterial = GetOrCreateMaterial(
                "ObjectPixelTest_Accent",
                new Color(0.31f, 0.22f, 0.18f));

            CreateCube(
                "Unpixelated Ground",
                new Vector3(0f, -0.55f, 1.5f),
                new Vector3(14f, 1f, 5f),
                floorMaterial);
            CreateCube(
                "Unpixelated Back Wall",
                new Vector3(0f, 3.4f, 3.5f),
                new Vector3(14f, 7f, 0.5f),
                backgroundMaterial);
            CreateCube(
                "Background Pillar Left",
                new Vector3(-5.2f, 1.3f, 1.8f),
                new Vector3(1.1f, 4.2f, 1.1f),
                accentMaterial);
            CreateCube(
                "Background Pillar Right",
                new Vector3(5.2f, 1.3f, 1.8f),
                new Vector3(1.1f, 4.2f, 1.1f),
                accentMaterial);
        }

        private static void BuildPixelCharacters(Scene scene)
        {
            CreatePixelPrefab(
                scene,
                "Assets/Synty/PolygonDungeon/Prefabs/Characters/SM_Chr_Goblin_WarChief_01.prefab",
                "PIXEL TARGET - Goblin War Chief",
                new Vector3(-1.35f, 0f, 0f),
                new Vector3(0f, 180f, 0f),
                1.05f);

            CreatePixelPrefab(
                scene,
                "Assets/Synty/PolygonDungeon/Prefabs/Characters/SM_Chr_Rock_Golem_01.prefab",
                "PIXEL TARGET - Rock Golem",
                new Vector3(1.55f, 0f, 0.2f),
                new Vector3(0f, 180f, 0f),
                0.9f);
        }

        private static void CreatePixelPrefab(
            Scene scene,
            string prefabPath,
            string objectName,
            Vector3 position,
            Vector3 rotation,
            float scale)
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab == null)
            {
                Debug.LogError(
                    "[ObjectPixelRenderer] Missing Synty prefab: " +
                    prefabPath);
                return;
            }

            GameObject instance = (GameObject)
                PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = objectName;
            instance.transform.position = position;
            instance.transform.rotation = Quaternion.Euler(rotation);
            instance.transform.localScale = Vector3.one * scale;
            SetLayerRecursively(instance.transform, 30);

            IsolatedPixelTarget target =
                instance.AddComponent<IsolatedPixelTarget>();
            target.pixelLayer = 30;
        }

        private static GameObject CreateCube(
            string name,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.GetComponent<MeshRenderer>().sharedMaterial = material;
            return cube;
        }

        private static Material GetOrCreateMaterial(
            string materialName,
            Color color)
        {
            string path = MaterialFolder + "/" + materialName + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                    shader = Shader.Find("Standard");

                material = new Material(shader)
                {
                    name = materialName
                };
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;

            for (int i = 0; i < root.childCount; i++)
                SetLayerRecursively(root.GetChild(i), layer);
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
                return;

            int split = assetPath.LastIndexOf('/');
            string parent = assetPath.Substring(0, split);
            string folder = assetPath.Substring(split + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
#endif
