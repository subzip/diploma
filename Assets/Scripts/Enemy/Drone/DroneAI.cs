// DroneAI.cs
using UnityEngine;

public class DroneAI : EnemyAI
{
    [Header("Flight Settings")]
    [SerializeField] private float flightSpeed = 3f;        // Скорость полёта
    [SerializeField] private float smoothTime = 0.2f; 
    [SerializeField] private Transform[] patrolPoints;     
    private int currentPatrolIndex = 0;

    private Vector3 destination;

    protected override void Start()
    {
        base.Start();
        // Инициализация начальной точки патрулирования
        if (patrolPoints.Length > 0)
        {
            destination = patrolPoints[currentPatrolIndex].position;
        }
    }

    private void Update()
    {
        if (isDying) { MoveToGround(); return; }
        if (health.IsDead) return;

        // Обновление состояний
        if (currentState != EnemyState.Attack && vision.CanSeePlayer())
        {
            destination = player.position;
            SwitchState(EnemyState.Attack);
        }
        else if (currentState == EnemyState.Attack && !vision.CanSeePlayer())
        {
            SwitchState(EnemyState.Patrol);
            // Вернуться к патрулированию
            if (patrolPoints.Length > 0)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                destination = patrolPoints[currentPatrolIndex].position;
            }
        }

        // Плавное перемещение к цели
        transform.position = Vector3.Lerp(transform.position, destination, flightSpeed * Time.deltaTime);

        // Обновление поворота к цели
        if (destination != transform.position)
        {
            Vector3 lookDir = (destination - transform.position).normalized;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), smoothTime);
        }

        // Обновление атаки
        if (currentState == EnemyState.Attack)
        {
            combat.Attack();
        }
    }

    // Упрощённый SwitchState (без Chase)
    public override void SwitchState(EnemyState newState)
    {
        if (newState == EnemyState.Chase) newState = EnemyState.Attack;
        base.SwitchState(newState);
    }
}