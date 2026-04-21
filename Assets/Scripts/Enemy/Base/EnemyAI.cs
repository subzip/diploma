
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

    [Header("Behavior")]
    [SerializeField] protected float reactionDelay = 0.15f;
    [SerializeField] protected float lostSightGrace = 1.8f;
    [SerializeField] protected float investigateDuration = 3.5f;
    [SerializeField] protected float investigateReachDistance = 1.4f;
    [SerializeField] protected float investigatePointRadius = 3.2f;
    [SerializeField] protected int investigatePointsMax = 3;

    protected EnemyState currentState = EnemyState.Patrol;
    protected bool isDying;
    protected float groundOffset = 0.1f;
    protected Transform player;
    protected Vector3 lastKnownPlayerPosition;

    private float canChaseAfterTime;
    private float investigateEndTime;
    private Vector3 currentInvestigatePoint;
    private int investigatePointsVisited;

    public enum EnemyState
    {
        Patrol,
        Chase,
        Attack,
        Investigate,
        Dead
    }

    protected void Awake()
    {
        player = PlayerLocator.GetPlayerTransform(forceRefresh: true);
        lastKnownPlayerPosition = player != null ? player.position : transform.position;
    }

    protected virtual void Start()
    {
        if (patrol == null) patrol = GetComponent<EnemyPatrol>();
        if (vision == null) vision = GetComponent<EnemyVision>();
        if (combat == null) combat = GetComponent<EnemyCombat>();
        if (health == null) health = GetComponent<EnemyHealth>();
        if (animator == null) animator = GetComponent<EnemyAnimator>();

        currentState = EnemyState.Patrol;
        animator?.SetState(currentState);
    }

    protected void Update()
    {
        if (player == null)
        {
            player = PlayerLocator.GetPlayerTransform(forceRefresh: true);
        }

        if (isDying) MoveToGround();
        if (health != null && health.IsDead) return;
        if (DeathScreen.GlobalDeathActive) return;

        bool canSee = vision != null && vision.CanSeePlayer();
        bool canSense = vision != null && vision.CanSensePlayer(canSee);

        if (canSee || canSense)
        {
            if (vision != null) lastKnownPlayerPosition = vision.GetLastKnownPlayerPosition();
            if (canChaseAfterTime <= 0f) canChaseAfterTime = Time.time + reactionDelay;
        }
        else
        {
            canChaseAfterTime = 0f;
        }

        switch (currentState)
        {
            case EnemyState.Patrol:
                patrol?.UpdatePatrol();
                if ((canSee || canSense) && Time.time >= canChaseAfterTime)
                {
                    SwitchState(EnemyState.Chase);
                }
                break;

            case EnemyState.Chase:
                combat?.UpdateChase();
                if (!canSee && vision != null && !vision.SeenRecently(lostSightGrace))
                {
                    EnterInvestigateState();
                    break;
                }

                if (combat != null && combat.IsInAttackRange() && combat.HasLineOfSightToPlayer())
                {
                    SwitchState(EnemyState.Attack);
                }
                break;

            case EnemyState.Attack:
                combat?.Attack();

                bool canKeepPressure = canSee || (vision != null && vision.SeenRecently(lostSightGrace));
                if (!canKeepPressure)
                {
                    EnterInvestigateState();
                    break;
                }

                if (combat != null && (!combat.IsInAttackRange() || !combat.HasLineOfSightToPlayer()))
                {
                    SwitchState(EnemyState.Chase);
                }
                break;

            case EnemyState.Investigate:
                if (canSee || canSense)
                {
                    SwitchState(EnemyState.Chase);
                    break;
                }

                combat?.MoveToPoint(currentInvestigatePoint);
                bool reached = combat == null || combat.ReachedPoint(investigateReachDistance);
                if (reached)
                {
                    investigatePointsVisited++;
                    currentInvestigatePoint = PickInvestigatePoint();
                }

                bool searchExhausted = investigatePointsVisited >= Mathf.Max(1, investigatePointsMax);
                if (Time.time >= investigateEndTime && (searchExhausted || reached))
                {
                    SwitchState(EnemyState.Patrol);
                }
                break;
        }
    }

    public virtual void SwitchState(EnemyState newState)
    {
        if (currentState == newState) return;

        if (currentState == EnemyState.Attack)
            combat?.OnExitAttackState();

        currentState = newState;

        if (currentState == EnemyState.Attack)
            combat?.OnEnterAttackState();

        animator?.SetState(currentState);
    }

    private void EnterInvestigateState()
    {
        if (vision != null) lastKnownPlayerPosition = vision.GetLastKnownPlayerPosition();
        investigateEndTime = Time.time + investigateDuration;
        investigatePointsVisited = 0;
        currentInvestigatePoint = PickInvestigatePoint();
        SwitchState(EnemyState.Investigate);
    }

    private Vector3 PickInvestigatePoint()
    {
        Vector3 fallback = lastKnownPlayerPosition;
        float radius = Mathf.Max(0.5f, investigatePointRadius);

        for (int i = 0; i < 6; i++)
        {
            Vector2 circle = Random.insideUnitCircle * radius;
            Vector3 candidate = lastKnownPlayerPosition + new Vector3(circle.x, 0f, circle.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit navHit, radius, NavMesh.AllAreas))
            {
                return navHit.position;
            }
        }

        return fallback;
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
        if (health == null || !health.IsDead) return;
        health.SetDead(true);
        isDying = true;
        currentState = EnemyState.Dead;

        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        if (animator != null && animator.animator != null)
        {
            animator.animator.SetBool("IsDead", true);
        }

        Invoke(nameof(DisableCollider), 0.5f);
    }

    protected void DisableCollider()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }
}
