using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FlyingDrone : MonoBehaviour, IDamageable
{
    [Header("Patrol Settings")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolSpeed = 3f;
    [SerializeField] private float waitTime = 1.5f;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 20f;
    [SerializeField] private float fieldOfView = 90f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Combat")]
    [SerializeField] private float attackRange = 15f;
    [SerializeField] private float fireRate = 1f;
    [SerializeField] private int damage = 10;
    [SerializeField] private ParticleSystem laserVFX;

    [Header("Search")]
    [SerializeField] private float searchTime = 1.5f; // Время поиска после потери игрока

    [Header("Health")]
    [SerializeField] private int maxHealth = 50;

    private int currentHealth;
    private int currentPatrolIndex = 0;
    private float lastAttackTime;
    private float arriveTime;
    private Vector3 currentTarget;
    private Vector3 lastKnownPlayerPosition; // ← НОВОЕ: последняя позиция игрока
    private Transform player;
    private bool isAttacking = false;
    private bool isSearching = false;        // ← НОВОЕ: в поиске
    private float searchStartTime;           // ← НОВОЕ: время начала поиска

    private void Awake()
    {
        currentHealth = maxHealth;
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (patrolPoints.Length > 0)
            currentTarget = patrolPoints[0].position;
    }

    private void Update()
    {
        if (player == null) return;

        bool canSeePlayer = CanSeePlayer();

        if (canSeePlayer)
        {
            // Видим игрока — атакуем
            lastKnownPlayerPosition = player.position;
            currentTarget = player.position;
            isAttacking = true;
            isSearching = false;
        }
        else if (isAttacking)
        {
            // Потеряли из виду — начинаем поиск
            isAttacking = false;
            isSearching = true;
            searchStartTime = Time.time;
            currentTarget = lastKnownPlayerPosition;
        }
        else if (isSearching)
        {
            // Если поисковое время вышло — возвращаемся к патрулированию
            if (Time.time - searchStartTime > searchTime)
            {
                isSearching = false;
                currentTarget = GetNextPatrolPoint();
            }
        }

        // Движение и поворот
        transform.position = Vector3.MoveTowards(transform.position, currentTarget, patrolSpeed * Time.deltaTime);
        if (currentTarget != transform.position)
        {
            Vector3 lookDir = (currentTarget - transform.position).normalized;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir, Vector3.up), 5f * Time.deltaTime);
        }

        // Достижение точки патрулирования
        if (!isAttacking && !isSearching && Vector3.Distance(transform.position, currentTarget) < 0.5f)
        {
            if (Time.time - arriveTime > waitTime)
            {
                currentTarget = GetNextPatrolPoint();
                arriveTime = Time.time;
            }
        }

        // Атака
        if (isAttacking)
        {
            AttackPlayer();
        }
    }

    private Vector3 GetNextPatrolPoint()
    {
        if (patrolPoints.Length == 0) return transform.position;
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        return patrolPoints[currentPatrolIndex].position;
    }

    private bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 targetPoint = player.position + Vector3.up * 1.2f;
        float distance = Vector3.Distance(transform.position, targetPoint);
        if (distance > detectionRange) return false;

        Vector3 toPlayer = targetPoint - transform.position;
        float angle = Vector3.Angle(transform.forward, toPlayer);
        if (angle > fieldOfView * 0.5f) return false;

        if (Physics.Raycast(transform.position, toPlayer.normalized, out RaycastHit hit, distance))
        {
            return hit.collider.CompareTag("Player");
        }
        return false;
    }

    private void AttackPlayer()
    {
        if (Time.time >= lastAttackTime + fireRate)
        {
            lastAttackTime = Time.time;
            if (laserVFX != null) laserVFX.Play();

            if (Vector3.Distance(transform.position, player.position) <= attackRange)
            {
                if (player.TryGetComponent<IDamageable>(out IDamageable target))
                {
                    target.TakeDamage(damage, player.position);
                }
            }
        }
    }

    public void TakeDamage(float damage, Vector3 hitPoint)
    {
        currentHealth -= Mathf.RoundToInt(damage);
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Дрон уничтожен!");
        Destroy(gameObject);
    }
}