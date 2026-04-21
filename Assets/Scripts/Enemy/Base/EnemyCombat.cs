
using UnityEngine;
using UnityEngine.AI;

public class EnemyCombat : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] protected float attackRange = 3f;
    [SerializeField] protected float attackCooldown = 1.5f;
    [SerializeField] protected int attackDamage = 25;
    [SerializeField] protected LayerMask playerLayer = ~0; 
    [SerializeField] private float attackFacingSpeed = 8f;
    [SerializeField] private float attackAimDelay = 0.3f;
    [SerializeField] private bool holdPositionWhileAttacking = true;

    [Header("Chase")]
    [SerializeField] private float preferredDistance = 2.2f;
    [SerializeField] private float retreatDistance = 1.3f;
    [SerializeField] private float chaseUpdateInterval = 0.12f;

    [Header("Strafe")]
    [SerializeField] private bool enableStrafe = true;
    [SerializeField] private float strafeRadius = 1.4f;
    [SerializeField] private float strafeInterval = 1.4f;

    [Header("LOS")]
    [SerializeField] private LayerMask lineOfSightMask = ~0;

    protected Transform player;
    protected NavMeshAgent agent;
    protected float lastAttackTime;

    private float nextChaseUpdateTime;
    private float nextStrafeSwitchTime;
    private int strafeDirection = 1;
    private float nextAttackReadyTime;

    protected void Start()
    {
        ResolvePlayerRef(force: true);
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError($"{name}: NavMeshAgent missing on enemy.");
        }
    }

    public void UpdateChase()
    {
        if (DeathScreen.GlobalDeathActive) return;
        ResolvePlayerRef();
        if (player == null || agent == null || !agent.enabled) return;
        if (agent.isStopped) agent.isStopped = false;

        if (Time.time < nextChaseUpdateTime) return;
        nextChaseUpdateTime = Time.time + chaseUpdateInterval;

        Vector3 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;
        Vector3 destination = player.position;

        
        if (distance < retreatDistance && distance > 0.01f)
        {
            Vector3 retreatDir = -toPlayer.normalized;
            destination = transform.position + retreatDir * (retreatDistance - distance + 0.7f);
        }
        
        else if (enableStrafe && distance <= attackRange * 1.3f)
        {
            if (Time.time >= nextStrafeSwitchTime)
            {
                strafeDirection *= Random.value > 0.5f ? 1 : -1;
                nextStrafeSwitchTime = Time.time + strafeInterval;
            }

            Vector3 lateral = Vector3.Cross(Vector3.up, toPlayer.normalized) * strafeDirection;
            Vector3 ringPoint = player.position - toPlayer.normalized * Mathf.Max(preferredDistance, 0.8f);
            destination = ringPoint + lateral * strafeRadius;
        }

        if (!agent.pathPending)
        {
            agent.SetDestination(destination);
        }
    }

    public void MoveToPoint(Vector3 point)
    {
        if (agent == null || !agent.enabled) return;
        agent.SetDestination(point);
    }

    public bool ReachedPoint(float threshold = 1.2f)
    {
        if (agent == null || !agent.enabled) return true;
        if (agent.pathPending) return false;
        return agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, threshold);
    }

    public virtual void Attack()
    {
        if (DeathScreen.GlobalDeathActive) return;
        ResolvePlayerRef();
        if (player == null) return;

        FacePlayer();

        if (!IsInAttackRange()) return;
        if (!HasLineOfSightToPlayer()) return;
        if (Time.time < nextAttackReadyTime) return;
        if (Time.time < lastAttackTime + attackCooldown) return;

        lastAttackTime = Time.time;

        if (!player.TryGetComponent<IDamageable>(out var target))
        {
            target = player.GetComponentInParent<IDamageable>();
        }

        if (target != null)
        {
            target.TakeDamage(attackDamage, player.position);
        }
    }

    public void OnEnterAttackState()
    {
        nextAttackReadyTime = Time.time + Mathf.Max(0f, attackAimDelay);
        if (agent != null && agent.enabled && holdPositionWhileAttacking)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    public void OnExitAttackState()
    {
        if (agent != null && agent.enabled && holdPositionWhileAttacking)
        {
            agent.isStopped = false;
        }
    }

    public virtual bool IsInAttackRange()
    {
        ResolvePlayerRef();
        if (player == null) return false;
        return Vector3.Distance(transform.position, player.position) <= attackRange;
    }

    public bool HasLineOfSightToPlayer()
    {
        ResolvePlayerRef();
        if (player == null) return false;

        Vector3 eyePos = transform.position + Vector3.up * 1.3f;
        Vector3 targetPos = player.position + Vector3.up * 1.1f;
        Vector3 direction = targetPos - eyePos;
        float distance = direction.magnitude;

        if (Physics.Raycast(eyePos, direction.normalized, out RaycastHit hit, distance, lineOfSightMask, QueryTriggerInteraction.Ignore))
        {
            return hit.collider.CompareTag("Player") || hit.collider.transform.root.CompareTag("Player");
        }
        return false;
    }

    protected void ResolvePlayerRef(bool force = false)
    {
        if (!force && player != null) return;
        player = PlayerLocator.GetPlayerTransform(force);
    }

    private void FacePlayer()
    {
        if (player == null) return;
        Vector3 look = player.position - transform.position;
        look.y = 0f;
        if (look.sqrMagnitude < 0.001f) return;
        Quaternion targetRot = Quaternion.LookRotation(look.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * attackFacingSpeed);
    }
}
