using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Collider))]
public class FlyingDrone : MonoBehaviour, IDamageable
{
    private enum DroneState
    {
        Patrol,
        Chase,
        Attack,
        Search
    }

    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolSpeed = 3.4f;
    [FormerlySerializedAs("waitTime")]
    [SerializeField] private float waitTimeAtPoint = 1.3f;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 24f;
    [SerializeField] private float fieldOfView = 100f;
    [SerializeField] private float hearingRange = 10f;
    [SerializeField] private float lostTargetGrace = 2f;
    [SerializeField] private float gunshotMemorySeconds = 1.25f;
    [SerializeField] private LayerMask obstacleMask = ~0;

    [Header("Attack")]
    [SerializeField] private float attackRange = 18f;
    [SerializeField] private float fireRate = 0.9f;
    [SerializeField] private int damage = 12;
    [SerializeField] private float attackAimDelay = 0.2f;
    [FormerlySerializedAs("playerLayer")]
    [SerializeField] private LayerMask playerLayer = ~0; // kept for prefab compatibility
    [SerializeField] private ParticleSystem laserVFX;

    [Header("Movement")]
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float attackOrbitSpeed = 4f;
    [SerializeField] private float attackOrbitRadius = 7f;
    [SerializeField] private float minAttackDistance = 3f;
    [SerializeField] private float desiredHeightOffset = 2.2f;
    [SerializeField] private float rotationSpeed = 7f;
    [SerializeField] private float minFacingDotToFire = 0.45f;

    [Header("Search")]
    [SerializeField] private float searchTime = 2.8f;

    [Header("Health")]
    [SerializeField] private int maxHealth = 50;

    private int currentHealth;
    private int currentPatrolIndex;
    private float lastAttackTime;
    private float arriveTime;
    private float searchEndTime;
    private float lastContactTime;
    private float attackReadyTime;
    private float orbitAngle;
    private int orbitDirection = 1;

    private Vector3 currentTarget;
    private Vector3 lastKnownPlayerPosition;
    private Transform player;
    private DroneState state = DroneState.Patrol;

    private void Awake()
    {
        currentHealth = maxHealth;
        ResolvePlayerRef(force: true);

        if (player != null) lastKnownPlayerPosition = player.position;
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            currentTarget = patrolPoints[0].position;
        }
        else
        {
            currentTarget = transform.position;
        }

