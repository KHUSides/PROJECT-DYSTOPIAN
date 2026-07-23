using UnityEngine;
using Dystopian.SuEun;

namespace Dystopian.SuEun.Lobby
{
    [RequireComponent(typeof(Collider))]
    public sealed class LobbyStageEntrance : MonoBehaviour
    {
        [SerializeField] private string targetSceneName_ = "TestScene";
        [SerializeField] private KeyCode interactKey_ = KeyCode.E;
        [SerializeField] private string buttonLabel_ = "전투 준비 - 스테이지 1 입장";

        private bool isPlayerInRange_;
        private bool isLoading_;
        private GUIStyle buttonStyle_;

        private void Awake()
        {
            Collider entranceCollider = GetComponent<Collider>();
            entranceCollider.isTrigger = true;
        }

        private void Update()
        {
            if (isPlayerInRange_ && Input.GetKeyDown(interactKey_))
                EnterStage();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<CharacterController>() != null)
                isPlayerInRange_ = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<CharacterController>() != null)
                isPlayerInRange_ = false;
        }

        private void OnGUI()
        {
            if (!isPlayerInRange_ || isLoading_)
                return;

            buttonStyle_ ??= new GUIStyle(GUI.skin.button)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold
            };

            Rect buttonRect = new Rect(Screen.width * 0.5f - 170f, Screen.height - 115f, 340f, 55f);
            if (GUI.Button(buttonRect, buttonLabel_ + "  [E]", buttonStyle_))
                EnterStage();
        }

        public void EnterStage()
        {
            if (isLoading_)
                return;

            if (!Application.CanStreamedLevelBeLoaded(targetSceneName_))
            {
                Debug.LogError($"[Lobby] Build Settings에 '{targetSceneName_}' 씬을 추가하세요.", this);
                return;
            }

            isLoading_ = SceneTransition.TryLoad(targetSceneName_);
        }
    }
}
