// EnemyAI.cs
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] protected EnemyPatrol patrol;
    [SerializeField] protected EnemyVision vision;
    [SerializeField] protected EnemyCombat combat;
    [SerializeField] protected EnemyHealth health;
    [SerializeField] protected EnemyAnimator animator;

    protected EnemyState currentState = EnemyState.Patrol;
    protected bool isDying = false;
    protected float groundOffset = 0.1f;
    protected Transform player;
    protected Vector3 lastKnownPlayerPosition;


    public enum EnemyState
    {
        Patrol,
        Chase,
        Attack,
        Dead
    }

    protected void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        lastKnownPlayerPosition = player.position;
    }

    protected virtual void Start()
    {
        SwitchState(EnemyState.Patrol);
    }

    protected void Update()
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

    public virtual void SwitchState(EnemyState newState)
    {
        currentState = newState;
        animator.SetState(currentState);
    }

    protected void MoveToGround()
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
        if (!health.IsDead) return;
        health.SetDead(true);
        isDying = true;

        NavMeshAgent agent = GetComponent<NavMeshAgent>();

        Debug.Log("Dead");

        // Останавливаем агента
        if (agent != null) agent.isStopped = true;

        // Включаем анимацию смерти
        animator.animator.SetBool("IsDead", true);

        // Отключаем коллайдер через пару секунд
        Invoke(nameof(DisableCollider), 0.5f);
        //Destroy(gameObject, 3f);
    }

    protected void DisableCollider()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }
}