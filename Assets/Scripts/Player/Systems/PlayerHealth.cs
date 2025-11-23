// PlayerHealth.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;


public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float healthRegenDelay = 7f; // Задержка перед восстановлением
    [SerializeField] private float healthRegenRate = 15f; // Скорость восстановления в секунду
    [SerializeField] private Slider healthSlider;

    [Header("Visual Feedback")]
    [SerializeField] private Image damageOverlayLeft;  // Красный прямоугольник слева
    [SerializeField] private Image damageOverlayRight; // Красный прямоугольник справа
    [SerializeField] private float damageFlashDuration = 0.3f;

    private float currentHealth;
    private bool isDead = false;
    private float lastDamageTime;
    private bool canRegen = false;

    

    private void Awake()
    {
        currentHealth = maxHealth;
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        damageOverlayLeft.canvasRenderer.SetAlpha(0);
        damageOverlayRight.canvasRenderer.SetAlpha(0);
    }

    private void Update()
    {
        if (isDead) return;

        // Восстановление здоровья
        if (canRegen)
        {
            currentHealth += healthRegenRate * Time.deltaTime;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        }
        else if (Time.time - lastDamageTime > healthRegenDelay)
        {
            canRegen = true;
        }

        // Обновление UI
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }
    }

    public void TakeDamage(float damage, Vector3 hitPoint)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);
        lastDamageTime = Time.time;
        canRegen = false;

        // Визуальный эффект урона
        ShowDamageOverlay();

        // Проверка смерти
        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
    }

    private void ShowDamageOverlay()
    {
        if (damageOverlayLeft != null)
        {
            damageOverlayLeft.color = new Color(1, 0, 0, 1f); // Красный 50% прозрачности
            damageOverlayLeft.CrossFadeAlpha(1, damageFlashDuration, true);
            //yield return new WaitForSeconds(damageFlashDuration);
        }
        if (damageOverlayRight != null)
        {
            damageOverlayRight.color = new Color(1, 0, 0, 1f);
            damageOverlayRight.CrossFadeAlpha(1, damageFlashDuration, true);
            //yield return new WaitForSeconds(damageFlashDuration);
        }

        damageOverlayLeft.CrossFadeAlpha(0, damageFlashDuration, true);
        damageOverlayRight.CrossFadeAlpha(0, damageFlashDuration, true);

    }

    private void Die()
    {
        isDead = true;
        healthSlider.value = 0;
        Debug.Log("Игрок мёртв!");
        // Позже можно добавить: экран смерти, перезапуск уровня и т.д.
        Time.timeScale = 0;
    }
}