// DroneAI.cs
using UnityEngine;

public class DroneAI : EnemyAI
{
    private void Update()
    {
        if (isDying) { MoveToGround(); return; }
        if (health.IsDead) return;

        // Только два состояния: Patrol и Attack
        if (currentState != EnemyState.Attack && vision.CanSeePlayer())
        {
            lastKnownPlayerPosition = player.position;
            SwitchState(EnemyState.Attack);
        }
        else if (currentState == EnemyState.Attack && !vision.CanSeePlayer())
        {
            SwitchState(EnemyState.Patrol);
        }

        // Обновление состояний
        switch (currentState)
        {
            case EnemyState.Patrol:
                patrol.UpdatePatrol();
                break;

            case EnemyState.Attack:
                combat.UpdateChase(); // Летит к игроку
                if (combat.IsInAttackRange())
                {
                    combat.Attack(); // Стреляет
                }
                break;
        }
    }


    public override void SwitchState(EnemyState newState)
    {
        // Запрещаем состояние Chase
        if (newState == EnemyState.Chase) newState = EnemyState.Attack;
        base.SwitchState(newState);
    }
}