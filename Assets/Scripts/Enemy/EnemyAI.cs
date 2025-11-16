using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Enemy))]
public class EnemyAI : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform player;
    [SerializeField] private float chaseRange = 10f;
    [SerializeField] private float attackRange = 2f;
    
    [Header("Behavior Settings")]
    [SerializeField] private float detectionDelay = 1.5f;
    [SerializeField] private float lostTargetDelay = 8f;
    [SerializeField] private float patrolRadius = 5f;
    [SerializeField] private float stuckCheckInterval = 1f;
    [SerializeField] private float stuckDistanceThreshold = 0.1f;
    
    private NavMeshAgent agent;
    private Enemy enemy;
    private bool isChasing;
    private Vector3 lastKnownPosition;
    private float detectionTimer;
    private float lostTargetTimer;
    private float stuckCheckTimer;
    private Vector3 lastPosition;
    private bool isStuck;
    private bool wasChasingBeforeStuck;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        enemy = GetComponent<Enemy>();
        
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
            else
            {
                Debug.LogError("Игрок не найден! Пометьте игрока тегом 'Player'");
            }
        }
    }
    
    private void Start()
    {
        lastPosition = transform.position;
        
        if (player != null)
        {
            lastKnownPosition = player.position;
        }
    }
    
    private void Update()
    {
        if (player == null || enemy.IsDead()) return;
        
        CheckIfStuck();
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        bool canSeePlayer = CanSeePlayer();
        
        if (distanceToPlayer <= chaseRange)
        {
            if (canSeePlayer)
            {
                lastKnownPosition = player.position;
                isChasing = true;
                lostTargetTimer = 0f;
                detectionTimer = 0f;
            }
            else if (isChasing)
            {
                detectionTimer += Time.deltaTime;
                
                if (detectionTimer >= detectionDelay)
                {
                    lostTargetTimer += Time.deltaTime;
                    
                    if (lostTargetTimer < lostTargetDelay)
                    {
                        MoveToPosition(lastKnownPosition);
                    }
                    else
                    {
                        isChasing = false;
                    }
                }
            }
        }
        else
        {
            isChasing = false;
            lostTargetTimer = 0f;
        }
        
        if (isChasing)
        {
            MoveToPosition(lastKnownPosition);
            
            if (distanceToPlayer <= attackRange && canSeePlayer)
            {
                AttackPlayer();
            }
        }
        else
        {
            Patrol();
        }
        
        RotateTowardsMovementDirection();
    }
    
    private void CheckIfStuck()
    {
        stuckCheckTimer += Time.deltaTime;
        
        if (stuckCheckTimer >= stuckCheckInterval)
        {
            float distanceMoved = Vector3.Distance(transform.position, lastPosition);
            
            if (distanceMoved < stuckDistanceThreshold && 
                agent.pathPending == false && 
                agent.hasPath && 
                agent.remainingDistance > agent.stoppingDistance)
            {
                isStuck = true;
                Debug.Log($"[EnemyAI] Враг [{gameObject.name}] застрял! Попытка решения...");
                
                wasChasingBeforeStuck = isChasing;
                
                ResolveStuck();
            }
            else
            {
                isStuck = false;
            }
            
            lastPosition = transform.position;
            stuckCheckTimer = 0f;
        }
    }
    
    private void ResolveStuck()
    {
        Vector3 directionToTarget = (lastKnownPosition - transform.position).normalized;
        Vector3 newDestination = transform.position + directionToTarget * 3f;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(newDestination, out hit, 5f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            return;
        }
        
        Vector3 randomPoint = transform.position + Random.insideUnitSphere * patrolRadius;
        randomPoint.y = transform.position.y;
        
        if (NavMesh.SamplePosition(randomPoint, out hit, patrolRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }
    
    private bool CanSeePlayer()
    {
        if (player == null) return false;
        
        Vector3 directionToPlayer = player.position - transform.position;
        float angle = Vector3.Angle(directionToPlayer, transform.forward);
        
        if (angle > 60f) return false; 
        
        if (Physics.Raycast(transform.position + Vector3.up * 1.2f, directionToPlayer.normalized, out RaycastHit hit, chaseRange))
        {
            return hit.collider.CompareTag("Player");
        }
        
        return false;
    }
    
    private void MoveToPosition(Vector3 position)
    {
        NavMeshPath path = new NavMeshPath();
        agent.CalculatePath(position, path);
        
        if (path.status == NavMeshPathStatus.PathComplete || path.status == NavMeshPathStatus.PathPartial)
        {
            agent.SetDestination(position);
        }
        else
        {
            Vector3 adjustedPosition = position;
            NavMeshHit hit;
            
            if (NavMesh.SamplePosition(position, out hit, 2f, NavMesh.AllAreas))
            {
                adjustedPosition = hit.position;
                agent.SetDestination(adjustedPosition);
            }
            else
            {
                isChasing = false;
            }
        }
    }
    
    private void RotateTowardsMovementDirection()
    {
        if (agent.velocity.magnitude > 0.1f)
        {
            Vector3 direction = agent.velocity.normalized;
            direction.y = 0; 
            
            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, agent.angularSpeed * Time.deltaTime * 0.01f);
            }
        }
    }
    
    private void AttackPlayer()
    {
        agent.isStopped = true;
        
        Debug.Log($"⚔️ Враг [{gameObject.name}] атакует игрока!");
    }
    
    private void Patrol()
    {
        agent.isStopped = false;
        
        bool shouldFindNewPoint = true;
        
        if (agent.hasPath && !agent.pathPending)
        {
            shouldFindNewPoint = agent.remainingDistance <= agent.stoppingDistance;
        }
        
        if (shouldFindNewPoint || isStuck)
        {
            Vector3 randomPoint = transform.position + Random.insideUnitSphere * patrolRadius;
            randomPoint.y = transform.position.y;
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, patrolRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (transform == null) return;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        Gizmos.color = Color.green;
        Vector3 viewAngle01 = Quaternion.AngleAxis(60, transform.up) * transform.forward * chaseRange;
        Vector3 viewAngle02 = Quaternion.AngleAxis(-60, transform.up) * transform.forward * chaseRange;
        
        Gizmos.DrawLine(transform.position, transform.position + viewAngle01);
        Gizmos.DrawLine(transform.position, transform.position + viewAngle02);
        
        if (isChasing)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(lastKnownPosition, 0.3f);
        }
        
        if (agent != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + agent.velocity.normalized * 2f);
        }
    }
}