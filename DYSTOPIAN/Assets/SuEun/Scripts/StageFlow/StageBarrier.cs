using UnityEngine;

namespace Dystopian.SuEun.StageFlow
{
    public class StageBarrier : MonoBehaviour
    {
        [SerializeField] private bool startClosed;
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private Collider blockingCollider;

        public bool IsClosed { get; private set; }

        private void Awake()
        {
            if (visualRoot == null)
                visualRoot = gameObject;

            if (blockingCollider == null)
                blockingCollider = GetComponent<Collider>();

            SetClosed(startClosed);
        }

        public void Close()
        {
            SetClosed(true);
        }

        public void Open()
        {
            SetClosed(false);
        }

        public void SetClosed(bool closed)
        {
            IsClosed = closed;

            if (visualRoot != null)
                visualRoot.SetActive(closed);

            if (blockingCollider != null)
                blockingCollider.enabled = closed;
        }
    }
}
