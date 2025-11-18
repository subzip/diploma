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

    public void UpdatePatrol()
    {
        if (patrolPoints.Length == 0) return;

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
            arriveTime = Time.time; // Обновляем время при движении
        }
    }
}