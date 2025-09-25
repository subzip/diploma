using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpHeight = 2f; // Высота прыжка
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float crouchSpeed = 2f;

    public Animator animator;

    [Header("Look")]
    [SerializeField] private float lookSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 80f;

    private PlayerInputActions inputActions;
    private CharacterController controller;

    private Vector2 moveInput;
    private Vector2 lookInput;

    private float yVelocity = 0f; // Вертикальная скорость (для прыжка и гравитации)
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
    }

    private void Move()
    {

        animator.SetBool("IsRunning", true);
        // 1. Горизонтальное движение (по земле)
        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
        float currentSpeed = isCrouching ? crouchSpeed : moveSpeed;
        Vector3 movement = moveDirection * currentSpeed * Time.deltaTime;

        // 2. Прыжок и гравитация
        if (isGrounded)
        {
            // Если на земле и прыгаем — задаём начальную вертикальную скорость
            if (yVelocity < 0)
            {
                yVelocity = -1f; // Малое значение, чтобы не "проваливался"
            }

            if (inputActions.Player.Jump.IsPressed()) // Прыжок только при нажатии
            {
                yVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity); // v = √(2 * h * |g|)
            }
        }
        else
        {
            // Применяем гравитацию в воздухе
            yVelocity += gravity * Time.deltaTime;
        }

        // 3. Добавляем вертикальное движение к общему перемещению
        movement.y = yVelocity * Time.deltaTime;

        // 4. Единый вызов Move() — всё движение за один кадр
        controller.Move(movement);
    }

    private void Jump()
    {
        // Этот метод вызывается при нажатии, но сам прыжок обрабатывается в Move()
        // Можно оставить пустым или использовать как триггер
    }

    private void ToggleCrouch()
    {
        isCrouching = !isCrouching;
        // Можно добавить изменение высоты: controller.height = isCrouching ? 1f : 2f;
    }

    private void Look()
    {
        // Поворот тела
        float yRotation = lookInput.x * lookSensitivity;
        transform.Rotate(Vector3.up * yRotation);

        // Поворот камеры вверх-вниз
        xRotation -= lookInput.y * lookSensitivity;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);
        Camera.main.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
    }
}