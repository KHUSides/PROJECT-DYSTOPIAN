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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            pendingScenePath_ = null;
            isTransitioning_ = false;
        }

        public static bool TryLoad(string targetScenePath)
        {
            if (isTransitioning_)
            {
                // LoadingScene이 아닌데 잠금이 남아 있다면 이전 전환이 비정상적으로
                // 끝난 경우입니다. 다음 씬 전환을 막지 않도록 상태를 복구합니다.
                if (SceneManager.GetActiveScene().name == "LoadingScene")
                    return false;

                Debug.LogWarning("[SceneTransition] 이전 전환 잠금을 복구합니다.");
                pendingScenePath_ = null;
                isTransitioning_ = false;
            }

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
