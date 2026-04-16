using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Collider))]
public class DroneShooterAI : MonoBehaviour, IDamageable
{
    [Header("References")]
    [SerializeField] private Transform eyePoint;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Animator animator;
    [SerializeField] private Collider hitCollider;

    [Header("Health")]
    [SerializeField] private float maxHealth = 50f;

    [Header("Perception")]
    [SerializeField, Range(30f, 180f)] private float fovDegrees = 180f;
    [SerializeField] private float visionRange = 30f;
    [SerializeField] private LayerMask obstacleMask = ~0;

    [Header("Idle + Patrol")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float idleMinSeconds = 1f;
    [SerializeField] private float idleMaxSeconds = 2f;
    [SerializeField] private float patrolWaitSeconds = 1.1f;
    [SerializeField] private float patrolReachDistance = 0.7f;

    [Header("Attack")]
    [SerializeField] private float attackRange = 20f;
    [SerializeField] private float searchDurationSeconds = 8f;
    [SerializeField] private float reactionDelaySeconds = 0.22f;
    [SerializeField] private float fireInterval = 0.3f;
    [SerializeField] private int baseDamage = 12;
    [SerializeField] private float aimSpreadDegrees = 1.4f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 9f;
    [SerializeField] private float desiredHeightOffset = 1.8f;
    [SerializeField] private float minAttackDistance = 3f;
    [SerializeField] private float weaveAmplitude = 1.2f;
    [SerializeField] private float weaveFrequency = 2.2f;
    [SerializeField, Range(-1f, 1f)] private float minFacingDotToFire = 0.35f;

    [Header("Patrol Path (NavMesh)")]
    [SerializeField] private float navMeshSampleDistance = 6f;
    [SerializeField] private float pathRebuildInterval = 0.65f;
    [SerializeField] private float cornerReachDistance = 0.75f;
    [SerializeField] private float pointTimeoutSeconds = 6f;

    [Header("Patrol Anti-Stuck")]
    [SerializeField] private float stuckDistanceEpsilon = 0.12f;
    [SerializeField] private float stuckTimeoutSeconds = 1.8f;

    [Header("Entropy Scaling")]
    [SerializeField] private bool useEntropyScaling = true;
    [SerializeField] private float tier2DamageMult = 1.08f;
    [SerializeField] private float tier3DamageMult = 1.18f;
    [SerializeField] private float tier4DamageMult = 1.3f;
    [SerializeField] private float tier2FireRateMult = 0.92f;
    [SerializeField] private float tier3FireRateMult = 0.84f;
    [SerializeField] private float tier4FireRateMult = 0.76f;

    [Header("Debug")]
    [SerializeField] private bool debugState;

    private Transform player;
    private EnemyStateMachine stateMachine;

    private float currentHealth;
    private bool dead;
    private int patrolIndex;
    private int entropyTier = 1;

    private Vector3 lastKnownPlayerPosition;
    private float lastSeenTime = -999f;
    private float stateTimer;
    private float fireReadyTime;
    private float attackAllowedAfter;
    private float weavePhase;
    private float patrolStuckTimer;
    private Vector3 lastPatrolPosition;
    private float nextPathRebuildTime;
    private float patrolPointElapsed;
    private int patrolCornerIndex;
    private NavMeshPath patrolPath;

    private IdleState idleState;
    private PatrolState patrolState;
    private AttackState attackState;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsAttackingHash = Animator.StringToHash("IsAttacking");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

    private void Awake()
    {
        if (eyePoint == null) eyePoint = transform;
        if (firePoint == null) firePoint = eyePoint;
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (hitCollider == null) hitCollider = GetComponent<Collider>();

        player = PlayerLocator.GetPlayerTransform(forceRefresh: true);
        currentHealth = maxHealth;
        stateMachine = new EnemyStateMachine();
        weavePhase = Random.value * 100f;
        lastPatrolPosition = transform.position;
        patrolPath = new NavMeshPath();

        idleState = new IdleState(this);
        patrolState = new PatrolState(this);
        attackState = new AttackState(this);
    }

    private void OnEnable()
    {
        if (DeathCycleManager.Instance != null)
        {
            entropyTier = DeathCycleManager.Instance.EntropyTier;
            DeathCycleManager.Instance.OnCycleStateChanged += OnCycleChanged;
        }
    }

    private void OnDisable()
    {
        if (DeathCycleManager.Instance != null)
        {
            DeathCycleManager.Instance.OnCycleStateChanged -= OnCycleChanged;
        }
    }

    private void Start()
    {
        stateMachine.ChangeState(idleState);
    }

    private void Update()
    {
        if (dead || DeathScreen.GlobalDeathActive) return;
        if (player == null) player = PlayerLocator.GetPlayerTransform(forceRefresh: true);
        stateMachine.Tick(Time.deltaTime);
        UpdateAnimator();
    }

    public void TakeDamage(float damage, Vector3 hitPoint)
    {
        if (dead) return;
        currentHealth -= Mathf.Max(0f, damage);
        if (currentHealth <= 0f) Die();
    }

    private void Die()
    {
        dead = true;
        if (hitCollider != null) hitCollider.enabled = false;
        if (animator != null) animator.SetBool(IsDeadHash, true);
        enabled = false;
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetFloat(SpeedHash, moveSpeed);
        animator.SetBool(IsAttackingHash, stateMachine.CurrentState == attackState);
    }

    private bool CanSeePlayer()
    {
        if (player == null) return false;
        Vector3 from = eyePoint.position;
        Vector3 to = player.position + Vector3.up * 1.1f;
        Vector3 dir = to - from;
        float dist = dir.magnitude;
        if (dist > visionRange) return false;

        float angle = Vector3.Angle(transform.forward, dir);
        if (angle > fovDegrees * 0.5f) return false;

        if (!Physics.Raycast(from, dir.normalized, out RaycastHit hit, dist, obstacleMask, QueryTriggerInteraction.Ignore))
            return false;

        bool seen = hit.collider.CompareTag("Player") || hit.collider.transform.root.CompareTag("Player");
        if (seen)
        {
            lastSeenTime = Time.time;
            lastKnownPlayerPosition = player.position;
        }
        return seen;
    }

    private bool SeenRecently(float seconds) => Time.time - lastSeenTime <= seconds;

    private void MoveTowards(Vector3 target)
    {
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
    }

    private void FaceTowards(Vector3 target, bool keepHorizontal = false)
    {
        Vector3 dir = target - transform.position;
        if (keepHorizontal) dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        Quaternion q = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, q, Time.deltaTime * rotateSpeed);
    }

