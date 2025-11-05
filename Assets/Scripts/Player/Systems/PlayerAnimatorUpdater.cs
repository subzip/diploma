// PlayerAnimatorUpdater.cs
using UnityEngine;

public class PlayerAnimatorUpdater : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerCrouch crouch;

    private void Update()
    {
        if (animator == null || movement == null || crouch == null) return;

        // Получаем данные из модулей
        bool isMoving = movement.IsMoving;
        bool isSprinting = movement.IsSprinting && movement.CurrentStamina > 0;
        bool isCrouching = crouch.IsCrouching;

        // Передаём в Animator
        animator.SetBool("IsRunning", isMoving);
        animator.SetBool("IsSprinting", isSprinting);
        animator.SetBool("IsCrouching", isCrouching);
    }
}