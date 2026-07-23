using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dystopian.SuEun
{
    /// <summary>
    /// LoadingScene에만 존재합니다.
    /// 이전 씬의 미사용 리소스를 정리하고 목적지 씬을 비동기로 적재합니다.
    /// </summary>
    public sealed class LoadingSceneLoader : MonoBehaviour
    {
        [Header("Loading UI")]
        [SerializeField]
        private Image loadingGaugeImage;

        [SerializeField]
        private TMP_Text loadingStatusText;

        [SerializeField]
        private TMP_Text loadingPercentText;

        [Header("Progress Settings")]
        [SerializeField, Range(0f, 0.5f)]
        private float unloadProgressWeight = 0.15f;

        private IEnumerator Start()
        {
            SetProgress(0f);
            SetStatus("로딩을 준비하는 중...");

            if (!SceneTransition.TryConsumePendingScene(
                    out string targetScenePath))
            {
                Debug.LogWarning(
                    "[LoadingScene] 목적지 씬 없이 직접 열렸습니다.",
                    this
                );

                SetStatus("목적지 씬을 찾을 수 없습니다.");
                yield break;
            }

            // 로딩 화면과 애니메이션을 최소 한 프레임 표시합니다.
            yield return null;

            // 이전 씬의 미사용 리소스 정리
            // yield return UnloadUnusedAssets();

            // 목적지 씬 로딩
            yield return LoadTargetScene(targetScenePath);
        }

        private IEnumerator UnloadUnusedAssets()
        {
            SetStatus("이전 데이터를 정리하는 중...");

            AsyncOperation unloadOperation =
                Resources.UnloadUnusedAssets();

            while (!unloadOperation.isDone)
            {
                float progress = Mathf.Lerp(
                    0f,
                    unloadProgressWeight,
                    unloadOperation.progress
                );

                SetProgress(progress);

                yield return null;
            }

            SetProgress(unloadProgressWeight);
        }

        private IEnumerator LoadTargetScene(string targetScenePath)
        {
            SetStatus("게임 데이터를 불러오는 중...");

            AsyncOperation loadOperation =
                SceneManager.LoadSceneAsync(
                    targetScenePath,
                    LoadSceneMode.Single
                );

            if (loadOperation == null)
            {
                Debug.LogError(
                    $"[LoadingScene] 씬을 불러올 수 없습니다: {targetScenePath}",
                    this
                );

                SetStatus("씬을 불러오지 못했습니다.");

                SceneTransition.Complete();
                yield break;
            }

            // 100%가 화면에 표시된 뒤 씬을 활성화하기 위해 막아둡니다.
            loadOperation.allowSceneActivation = false;

            while (loadOperation.progress < 0.9f)
            {
                float normalizedLoadProgress =
                    Mathf.Clamp01(loadOperation.progress / 0.9f);

                float totalProgress = Mathf.Lerp(
                    unloadProgressWeight,
                    1f,
                    normalizedLoadProgress
                );

                SetProgress(totalProgress);

                yield return null;
            }

            SetProgress(1f);
            SetStatus("로딩 완료");

            // 100%와 완료 문구가 최소 한 프레임 표시됩니다.
            // yield return null;
            yield return new WaitForSeconds(1.5f);

            SceneTransition.Complete();

            loadOperation.allowSceneActivation = true;

            yield return loadOperation;
        }

        private void SetProgress(float progress)
        {
            float clampedProgress = Mathf.Clamp01(progress);

            if (loadingGaugeImage != null)
            {
                loadingGaugeImage.fillAmount = clampedProgress;
            }

            if (loadingPercentText != null)
            {
                int percentage = Mathf.RoundToInt(
                    clampedProgress * 100f
                );

                loadingPercentText.text = $"{percentage}%";
            }
        }

        private void SetStatus(string status)
        {
            if (loadingStatusText != null)
            {
                loadingStatusText.text = status;
            }
        }
    }
}