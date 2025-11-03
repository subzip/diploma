using UnityEngine;

public class SearchState : EnemyStateBase
{
    private float stateStartTime;
    private readonly float searchDuration = 10f;

    public SearchState(EnemyBase enemy) : base(enemy) { }

    public override void OnEnter()
    {
        stateStartTime = Time.time;
        if (agent != null)
        {
            agent.isStopped = false;
            agent.speed = enemy.patrolSpeed * 0.7f; // Медленнее при поиске
        }
        if (enemy.animator != null)
        {
            enemy.animator.SetBool("IsSearching", true);
        }
    }

    public override void OnUpdate()
    {
        // Двигаемся к последней известной позиции игрока
        agent.SetDestination(enemy.lastKnownPlayerPosition);

        // Если пришли к позиции и не видим игрока
        if (Vector3.Distance(enemy.transform.position, enemy.lastKnownPlayerPosition) < agent.stoppingDistance)
        {
            // Проверяем, может, игрок появился
            if (enemy.IsPlayerInFOV())
            {
                enemy.SwitchState(new ChaseState(enemy));
            }
            else
            {
                // Время поиска вышло
                if (Time.time > stateStartTime + searchDuration)
                {
                    enemy.SwitchState(new PatrolState(enemy));
                }
            }
        }
    }

    public override void OnExit()
    {
        if (agent != null)
        {
            agent.speed = enemy.patrolSpeed;
        }
        if (enemy.animator != null)
        {
            enemy.animator.SetBool("IsSearching", false);
        }
    }
}