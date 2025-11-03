using UnityEngine;
using UnityEngine.AI;

public abstract class EnemyBase : MonoBehaviour
{
    [Header("Настройки ИИ")]
    public float hearingRadius = 15f;
    public float visionRange = 20f;
    public float fov = 90f;
    public float attackRange = 10f;
    public float chaseSpeed = 3.5f;
    public float patrolSpeed = 2f;

    [Header("Ссылки")]
    public Transform player;
    public NavMeshAgent agent;
    public Animator animator;

    // Текущее состояние
    protected EnemyState currentState = EnemyState.Idle;
    protected EnemyState previousState = EnemyState.Idle;

    // Временные переменные
    protected float stateStartTime;
    protected Vector3 lastKnownPlayerPosition;
    protected Vector3 investigatePosition;
    protected float hearingCooldown = 0f;

    // События
    public System.Action OnDie;
    public System.Action OnSpotPlayer;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    protected virtual void Start()
    {
        SwitchState(EnemyState.Idle);
    }

    protected virtual void Update()
    {
        UpdateHearing();
        UpdateVision();
        UpdateCurrentState();
    }

    // Метод, который будет переопределять каждый конкретный тип врага
    protected abstract void UpdateCurrentState();

    // Переключение состояния
    public void SwitchState(EnemyState newState)
    {
        if (newState == currentState) return;

        OnStateExit(currentState);
        previousState = currentState;
        currentState = newState;
        stateStartTime = Time.time;
        OnStateEnter(newState);
    }

    public Transform[] GetPatrolPoints()
    {
        return GetComponentsInChildren<Transform>()
            .Where(t => t.CompareTag("PatrolPoint"))
            .ToArray();
    }

    protected virtual void OnStateEnter(EnemyState state)
    {
        //Debug.Log($"[{gameObject.name}] Входит в состояние: {state}");
    }

    protected virtual void OnStateExit(EnemyState state)
    {
        //Debug.Log($"[{gameObject.name}] Выходит из состояния: {state}");
    }

    // --- СИСТЕМА СЛЫШИМОСТИ ---
    protected virtual void UpdateHearing()
    {
        if (hearingCooldown > 0)
        {
            hearingCooldown -= Time.deltaTime;
            return;
        }

        // Логика "слышит ли враг звук"
        // Это может быть вызвано извне через событие SoundManager.EmitSound(...)
    }

    public virtual void HearSound(Vector3 position, string type)
    {
        if (currentState == EnemyState.Dead) return;

        // Пример логики:
        if (Vector3.Distance(transform.position, position) <= hearingRadius)
        {
            investigatePosition = position;
            if (currentState != EnemyState.Chase && currentState != EnemyState.Attack)
            {
                SwitchState(EnemyState.Alerted);
            }
        }
    }

    // --- СИСТЕМА ЗРЕНИЯ ---
    protected virtual void UpdateVision()
    {
        if (player == null) return;

        Vector3 directionToPlayer = player.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;

        if (distanceToPlayer > visionRange) return;

        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        if (angle <= fov * 0.5f)
        {
            // Raycast для проверки преград
            if (Physics.Raycast(transform.position + Vector3.up * 1.5f, directionToPlayer.normalized, out RaycastHit hit, visionRange))
            {
                if (hit.collider.CompareTag("Player"))
                {
                    OnSeePlayer();
                }
            }
        }
    }

    protected virtual void OnSeePlayer()
    {
        if (currentState == EnemyState.Dead) return;

        lastKnownPlayerPosition = player.position;
        OnSpotPlayer?.Invoke();

        if (currentState != EnemyState.Chase && currentState != EnemyState.Attack)
        {
            SwitchState(EnemyState.Chase);
        }
    }

    // --- ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ---
    public bool IsPlayerInFOV()
    {
        if (player == null) return false;

        Vector3 directionToPlayer = player.position - transform.position;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        return angle <= fov * 0.5f;
    }

    public bool IsPlayerInRange(float range)
    {
        if (player == null) return false;
        return Vector3.Distance(transform.position, player.position) <= range;
    }

    public void Die()
    {
        SwitchState(EnemyState.Dead);
        OnDie?.Invoke();
        // Отключение агента, анимация смерти и т.д.
    }
}