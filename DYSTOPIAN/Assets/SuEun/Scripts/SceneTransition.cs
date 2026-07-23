using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dystopian.SuEun
{
    /// <summary>
    /// 모든 일반 씬 전환의 진입점입니다. LoadingScene을 먼저 거쳐 이전 씬을 메모리에서 내립니다.
    /// </summary>
    public static class SceneTransition
    {
        public const string LoadingScenePath = "SuEun/Scene/LoadingScene";

        private static string pendingScenePath_;
        private static bool isTransitioning_;

        public static bool TryLoad(string targetScenePath)
        {
            if (isTransitioning_)
                return false;

            if (string.IsNullOrWhiteSpace(targetScenePath) ||
                !Application.CanStreamedLevelBeLoaded(targetScenePath))
            {
                Debug.LogError($"[SceneTransition] Build Settings에 목적지 씬을 추가하세요: {targetScenePath}");
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(LoadingScenePath))
            {
                Debug.LogError($"[SceneTransition] Build Settings에 LoadingScene을 추가하세요: {LoadingScenePath}");
                return false;
            }

            pendingScenePath_ = targetScenePath;
            isTransitioning_ = true;
            SceneManager.LoadSceneAsync(LoadingScenePath, LoadSceneMode.Single);
            return true;
        }

        internal static bool TryConsumePendingScene(out string targetScenePath)
        {
            targetScenePath = pendingScenePath_;
            pendingScenePath_ = null;
            return !string.IsNullOrWhiteSpace(targetScenePath);
        }

        internal static void Complete()
        {
            isTransitioning_ = false;
        }
    }
}