        orbitAngle = Random.Range(0f, 360f);
        orbitDirection = Random.value > 0.5f ? 1 : -1;
        lastContactTime = -999f;
    }

    private void Update()
    {
        if (DeathScreen.GlobalDeathActive) return;

        ResolvePlayerRef();
        if (player == null)
        {
            PatrolUpdate();
            return;
        }

        bool canSee = CanSeePlayer();
        bool canSense = CanSensePlayer(canSee);
        if (canSee || canSense)
        {
            lastKnownPlayerPosition = player.position;
            lastContactTime = Time.time;
        }

        switch (state)
        {
            case DroneState.Patrol:
                PatrolUpdate();
                if (canSee || canSense) SetState(DroneState.Chase);
                break;

            case DroneState.Chase:
                ChaseUpdate();
                if (canSee && IsInAttackRange())
                {
                    SetState(DroneState.Attack);
                }
                else if (Time.time - lastContactTime > lostTargetGrace)
                {
                    EnterSearch();
                }
                break;

            case DroneState.Attack:
                AttackUpdate();
                if (canSee && !IsInAttackRange())
                {
                    SetState(DroneState.Chase);
                }
                else if (Time.time - lastContactTime > lostTargetGrace)
                {
                    EnterSearch();
                }
                break;

            case DroneState.Search:
                SearchUpdate();
                if (canSee || canSense)
                {
                    SetState(DroneState.Chase);
                }
                break;
        }
    }

    private void PatrolUpdate()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            currentTarget = transform.position;
            return;
        }

        currentTarget = patrolPoints[currentPatrolIndex].position;
        MoveTowards(currentTarget, patrolSpeed);

        if (Vector3.Distance(transform.position, currentTarget) <= 0.5f)
        {
            if (Time.time - arriveTime > waitTimeAtPoint)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                arriveTime = Time.time;
            }
        }
        else
        {
            arriveTime = Time.time;
        }
    }

    private void ChaseUpdate()
    {
        Vector3 target = player.position + Vector3.up * desiredHeightOffset;
        MoveTowards(target, chaseSpeed);
    }

    private void AttackUpdate()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // If too close, step back first to avoid spinning around the player.
        if (distanceToPlayer < minAttackDistance)
        {
            Vector3 away = (transform.position - player.position);
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f) away = -transform.forward;
            Vector3 retreatTarget = player.position + away.normalized * Mathf.Max(minAttackDistance, 1.5f) + Vector3.up * desiredHeightOffset;
            MoveTowards(retreatTarget, chaseSpeed);
            FaceTowards(player.position + Vector3.up * 1.2f);
            TryAttack();
            return;
        }

        orbitAngle += orbitDirection * attackOrbitSpeed * 30f * Time.deltaTime;
        Vector3 around = Quaternion.Euler(0f, orbitAngle, 0f) * Vector3.forward * attackOrbitRadius;
        Vector3 orbitPoint = player.position + around + Vector3.up * desiredHeightOffset;

        MoveTowards(orbitPoint, chaseSpeed);
        FaceTowards(player.position + Vector3.up * 1.2f);

        TryAttack();
    }

    private void SearchUpdate()
    {
        Vector3 target = lastKnownPlayerPosition + Vector3.up * desiredHeightOffset;
        MoveTowards(target, patrolSpeed);
        FaceTowards(lastKnownPlayerPosition + Vector3.up * 1.2f);

        bool reached = Vector3.Distance(transform.position, target) <= 1.1f;
        if (reached && Time.time >= searchEndTime)
        {
            SetState(DroneState.Patrol);
            if (Random.value > 0.6f) orbitDirection *= -1;
        }
    }

    private void EnterSearch()
    {
        SetState(DroneState.Search);
        searchEndTime = Time.time + searchTime;
    }

    private bool IsInAttackRange()
    {
        if (player == null) return false;
        return Vector3.Distance(transform.position, player.position) <= attackRange;
    }

    private void TryAttack()
    {
        if (player == null) return;
        if (Time.time < attackReadyTime) return;
        if (Time.time < lastAttackTime + fireRate) return;
        if (!HasLineOfSightToPlayer()) return;
        if (!IsFacingPlayer()) return;

        lastAttackTime = Time.time;
        if (laserVFX != null) laserVFX.Play();

        if (player.TryGetComponent<IDamageable>(out var target))
        {
            target.TakeDamage(damage, player.position);
        }
        else
        {
            target = player.GetComponentInParent<IDamageable>();
            if (target != null) target.TakeDamage(damage, player.position);
        }
    }

    private bool IsFacingPlayer()
    {
        if (player == null) return false;
        Vector3 toPlayer = (player.position + Vector3.up * 1.1f) - transform.position;
        if (toPlayer.sqrMagnitude < 0.0001f) return true;
        float dot = Vector3.Dot(transform.forward.normalized, toPlayer.normalized);
        return dot >= Mathf.Clamp(minFacingDotToFire, -1f, 1f);
    }

    private bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 targetPoint = player.position + Vector3.up * 1.2f;
        Vector3 toPlayer = targetPoint - transform.position;
        float distance = toPlayer.magnitude;
        if (distance > detectionRange) return false;

        float angle = Vector3.Angle(transform.forward, toPlayer);
        if (angle > fieldOfView * 0.5f) return false;

        return HasLineOfSightToPlayer();
    }

    private bool CanSensePlayer(bool canSeePlayerAlready)
    {
        if (canSeePlayerAlready) return true;
        if (player == null) return false;

        if (Vector3.Distance(transform.position, player.position) <= hearingRange &&
            player.TryGetComponent<PlayerMovement>(out var movement) &&
            movement.IsMoving)
        {
            return true;
        }

        if (CombatStimulusHub.TryGetRecentGunshot(gunshotMemorySeconds, out Vector3 gunshotPos, out float gunshotRadius))
        {
            float effectiveRange = Mathf.Max(hearingRange, gunshotRadius);
            if (Vector3.Distance(transform.position, gunshotPos) <= effectiveRange)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasLineOfSightToPlayer()
    {
        if (player == null) return false;
        Vector3 from = transform.position;
        Vector3 to = player.position + Vector3.up * 1.1f;
        Vector3 dir = to - from;
        float distance = dir.magnitude;

        if (Physics.Raycast(from, dir.normalized, out RaycastHit hit, distance, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            return hit.collider.CompareTag("Player") || hit.collider.transform.root.CompareTag("Player");
        }
        return false;
    }

    private void MoveTowards(Vector3 target, float speed)
    {
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        FaceTowards(target);
    }

    private void FaceTowards(Vector3 target)
    {
        Vector3 lookDir = target - transform.position;
        if (lookDir.sqrMagnitude < 0.0001f) return;
        Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
    }

    public void TakeDamage(float incomingDamage, Vector3 hitPoint)
    {
        currentHealth -= Mathf.RoundToInt(incomingDamage);
        if (currentHealth <= 0)
        {
            Destroy(gameObject);
        }
    }

    private void ResolvePlayerRef(bool force = false)
    {
        if (!force && player != null) return;
        player = PlayerLocator.GetPlayerTransform(force);
    }

    private void SetState(DroneState newState)
    {
        if (state == newState) return;
        state = newState;

        if (state == DroneState.Attack)
        {
            attackReadyTime = Time.time + Mathf.Max(0f, attackAimDelay);
        }
    }
}
