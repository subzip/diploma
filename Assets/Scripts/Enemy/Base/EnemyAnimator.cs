// EnemyAnimator.cs
using UnityEngine;

public class EnemyAnimator : MonoBehaviour
{
    public Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator не найден на враге!");
        }
    }

    public void SetState(EnemyAI.EnemyState state)
    {
        if (animator == null) return;

        animator.SetBool("IsRunning", state == EnemyAI.EnemyState.Patrol || state == EnemyAI.EnemyState.Chase);
        animator.SetBool("IsAttacking", state == EnemyAI.EnemyState.Attack);
        animator.SetBool("IsDead", state == EnemyAI.EnemyState.Dead);
    }
}