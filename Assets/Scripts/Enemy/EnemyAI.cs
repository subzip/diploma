// EnemyAI.cs
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyPatrol patrol;
    [SerializeField] private EnemyVision vision;
    [SerializeField] private EnemyCombat combat;
    [SerializeField] private EnemyHealth health;
    [SerializeField] private EnemyAnimator animator;

    private EnemyState currentState = EnemyState.Patrol;

    public enum EnemyState
    {
        Patrol,
        Chase,
        Attack,
        Dead
    }

    private void Start()
    {
        SwitchState(EnemyState.Patrol);
    }

    private void Update()
    {
        if (health.IsDead) return;

        switch (currentState)
        {
            case EnemyState.Patrol:
                patrol.UpdatePatrol();
                if (vision.CanSeePlayer()) SwitchState(EnemyState.Chase);
                break;

            case EnemyState.Chase:
                combat.UpdateChase();
                if (combat.IsInAttackRange()) SwitchState(EnemyState.Attack);
                else if (!vision.CanSeePlayer()) SwitchState(EnemyState.Patrol);
                break;

            case EnemyState.Attack:
                combat.Attack();
                if (!combat.IsInAttackRange()) SwitchState(EnemyState.Chase);
                break;
        }
    }

    private void SwitchState(EnemyState newState)
    {
        currentState = newState;
        animator.SetState(currentState);
    }

    // Вызывается из EnemyHealth
    public void Die()
    {
        currentState = EnemyState.Dead;
        transform.position = new Vector3(transform.position.x, 0f, transform.position.z);
        animator.SetState(EnemyState.Dead);
    }
}