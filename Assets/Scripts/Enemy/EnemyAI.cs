// EnemyAI.cs
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyPatrol patrol;
    [SerializeField] private EnemyVision vision;
    [SerializeField] private EnemyCombat combat;
    [SerializeField] private EnemyHealth health;
    [SerializeField] private EnemyAnimator animator;

    private EnemyState currentState = EnemyState.Patrol;
    private bool isDying = false;
    private float groundOffset = 0.1f;

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
        if (isDying)
        {
            MoveToGround();
        }
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

    private void MoveToGround()
    {
        // Луч вниз для определения пола
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 10f))
        {
            float targetY = hit.point.y + groundOffset;
            Vector3 targetPosition = new Vector3(transform.position.x, targetY, transform.position.z);

            // Плавно опускаем
            transform.position = Vector3.Lerp(transform.position, targetPosition, 10f * Time.deltaTime);

            // Если почти касаемся — фиксируем и останавливаем
            if (Mathf.Abs(transform.position.y - targetY) < 0.01f)
            {
                transform.position = targetPosition;
                isDying = false;
            }
        }
    }

    public void Die()
    {
        if (health.IsDead) return;
        health.SetDead(true);
        isDying = true;

        NavMeshAgent agent = GetComponent<NavMeshAgent>();

        // Останавливаем агента
        if (agent != null) agent.isStopped = true;

        // Отключаем CharacterController (если есть)
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // Включаем анимацию смерти
        animator.animator.SetBool("IsDead", true);

        // Отключаем коллайдер через пару секунд
        Invoke(nameof(DisableCollider), 0.5f);
        Destroy(gameObject, 3f);
    }

    private void DisableCollider()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }
}