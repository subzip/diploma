using UnityEngine;
using UnityEngine.AI;
using System.Linq;

public abstract class EnemyBase : MonoBehaviour
{
    [Header("Настройки ИИ")]
    public float hearingRadius = 15f;
    public float visionRange = 20f;
    public float fov = 90f;
    public float attackRange = 3f;
    public float patrolSpeed = 2f;
    public float chaseSpeed = 3.5f;

    [Header("Ссылки")]
    public Transform player;
    public NavMeshAgent agent;
    public Animator animator;

    
    protected EnemyStateBase currentState;
    protected EnemyState previousState = EnemyState.Idle;
    protected EnemyState previousStateEnum = EnemyState.Idle;

      protected float stateStartTime;
    public Vector3 lastKnownPlayerPosition;
    public Vector3 investigatePosition;
    protected float hearingCooldown = 0f;

  
    public System.Action OnDie;
    public System.Action OnSpotPlayer;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        
        if (agent != null)
        {
            agent.speed = patrolSpeed;
        }
    }

    protected virtual void Start()
    {
     
        SwitchState(new IdleState(this));
    }

    protected virtual void Update()
    {
     
        currentState?.OnUpdate();

        UpdateVision();
    }

    
    protected abstract void UpdateVision();

    
    public void SwitchState(EnemyStateBase newState)
    {
        if (newState == currentState) return;

        currentState?.OnExit();

      
        if (currentState is IdleState) previousStateEnum = EnemyState.Idle;
        else if (currentState is PatrolState) previousStateEnum = EnemyState.Patrol;
        else if (currentState is ChaseState) previousStateEnum = EnemyState.Chase;
     

        currentState = newState;
        stateStartTime = Time.time;
        currentState.OnEnter();
    }

   
    protected virtual void UpdateHearing()
    {
        if (hearingCooldown > 0)
        {
            hearingCooldown -= Time.deltaTime;
            return;
        }

    }

    public virtual void HearSound(Vector3 position, string type)
    {
        if (currentState.GetType().Name == "DeadState") return;

      
        if (Vector3.Distance(transform.position, position) <= hearingRadius)
        {
            investigatePosition = position;
            SwitchState(new AlertedState(this));
        }
    }

    
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
        SwitchState(new DeadState(this));
        OnDie?.Invoke();
        
    }


    private void OnDrawGizmosSelected()
    {
  
        if (fov > 0 && visionRange > 0)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, visionRange);
        }
   
        if (hearingRadius > 0)
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(transform.position, hearingRadius);
        }
        
        if (investigatePosition != Vector3.zero)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(investigatePosition, 0.3f);
        }
    }
}