    private bool IsFacingPlayer()
    {
        if (player == null) return false;
        Vector3 toPlayer = (player.position + Vector3.up * 1.1f) - transform.position;
        if (toPlayer.sqrMagnitude < 0.0001f) return true;
        float dot = Vector3.Dot(transform.forward.normalized, toPlayer.normalized);
        return dot >= minFacingDotToFire;
    }

    private int GetScaledDamage()
    {
        float mult = useEntropyScaling ? entropyTier switch
        {
            2 => tier2DamageMult,
            3 => tier3DamageMult,
            4 => tier4DamageMult,
            _ => 1f
        } : 1f;

        DeathCycleManager manager = DeathCycleManager.Instance;
        if (manager != null)
        {
            mult *= manager.RecoveryDamageMultiplier;
        }

        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * mult));
    }

    private float GetScaledFireInterval()
    {
        float mult = useEntropyScaling ? entropyTier switch
        {
            2 => tier2FireRateMult,
            3 => tier3FireRateMult,
            4 => tier4FireRateMult,
            _ => 1f
        } : 1f;

        DeathCycleManager manager = DeathCycleManager.Instance;
        if (manager != null)
        {
            mult *= manager.RecoveryFireIntervalMultiplier;
        }

        return Mathf.Max(0.08f, fireInterval * mult);
    }

    private void TryShootPlayer()
    {
        if (player == null) return;
        if (Time.time < fireReadyTime) return;
        if (!IsFacingPlayer()) return;
        fireReadyTime = Time.time + GetScaledFireInterval();

        Vector3 from = firePoint.position;
        Vector3 to = player.position + Vector3.up * 1.1f;
        Vector3 dir = (to - from).normalized;
        dir = Quaternion.Euler(Random.Range(-aimSpreadDegrees, aimSpreadDegrees), Random.Range(-aimSpreadDegrees, aimSpreadDegrees), 0f) * dir;

        if (Physics.Raycast(from, dir, out RaycastHit hit, attackRange, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.TryGetComponent<IDamageable>(out var target))
            {
                target.TakeDamage(GetScaledDamage(), hit.point);
            }
            else
            {
                target = hit.collider.GetComponentInParent<IDamageable>();
                if (target != null) target.TakeDamage(GetScaledDamage(), hit.point);
            }
        }
    }

    private bool IsPatrolStuck(float deltaTime)
    {
        float moved = Vector3.Distance(transform.position, lastPatrolPosition);
        if (moved <= stuckDistanceEpsilon)
            patrolStuckTimer += deltaTime;
        else
            patrolStuckTimer = 0f;

        lastPatrolPosition = transform.position;
        return patrolStuckTimer >= stuckTimeoutSeconds;
    }

    private bool BuildPathToCurrentPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return false;

        Vector3 waypoint = patrolPoints[patrolIndex].position;
        if (!TrySampleNavMesh(transform.position, out Vector3 from)) return false;
        if (!TrySampleNavMesh(waypoint, out Vector3 to)) return false;

        bool ok = NavMesh.CalculatePath(from, to, NavMesh.AllAreas, patrolPath);
        if (!ok || patrolPath == null || patrolPath.status == NavMeshPathStatus.PathInvalid || patrolPath.corners == null || patrolPath.corners.Length == 0)
            return false;

        patrolCornerIndex = patrolPath.corners.Length > 1 ? 1 : 0;
        nextPathRebuildTime = Time.time + Mathf.Max(0.1f, pathRebuildInterval);
        return true;
    }

    private bool TryGetCurrentPathCorner(out Vector3 cornerTarget)
    {
        cornerTarget = default;
        if (patrolPath == null || patrolPath.corners == null || patrolPath.corners.Length == 0) return false;
        if (patrolCornerIndex < 0 || patrolCornerIndex >= patrolPath.corners.Length) return false;

        Vector3 corner = patrolPath.corners[patrolCornerIndex];
        cornerTarget = new Vector3(corner.x, corner.y + desiredHeightOffset, corner.z);
        return true;
    }

    private bool TrySampleNavMesh(Vector3 world, out Vector3 sampled)
    {
        if (NavMesh.SamplePosition(world, out NavMeshHit hit, Mathf.Max(0.5f, navMeshSampleDistance), NavMesh.AllAreas))
        {
            sampled = hit.position;
            return true;
        }

        sampled = default;
        return false;
    }

    private void AdvancePatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;
        patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
        patrolPointElapsed = 0f;
        patrolStuckTimer = 0f;
        BuildPathToCurrentPatrolPoint();
    }

    private void OnCycleChanged(int cycle, int variant, int entropy, int tier)
    {
        entropyTier = Mathf.Clamp(tier, 1, 4);
    }

    private sealed class IdleState : IAIState
    {
        private readonly DroneShooterAI ai;
        public string Name => "Idle";
        public IdleState(DroneShooterAI ai) => this.ai = ai;
        public void Enter()
        {
            ai.stateTimer = Random.Range(ai.idleMinSeconds, ai.idleMaxSeconds);
            if (ai.debugState) Debug.Log($"{ai.name}: Idle");
        }
        public void Tick(float deltaTime)
        {
            if (ai.CanSeePlayer())
            {
                ai.attackAllowedAfter = Time.time + ai.reactionDelaySeconds;
                ai.stateMachine.ChangeState(ai.attackState);
                return;
            }
            ai.stateTimer -= deltaTime;
            if (ai.stateTimer <= 0f) ai.stateMachine.ChangeState(ai.patrolState);
        }
        public void Exit() { }
    }

    private sealed class PatrolState : IAIState
    {
        private readonly DroneShooterAI ai;
        public string Name => "Patrol";
        public PatrolState(DroneShooterAI ai) => this.ai = ai;

        public void Enter()
        {
            if (ai.debugState) Debug.Log($"{ai.name}: Patrol");
            if (ai.patrolPoints != null && ai.patrolPoints.Length > 0)
                ai.patrolIndex = (ai.patrolIndex + 1) % ai.patrolPoints.Length;
            ai.stateTimer = ai.patrolWaitSeconds;
            ai.patrolStuckTimer = 0f;
            ai.lastPatrolPosition = ai.transform.position;
            ai.patrolPointElapsed = 0f;
            ai.BuildPathToCurrentPatrolPoint();
        }

        public void Tick(float deltaTime)
        {
            if (ai.CanSeePlayer())
            {
                ai.attackAllowedAfter = Time.time + ai.reactionDelaySeconds;
                ai.stateMachine.ChangeState(ai.attackState);
                return;
            }

            if (ai.patrolPoints == null || ai.patrolPoints.Length == 0) return;

            ai.patrolPointElapsed += deltaTime;
            if (Time.time >= ai.nextPathRebuildTime)
            {
                ai.BuildPathToCurrentPatrolPoint();
            }

            Vector3 waypoint = ai.patrolPoints[ai.patrolIndex].position + Vector3.up * ai.desiredHeightOffset;
            bool hasCorner = ai.TryGetCurrentPathCorner(out Vector3 cornerTarget);

            if (hasCorner)
            {
                ai.MoveTowards(cornerTarget);
                ai.FaceTowards(cornerTarget);

                if (Vector3.Distance(ai.transform.position, cornerTarget) <= ai.cornerReachDistance)
                {
                    ai.patrolCornerIndex++;
                }
            }
            else
            {
                // Fallback if path wasn't available this frame.
                ai.MoveTowards(waypoint);
                ai.FaceTowards(waypoint);
            }

            bool reached = Vector3.Distance(ai.transform.position, waypoint) <= Mathf.Max(0.2f, ai.patrolReachDistance);
            if (reached)
            {
                ai.stateTimer -= deltaTime;
                if (ai.stateTimer <= 0f)
                {
                    ai.patrolIndex = (ai.patrolIndex + 1) % ai.patrolPoints.Length;
                    ai.stateTimer = ai.patrolWaitSeconds;
                }
            }
            else
            {
                ai.stateTimer = ai.patrolWaitSeconds;
            }

            // If drone is boxed by geometry/cornering, rebuild path first, then advance point.
            if (ai.IsPatrolStuck(deltaTime))
            {
                if (!ai.BuildPathToCurrentPatrolPoint())
                {
                    ai.AdvancePatrolPoint();
                }
                else
                {
                    ai.patrolStuckTimer = 0f;
                }
            }

            // Prevent infinite attempts on a blocked point.
            if (ai.patrolPointElapsed >= ai.pointTimeoutSeconds)
            {
                ai.AdvancePatrolPoint();
            }
        }

        public void Exit() { }
    }

    private sealed class AttackState : IAIState
    {
        private readonly DroneShooterAI ai;
        public string Name => "Attack";
        public AttackState(DroneShooterAI ai) => this.ai = ai;

        public void Enter()
        {
            if (ai.debugState) Debug.Log($"{ai.name}: Attack");
            ai.fireReadyTime = Time.time + ai.reactionDelaySeconds;
        }

        public void Tick(float deltaTime)
        {
            bool hasVision = ai.CanSeePlayer();
            bool keepSearching = ai.SeenRecently(ai.searchDurationSeconds);

            if (!hasVision && !keepSearching)
            {
                ai.stateMachine.ChangeState(ai.patrolState);
                return;
            }

            Vector3 targetPos = hasVision ? ai.player.position : ai.lastKnownPlayerPosition;
            Vector3 target = targetPos + Vector3.up * ai.desiredHeightOffset;
            Vector3 toPlayer = targetPos - ai.transform.position;
            float distance = toPlayer.magnitude;

            Vector3 lateral = Vector3.Cross(Vector3.up, toPlayer.normalized) *
                              Mathf.Sin((Time.time + ai.weavePhase) * ai.weaveFrequency) * ai.weaveAmplitude;

            if (distance < ai.minAttackDistance)
            {
                Vector3 away = (ai.transform.position - targetPos);
                away.y = 0f;
                if (away.sqrMagnitude < 0.0001f) away = -ai.transform.forward;
                target = targetPos + away.normalized * ai.minAttackDistance + Vector3.up * ai.desiredHeightOffset;
            }
            else if (distance > ai.attackRange * 0.9f)
            {
                target += lateral;
            }
            else
            {
                target = ai.transform.position + lateral * 0.6f;
            }

            ai.MoveTowards(target);
            ai.FaceTowards(targetPos + Vector3.up * 1.1f);

            if (hasVision && Time.time >= ai.attackAllowedAfter)
            {
                ai.TryShootPlayer();
            }
        }

        public void Exit() { }
    }
}
