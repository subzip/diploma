using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -9.81f;

    [SerializeField] private float crouchSpeed = 2f;
    [SerializeField] private float standHeight = 2f;      // Высота в обычном состоянии
    [SerializeField] private float crouchHeight = 1f;     // Высота в приседе
    [SerializeField] private float crouchSmoothTime = 0.22f; // Плавность изменения высоты

    private bool isCrouching = false;
    private float currentHeight;
    private float heightVelocity = 0f;


    [SerializeField] private AudioClip[] footstepSounds;     // Массив шагов
    [SerializeField] private AudioClip breathIdle;           // Дыхание в покое
    [SerializeField] private AudioClip breathRun;            // Дыхание при беге
    [SerializeField] private AudioClip sighSound;            // Вздох

    private AudioSource audioSource;
    private float lastFootstepTime = 0f;
    private float footstepInterval = 0.5f; // Интервал между шагами при беге
    private float lastSighTime = 0f;
    private float sighInterval = 20f;

    private bool isBreathing = false;
    private float lastBreathTime = 0f;
    private float breathIntervalMin = 2f;   // Минимальный интервал между вдохами
    private float breathIntervalMax = 4f;  

    
    public Animator animator;

    [SerializeField] private Camera playerCamera; 
    [SerializeField] private float standCameraHeight = 1.65f;   // Высота камеры в стойке
    [SerializeField] private float crouchCameraHeight = 1.0f;    // Высота камеры в приседе
    private float cameraHeightVelocity = 0f; // Для плавности камеры

    [Header("Look")]
    [SerializeField] private float lookSensitivity = 2f;

    [SerializeField] private float maxLookUp = 80f;    // насколько можно смотреть ВВЕРХ
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

        currentHeight = standHeight;
        controller.height = standHeight;
        controller.center = Vector3.up * (standHeight / 2);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.maxDistance = 15f;
        }
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();

        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += ctx => lookInput = Vector2.zero;

        inputActions.Player.Jump.performed += ctx => Jump();
        inputActions.Player.Crouch.performed += ctx => ToggleCrouch();
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

    private void Update()
    {
        isGrounded = controller.isGrounded;
        
        Move();
        Look();
        HandleCrouching();
        UpdateAnimation();
        
        // --- УПРАВЛЕНИЕ ЗВУКАМИ ---
        HandleFootsteps();
        HandleBreathing();
        HandleSigh();
    }

    private void LateUpdate()
    {
        // Только позиционирование КАМЕРЫ
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

    private void HandleFootsteps()
    {
        if (isGrounded && moveInput.magnitude > 0.1f)
        {
            float currentSpeed = isCrouching ? crouchSpeed : moveSpeed;
            float currentInterval = footstepInterval * (moveSpeed / currentSpeed);

            if (Time.time - lastFootstepTime > currentInterval)
            {
                if (footstepSounds.Length > 0)
                {
                    AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
                    audioSource.PlayOneShot(clip, 0.7f); // немного тише
                }
                lastFootstepTime = Time.time;
                
                // После шага — НЕЛЬЗЯ сразу дышать (пауза 0.3 сек)
                isBreathing = false;
                lastBreathTime = Time.time - breathIntervalMin + 0.3f;
            }
        }
    }

// --- НОВЫЙ МЕТОД: ДЫХАНИЕ ---
    private void HandleBreathing()
    {
        if (!isGrounded) return;

        // Если игрок НЕ движется — тихое дыхание
        if (moveInput.magnitude <= 0.1f)
        {
            if (!isBreathing && Time.time - lastBreathTime > Random.Range(3f, 6f))
            {
                audioSource.PlayOneShot(breathIdle, 0.3f); // очень тихо
                isBreathing = true;
                lastBreathTime = Time.time;
                Invoke("ResetBreathingFlag", 1.5f); // через 1.5 сек можно снова дышать
            }
        }
        // Если игрок ДВИЖЕТСЯ — дыхание почти не слышно и редкое
        else
        {
            if (!isBreathing && Time.time - lastBreathTime > Random.Range(breathIntervalMin, breathIntervalMax))
            {
                // Дыхание при беге — очень тихое и короткое
                audioSource.PlayOneShot(breathRun, 0.2f); // громкость 0.2 вместо 0.5!
                isBreathing = true;
                lastBreathTime = Time.time;
                Invoke("ResetBreathingFlag", 1.0f);
            }
        }
    }

// --- НОВЫЙ МЕТОД: ВЗДОХ ---
    private void HandleSigh()
    {
        if (isGrounded && Time.time - lastSighTime > sighInterval)
        {
            if (Random.value < 0.2f) // реже — 20% шанс
            {
                audioSource.PlayOneShot(sighSound, 0.5f);
                lastSighTime = Time.time;
            }
        }
    }

    // --- ВСПОМОГАТЕЛЬНЫЙ МЕТОД ---
    private void ResetBreathingFlag()
    {
        isBreathing = false;
    }

    private void Move()
    {
            /// Горизонтальное движение
        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
        float currentSpeed = isCrouching ? crouchSpeed : moveSpeed;
        Vector3 movement = moveDirection * currentSpeed * Time.deltaTime;

        // Прыжок и гравитация
        if (isGrounded)
        {
            if (yVelocity < 0) yVelocity = -1f;

            if (inputActions.Player.Jump.IsPressed() && !isCrouching) // Прыгать можно только стоя
            {
                yVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }
        else
        {
            yVelocity += gravity * Time.deltaTime;
        }

        // Вертикальное движение от гравитации
        movement.y = yVelocity * Time.deltaTime;

        // Применяем движение
        controller.Move(movement);

        if (isGrounded && moveInput.magnitude > 0.1f)
        {
            currentSpeed = isCrouching ? crouchSpeed : moveSpeed;
            float currentInterval = footstepInterval * (moveSpeed / currentSpeed); // Быстрее шаги при беге

            if (Time.time - lastFootstepTime > currentInterval)
            {
                if (footstepSounds.Length > 0)
                {
                    AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
                    audioSource.PlayOneShot(clip, 0.8f);
                }
                lastFootstepTime = Time.time;
            }
        }
    }

    private void UpdateAnimation()
    {
        // Проверяем, есть ли фактическое движение
        bool isMoving = moveInput.magnitude > 0.1f;
        animator.SetBool("IsRunning", isMoving);
    }

    private void Jump()
    {
        // Можно оставить пустым
    }

   private void ToggleCrouch()
    {
        isCrouching = !isCrouching;
        
        // Обновляем параметр аниматора (если используете)
        if (animator != null)
            animator.SetBool("IsCrouching", isCrouching);
    }

    private void HandleCrouching()
        {
            float targetHeight = isCrouching ? crouchHeight : standHeight;
            float targetCenterY = targetHeight / 2f;

            // 🔥 КЛЮЧЕВОЙ МОМЕНТ: сначала сдвинуть ПОЗИЦИЮ, потом менять height/center
            if (isGrounded)
            {
                float currentBottom = transform.position.y - controller.center.y + controller.height / 2f;
                float newBottom = transform.position.y - targetCenterY + targetHeight / 2f;
                float heightDiff = currentBottom - newBottom; // на сколько "поднялись ноги"

                // Опускаем персонажа ВНИЗ, чтобы ноги остались на земле
                transform.position -= Vector3.up * heightDiff;
            }

            // Теперь безопасно меняем параметры контроллера
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
        xRotation = Mathf.Clamp(xRotation, -maxLookDown, maxLookUp); // min → max
        Camera.main.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
    }
}