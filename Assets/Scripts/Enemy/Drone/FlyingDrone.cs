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
    [SerializeField] private float searchTime = 1.5f; 

    [Header("Health")]
    [SerializeField] private int maxHealth = 50;

    private int currentHealth;
    private int currentPatrolIndex = 0;
    private float lastAttackTime;
    private float arriveTime;
    private Vector3 currentTarget;
    private Vector3 lastKnownPlayerPosition;
    private Transform player;
    private bool isAttacking = false;
    private bool isSearching = false;      
    private float searchStartTime; 

    private void Awake()
    {
        currentHealth = maxHealth;
        ResolvePlayerRef(force: true);

        if (patrolPoints.Length > 0)
            currentTarget = patrolPoints[0].position;
    }

    private void Update()
    {
        if (DeathScreen.GlobalDeathActive) return;
        ResolvePlayerRef();
        if (player == null) return;

        bool canSeePlayer = CanSeePlayer();

        if (canSeePlayer)
        {
            lastKnownPlayerPosition = player.position;
            currentTarget = player.position;
            isAttacking = true;
            isSearching = false;
        }
        else if (isAttacking)
        {
            isAttacking = false;
            isSearching = true;
            searchStartTime = Time.time;
            currentTarget = lastKnownPlayerPosition;
        }
        else if (isSearching)
        {
            if (Time.time - searchStartTime > searchTime)
            {
                isSearching = false;
                currentTarget = GetNextPatrolPoint();
            }
        }

        transform.position = Vector3.MoveTowards(transform.position, currentTarget, patrolSpeed * Time.deltaTime);
        if (currentTarget != transform.position)
        {
            Vector3 lookDir = (currentTarget - transform.position).normalized;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir, Vector3.up), 5f * Time.deltaTime);
        }

        if (!isAttacking && !isSearching && Vector3.Distance(transform.position, currentTarget) < 0.5f)
        {
            if (Time.time - arriveTime > waitTime)
            {
                currentTarget = GetNextPatrolPoint();
                arriveTime = Time.time;
            }
        }

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
        if (DeathScreen.GlobalDeathActive) return;
        ResolvePlayerRef();
        if (player == null) return;

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
        Debug.Log("DroneDead!");
        Destroy(gameObject);
    }

    private void ResolvePlayerRef(bool force = false)
    {
        if (!force && player != null) return;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        player = playerObj != null ? playerObj.transform : null;
    }
}
