using UnityEngine;

public class ChaseState : EnemyStateBase
{
    public ChaseState(EnemyBase enemy) : base(enemy) { }

    public override void OnEnter()
    {
        if (agent != null)
        {
            agent.speed = enemy.chaseSpeed;
            agent.isStopped = false;
        }
        if (enemy.animator != null)
        {
            enemy.animator.SetBool("IsRunning", true);
            enemy.animator.SetBool("IsChasing", true);
        }
    }

    public override void OnUpdate()
    {
        if (enemy.player != null)
        {
       
            agent.SetDestination(enemy.player.position);

          
            if (IsPlayerInCover())
            {
                enemy.SwitchState(new SearchState(enemy));
            }
            else if (Vector3.Distance(enemy.transform.position, enemy.player.position) <= enemy.attackRange)
            {
                enemy.SwitchState(new AttackState(enemy)); 
            }
            else if (!enemy.IsPlayerInFOV()) 
            {
                enemy.lastKnownPlayerPosition = enemy.player.position; 
                enemy.SwitchState(new SearchState(enemy));
            }
        }
    }

    public override void OnExit()
    {
        if (enemy.animator != null)
        {
            enemy.animator.SetBool("IsChasing", false);
        }
    }

    private bool IsPlayerInCover()
    {
       
        if (enemy.player == null) return false;

        Vector3 directionToPlayer = enemy.player.position - enemy.transform.position;
        if (Physics.Raycast(enemy.transform.position + Vector3.up * 1.5f, directionToPlayer.normalized, out RaycastHit hit, directionToPlayer.magnitude))
        {
            if (!hit.collider.CompareTag("Player") && !hit.collider.CompareTag("IgnoreRaycast"))
            {
                return true; 
            }
        }
        return false;
    }
}