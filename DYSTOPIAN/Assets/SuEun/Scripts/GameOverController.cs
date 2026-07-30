using System.Collections;
using UnityEngine;

namespace Dystopian.SuEun
{
    /// <summary>
    /// TestScene에서 플레이어의 HP가 최대치에 도달했을 때 게임 오버를 표시하고,
    /// LoadingScene을 거쳐 로비로 되돌립니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameOverController : MonoBehaviour
    {
        // SceneTransition/LoadingSceneLoader가 사용하는 Build Settings 경로입니다.
        private const string LobbyScenePath = "SuEun/Scene/Lobby";
        private const float DisplayDuration = 2f;

        [SerializeField] private GameObject gameOverPanel_;
        private PlayerHealth playerHealth_;
        private bool isGameOver_;

        private void Awake()
        {
            playerHealth_ = GetComponent<PlayerHealth>();

            if (playerHealth_ == null)
                playerHealth_ = FindFirstObjectByType<PlayerHealth>();

            if (gameOverPanel_ != null)
                gameOverPanel_.SetActive(false);
        }

        private void Update()
        {
            if (!isGameOver_ && playerHealth_ != null && playerHealth_.IsFull)
                StartCoroutine(GameOverRoutine());
        }

        private IEnumerator GameOverRoutine()
        {
            isGameOver_ = true;
            if (gameOverPanel_ == null)
            {
                Debug.LogError("[GameOver] TestScene의 GameOverPanel 참조가 없습니다.", this);
                yield break;
            }

            gameOverPanel_.SetActive(true);

            // 로딩 씬은 자체 코루틴과 비동기 로드를 사용하므로 Time.timeScale을
            // 변경하지 않습니다. 게임 오버 표시는 실시간으로만 잠시 유지합니다.
            yield return new WaitForSecondsRealtime(DisplayDuration);

            if (!SceneTransition.TryLoad(LobbyScenePath))
            {
                Debug.LogError("[GameOver] 로비 씬 전환을 시작할 수 없습니다.", this);
                isGameOver_ = false;
            }
        }

    }
}
