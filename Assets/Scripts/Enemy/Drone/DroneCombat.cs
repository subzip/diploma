
using UnityEngine;

[System.Obsolete("Legacy drone combat. Use FlyingDrone instead.")]
public class DroneCombat : EnemyCombat
{
    [Header("Drone Specific")]
    [SerializeField] private float laserRange = 20f;
    [SerializeField] private float laserDamage = 15f;
    [SerializeField] private float fireRate = 0.8f;
    [SerializeField] private ParticleSystem laserVFX;

    private void Awake()
    {
        Debug.LogWarning($"{name}: DroneCombat is legacy. Prefer FlyingDrone for active drone behavior.");
        attackRange = laserRange;
        attackCooldown = fireRate;
        attackDamage = Mathf.RoundToInt(laserDamage);
    }

    public override void Attack()
    {
        if (DeathScreen.GlobalDeathActive) return;
        ResolvePlayerRef();

        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;

            if (laserVFX != null) laserVFX.Play();

            if (player != null && Vector3.Distance(transform.position, player.position) <= laserRange)
            {
                if (player.TryGetComponent<IDamageable>(out IDamageable target))
                {
                    target.TakeDamage(laserDamage, player.position);
                }
            }
        }
    }

    public override bool IsInAttackRange()
    {
        ResolvePlayerRef();
        if (player == null) return false;
        return Vector3.Distance(transform.position, player.position) <= laserRange;
    }
}
