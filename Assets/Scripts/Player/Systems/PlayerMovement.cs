using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Speed")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float crouchSpeed = 2f;
    [SerializeField] private float jumpHeight = 2f;

    [Header("Source-Lite Movement")]
    [SerializeField] private float groundAcceleration = 14f;
    [SerializeField] private float airAcceleration = 22f;
    [SerializeField] private float airMaxWishSpeed = 3.2f;
    [SerializeField] private float groundFriction = 10f;
    [SerializeField] private float gravityStrength = -30f;
    [SerializeField] private float maxFallSpeed = 80f;

    [Header("Jump Assist")]
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Stamina")]
    [SerializeField] private float staminaDrainRate = 20f;
    [SerializeField] private float staminaRegenRate = 15f;
    [SerializeField] private float staminaRegenDelay = 2f;
    [SerializeField] private float staminaMax = 100f;

    private CharacterController controller;
    private PlayerInputActions inputActions;
    private PlayerCrouch crouch;

    private Vector2 moveInput;
    private Vector3 horizontalVelocity = Vector3.zero;
    private float yVelocity;

    private bool isGrounded;
    private bool sprintHeld;
    private bool canRegenStamina;
    private float lastSprintTime;
    private float currentStamina = 100f;
    private float coyoteCounter;
    private float jumpBufferCounter;
    private float aimMultiplier = 1f;
    private float neuroMultiplier = 1f;

    public bool IsGrounded => isGrounded;
    public bool IsSprinting => sprintHeld && !IsCrouching && currentStamina > 0f && moveInput.sqrMagnitude > 0.01f;
    public bool IsCrouching => crouch != null && crouch.IsCrouching;
    public float CurrentStamina => currentStamina;
    public float MaxStamina => staminaMax;
    public bool IsMoving => horizontalVelocity.sqrMagnitude > 0.01f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        crouch = GetComponent<PlayerCrouch>();
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        inputActions = GameInput.Instance.Actions;
        inputActions.Player.Move.performed += OnMovePerformed;
        inputActions.Player.Move.canceled += OnMoveCanceled;
        inputActions.Player.Sprint.performed += OnSprintPerformed;
        inputActions.Player.Sprint.canceled += OnSprintCanceled;
        inputActions.Player.Jump.performed += OnJumpPerformed;
    }

    private void OnDisable()
    {
        if (inputActions == null) return;
        inputActions.Player.Move.performed -= OnMovePerformed;
        inputActions.Player.Move.canceled -= OnMoveCanceled;
        inputActions.Player.Sprint.performed -= OnSprintPerformed;
        inputActions.Player.Sprint.canceled -= OnSprintCanceled;
        inputActions.Player.Jump.performed -= OnJumpPerformed;
    }

    private void OnMovePerformed(InputAction.CallbackContext ctx) => moveInput = ctx.ReadValue<Vector2>();
    private void OnMoveCanceled(InputAction.CallbackContext ctx) => moveInput = Vector2.zero;
    private void OnSprintPerformed(InputAction.CallbackContext ctx) => sprintHeld = true;
    private void OnSprintCanceled(InputAction.CallbackContext ctx) => sprintHeld = false;
    private void OnJumpPerformed(InputAction.CallbackContext ctx) => jumpBufferCounter = jumpBufferTime;

    private void Update()
    {
        isGrounded = controller.isGrounded;
        coyoteCounter = isGrounded ? coyoteTime : Mathf.Max(coyoteCounter - Time.deltaTime, 0f);
        jumpBufferCounter = Mathf.Max(jumpBufferCounter - Time.deltaTime, 0f);

        HandleJump();
        HandleStamina();
        HandleMovement();
    }

    private void HandleJump()
    {
        if (jumpBufferCounter > 0f && coyoteCounter > 0f && !IsCrouching)
        {
            yVelocity = Mathf.Sqrt(jumpHeight * -2f * gravityStrength);
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }
    }

    private void HandleStamina()
    {
        if (IsSprinting)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0f, staminaMax);
            lastSprintTime = Time.time;
            canRegenStamina = false;

            if (currentStamina <= 0.01f) sprintHeld = false;
        }
        else
        {
            if (!canRegenStamina && Time.time - lastSprintTime > staminaRegenDelay)
                canRegenStamina = true;

            if (canRegenStamina)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                currentStamina = Mathf.Clamp(currentStamina, 0f, staminaMax);
            }
        }
    }

    private void HandleMovement()
    {
        float targetMaxSpeed = moveSpeed;
        if (IsCrouching) targetMaxSpeed = crouchSpeed;
        else if (IsSprinting) targetMaxSpeed = sprintSpeed;
        targetMaxSpeed *= aimMultiplier * neuroMultiplier;

        Vector3 wishDirection = (transform.right * moveInput.x + transform.forward * moveInput.y);
        if (wishDirection.sqrMagnitude > 1f) wishDirection.Normalize();

        if (isGrounded)
        {
            ApplyGroundFriction();
            Accelerate(wishDirection, targetMaxSpeed, groundAcceleration);

            if (yVelocity < 0f) yVelocity = -2f;
        }
        else
        {
            AirAccelerate(wishDirection, targetMaxSpeed, airAcceleration);
        }

        yVelocity = Mathf.Max(yVelocity + gravityStrength * Time.deltaTime, -maxFallSpeed);

        Vector3 velocity = horizontalVelocity + Vector3.up * yVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    private void ApplyGroundFriction()
    {
        float speed = horizontalVelocity.magnitude;
        if (speed < 0.001f)
        {
            horizontalVelocity = Vector3.zero;
            return;
        }

        float drop = speed * groundFriction * Time.deltaTime;
        float newSpeed = Mathf.Max(speed - drop, 0f);
        horizontalVelocity *= newSpeed / speed;
    }

    private void Accelerate(Vector3 wishDir, float wishSpeed, float accel)
    {
        if (wishDir.sqrMagnitude < 0.0001f || wishSpeed <= 0f) return;

        float currentSpeed = Vector3.Dot(horizontalVelocity, wishDir);
        float addSpeed = wishSpeed - currentSpeed;
        if (addSpeed <= 0f) return;

        float accelSpeed = accel * wishSpeed * Time.deltaTime;
        if (accelSpeed > addSpeed) accelSpeed = addSpeed;
        horizontalVelocity += wishDir * accelSpeed;
    }

    private void AirAccelerate(Vector3 wishDir, float wishSpeed, float accel)
    {
        if (wishDir.sqrMagnitude < 0.0001f || wishSpeed <= 0f) return;

        float cappedWishSpeed = Mathf.Min(wishSpeed, airMaxWishSpeed);
        float currentSpeed = Vector3.Dot(horizontalVelocity, wishDir);
        float addSpeed = cappedWishSpeed - currentSpeed;
        if (addSpeed <= 0f) return;

        float accelSpeed = accel * cappedWishSpeed * Time.deltaTime;
        if (accelSpeed > addSpeed) accelSpeed = addSpeed;
        horizontalVelocity += wishDir * accelSpeed;
    }

    public float GetMoveSpeed()
    {
        return horizontalVelocity.magnitude;
    }

    public void SetAimMultiplier(float multiplier) => aimMultiplier = Mathf.Max(0.1f, multiplier);
    public void SetNeuroMultiplier(float multiplier) => neuroMultiplier = Mathf.Max(0.1f, multiplier);
}
