// EnemyCombat.cs
using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private int attackDamage = 25;
    [SerializeField] private LayerMask playerLayer;

    private Transform player;
    private float lastAttackTime;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    public void UpdateChase()
    {
        // Может быть расширен для движения к игроку
        // Пока используем только зрение из EnemyVision
    }

    public void Attack()
    {
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;

            Collider[] hits = Physics.OverlapSphere(transform.position, attackRange, playerLayer);
            foreach (Collider col in hits)
            {
                if (col.TryGetComponent<IDamageable>(out IDamageable target))
                {
                    target.TakeDamage(attackDamage, transform.position);
                    break;
                }
            }
        }
    }

    public bool IsInAttackRange()
    {
        if (player == null) return false;
        return Vector3.Distance(transform.position, player.position) <= attackRange;
    }
}