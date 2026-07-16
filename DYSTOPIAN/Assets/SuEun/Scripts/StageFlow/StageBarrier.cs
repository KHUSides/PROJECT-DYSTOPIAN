using UnityEngine;
using UnityEngine.Serialization;

namespace Dystopian.SuEun.StageFlow
{
    public class StageBarrier : MonoBehaviour
    {
        [FormerlySerializedAs("startClosed")]
        [SerializeField] private bool startClosed_;
        [FormerlySerializedAs("visualRoot")]
        [SerializeField] private GameObject visualRoot_;
        [FormerlySerializedAs("blockingCollider")]
        [SerializeField] private Collider blockingCollider_;

        public bool IsClosed { get; private set; }

        private void Awake()
        {
            if (visualRoot_ == null)
                visualRoot_ = gameObject;

            if (blockingCollider_ == null)
                blockingCollider_ = GetComponent<Collider>();

            SetClosed(startClosed_);
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

            if (visualRoot_ != null)
                visualRoot_.SetActive(closed);

            if (blockingCollider_ != null)
                blockingCollider_.enabled = closed;
        }
    }
}
