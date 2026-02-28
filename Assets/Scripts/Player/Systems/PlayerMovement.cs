using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float crouchSpeed = 2f;
    [SerializeField] private float sprintSpeed = 8f;

    [Header("Smoothing")]
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float deceleration = 14f;
    [SerializeField] private float airAcceleration = 6f;
    [SerializeField] private float airDeceleration = 2f;
    [SerializeField] private float brakingDeceleration = 20f; // резкий стоп при смене направления
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.12f;
    [SerializeField] private float groundFriction = 8f;
    [SerializeField] private float gravityStrength = -30f;

    [SerializeField] private float staminaDrainRate = 20f;
    [SerializeField] private float staminaRegenRate = 15f;
    [SerializeField] private float staminaRegenDelay = 2f;
    [SerializeField] private float staminaMax = 100f;

    private CharacterController controller;
    private PlayerInputActions inputActions;
    private Vector2 moveInput;
    private Vector3 horizontalVelocity = Vector3.zero;
    private float yVelocity = 0f;
    private bool isGrounded;
    private bool isSprinting = false;
    private bool isCrouching = false;
    private bool canRegenStamina = false;
    private float lastSprintTime = 0f;
    private float currentStamina = 100f;
    private float coyoteCounter = 0f;
    private float jumpBufferCounter = 0f;

    public bool IsGrounded => isGrounded;
    public bool IsSprinting => isSprinting;
    public bool IsCrouching => isCrouching;
    public float CurrentStamina => currentStamina;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        inputActions = new PlayerInputActions();
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;
        inputActions.Player.Sprint.performed += _ => isSprinting = true;
        inputActions.Player.Sprint.canceled += _ => isSprinting = false;
        inputActions.Player.Crouch.performed += _ => isCrouching = !isCrouching;
        inputActions.Player.Jump.performed += _ => jumpBufferCounter = jumpBufferTime;
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

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
        if (jumpBufferCounter > 0f && coyoteCounter > 0f && !isCrouching)
        {
            yVelocity = Mathf.Sqrt(jumpHeight * -2f * gravityStrength);
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }
    }

    private void HandleStamina()
    {
        if (isSprinting && moveInput.magnitude > 0.1f)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0, staminaMax);
            lastSprintTime = Time.time;
            canRegenStamina = false;

            if (currentStamina <= 0.01f) isSprinting = false;
        }
        else
        {
            if (!canRegenStamina && Time.time - lastSprintTime > staminaRegenDelay)
                canRegenStamina = true;
            if (canRegenStamina)
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                currentStamina = Mathf.Clamp(currentStamina, 0, staminaMax);
            }
        }
    }

    private void HandleMovement()
    {
        float targetSpeed = moveSpeed;
        if (isCrouching) targetSpeed = crouchSpeed;
        else if (isSprinting && currentStamina > 0f) targetSpeed = sprintSpeed;

        Vector3 inputDir = (transform.right * moveInput.x + transform.forward * moveInput.y);
        inputDir = inputDir.sqrMagnitude > 1f ? inputDir.normalized : inputDir;

        Vector3 desiredHorizontal = inputDir * targetSpeed;

        bool hasInput = inputDir.sqrMagnitude > 0.01f;
        bool reversing = hasInput && Vector3.Dot(horizontalVelocity, desiredHorizontal) < 0f;

        float accel = isGrounded ? acceleration : airAcceleration;
        float decel = isGrounded ? deceleration : airDeceleration;
        float brake = isGrounded ? brakingDeceleration : decel;

        float moveRate = hasInput
            ? (reversing ? brake : accel)
            : decel;

        // Если нет ввода — тянем к нулю
        Vector3 targetVel = hasInput ? desiredHorizontal : Vector3.zero;

        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVel, moveRate * Time.deltaTime);

        if (isGrounded)
        {
            if (yVelocity < 0) yVelocity = -5f; // сильнее прижимаем к земле, чтобы не зависать

            if (inputDir.sqrMagnitude < 0.0001f)
            {
                // дополнительное сухое трение, чтобы не скользить
                float frictionFactor = Mathf.Clamp01(groundFriction * Time.deltaTime);
                horizontalVelocity *= (1f - frictionFactor);
                if (horizontalVelocity.sqrMagnitude < 0.01f) horizontalVelocity = Vector3.zero;
            }
        }

        yVelocity = Mathf.Max(yVelocity + gravityStrength * Time.deltaTime, -80f);

        Vector3 moveThisFrame = new Vector3(horizontalVelocity.x, 0f, horizontalVelocity.z);
        moveThisFrame += Vector3.up * yVelocity;

        controller.Move(moveThisFrame * Time.deltaTime);
    }

    

    public float GetMoveSpeed()
    {
        return new Vector3(horizontalVelocity.x, 0, horizontalVelocity.z).magnitude;
    }
    public bool IsMoving => new Vector3(horizontalVelocity.x, 0, horizontalVelocity.z).magnitude > 0.1f;

}
