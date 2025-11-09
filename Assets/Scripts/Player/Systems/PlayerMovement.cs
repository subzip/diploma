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

    [SerializeField] private float staminaDrainRate = 20f;
    [SerializeField] private float staminaRegenRate = 15f;
    [SerializeField] private float staminaRegenDelay = 2f;
    [SerializeField] private float staminaMax = 100f;

    private CharacterController controller;
    private PlayerInputActions inputActions;
    private Vector2 moveInput;
    private float yVelocity = 0f;
    private bool isGrounded;
    private bool isSprinting = false;
    private bool isCrouching = false;
    private bool canRegenStamina = false;
    private float lastSprintTime = 0f;
    private float currentStamina = 100f;

    public bool IsGrounded => isGrounded;
    public bool IsSprinting => isSprinting;
    public bool IsCrouching => isCrouching;
    public float CurrentStamina => currentStamina;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;
        inputActions.Player.Sprint.performed += _ => isSprinting = true;
        inputActions.Player.Sprint.canceled += _ => isSprinting = false;
        inputActions.Player.Crouch.performed += _ => isCrouching = !isCrouching;
        inputActions.Player.Jump.performed += _ => {
            if (isGrounded && !isCrouching)
                yVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        };
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

    private void Update()
    {
        isGrounded = controller.isGrounded;
        HandleStamina();
        HandleMovement();
    }

    private void HandleStamina()
    {
        if (isSprinting && moveInput.magnitude > 0.1f)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0, staminaMax);
            lastSprintTime = Time.time;
            canRegenStamina = false;
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
        float speed = moveSpeed;
        if (isCrouching) speed = crouchSpeed;
        else if (isSprinting && currentStamina > 0) speed = sprintSpeed;

        Vector3 direction = transform.right * moveInput.x + transform.forward * moveInput.y;
        Vector3 movement = direction * speed * Time.deltaTime;

        if (isGrounded && yVelocity < 0) yVelocity = -1f;
        yVelocity += gravity * Time.deltaTime;
        movement.y = yVelocity * Time.deltaTime;

        controller.Move(movement);
    }


    public bool IsMoving => moveInput.magnitude > 0.1f;

}