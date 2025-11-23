// WeaponSway.cs
using UnityEngine;

public class WeaponSway : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform playerCamera;

    [Header("Settings")]
    [SerializeField] private float maxSpeed = 8f; // Макс. скорость бега

    private PlayerMovement playerMovement; // Или любой компонент с moveInput

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (playerMovement == null) playerMovement = FindObjectOfType<PlayerMovement>();
    }

    private void LateUpdate()
    {
        if (playerMovement == null) return;

        // Получаем скорость игрока (0 = стоит, 1 = бежит)
        float moveSpeed = playerMovement.GetMoveSpeed(); // Нужно добавить метод в PlayerMovement
        float normalizedSpeed = Mathf.Clamp01(moveSpeed / maxSpeed);

        animator.SetFloat("Speed", normalizedSpeed);
    }
}