using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCrouch : MonoBehaviour
{
    [Header("Crouch")]
    [SerializeField] private float standHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchSmoothTime = 0.22f;

    private CharacterController controller;
    private PlayerInputActions inputActions;
    private PlayerMovement movement;
    private bool isCrouching = false;
    private float currentHeight;
    private float heightVelocity = 0f;

    public bool IsCrouching => isCrouching;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        inputActions = GameInput.Instance.Actions;
        movement = GetComponent<PlayerMovement>();
    }

    private void OnEnable()
    {
        inputActions.Player.Crouch.performed += OnCrouch;
    }

    private void OnDisable()
    {
        if (inputActions == null) return;
        inputActions.Player.Crouch.performed -= OnCrouch;
    }

    private void OnCrouch(UnityEngine.InputSystem.InputAction.CallbackContext _) => isCrouching = !isCrouching;

    private void Update()
    {
        HandleCrouching();
    }

    public void ForceStand()
    {
        isCrouching = false;
        HandleCrouching();
    }

    private void HandleCrouching()
    {
        float targetHeight = isCrouching ? crouchHeight : standHeight;
        float targetCenterY = targetHeight / 2f;

        if (movement != null && movement.IsGrounded)
        {
            float currentBottom = transform.position.y - controller.center.y + controller.height / 2f;
            float newBottom = transform.position.y - targetCenterY + targetHeight / 2f;
            float heightDiff = currentBottom - newBottom;
            transform.position -= Vector3.up * heightDiff;
        }

        controller.height = targetHeight;
        controller.center = Vector3.up * targetCenterY;
    }

}
