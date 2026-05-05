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
    [SerializeField] private Transform visualRoot;

    [Header("Health")]
    [SerializeField] private float maxHealth = 80f;
    
    [Header("Death")]
    [SerializeField] private string deathStateName = "Death_Anim";
    [SerializeField] private string deathStateFullPath = "Base Layer.Death_Anim";
    [SerializeField] private float deathCrossfadeDuration = 0.05f;
    [SerializeField] private float deathAnimLockSeconds = 1.2f;
    [SerializeField] private bool freezeOnDeathLastFrame = false;
    [SerializeField] private bool destroyOnDeath = false;
    [SerializeField] private float destroyDelaySeconds = 8f;

    [Header("Perception")]
    [SerializeField, Range(30f, 180f)] private float fovDegrees = 160f;
    [SerializeField] private float visionRange = 30f;
    [SerializeField, Range(-180f, 180f)] private float lookYawOffset = 90f;
    [SerializeField] private LayerMask obstacleMask = ~0;

    [Header("Idle + Patrol")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float idleMinSeconds = 1f;
    [SerializeField] private float idleMaxSeconds = 2f;
    [SerializeField] private float patrolWaitSeconds = 1.25f;

    [Header("Combat")]
    [SerializeField] private float attackRange = 16f;
    [SerializeField] private float searchDurationSeconds = 5.5f;
    [SerializeField] private float reactionDelaySeconds = 0.6f;
    [SerializeField] private float fireInterval = 0.32f;
    [SerializeField] private int baseDamage = 3;
    [SerializeField] private float aimSpreadDegrees = 3.1f;
    [SerializeField] private float burstMinSeconds = 1.2f;
    [SerializeField] private float burstMaxSeconds = 1.8f;
    [SerializeField] private float repositionMinSeconds = 1.2f;
    [SerializeField] private float repositionMaxSeconds = 1.9f;
    [SerializeField] private float repositionDistance = 2.2f;

    [Header("Shoot VFX")]
    [SerializeField] private GameObject muzzlePrefab;
    [SerializeField] private float muzzleLifetime = 0.08f;
    [SerializeField] private GameObject tracerEffectPrefab;
    [SerializeField] private float tracerSpeed = 200f;
    [SerializeField] private float tracerWidth = 0.018f;
    [SerializeField] private float tracerLength = 0.45f;
    [SerializeField] private Color tracerColor = new Color(1f, 0.85f, 0.55f, 0.95f);
    [SerializeField] private Material tracerMaterial;
    [SerializeField] private float tracerFadeOut = 0.04f;
    [SerializeField] private AudioCue shootCue;

    [Header("Entropy Scaling")]
    [SerializeField] private bool useEntropyScaling = true;
    [SerializeField] private float tier2DamageMult = 1.04f;
    [SerializeField] private float tier3DamageMult = 1.1f;
    [SerializeField] private float tier4DamageMult = 1.17f;
    [SerializeField] private float tier2FireRateMult = 0.98f;
    [SerializeField] private float tier3FireRateMult = 0.93f;
    [SerializeField] private float tier4FireRateMult = 0.88f;

    [Header("Debug")]
    [SerializeField] private bool debugState;
    [SerializeField] private bool debugDeathAnimation;

    [Header("Difficulty Profile")]
    [SerializeField] private CombatDifficultyProfile difficultyProfile;

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
    private int patrolIndex;
    private int entropyTier = 1;
    private bool difficultyApplied;
    private float baseMaxHealth;
    private int baseBaseDamage;
    private float baseFireInterval;
    private float baseReactionDelay;
    private float baseSearchDuration;
    private float baseAimSpread;
    private float baseBurstMin;
    private float baseBurstMax;
    private float baseRepositionDistance;

    private IdleState idleState;
    private PatrolState patrolState;
    private AttackState attackState;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsAimingHash = Animator.StringToHash("IsAiming");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

    private void Awake()
    {
        CacheBaseTuningIfNeeded();
        ApplyDifficultyProfile();

        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = true;
        agent.updatePosition = true;
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (hitCollider == null) hitCollider = GetComponent<Collider>();
        if (eyePoint == null) eyePoint = transform;
        if (firePoint == null) firePoint = eyePoint;
        if (visualRoot == null) visualRoot = transform;

        player = PlayerLocator.GetPlayerTransform(forceRefresh: true);
        currentHealth = maxHealth;
        stateMachine = new EnemyStateMachine();

        idleState = new IdleState(this);
        patrolState = new PatrolState(this);
        attackState = new AttackState(this);
    }

    private void OnValidate()
    {
        if (Application.isPlaying) return;
        CacheBaseTuningIfNeeded();
        difficultyApplied = false;
        ApplyDifficultyProfile();
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

    private void CacheBaseTuningIfNeeded()
    {
        if (baseMaxHealth > 0f) return;
        baseMaxHealth = maxHealth;
        baseBaseDamage = baseDamage;
        baseFireInterval = fireInterval;
        baseReactionDelay = reactionDelaySeconds;
        baseSearchDuration = searchDurationSeconds;
        baseAimSpread = aimSpreadDegrees;
        baseBurstMin = burstMinSeconds;
        baseBurstMax = burstMaxSeconds;
        baseRepositionDistance = repositionDistance;
    }

    private void ApplyDifficultyProfile()
    {
        if (difficultyApplied) return;
        if (difficultyProfile == null) return;

        maxHealth = Mathf.Max(1f, baseMaxHealth * difficultyProfile.HealthMultiplier);
        baseDamage = Mathf.Max(1, Mathf.RoundToInt(baseBaseDamage * difficultyProfile.DamageMultiplier));
        fireInterval = Mathf.Max(0.05f, baseFireInterval * difficultyProfile.FireIntervalMultiplier);
        reactionDelaySeconds = Mathf.Max(0.05f, baseReactionDelay * difficultyProfile.ReactionDelayMultiplier);
        searchDurationSeconds = Mathf.Max(1f, baseSearchDuration * difficultyProfile.SearchDurationMultiplier);
        aimSpreadDegrees = Mathf.Max(0.1f, baseAimSpread * difficultyProfile.AimSpreadMultiplier);

        float burstMult = difficultyProfile.GroundBurstDurationMultiplier;
        burstMinSeconds = Mathf.Max(0.2f, baseBurstMin * burstMult);
        burstMaxSeconds = Mathf.Max(burstMinSeconds, baseBurstMax * burstMult);
        repositionDistance = Mathf.Max(0.5f, baseRepositionDistance * difficultyProfile.GroundRepositionDistanceMultiplier);

        difficultyApplied = true;
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
        OnDamaged(hitPoint);
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void OnDamaged(Vector3 hitPoint)
    {
        if (player != null)
        {
            lastKnownPlayerPosition = player.position;
            lastSeenTime = Time.time;
        }
        else
        {
            lastKnownPlayerPosition = hitPoint;
            lastSeenTime = Time.time;
        }

        attackAllowedAfter = Time.time + reactionDelaySeconds * 0.5f;
        if (stateMachine.CurrentState != attackState)
        {
            stateMachine.ChangeState(attackState);
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
        SetDeathCollidersTrigger();
        DisableSiblingCombatBehaviours();

        if (animator != null)
        {
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.SetFloat(SpeedHash, 0f);
            animator.SetBool(IsAimingHash, false);
            animator.SetBool(IsDeadHash, true);
            animator.speed = 1f;
            animator.applyRootMotion = false;
            TryForcePlayDeath();
            StartCoroutine(ClearDeathBoolAfterStateEnter());
            StartCoroutine(LockDeathPoseAfterDelay());
        }
        if (destroyOnDeath && destroyDelaySeconds > 0f)
        {
            Destroy(gameObject, destroyDelaySeconds);
        }
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
                animator.Update(0f);
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
                animator.Update(0f);
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
                animator.Update(0f);
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

    private IEnumerator ClearDeathBoolAfterStateEnter()
    {
        if (animator == null) yield break;
        yield return null;
        if (animator != null)
        {
            animator.SetBool(IsDeadHash, false);
        }
    }

    private void DisableSiblingCombatBehaviours()
    {
        MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour b = behaviours[i];
            if (b == null || b == this) continue;
            if (b is EnemyCombat || b is EnemyAI || b is EnemyPatrol || b is EnemyVision || b is EnemyHealth || b is DroneAI || b is DroneCombat)
            {
                b.enabled = false;
            }
        }
    }

    private void SetDeathCollidersTrigger()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider c = colliders[i];
            if (c == null) continue;
            c.enabled = true;
            c.isTrigger = true;
        }
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;
        float speed = HasValidAgent() ? agent.velocity.magnitude : 0f;
        animator.SetFloat(SpeedHash, speed);
        animator.SetBool(IsAimingHash, stateMachine.CurrentState == attackState);
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

    private void SetAgentAutoRotationSafe(bool value)
    {
        if (agent == null) return;
        agent.updateRotation = value;
    }

    private bool SetAgentDestinationSafe(Vector3 destination)
    {
        if (!HasValidAgent()) return false;
        return agent.SetDestination(destination);
    }

    private bool TryGetRepositionPoint(out Vector3 point)
    {
        point = transform.position;
        Vector3 origin = transform.position;
        Vector3 toPlayer = (lastKnownPlayerPosition - origin);
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.05f) toPlayer = transform.forward;

        Vector3 side = Vector3.Cross(Vector3.up, toPlayer.normalized);
        if (Random.value < 0.5f) side = -side;
        Vector3 desired = origin + side * repositionDistance;

        if (NavMesh.SamplePosition(desired, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
        {
            point = hit.position;
            return true;
        }

        for (int i = 0; i < 6; i++)
        {
            Vector2 rnd2 = Random.insideUnitCircle * repositionDistance;
            Vector3 rnd = origin + new Vector3(rnd2.x, 0f, rnd2.y);
            if (NavMesh.SamplePosition(rnd, out hit, 2f, NavMesh.AllAreas))
            {
                point = hit.position;
                return true;
            }
        }

        return false;
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
        Quaternion baseTarget = Quaternion.LookRotation(to.normalized, Vector3.up);

        if (visualRoot != null && visualRoot != transform)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, baseTarget, Time.deltaTime * speed);
            Quaternion visualTarget = baseTarget * Quaternion.Euler(0f, lookYawOffset, 0f);
            visualRoot.rotation = Quaternion.Slerp(visualRoot.rotation, visualTarget, Time.deltaTime * speed);
            return;
        }

        Quaternion targetWithOffset = baseTarget * Quaternion.Euler(0f, lookYawOffset, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetWithOffset, Time.deltaTime * speed);
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

    private void TryShootAtPlayer()
    {
        if (player == null) return;
        if (Time.time < fireReadyTime) return;
        fireReadyTime = Time.time + GetScaledFireInterval();
        if (firePoint != null)
        {
            Vector3 fireTo = (player.position + Vector3.up * 1.1f) - firePoint.position;
            if (fireTo.sqrMagnitude > 0.0001f)
            {
                Quaternion fireLook = Quaternion.LookRotation(fireTo.normalized, Vector3.up);
                firePoint.rotation = fireLook;
            }
        }
        AudioService.PlayAt(shootCue, firePoint.position, 1f);
        SpawnMuzzleFlash();

        Vector3 from = firePoint.position;
        Vector3 to = player.position + Vector3.up * 1.1f;
        Vector3 dir = (to - from).normalized;
        dir = Quaternion.Euler(Random.Range(-aimSpreadDegrees, aimSpreadDegrees), Random.Range(-aimSpreadDegrees, aimSpreadDegrees), 0f) * dir;

        Vector3 tracerEnd = from + dir * attackRange;
        if (Physics.Raycast(from, dir, out RaycastHit hit, attackRange, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            tracerEnd = hit.point;
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

        SpawnTracer(from, tracerEnd);
    }

    private void SpawnMuzzleFlash()
    {
        if (muzzlePrefab == null || firePoint == null) return;
        GameObject flash = Instantiate(muzzlePrefab, firePoint.position, firePoint.rotation, firePoint);
        MuzzleFlashOneShot oneShot = flash.GetComponent<MuzzleFlashOneShot>();
        if (oneShot == null) oneShot = flash.AddComponent<MuzzleFlashOneShot>();
        oneShot.PlayAndAutoDestroy(muzzleLifetime);
    }

    private void SpawnTracer(Vector3 from, Vector3 to)
    {
        GameObject tracerObj = tracerEffectPrefab != null
            ? Instantiate(tracerEffectPrefab, from, Quaternion.identity)
            : new GameObject("EnemyTracer");

        TracerVFX tracer = tracerObj.GetComponent<TracerVFX>();
        if (tracer == null) tracer = tracerObj.AddComponent<TracerVFX>();
        tracer.Initialize(
            from,
            to,
            tracerSpeed,
            tracerWidth,
            tracerLength,
            tracerColor,
            tracerMaterial,
            tracerFadeOut
        );
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
        private bool firingBurst;
        private Vector3 repositionTarget;
        public string Name => "Attack";
        public AttackState(GroundShooterAI ai) => this.ai = ai;

        public void Enter()
        {
            if (ai.debugState) Debug.Log($"{ai.name}: Attack");
            ai.fireReadyTime = Time.time + ai.reactionDelaySeconds;
            firingBurst = true;
            ai.stateTimer = Random.Range(ai.burstMinSeconds, ai.burstMaxSeconds);
            ai.SetAgentStoppedSafe(true);
            ai.SetAgentAutoRotationSafe(false);
        }

        public void Tick(float deltaTime)
        {
            bool hasVision = ai.CanSeePlayer();
            bool shouldSearch = ai.SeenRecently(ai.searchDurationSeconds);

            if (!hasVision && !shouldSearch)
            {
                ai.stateMachine.ChangeState(ai.patrolState);
                return;
            }

            if (hasVision && Time.time >= ai.attackAllowedAfter)
            {
                ai.lastKnownPlayerPosition = ai.player.position;
            }

            ai.FacePlayer(14f);

            if (firingBurst)
            {
                ai.SetAgentStoppedSafe(true);
                if (hasVision && Time.time >= ai.attackAllowedAfter)
                    ai.TryShootAtPlayer();

                ai.stateTimer -= deltaTime;
                if (ai.stateTimer <= 0f)
                {
                    firingBurst = false;
                    ai.stateTimer = Random.Range(ai.repositionMinSeconds, ai.repositionMaxSeconds);
                    if (!ai.TryGetRepositionPoint(out repositionTarget))
                        repositionTarget = ai.transform.position;
                    ai.SetAgentStoppedSafe(false);
                    ai.SetAgentDestinationSafe(repositionTarget);
                }
                return;
            }

            ai.SetAgentStoppedSafe(false);
            ai.SetAgentDestinationSafe(repositionTarget);

            ai.stateTimer -= deltaTime;
            bool reached = ai.HasValidAgent() &&
                           !ai.agent.pathPending &&
                           ai.agent.remainingDistance <= Mathf.Max(ai.agent.stoppingDistance, 0.3f);
            if (reached || ai.stateTimer <= 0f)
            {
                firingBurst = true;
                ai.stateTimer = Random.Range(ai.burstMinSeconds, ai.burstMaxSeconds);
                ai.SetAgentStoppedSafe(true);
            }
        }

        public void Exit()
        {
            ai.SetAgentStoppedSafe(false);
            ai.SetAgentAutoRotationSafe(true);
        }
    }
}
