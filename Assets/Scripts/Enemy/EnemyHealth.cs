// Assets/Scripts/Enemies/EnemyHealth.cs
using UnityEngine;
using UnityEngine.Events;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] private EnemyStats stats;

    [Header("Events")]
    public UnityEvent OnDeath;

    private int currentHealth;
    private bool isDead = false;

    public bool IsDead => isDead;

    private void Awake()
    {
        if (stats == null)
        {
            Debug.LogError($"[{name}] EnemyStats не назначен!");
            enabled = false;
            return;
        }

        currentHealth = stats.maxHealth;
    }

    public void TakeDamage(float damage, Vector3 hitPoint)
    {
        if (isDead) return;

        currentHealth -= Mathf.RoundToInt(damage);
        Debug.Log($"[{name}] Получено {damage} урона. Здоровье: {currentHealth}/{stats.maxHealth}");

        if (currentHealth <= 0 && !isDead)
        {
            Die(hitPoint);
        }
    }

    private void Die(Vector3 deathPosition)
    {
        isDead = true;

        // Эффект смерти
        if (stats.deathEffectPrefab != null)
        {
            Instantiate(stats.deathEffectPrefab, deathPosition, Quaternion.identity);
        }

        // Звук смерти
        if (stats.deathSound != null)
        {
            AudioSource.PlayClipAtPoint(stats.deathSound, transform.position);
        }

        // Событие смерти (для очков, триггеров и т.д.)
        OnDeath?.Invoke();

        // Удаление через задержку
        Destroy(gameObject, stats.deathDelay);
    }
}