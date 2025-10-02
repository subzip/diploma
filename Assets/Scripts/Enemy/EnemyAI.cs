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
    [SerializeField] private float detectionDelay = 1.5f;  // Увеличено для более стабильного преследования
    [SerializeField] private float lostTargetDelay = 8f;    // Увеличено для более длительного поиска
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
        
        // Настройка NavMeshAgent для правильного вращения
        agent.updateRotation = false; // Отключаем автоматическое вращение
        agent.updateUpAxis = false;
        
        // Автоматическое определение игрока
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
        // Инициализация позиции
        lastPosition = transform.position;
        
        // Инициализируем последнюю известную позицию как текущую позицию игрока
        if (player != null)
        {
            lastKnownPosition = player.position;
        }
    }
    
    private void Update()
    {
        if (player == null || enemy.IsDead()) return;
        
        // Проверка застревания
        CheckIfStuck();
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // Проверка видимости игрока
        bool canSeePlayer = CanSeePlayer();
        
        // Основная логика преследования
        if (distanceToPlayer <= chaseRange)
        {
            if (canSeePlayer)
            {
                // Игрок в зоне видимости - обновляем последнюю известную позицию
                lastKnownPosition = player.position;
                isChasing = true;
                lostTargetTimer = 0f;
                detectionTimer = 0f;
            }
            else if (isChasing)
            {
                // Игрок вне зоны видимости, но в пределах дистанции преследования
                detectionTimer += Time.deltaTime;
                
                if (detectionTimer >= detectionDelay)
                {
                    lostTargetTimer += Time.deltaTime;
                    
                    // Продолжаем преследование до истечения lostTargetDelay
                    if (lostTargetTimer < lostTargetDelay)
                    {
                        // Продолжаем двигаться к последней известной позиции
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
            // Игрок вне зоны преследования
            isChasing = false;
            lostTargetTimer = 0f;
        }
        
        // Обновление пути
        if (isChasing)
        {
            // Двигаемся к последней известной позиции
            MoveToPosition(lastKnownPosition);
            
            // Проверка возможности атаки
            if (distanceToPlayer <= attackRange && canSeePlayer)
            {
                AttackPlayer();
            }
        }
        else
        {
            // Патрулирование на месте
            Patrol();
        }
        
        // Вращение врага в сторону движения
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
                
                // Сохраняем состояние преследования
                wasChasingBeforeStuck = isChasing;
                
                // Попытка решить проблему застревания
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
        // Попытка 1: Найти новую точку в том же направлении
        Vector3 directionToTarget = (lastKnownPosition - transform.position).normalized;
        Vector3 newDestination = transform.position + directionToTarget * 3f;
        
        NavMeshHit hit;
        if (NavMesh.SamplePosition(newDestination, out hit, 5f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
            return;
        }
        
        // Попытка 2: Найти точку патрулирования
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
        
        // Проверяем угол обзора
        if (angle > 60f) return false;  // Увеличен угол обзора для более стабильного обнаружения
        
        // Проверяем наличие препятствий между врагом и игроком
        // Увеличена высота начала Raycast для лучшей видимости
        if (Physics.Raycast(transform.position + Vector3.up * 1.2f, directionToPlayer.normalized, out RaycastHit hit, chaseRange))
        {
            // Проверяем, что это действительно игрок
            return hit.collider.CompareTag("Player");
        }
        
        return false;
    }
    
    private void MoveToPosition(Vector3 position)
    {
        // Проверяем, можем ли мы достичь цели
        NavMeshPath path = new NavMeshPath();
        agent.CalculatePath(position, path);
        
        if (path.status == NavMeshPathStatus.PathComplete || path.status == NavMeshPathStatus.PathPartial)
        {
            agent.SetDestination(position);
        }
        else
        {
            // Если путь недоступен, ищем альтернативную точку
            Vector3 adjustedPosition = position;
            NavMeshHit hit;
            
            if (NavMesh.SamplePosition(position, out hit, 2f, NavMesh.AllAreas))
            {
                adjustedPosition = hit.position;
                agent.SetDestination(adjustedPosition);
            }
            else
            {
                // Если не можем найти точку, возвращаемся к патрулированию
                isChasing = false;
            }
        }
    }
    
    private void RotateTowardsMovementDirection()
    {
        if (agent.velocity.magnitude > 0.1f)
        {
            // Получаем направление движения
            Vector3 direction = agent.velocity.normalized;
            direction.y = 0; // Игнорируем вертикальное движение
            
            // Плавно поворачиваемся в сторону движения
            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, agent.angularSpeed * Time.deltaTime * 0.01f);
            }
        }
    }
    
    private void AttackPlayer()
    {
        // Останавливаем движение для атаки
        agent.isStopped = true;
        
        // Выводим информацию о атаке в консоль
        Debug.Log($"⚔️ Враг [{gameObject.name}] атакует игрока!");
    }
    
    private void Patrol()
    {
        agent.isStopped = false;
        
        // Проверяем, что путь завершен или отсутствует
        bool shouldFindNewPoint = true;
        
        // Безопасная проверка
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
    
    // Для отладки: отображение области видимости
    private void OnDrawGizmosSelected()
    {
        if (transform == null) return;
        
        // Область преследования
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        
        // Область атаки
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Линия обзора
        Gizmos.color = Color.green;
        Vector3 viewAngle01 = Quaternion.AngleAxis(60, transform.up) * transform.forward * chaseRange;
        Vector3 viewAngle02 = Quaternion.AngleAxis(-60, transform.up) * transform.forward * chaseRange;
        
        Gizmos.DrawLine(transform.position, transform.position + viewAngle01);
        Gizmos.DrawLine(transform.position, transform.position + viewAngle02);
        
        // Последняя известная позиция
        if (isChasing)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(lastKnownPosition, 0.3f);
        }
        
        // Вектор движения
        if (agent != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + agent.velocity.normalized * 2f);
        }
    }
}