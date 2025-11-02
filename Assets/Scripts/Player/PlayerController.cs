using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging;
using UnityEngine.UI;

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

    [Header("IK & Weapon")]
    [SerializeField] private TwoBoneIKConstraint leftHandIK;
    [SerializeField] private Transform rightHandGripPoint;

    [Header("Audio")]
    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] private AudioClip breathIdle;
    [SerializeField] private AudioClip sighSound;

    [Header("Camera")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private float standCameraHeight = 1.65f;
    [SerializeField] private float crouchCameraHeight = 1.0f;
    [SerializeField] private float cameraSmoothTime = 0.2f;

    [Header("Look")]
    [SerializeField] private float lookSensitivity = 2f;
    [SerializeField] private float maxLookUp = 80f;
    [SerializeField] private float maxLookDown = 45f;

    [SerializeField] private RigBuilder rigBuilder;

    [SerializeField] private Slider staminaSlider; // Ссылка на UI-слайдер

    private bool isCrouching = false;
    private float currentStamina = 0f;
    private bool isSprinting = false;
    private bool canRegenStamina = false;
    private float lastSprintTime = 0f;

    private AudioSource audioSource;
    private float lastFootstepTime = 0f;
    private float footstepInterval = 0.5f;
    private float lastSighTime = 0f;
    private float sighInterval = 20f;
    private bool wasMoving = false;

    public Animator animator;
    private PlayerInputActions inputActions;
    private CharacterController controller;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float yVelocity = 0f;
    private bool isGrounded;
    private float xRotation = 0f;
    private float cameraHeightVelocity = 0f;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();

        // Настройка AudioSource
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.maxDistance = 15f;

        // Инициализация высоты
        controller.height = standHeight;
        controller.center = Vector3.up * (standHeight / 2f);
    }

    private void Start()
    {
        // Другие инициализации...
        if (rigBuilder != null)
            rigBuilder.enabled = true;
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
        inputActions.Player.PickUp.performed += _ => TryPickUpWeapon();
    }

    private void OnDisable() => inputActions.Player.Disable();

    private void Update()
    {
        isGrounded = controller.isGrounded;
        Move();
        Look();
        HandleCrouching();
        UpdateAnimation();
        HandleAudio();
    }

    private void LateUpdate()
    {
        // Плавное изменение высоты камеры
        float targetHeight = isCrouching ? crouchCameraHeight : standCameraHeight;
        float currentHeight = Mathf.SmoothDamp(
            cameraPivot.localPosition.y,
            targetHeight,
            ref cameraHeightVelocity,
            cameraSmoothTime
        );
        cameraPivot.localPosition = new Vector3(0f, currentHeight, cameraPivot.localPosition.z);
    }

    private void HandleAudio()
    {
        if (!isGrounded) return;

        bool isMoving = moveInput.magnitude > 0.1f;

        // Шаги
        if (isMoving)
        {
            float speed = isCrouching ? crouchSpeed : (isSprinting && currentStamina > 0 ? sprintSpeed : moveSpeed);
            float interval = footstepInterval * (moveSpeed / speed);

            if (Time.time - lastFootstepTime > interval && footstepSounds.Length > 0)
            {
                AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
                audioSource.PlayOneShot(clip, 0.7f);
                lastFootstepTime = Time.time;
            }
            wasMoving = true;
        }
        // Дыхание после остановки
        else if (wasMoving)
        {
            audioSource.PlayOneShot(breathIdle, 0.3f);
            wasMoving = false;
        }

        // Вздохи
        if (Time.time - lastSighTime > sighInterval && Random.value < 0.15f)
        {
            audioSource.PlayOneShot(sighSound, 0.5f);
            lastSighTime = Time.time;
        }
    }

    private void Move()
    {
        UpdateStamina();

        float speed = moveSpeed;
        if (isCrouching) speed = crouchSpeed;
        else if (isSprinting && currentStamina > 0) speed = sprintSpeed;

        Vector3 direction = transform.right * moveInput.x + transform.forward * moveInput.y;
        Vector3 movement = direction * speed * Time.deltaTime;

        // Гравитация и прыжок
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

        // ➕ ОБНОВЛЕНИЕ СЛАЙДЕРА
        if (staminaSlider != null)
        {
            staminaSlider.value = currentStamina / staminaMax; // Нормализуем от 0 до 1
        }
    }

    private void UpdateAnimation()
    {
        bool isMoving = moveInput.magnitude > 0.1f;
        bool isSprintingNow = isSprinting && currentStamina > 0 && !isCrouching;

        animator.SetBool("IsRunning", isMoving);
        animator.SetBool("IsSprinting", isSprintingNow);
        animator.SetBool("IsCrouching", isCrouching);
    }

    private void Jump() { }

    private void ToggleCrouch()
    {
        isCrouching = !isCrouching;
    }

    private void HandleCrouching()
    {
        float targetHeight = isCrouching ? crouchHeight : standHeight;
        float targetCenterY = targetHeight / 2f;

        if (isGrounded)
        {
            // Коррекция позиции, чтобы ноги не отрывались от земли
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
        // Поворот тела
        float yRotation = lookInput.x * lookSensitivity;
        transform.Rotate(Vector3.up * yRotation);

        // Поворот камеры вверх-вниз
        xRotation -= lookInput.y * lookSensitivity;
        xRotation = Mathf.Clamp(xRotation, -maxLookDown, maxLookUp);
        cameraPivot.localRotation = Quaternion.Euler(xRotation, 0, 0);
    }

    // === ОРУЖИЕ ===
    private void TryPickUpWeapon()
    {
        if (Physics.Raycast(cameraPivot.position, cameraPivot.forward, out RaycastHit hit, 3f))
        {
            if (hit.collider.TryGetComponent<WeaponPickup>(out var pickup))
            {
                pickup.gameObject.SetActive(false);
                PickUpWeapon(pickup.weaponPrefab);
                Destroy(pickup.gameObject);
            }
        }
    }

    public void PickUpWeapon(GameObject weaponPrefab)
    {
        // Удаление старого оружия
        if (transform.childCount > 0)
        {
            foreach (Transform child in transform)
            {
                if (child.CompareTag("Weapon")) Destroy(child.gameObject);
            }
        }

        // Создание нового оружия
        GameObject weapon = Instantiate(weaponPrefab, rightHandGripPoint.position, rightHandGripPoint.rotation);
        weapon.transform.SetParent(transform);
        weapon.tag = "Weapon";

        // Привязка IK
        if (leftHandIK != null)
        {
            Transform leftGrip = weapon.transform.Find("LeftHandP");
            if (leftGrip != null) leftHandIK.data.target = leftGrip;
        }
    }
}