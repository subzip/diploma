using UnityEngine;

public class IdleState : EnemyStateBase
{
    private float idleTime = 0f;
    private readonly float maxIdleTime = 5f;

    public IdleState(EnemyBase enemy) : base(enemy) { }

    public override void OnEnter()
    {
        idleTime = 0f;
        enemy.animator.SetBool("IsMoving", false);
        // Можно добавить анимацию "осматривается"
    }

    public override void OnUpdate()
    {
        idleTime += Time.deltaTime;

        // Случайно может начать патрулировать
        if (idleTime >= maxIdleTime)
        {
            enemy.SwitchState(EnemyState.Patrol);
            return;
        }

        // Также может перейти в Alerted, если услышал звук
        // Но это обрабатывается в UpdateHearing() в EnemyBase
    }

    public override void OnExit()
    {
        // Очистка, если нужно
    }
}