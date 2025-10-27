using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float crouchSpeed = 2f;
    [SerializeField] private float standHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchSmoothTime = 0.22f;

    [Header("Sprint & Stamina")]
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float staminaMax = 100f;
    [SerializeField] private float staminaDrainRate = 20f;
    [SerializeField] private float staminaRegenRate = 15f;
    [SerializeField] private float staminaRegenDelay = 2f;

    [Header("Audio")]
    [SerializeField] private AudioClip[] footstepSounds;  // 0.3–0.5 сек
    [SerializeField] private AudioClip breathIdle;         // 0.8–1.2 сек
    [SerializeField] private AudioClip sighSound;          // 1.0–1.5 сек

    private bool isCrouching = false;
    private float currentHeight;
    private float heightVelocity = 0f;

    private float currentStamina;
    private bool isSprinting = false;
    private bool canRegenStamina = false;
    private float lastSprintTime = 0f;

    private AudioSource audioSource;
    private float lastFootstepTime = 0f;
    private float footstepInterval = 0.5f;
    private float lastSighTime = 0f;
    private float sighInterval = 20f;
    private bool wasMoving = false; // Ключевой флаг для дыхания

    public Animator animator;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float standCameraHeight = 1.65f;
    [SerializeField] private float crouchCameraHeight = 1.0f;
    private float cameraHeightVelocity = 0f;

    [Header("Look")]
    [SerializeField] private float lookSensitivity = 2f;
    [SerializeField] private float maxLookUp = 80f;
    [SerializeField] private float maxLookDown = 45f;

    private PlayerInputActions inputActions;
    private CharacterController controller;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float yVelocity = 0f;
    private bool isGrounded;
    private float xRotation = 0f;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.maxDistance = 15f;
        }

        currentHeight = standHeight;
        controller.height = standHeight;
        controller.center = Vector3.up * (standHeight / 2);
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;
        inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += ctx => lookInput = Vector2.zero;
        inputActions.Player.Jump.performed += _ => Jump();
        inputActions.Player.Crouch.performed += _ => ToggleCrouch();
        inputActions.Player.Sprint.performed += _ => isSprinting = true;
        inputActions.Player.Sprint.canceled += _ => isSprinting = false;
    }

    private void OnDisable() => inputActions.Player.Disable();

    private void Update()
    {
        isGrounded = controller.isGrounded;
        Move();
        Look();
        HandleCrouching();
        UpdateAnimation();
        HandleAudio(); // Единая точка управления звуком
    }

    private void LateUpdate()
    {
        if (playerCamera != null)
        {
            float targetCameraHeight = isCrouching ? crouchCameraHeight : standCameraHeight;
            float currentCamHeight = Mathf.SmoothDamp(
                playerCamera.transform.localPosition.y,
                targetCameraHeight,
                ref cameraHeightVelocity,
                crouchSmoothTime
            );
            playerCamera.transform.localPosition = new Vector3(0f, currentCamHeight, 0f);
        }
    }

    // === ЕДИНЫЙ МЕТОД ЗВУКОВ ===
    private void HandleAudio()
    {
        if (!isGrounded) return;

        bool isMoving = moveInput.magnitude > 0.1f;

        // --- ШАГИ ---
        if (isMoving)
        {
            float currentSpeed = isCrouching ? crouchSpeed : (isSprinting && currentStamina > 0 ? sprintSpeed : moveSpeed);
            float interval = footstepInterval * (moveSpeed / currentSpeed);

            if (Time.time - lastFootstepTime > interval)
            {
                if (footstepSounds.Length > 0)
                {
                    AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
                    audioSource.PlayOneShot(clip, 0.7f);
                }
                lastFootstepTime = Time.time;
            }
            wasMoving = true;
        }
        // --- ДЫХАНИЕ: ТОЛЬКО ПОСЛЕ ОСТАНОВКИ ---
        else if (wasMoving)
        {
            audioSource.PlayOneShot(breathIdle, 0.3f);
            wasMoving = false;
        }

        // --- ВЗДОХИ (редко) ---
        if (Time.time - lastSighTime > sighInterval && Random.value < 0.15f)
        {
            audioSource.PlayOneShot(sighSound, 0.5f);
            lastSighTime = Time.time;
        }
    }

    private void Move()
    {
        UpdateStamina();

        float currentSpeed = moveSpeed;
        if (isCrouching) currentSpeed = crouchSpeed;
        else if (isSprinting && currentStamina > 0) currentSpeed = sprintSpeed;

        Vector3 moveDir = transform.right * moveInput.x + transform.forward * moveInput.y;
        Vector3 movement = moveDir * currentSpeed * Time.deltaTime;

        if (isGrounded)
        {
            if (yVelocity < 0) yVelocity = -1f;
            if (inputActions.Player.Jump.IsPressed() && !isCrouching)
                yVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        else
        {
            yVelocity += gravity * Time.deltaTime;
        }

        movement.y = yVelocity * Time.deltaTime;
        controller.Move(movement);
    }

    private void UpdateStamina()
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

    private void UpdateAnimation()
    {
        bool isMoving = moveInput.magnitude > 0.1f;
        bool isSprintingNow = isSprinting && currentStamina > 0 && !isCrouching;

        animator.SetBool("IsRunning", isMoving);
        animator.SetBool("IsSprinting", isSprintingNow);
    }

    private void Jump() { }

    private void ToggleCrouch()
    {
        isCrouching = !isCrouching;
        if (animator != null) animator.SetBool("IsCrouching", isCrouching);
    }

    private void HandleCrouching()
    {
        float targetHeight = isCrouching ? crouchHeight : standHeight;
        float targetCenterY = targetHeight / 2f;

        if (isGrounded)
        {
            float currentBottom = transform.position.y - controller.center.y + controller.height / 2f;
            float newBottom = transform.position.y - targetCenterY + targetHeight / 2f;
            float heightDiff = currentBottom - newBottom;
            transform.position -= Vector3.up * heightDiff;
        }

        controller.height = targetHeight;
        controller.center = Vector3.up * targetCenterY;
    }

    private void Look()
    {
        float yRotation = lookInput.x * lookSensitivity;
        transform.Rotate(Vector3.up * yRotation);

        xRotation -= lookInput.y * lookSensitivity;
        xRotation = Mathf.Clamp(xRotation, -maxLookDown, maxLookUp);
        Camera.main.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
    }
}