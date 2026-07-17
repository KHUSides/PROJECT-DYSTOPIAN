using UnityEngine;

namespace Dystopian.EnemyTest
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class DummyPlayerController : MonoBehaviour
    {
        [Header("Side View")]
        [SerializeField] private float lockedZ = 0f;
        [SerializeField] private bool forcePlayerLayer = true;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 7f;
        [SerializeField, Min(0f)] private float jumpVelocity = 15f;
        [SerializeField] private float gravity = -35f;
        [SerializeField] private float groundedStickVelocity = -2f;

        private CharacterController characterController;
        private float verticalVelocity;
        private int facingSign = 1;

        public int FacingSign => facingSign;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            ApplyPlayerLayerIfPossible();
        }

        private void Update()
        {
            float horizontalInput = GetHorizontalInput();
            if (Mathf.Abs(horizontalInput) > 0.01f)
            {
                facingSign = horizontalInput < 0f ? -1 : 1;
            }

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = groundedStickVelocity;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow) && characterController.isGrounded)
            {
                verticalVelocity = jumpVelocity;
            }

            verticalVelocity += gravity * Time.deltaTime;

            Vector3 movement = new Vector3(
                horizontalInput * moveSpeed,
                verticalVelocity,
                0f);

            characterController.Move(movement * Time.deltaTime);
            LockSideViewPlane();
        }

        private float GetHorizontalInput()
        {
            bool left = Input.GetKey(KeyCode.LeftArrow);
            bool right = Input.GetKey(KeyCode.RightArrow);

            if (left == right)
            {
                return 0f;
            }

            return left ? -1f : 1f;
        }

        private void ApplyPlayerLayerIfPossible()
        {
            if (!forcePlayerLayer)
            {
                return;
            }

            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
            {
                gameObject.layer = playerLayer;
            }
        }

        private void LockSideViewPlane()
        {
            Vector3 position = transform.position;
            position.z = lockedZ;
            transform.position = position;
        }

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0f, moveSpeed);
            jumpVelocity = Mathf.Max(0f, jumpVelocity);
        }
    }
}
