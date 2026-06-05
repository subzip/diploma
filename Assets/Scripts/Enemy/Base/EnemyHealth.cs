
using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private EnemyAI enemyAI;

    private int currentHealth;
    private bool isDead = false;

    public bool IsDead => isDead;

    public void SetDead(bool state)
    {
        isDead = state;
    }

    private void Awake()
    {
        if (enemyAI == null) enemyAI = GetComponent<EnemyAI>();
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage, Vector3 hitPoint)
    {
        if (isDead) return;

        currentHealth -= Mathf.RoundToInt(damage);
        if (currentHealth <= 0)
        {
            isDead = true;
            enemyAI.Die();
        }
    }
}