using UnityEngine;

public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float deathAnimationTime = 0.5f;

    private float currentHealth;
    private bool isDead = false;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage, Vector3 hitPoint)
    {
        // Не обрабатываем урон, если враг уже умер
        if (isDead) return;

        // Уменьшаем здоровье
        currentHealth -= damage;

        // Логируем получение урона
        Debug.Log($"💥 Враг [{gameObject.name}] получил {damage} урона. Здоровье: {currentHealth:F1}/{maxHealth}");

        // Проверяем, умер ли враг
        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;

        // Логируем смерть
        Debug.Log($"💀 Враг [{gameObject.name}] уничтожен!");

        // Добавляем небольшую задержку для визуального эффекта
        StartCoroutine(DeathRoutine());
    }

    private System.Collections.IEnumerator DeathRoutine()
    {
        // Можно добавить визуальный эффект смерти здесь
        yield return new WaitForSeconds(deathAnimationTime);

        // Уничтожаем объект врага
        Destroy(gameObject);
    }

    // Метод для отладки: отображение здоровья во время игры
    private void OnGUI()
    {
        if (!isDead)
        {
            Vector3 screenPoint = Camera.main.WorldToScreenPoint(transform.position);
            if (screenPoint.z > 0)
            {
                // Отображаем здоровье над головой врага
                Rect healthBar = new Rect(screenPoint.x - 50, Screen.height - screenPoint.y - 20, 100, 10);
                float healthPercentage = Mathf.Clamp01(currentHealth / maxHealth);

                GUI.color = Color.red;
                GUI.DrawTexture(healthBar, Texture2D.whiteTexture);

                GUI.color = Color.green;
                GUI.DrawTexture(new Rect(healthBar.x, healthBar.y, healthBar.width * healthPercentage, healthBar.height), Texture2D.whiteTexture);

                GUI.color = Color.white;
                GUI.Label(new Rect(screenPoint.x - 15, Screen.height - screenPoint.y - 35, 30, 20), $"{(int)currentHealth}");
            }
        }
    }
    
    public bool IsDead() => isDead;
}