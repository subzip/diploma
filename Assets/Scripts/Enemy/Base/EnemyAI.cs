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
    [SerializeField] protected float lostSightGrace = 1.5f;

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
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            lastKnownPlayerPosition = player.position;
        }
        else
        {
            player = null;
            lastKnownPlayerPosition = transform.position;
        }
    }

    protected virtual void Start()
    {
        SwitchState(EnemyState.Patrol);
    }

    protected void Update()
    {
        if (player == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

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
                else if (!vision.CanSeePlayer() && !vision.SeenRecently(lostSightGrace)) SwitchState(EnemyState.Patrol);
                break;

            case EnemyState.Attack:
                combat.Attack();
                if (!combat.IsInAttackRange() && (vision.CanSeePlayer() || vision.SeenRecently(lostSightGrace)))
                    SwitchState(EnemyState.Chase);
                else if (!vision.SeenRecently(lostSightGrace))
                    SwitchState(EnemyState.Patrol);
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
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 10f))
        {
            float targetY = hit.point.y + groundOffset;
            Vector3 targetPosition = new Vector3(transform.position.x, targetY, transform.position.z);

            transform.position = Vector3.Lerp(transform.position, targetPosition, 10f * Time.deltaTime);

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

        if (agent != null) agent.isStopped = true;
        if (agent != null) agent.enabled = false;

        animator.animator.SetBool("IsDead", true);

        Invoke(nameof(DisableCollider), 0.5f);
        //Destroy(gameObject, 3f);
    }

    protected void DisableCollider()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }
}
