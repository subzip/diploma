using UnityEngine;

public class AlertedState : EnemyStateBase
{
    private float stateStartTime;
    private readonly float alertDuration = 5f;

    public AlertedState(EnemyBase enemy) : base(enemy) { }

    public override void OnEnter()
    {
        stateStartTime = Time.time;
        if (agent != null)
        {
            agent.isStopped = true;
        }
        if (enemy.animator != null)
        {
            enemy.animator.SetBool("IsAlerted", true);
        }
    }

    public override void OnUpdate()
    {
        // Поворачиваемся в сторону звука
        if (enemy.investigatePosition != Vector3.zero)
        {
            Vector3 directionToSound = (enemy.investigatePosition - enemy.transform.position).normalized;
            directionToSound.y = 0f;
            if (directionToSound != Vector3.zero)
            {
                enemy.transform.rotation = Quaternion.LookRotation(directionToSound);
            }
        }

        // Если прошло достаточно времени
        if (Time.time > stateStartTime + alertDuration)
        {
            if (enemy.IsPlayerInFOV())
            {
                enemy.SwitchState(new ChaseState(enemy));
            }
            else
            {
                enemy.SwitchState(new PatrolState(enemy));
            }
        }
    }

    public override void OnExit()
    {
        if (enemy.animator != null)
        {
            enemy.animator.SetBool("IsAlerted", false);
        }
    }
}