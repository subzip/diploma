using UnityEngine;

public class DeadState : EnemyStateBase
{
    public DeadState(EnemyBase enemy) : base(enemy) { }

    public override void OnEnter()
    {
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false; // Отключаем NavMeshAgent
        }
        if (enemy.animator != null)
        {
            enemy.animator.SetBool("IsDead", true);
            // Запуск анимации смерти
        }
      
    }

    public override void OnUpdate()
    {
        // Ничего не делаем
    }

    public override void OnExit()
    {
        // Ничего не делаем
    }
}