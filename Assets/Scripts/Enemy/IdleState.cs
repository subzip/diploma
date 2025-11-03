using UnityEngine;

public class IdleState : EnemyStateBase
{
    private float idleTime = 0f;
    private readonly float maxIdleTime = 5f;

    public IdleState(EnemyBase enemy) : base(enemy) { }

    public override void OnEnter()
    {
        idleTime = 0f;
        if (enemy.animator != null)
            enemy.animator.SetBool("IsMoving", false);

        if (enemy.agent != null)
            enemy.agent.isStopped = true;
    }

    public override void OnUpdate()
    {
        idleTime += Time.deltaTime;

        // Случайно может начать патрулировать
        if (idleTime >= maxIdleTime)
        {
            enemy.SwitchState(new PatrolState(enemy));
            return;
        }

        // Также может перейти в Alerted, если услышал звук
        // Это обрабатывается в UpdateHearing() в EnemyBase
    }

    public override void OnExit()
    {
        if (enemy.agent != null)
            enemy.agent.isStopped = false;
    }
}