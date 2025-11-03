using UnityEngine;

public class StigmarEnemy : EnemyBase
{
    [Header("Stigmar-Specific")]
    public float specialAttackRange = 5f;

    protected override void UpdateVision()
    {
        if (player == null) return;

        Vector3 directionToPlayer = player.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;

        if (distanceToPlayer <= visionRange)
        {
            float angle = Vector3.Angle(transform.forward, directionToPlayer);

            if (angle <= fov * 0.5f)
            {
                // Raycast для проверки преград
                if (Physics.Raycast(transform.position + Vector3.up * 1.5f, directionToPlayer.normalized, out RaycastHit hit, visionRange))
                {
                    if (hit.collider.CompareTag("Player"))
                    {
                        // Игрок виден!
                        lastKnownPlayerPosition = player.position;
                        OnSpotPlayer?.Invoke(); // Сигнал о том, что увидели

                        // Переход в ChaseState
                        if (currentState.GetType().Name != "ChaseState" && currentState.GetType().Name != "AttackState")
                        {
                            SwitchState(new ChaseState(this));
                        }
                    }
                }
            }
        }
    }
}