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
        ResolvePlayerRef(force: true);
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent!");
        }
    }

    public void UpdateChase()
    {
        if (DeathScreen.GlobalDeathActive) return;
        ResolvePlayerRef();

        if (player != null && agent != null)
        {
            if (agent.destination != player.position)
                agent.SetDestination(player.position);
        }
    }

    public virtual void Attack()
    {
        if (DeathScreen.GlobalDeathActive) return;
        ResolvePlayerRef();

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
        ResolvePlayerRef();
        if (player == null) return false;
        return Vector3.Distance(transform.position, player.position) <= attackRange;
    }

    protected void ResolvePlayerRef(bool force = false)
    {
        if (!force && player != null) return;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        player = playerObj != null ? playerObj.transform : null;
    }
}
