// EnemyCombat.cs
using UnityEngine;
using UnityEngine.AI;

public class EnemyCombat : MonoBehaviour
{
    [SerializeField] protected float attackRange = 3f;
    [SerializeField] protected float attackCooldown = 1.5f;
    [SerializeField] protected int attackDamage = 25;
    [SerializeField] protected LayerMask playerLayer;

    protected Transform player;
    protected NavMeshAgent agent;
    protected float lastAttackTime;

    protected void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent!");
        }
    }

    public void UpdateChase()
    {
        if (player != null && agent != null)
        {
            agent.SetDestination(player.position);
        }
    }

    public virtual void Attack()
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

    public virtual bool IsInAttackRange()
    {
        if (player == null) return false;
        return Vector3.Distance(transform.position, player.position) <= attackRange;
    }
}