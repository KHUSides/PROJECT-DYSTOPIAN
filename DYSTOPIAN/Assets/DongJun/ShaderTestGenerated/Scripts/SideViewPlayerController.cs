using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class SideViewPlayerController : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 4f;
    [SerializeField] private Vector2 horizontalLimits = new Vector2(-8.5f, 8.5f);
    [SerializeField] private Vector2 depthLimits = new Vector2(-1.2f, 1.2f);
    [SerializeField] private Transform visualRoot;

    [Header("Jump and Gravity")]
    [SerializeField, Min(0.1f)] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -25f;
    [SerializeField] private float groundedPull = -2f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField, Min(0f)] private float animationDampTime = 0.1f;


    private CharacterController characterController;
    private float verticalVelocity;


private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (animator == null && visualRoot != null)
        {
            animator = visualRoot.GetComponentInChildren<Animator>(true);
        }

        if (animator != null)
        {
            animator.enabled = true;
            animator.applyRootMotion = false;
        }
    }

private void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float depth = Input.GetAxisRaw("Vertical");

        Vector3 planarInput = Vector3.ClampMagnitude(
            new Vector3(horizontal, 0f, depth),
            1f);

        bool wasGrounded = characterController.isGrounded;
        if (wasGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedPull;
        }

        if (wasGrounded && Input.GetButtonDown("Jump"))
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            wasGrounded = false;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = planarInput * moveSpeed;
        velocity.y = verticalVelocity;

        CollisionFlags collisionFlags = characterController.Move(
            velocity * Time.deltaTime);

        bool isGrounded = (collisionFlags & CollisionFlags.Below) != 0
            || characterController.isGrounded;

        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedPull;
        }

        Vector3 position = transform.position;
        position.x = Mathf.Clamp(position.x, horizontalLimits.x, horizontalLimits.y);
        position.z = Mathf.Clamp(position.z, depthLimits.x, depthLimits.y);
        transform.position = position;

        if (visualRoot != null && Mathf.Abs(horizontal) > 0.01f)
        {
            float facingAngle = horizontal > 0f ? 90f : -90f;
            visualRoot.localRotation = Quaternion.Euler(0f, facingAngle, 0f);
        }

        if (animator != null)
        {
            animator.SetFloat(
                "Speed",
                planarInput.magnitude,
                animationDampTime,
                Time.deltaTime);
            animator.SetBool("Grounded", isGrounded);
            animator.SetFloat("VerticalSpeed", verticalVelocity);
        }
    }

public void SetVisualRoot(Transform value)
    {
        visualRoot = value;
        animator = visualRoot == null
            ? null
            : visualRoot.GetComponentInChildren<Animator>(true);
    }
}
