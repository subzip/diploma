using UnityEngine;

public class AttackState : EnemyStateBase
{
    private float lastAttackTime = 0f;
    private readonly float attackCooldown = 1f;

    public AttackState(EnemyBase enemy) : base(enemy) { }

    public override void OnEnter()
    {
        if (agent != null)
        {
            agent.isStopped = true;
        }
        if (enemy.animator != null)
        {
            enemy.animator.SetBool("IsInAttackRange", true);
        }
    }

    public override void OnUpdate()
    {
        if (enemy.player != null)
        {
            // Поворачиваемся к игроку
            Vector3 directionToPlayer = (enemy.player.position - enemy.transform.position).normalized;
            directionToPlayer.y = 0f; // Игнорируем высоту
            if (directionToPlayer != Vector3.zero)
            {
                enemy.transform.rotation = Quaternion.LookRotation(directionToPlayer);
            }

            // Имитация выстрела (замените на реальную систему стрельбы)
            if (Time.time > lastAttackTime + attackCooldown)
            {
                ShootAtPlayer();
                lastAttackTime = Time.time;
            }

            // Проверяем, ушел ли игрок
            if (Vector3.Distance(enemy.transform.position, enemy.player.position) > enemy.attackRange * 1.5f)
            {
                enemy.SwitchState(new ChaseState(enemy));
            }
        }
    }

    public override void OnExit()
    {
        if (enemy.animator != null)
        {
            enemy.animator.SetBool("IsInAttackRange", false);
        }
    }

    private void ShootAtPlayer()
    {
        // Звук выстрела
        // Анимация выстрела
        // Логика урона игроку
        Debug.Log($"{enemy.name} стреляет в игрока!");
    }
}