using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class GroundShooterAI : MonoBehaviour, IDamageable
{
    [Header("References")]
    [SerializeField] private Transform eyePoint;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Animator animator;
    [SerializeField] private Collider hitCollider;

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    
    [Header("Death")]
    [SerializeField] private string deathStateName = "Death_Anim";
    [SerializeField] private string deathStateFullPath = "Base Layer.Death_Anim";
    [SerializeField] private float deathCrossfadeDuration = 0.05f;
    [SerializeField] private float deathAnimLockSeconds = 1.2f;
    [SerializeField] private bool freezeOnDeathLastFrame = false;
    [SerializeField] private bool destroyOnDeath = false;
    [SerializeField] private float destroyDelaySeconds = 8f;

    [Header("Perception")]
    [SerializeField, Range(30f, 180f)] private float fovDegrees = 180f;
    [SerializeField] private float visionRange = 35f;
    [SerializeField] private LayerMask obstacleMask = ~0;

    [Header("Idle + Patrol")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float idleMinSeconds = 1f;
    [SerializeField] private float idleMaxSeconds = 2f;
    [SerializeField] private float patrolWaitSeconds = 1.25f;

    [Header("Combat")]
    [SerializeField] private float attackRange = 24f;
    [SerializeField] private float preferredDistance = 14f;
    [SerializeField] private float searchDurationSeconds = 8f;
    [SerializeField] private float reactionDelaySeconds = 0.3f;
    [SerializeField] private float fireInterval = 0.16f;
    [SerializeField] private int baseDamage = 10;
    [SerializeField] private float aimSpreadDegrees = 1.8f;

    [Header("Cover + Peek")]
    [SerializeField] private float coverSearchRadius = 18f;
    [SerializeField] private float coverRepositionCooldown = 2.2f;
    [SerializeField] private float coverHoldSeconds = 1.0f;
    [SerializeField] private float peekSeconds = 0.45f;

    [Header("Entropy Scaling")]
    [SerializeField] private bool useEntropyScaling = true;
    [SerializeField] private float tier2DamageMult = 1.1f;
    [SerializeField] private float tier3DamageMult = 1.22f;
    [SerializeField] private float tier4DamageMult = 1.35f;
    [SerializeField] private float tier2FireRateMult = 0.92f;
    [SerializeField] private float tier3FireRateMult = 0.84f;
    [SerializeField] private float tier4FireRateMult = 0.76f;

    [Header("Debug")]
    [SerializeField] private bool debugState;
    [SerializeField] private bool debugDeathAnimation;

    private NavMeshAgent agent;
    private Transform player;
    private EnemyStateMachine stateMachine;

    private float currentHealth;
    private bool dead;

    private Vector3 lastKnownPlayerPosition;
    private float lastSeenTime = -999f;
    private float stateTimer;
    private float fireReadyTime;
    private float attackAllowedAfter;
    private float lastCoverPickTime = -999f;
    private int patrolIndex;
    private int entropyTier = 1;

    private CoverPoint currentCover;
    private bool peeking;

    private IdleState idleState;
    private PatrolState patrolState;
    private AttackState attackState;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsAimingHash = Animator.StringToHash("IsAiming");
    private static readonly int IsInCoverHash = Animator.StringToHash("IsInCover");
    private static readonly int IsPeekingHash = Animator.StringToHash("IsPeeking");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = true;
        agent.updatePosition = true;
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (hitCollider == null) hitCollider = GetComponent<Collider>();
        if (eyePoint == null) eyePoint = transform;
        if (firePoint == null) firePoint = eyePoint;

        player = PlayerLocator.GetPlayerTransform(forceRefresh: true);
        currentHealth = maxHealth;
        stateMachine = new EnemyStateMachine();

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
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (dead) return;
        dead = true;
        currentHealth = 0f;
        if (debugDeathAnimation) Debug.Log($"{name}: Die() called");
        SetAgentStoppedSafe(true);
        if (agent != null) agent.enabled = false;
        if (hitCollider != null) hitCollider.enabled = false;

        // Prevent legacy/parallel combat scripts on the same object from continuing to fire.
        DisableSiblingBehaviours();

        if (animator != null)
        {
            animator.SetFloat(SpeedHash, 0f);
            animator.SetBool(IsAimingHash, false);
            animator.SetBool(IsInCoverHash, false);
            animator.SetBool(IsPeekingHash, false);
            animator.SetBool(IsDeadHash, true);
            animator.speed = 1f;
            animator.applyRootMotion = false;
            TryForcePlayDeath();
            StartCoroutine(LockDeathPoseAfterDelay());
        }

        // Keep corpse by default. Optional auto-cleanup can be enabled in inspector.
        if (destroyOnDeath && destroyDelaySeconds > 0f)
        {
            Destroy(gameObject, destroyDelaySeconds);
        }

        // Disable this AI logic after death lock is applied.
        enabled = false;
    }

    private void TryForcePlayDeath()
    {
        if (animator == null) return;

        bool played = false;
        float fade = Mathf.Max(0f, deathCrossfadeDuration);

        if (!string.IsNullOrWhiteSpace(deathStateFullPath))
        {
            int fullPathHash = Animator.StringToHash(deathStateFullPath);
            if (animator.HasState(0, fullPathHash))
            {
                animator.Play(fullPathHash, 0, 0f);
                animator.CrossFadeInFixedTime(fullPathHash, fade, 0, 0f);
                played = true;
                if (debugDeathAnimation) Debug.Log($"{name}: death by fullPath '{deathStateFullPath}'");
            }
        }

        if (!played && !string.IsNullOrWhiteSpace(deathStateName))
        {
            int nameHash = Animator.StringToHash(deathStateName);
            if (animator.HasState(0, nameHash))
            {
                animator.Play(nameHash, 0, 0f);
                animator.CrossFadeInFixedTime(nameHash, fade, 0, 0f);
                played = true;
                if (debugDeathAnimation) Debug.Log($"{name}: death by stateName '{deathStateName}' on layer 0");
            }
        }

        if (!played && !string.IsNullOrWhiteSpace(deathStateName))
        {
            for (int layer = 0; layer < animator.layerCount; layer++)
            {
                string layerPath = $"{animator.GetLayerName(layer)}.{deathStateName}";
                int hash = Animator.StringToHash(layerPath);
                if (!animator.HasState(layer, hash)) continue;

                animator.Play(hash, layer, 0f);
                animator.CrossFadeInFixedTime(hash, fade, layer, 0f);
                played = true;
                if (debugDeathAnimation) Debug.Log($"{name}: death by layerPath '{layerPath}'");
                break;
            }
        }

        if (!played)
        {
            Debug.LogWarning($"{name}: failed to force death state. Check Animator state name/path. deathStateName='{deathStateName}', deathStateFullPath='{deathStateFullPath}'");
        }
    }

    private IEnumerator LockDeathPoseAfterDelay()
    {
        if (animator == null) yield break;
        yield return new WaitForSeconds(Mathf.Max(0.05f, deathAnimLockSeconds));
        if (freezeOnDeathLastFrame && animator != null)
        {
            animator.speed = 0f;
        }
    }

    private void DisableSiblingBehaviours()
    {
        MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour b = behaviours[i];
            if (b == null || b == this) continue;
            b.enabled = false;
        }
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;
        float speed = HasValidAgent() ? agent.velocity.magnitude : 0f;
        animator.SetFloat(SpeedHash, speed);
        animator.SetBool(IsAimingHash, stateMachine.CurrentState == attackState);
        animator.SetBool(IsInCoverHash, currentCover != null);
        animator.SetBool(IsPeekingHash, peeking);
    }

    private bool HasValidAgent()
    {
        return agent != null && agent.enabled && agent.isOnNavMesh;
    }

    private void SetAgentStoppedSafe(bool value)
    {
        if (!HasValidAgent()) return;
        agent.isStopped = value;
    }

    private bool SetAgentDestinationSafe(Vector3 destination)
    {
        if (!HasValidAgent()) return false;
        return agent.SetDestination(destination);
    }

    private bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 from = eyePoint.position;
        Vector3 to = player.position + Vector3.up * 1.2f;
        Vector3 dir = to - from;
        float distance = dir.magnitude;
        if (distance > visionRange) return false;

        float angle = Vector3.Angle(transform.forward, dir);
        if (angle > fovDegrees * 0.5f) return false;

        if (!Physics.Raycast(from, dir.normalized, out RaycastHit hit, distance, obstacleMask, QueryTriggerInteraction.Ignore))
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

    private void FacePlayer(float speed = 8f)
    {
        if (player == null) return;
        Vector3 to = player.position - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude < 0.001f) return;
        Quaternion target = Quaternion.LookRotation(to.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * speed);
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

        return Mathf.Max(0.05f, fireInterval * mult);
    }

    private void OnCycleChanged(int cycle, int variant, int entropy, int tier)
    {
        entropyTier = Mathf.Clamp(tier, 1, 4);
    }

    private bool TryFindBestCover(out CoverPoint best)
    {
        best = null;
        if (player == null) return false;

        float bestScore = float.MinValue;
        Vector3 playerPos = player.position + Vector3.up * 1.1f;

        foreach (CoverPoint cover in CoverPoint.Active)
        {
            if (cover == null) continue;
            Vector3 hide = cover.HidePosition;
            float distToEnemy = Vector3.Distance(transform.position, hide);
            if (distToEnemy > coverSearchRadius) continue;

            Vector3 playerToHide = hide - playerPos;
            float playerDistance = playerToHide.magnitude;
            if (playerDistance < 0.5f) continue;

            bool blocked = Physics.Raycast(playerPos, playerToHide.normalized, out RaycastHit hit, playerDistance, obstacleMask, QueryTriggerInteraction.Ignore)
                           && !hit.collider.CompareTag("Player")
                           && !hit.collider.transform.root.CompareTag("Player");
            if (!blocked) continue;

            float score = -distToEnemy;
            if (score > bestScore)
            {
                bestScore = score;
                best = cover;
            }
        }

        return best != null;
    }

    private void TryShootAtPlayer()
    {
        if (player == null) return;
        if (Time.time < fireReadyTime) return;
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

    private sealed class IdleState : IAIState
    {
        private readonly GroundShooterAI ai;
        public string Name => "Idle";
        public IdleState(GroundShooterAI ai) => this.ai = ai;

        public void Enter()
        {
            ai.stateTimer = Random.Range(ai.idleMinSeconds, ai.idleMaxSeconds);
            ai.SetAgentStoppedSafe(true);
            ai.peeking = false;
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
            if (ai.stateTimer <= 0f)
                ai.stateMachine.ChangeState(ai.patrolState);
        }

        public void Exit()
        {
            ai.SetAgentStoppedSafe(false);
        }
    }

    private sealed class PatrolState : IAIState
    {
        private readonly GroundShooterAI ai;
        public string Name => "Patrol";
        public PatrolState(GroundShooterAI ai) => this.ai = ai;

        public void Enter()
        {
            ai.peeking = false;
            ai.currentCover = null;
            if (ai.debugState) Debug.Log($"{ai.name}: Patrol");
            MoveToNextPoint();
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
            if (!ai.HasValidAgent()) return;
            if (ai.agent.pathPending) return;

            if (ai.agent.remainingDistance <= Mathf.Max(ai.agent.stoppingDistance, 0.3f))
            {
                ai.stateTimer -= deltaTime;
                if (ai.stateTimer <= 0f)
                {
                    MoveToNextPoint();
                }
            }
            else
            {
                ai.stateTimer = ai.patrolWaitSeconds;
            }
        }

        public void Exit() { }

        private void MoveToNextPoint()
        {
            if (ai.patrolPoints == null || ai.patrolPoints.Length == 0) return;
            ai.patrolIndex = (ai.patrolIndex + 1) % ai.patrolPoints.Length;
            ai.SetAgentDestinationSafe(ai.patrolPoints[ai.patrolIndex].position);
            ai.stateTimer = ai.patrolWaitSeconds;
        }
    }

    private sealed class AttackState : IAIState
    {
        private readonly GroundShooterAI ai;
        private bool movingToCover;
        private bool hiding;
        private bool peekingLeft;
        public string Name => "Attack";
        public AttackState(GroundShooterAI ai) => this.ai = ai;

        public void Enter()
        {
            if (ai.debugState) Debug.Log($"{ai.name}: Attack");
            ai.peeking = false;
            ai.fireReadyTime = Time.time + ai.reactionDelaySeconds;
            AcquireCover(force: true);
        }

        public void Tick(float deltaTime)
        {
            bool hasVision = ai.CanSeePlayer();
            bool shouldSearch = ai.SeenRecently(ai.searchDurationSeconds);

            if (!hasVision && !shouldSearch)
            {
                ai.currentCover = null;
                ai.stateMachine.ChangeState(ai.patrolState);
                return;
            }

            if (hasVision && Time.time >= ai.attackAllowedAfter)
            {
                ai.lastKnownPlayerPosition = ai.player.position;
            }

            if (ai.currentCover == null || Time.time - ai.lastCoverPickTime >= ai.coverRepositionCooldown)
            {
                AcquireCover(force: ai.currentCover == null);
            }

            if (ai.currentCover != null)
            {
                TickCoverCombat(deltaTime, hasVision);
                return;
            }

            TickFallbackCombat(hasVision);
        }

        public void Exit()
        {
            ai.SetAgentStoppedSafe(false);
            ai.peeking = false;
        }

        private void TickCoverCombat(float deltaTime, bool hasVision)
        {
            Vector3 hidePos = ai.currentCover.HidePosition;
            Vector3 peekPos = peekingLeft ? ai.currentCover.PeekLeftPosition : ai.currentCover.PeekRightPosition;

            if (movingToCover)
            {
                ai.SetAgentStoppedSafe(false);
                ai.SetAgentDestinationSafe(hidePos);
                if (ai.HasValidAgent() && !ai.agent.pathPending && ai.agent.remainingDistance <= Mathf.Max(ai.agent.stoppingDistance, 0.25f))
                {
                    movingToCover = false;
                    hiding = true;
                    ai.stateTimer = ai.coverHoldSeconds;
                    ai.SetAgentStoppedSafe(true);
                }
                return;
            }

            if (hiding)
            {
                ai.FacePlayer(10f);
                ai.stateTimer -= deltaTime;
                if (ai.stateTimer <= 0f)
                {
                    hiding = false;
                    ai.peeking = true;
                    ai.stateTimer = ai.peekSeconds;
                    ai.SetAgentStoppedSafe(false);
                    ai.SetAgentDestinationSafe(peekPos);
                }
                return;
            }

            ai.SetAgentStoppedSafe(false);
            ai.SetAgentDestinationSafe(peekPos);
            ai.FacePlayer(12f);

            if (ai.HasValidAgent() && !ai.agent.pathPending && ai.agent.remainingDistance <= Mathf.Max(ai.agent.stoppingDistance, 0.2f))
            {
                if (hasVision && Time.time >= ai.attackAllowedAfter)
                {
                    ai.TryShootAtPlayer();
                }
            }

            ai.stateTimer -= deltaTime;
            if (ai.stateTimer <= 0f)
            {
                ai.peeking = false;
                hiding = true;
                ai.stateTimer = ai.coverHoldSeconds;
                ai.SetAgentDestinationSafe(hidePos);
            }
        }

        private void TickFallbackCombat(bool hasVision)
        {
            ai.SetAgentStoppedSafe(false);
            Vector3 target = hasVision ? ai.player.position : ai.lastKnownPlayerPosition;
            Vector3 toTarget = target - ai.transform.position;
            float distance = toTarget.magnitude;

            if (distance > ai.preferredDistance)
                ai.SetAgentDestinationSafe(target);
            else
                ai.SetAgentDestinationSafe(ai.transform.position - toTarget.normalized * 2f);

            // Let NavMeshAgent control facing while moving to avoid sideways sliding.
            if (!ai.HasValidAgent() || ai.agent.velocity.sqrMagnitude < 0.04f)
            {
                ai.FacePlayer(12f);
            }
            if (hasVision && distance <= ai.attackRange && Time.time >= ai.attackAllowedAfter)
            {
                ai.TryShootAtPlayer();
            }
        }

        private void AcquireCover(bool force)
        {
            if (!force && ai.currentCover != null) return;
            if (ai.TryFindBestCover(out CoverPoint cover))
            {
                ai.currentCover = cover;
                ai.lastCoverPickTime = Time.time;
                peekingLeft = Random.value > 0.5f;
                movingToCover = true;
                hiding = false;
                ai.peeking = false;
                return;
            }

            ai.currentCover = null;
            movingToCover = false;
            hiding = false;
        }
    }
}
