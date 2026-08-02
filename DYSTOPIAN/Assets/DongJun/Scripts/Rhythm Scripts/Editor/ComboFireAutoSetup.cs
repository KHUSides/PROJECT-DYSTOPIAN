using Dystopian.Rhythm;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dystopian.EditorTools
{
    internal static class ComboFireAutoSetup
    {
        private const string PrefabPath = "Assets/DongJun/Prefabs/Rhythm System.prefab";
        private const string ScenePath = "Assets/DongJun/Scenes/RhythmTest.unity";
        private const string SmallFolder = "Assets/DongJun/Images/ComboFires/Small";
        private const string MediumFolder = "Assets/DongJun/Images/ComboFires/Medium";
        private const string BigFolder = "Assets/DongJun/Images/ComboFires/Big";
        private static readonly Vector2 FireSize = new Vector2(280f, 280f);
        private static readonly Vector2 FireOffsetFromCombo = new Vector2(0f, 100f);

        [MenuItem("Tools/Dystopian/Setup Combo Fire UI")]
        private static void RunFromMenu()
        {
            RunSetup(true);
        }

        private static void RunSetup(bool logWhenUnchanged)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            EnsureFrameImportSettings(SmallFolder);
            EnsureFrameImportSettings(MediumFolder);
            EnsureFrameImportSettings(BigFolder);

            Sprite[] smallFrames = LoadFrames(SmallFolder);
            Sprite[] mediumFrames = LoadFrames(MediumFolder);
            Sprite[] bigFrames = LoadFrames(BigFolder);
            if (!HasTwentyFrames(smallFrames) || !HasTwentyFrames(mediumFrames) || !HasTwentyFrames(bigFrames))
            {
                Debug.LogError("[Combo Fire] Small, Medium, and Big folders must each contain frame_01.png through frame_20.png.");
                return;
            }

            bool prefabChanged = ConfigurePrefab(smallFrames, mediumFrames, bigFrames);
            bool sceneChanged = ConfigureScene(smallFrames, mediumFrames, bigFrames);
            if (prefabChanged || sceneChanged || logWhenUnchanged)
            {
                Debug.Log(
                    "[Combo Fire] Setup complete. 10-19: Small, 20-29: Medium, 30+: Big. " +
                    "The fire is immediately behind Combo Text.");
            }
        }

        private static bool ConfigurePrefab(Sprite[] smallFrames, Sprite[] mediumFrames, Sprite[] bigFrames)
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (prefabRoot == null)
            {
                Debug.LogError("[Combo Fire] Could not load prefab: " + PrefabPath);
                return false;
            }

            bool changed = false;
            try
            {
                RhythmSystem[] rhythmSystems = prefabRoot.GetComponentsInChildren<RhythmSystem>(true);
                for (int i = 0; i < rhythmSystems.Length; i++)
                {
                    changed |= ConfigureRhythmUi(rhythmSystems[i], smallFrames, mediumFrames, bigFrames);
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            return changed;
        }

        private static bool ConfigureScene(Sprite[] smallFrames, Sprite[] mediumFrames, Sprite[] bigFrames)
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool wasLoaded = scene.IsValid() && scene.isLoaded;
            if (!wasLoaded)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            }

            bool changed = false;
            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                for (int i = 0; i < roots.Length; i++)
                {
                    RhythmSystem[] rhythmSystems = roots[i].GetComponentsInChildren<RhythmSystem>(true);
                    for (int j = 0; j < rhythmSystems.Length; j++)
                    {
                        changed |= ConfigureRhythmUi(rhythmSystems[j], smallFrames, mediumFrames, bigFrames);
                    }
                }

                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                }
            }
            finally
            {
                if (!wasLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }

            return changed;
        }

        private static bool ConfigureRhythmUi(
            RhythmSystem rhythmSystem,
            Sprite[] smallFrames,
            Sprite[] mediumFrames,
            Sprite[] bigFrames)
        {
            Transform panel = rhythmSystem.transform.Find("Rhythm UI/Track Panel");
            Transform comboText = panel != null ? panel.Find("Combo Text") : null;
            if (panel == null || comboText == null)
            {
                Debug.LogError("[Combo Fire] Rhythm UI/Track Panel/Combo Text was not found.", rhythmSystem);
                return false;
            }

            bool changed = false;
            Transform fireTransform = panel.Find("Combo Fire");
            if (fireTransform == null)
            {
                GameObject fireObject = new GameObject(
                    "Combo Fire",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(ComboFireUI));
                fireObject.layer = panel.gameObject.layer;
                fireTransform = fireObject.transform;
                fireTransform.SetParent(panel, false);
                changed = true;
            }

            RectTransform fireRect = fireTransform as RectTransform;
            RectTransform comboRect = comboText as RectTransform;
            if (fireRect == null || comboRect == null)
            {
                Debug.LogError("[Combo Fire] Combo Fire and Combo Text must use RectTransform.", rhythmSystem);
                return changed;
            }

            Vector2 expectedPosition = comboRect.anchoredPosition + FireOffsetFromCombo;
            changed |= SetRectTransform(fireRect, expectedPosition);

            if (fireTransform.GetSiblingIndex() >= comboText.GetSiblingIndex())
            {
                fireTransform.SetSiblingIndex(comboText.GetSiblingIndex());
                changed = true;
            }

            Image image = fireTransform.GetComponent<Image>();
            ComboFireUI controller = fireTransform.GetComponent<ComboFireUI>();
            if (image == null || controller == null)
            {
                Debug.LogError("[Combo Fire] Required Image or ComboFireUI component is missing.", fireTransform);
                return changed;
            }

            if (image.raycastTarget || !image.preserveAspect || image.type != Image.Type.Simple ||
                image.useSpriteMesh || image.color != Color.white || image.sprite != smallFrames[0])
            {
                image.raycastTarget = false;
                image.preserveAspect = true;
                image.type = Image.Type.Simple;
                image.useSpriteMesh = false;
                image.color = Color.white;
                image.sprite = smallFrames[0];
                image.enabled = false;
                EditorUtility.SetDirty(image);
                changed = true;
            }

            if (!controller.MatchesEditorConfiguration(
                    rhythmSystem,
                    image,
                    smallFrames,
                    mediumFrames,
                    bigFrames))
            {
                controller.ConfigureInEditor(rhythmSystem, image, smallFrames, mediumFrames, bigFrames);
                EditorUtility.SetDirty(controller);
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(fireRect);
                EditorUtility.SetDirty(fireTransform.gameObject);
            }

            return changed;
        }

        private static bool SetRectTransform(RectTransform rect, Vector2 anchoredPosition)
        {
            bool changed = rect.anchorMin != new Vector2(0.5f, 0.5f) ||
                rect.anchorMax != new Vector2(0.5f, 0.5f) ||
                rect.pivot != new Vector2(0.5f, 0.5f) ||
                rect.anchoredPosition != anchoredPosition ||
                rect.sizeDelta != FireSize ||
                rect.localScale != Vector3.one ||
                rect.localRotation != Quaternion.identity;
            if (!changed)
            {
                return false;
            }

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = FireSize;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            return true;
        }

        private static Sprite[] LoadFrames(string folder)
        {
            Sprite[] frames = new Sprite[20];
            for (int i = 0; i < frames.Length; i++)
            {
                string path = folder + "/frame_" + (i + 1).ToString("00") + ".png";
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            return frames;
        }

        private static bool HasTwentyFrames(Sprite[] frames)
        {
            if (frames == null || frames.Length != 20)
            {
                return false;
            }

            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] == null)
                {
                    return false;
                }
            }

            return true;
        }

        private static void EnsureFrameImportSettings(string folder)
        {
            for (int i = 1; i <= 20; i++)
            {
                string path = folder + "/frame_" + i.ToString("00") + ".png";
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                bool changed = importer.textureType != TextureImporterType.Sprite ||
                    importer.spriteImportMode != SpriteImportMode.Single ||
                    importer.mipmapEnabled ||
                    !importer.alphaIsTransparency ||
                    importer.wrapMode != TextureWrapMode.Clamp ||
                    importer.filterMode != FilterMode.Bilinear ||
                    settings.spriteMeshType != SpriteMeshType.FullRect;
                if (!changed)
                {
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }
    }
}
