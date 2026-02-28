// EnemyPatrol.cs
using UnityEngine;
using UnityEngine.AI;

public class EnemyPatrol : MonoBehaviour
{
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float waitTime = 2f;

    private NavMeshAgent agent;
    private int currentPointIndex = 0;
    private float arriveTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        if (agent != null && agent.enabled && patrolPoints.Length > 0)
        {
            agent.SetDestination(patrolPoints[0].position);
            arriveTime = Time.time;
        }
    }

    public void UpdatePatrol()
    {
        if (patrolPoints.Length == 0) return;
        if (agent == null || !agent.enabled) return;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (Time.time - arriveTime > waitTime)
            {
                currentPointIndex = (currentPointIndex + 1) % patrolPoints.Length;
                agent.SetDestination(patrolPoints[currentPointIndex].position);
                arriveTime = Time.time;
            }
        }
        else
        {
            arriveTime = Time.time;
        }
    }
}
