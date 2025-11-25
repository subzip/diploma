// PlayerHealth.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;


public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float healthRegenDelay = 7f;
    [SerializeField] private float healthRegenRate = 15f;
    [SerializeField] private Slider healthSlider;

    [Header("Visual Feedback")]
    [SerializeField] private Image damageOverlayLeft;
    [SerializeField] private Image damageOverlayRight;
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

        if (canRegen)
        {
            currentHealth += healthRegenRate * Time.deltaTime;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        }
        else if (Time.time - lastDamageTime > healthRegenDelay)
        {
            canRegen = true;
        }

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
            damageOverlayLeft.color = new Color(1, 0, 0, 1f);
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
        DeathScreen deathScreen = FindObjectOfType<DeathScreen>();

        if (deathScreen != null)
        {
            deathScreen.ShowDeathScreen();
        }
        else
        {
            Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
}