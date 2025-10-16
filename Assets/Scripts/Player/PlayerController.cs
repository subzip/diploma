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

    
    public Animator animator;

    [Header("Look")]
    [SerializeField] private float lookSensitivity = 2f;

    [SerializeField] private float maxLookUp = 80f;    // насколько можно смотреть ВВЕРХ
    [SerializeField] private float maxLookDown = 45f; 

    private PlayerInputActions inputActions;
    private CharacterController controller;

    private Vector2 moveInput;
    private Vector2 lookInput;

    private float yVelocity = 0f;
    private bool isCrouching = false;
    private bool isGrounded;

    private float xRotation = 0f;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
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

        // Обновляем параметр аниматора на основе фактического движения
        UpdateAnimation();
    }

    private void Move()
    {
        // 1. Горизонтальное движение (по земле)
        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
        float currentSpeed = isCrouching ? crouchSpeed : moveSpeed;
        Vector3 movement = moveDirection * currentSpeed * Time.deltaTime;

        // 2. Прыжок и гравитация
        if (isGrounded)
        {
            if (yVelocity < 0)
            {
                yVelocity = -1f;
            }

            if (inputActions.Player.Jump.IsPressed())
            {
                yVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }
        else
        {
            yVelocity += gravity * Time.deltaTime;
        }

        // 3. Добавляем вертикальное движение к общему перемещению
        movement.y = yVelocity * Time.deltaTime;

        // 4. Единый вызов Move() — всё движение за один кадр
        controller.Move(movement);
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
        // Дополнительно: можно добавить анимацию приседания
        // animator.SetBool("IsCrouching", isCrouching);
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