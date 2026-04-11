// DroneAI.cs
using UnityEngine;

[System.Obsolete("Legacy drone AI. Use FlyingDrone instead.")]
public class DroneAI : EnemyAI
{
    [Header("Flight Settings")]
    [SerializeField] private float flightSpeed = 3f;
    [SerializeField] private float smoothTime = 0.2f; 
    [SerializeField] private Transform[] patrolPoints;     
    private int currentPatrolIndex = 0;

    private Vector3 destination;

    protected override void Start()
    {
        base.Start();
        Debug.LogWarning($"{name}: DroneAI is legacy. Prefer FlyingDrone for active drone behavior.");
        if (patrolPoints.Length > 0)
        {
            destination = patrolPoints[currentPatrolIndex].position;
        }
    }

    private void Update()
    {
        if (isDying) { MoveToGround(); return; }
        if (health.IsDead) return;

        if (currentState != EnemyState.Attack && vision.CanSeePlayer())
        {
            destination = player.position;
            SwitchState(EnemyState.Attack);
        }
        else if (currentState == EnemyState.Attack && !vision.CanSeePlayer())
        {
            SwitchState(EnemyState.Patrol);

            if (patrolPoints.Length > 0)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                destination = patrolPoints[currentPatrolIndex].position;
            }
        }

        transform.position = Vector3.Lerp(transform.position, destination, flightSpeed * Time.deltaTime);

        if (destination != transform.position)
        {
            Vector3 lookDir = (destination - transform.position).normalized;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), smoothTime);
        }

        if (currentState == EnemyState.Attack)
        {
            combat.Attack();
        }
    }

    public override void SwitchState(EnemyState newState)
    {
        if (newState == EnemyState.Chase) newState = EnemyState.Attack;
        base.SwitchState(newState);
    }
}
