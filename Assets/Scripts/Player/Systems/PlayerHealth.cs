using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 150f;
    [SerializeField] private Slider healthSlider;

    [Header("Damage Feedback")]
    [SerializeField] private BloodSplatUI bloodSplatUI;

    private float currentHealth;
    private bool isDead;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;

    public event Action<float, float> OnHealthChanged;
    public event Action<float, Vector3> OnDamaged;

    private void Awake()
    {
        currentHealth = maxHealth;
        if (bloodSplatUI == null) bloodSplatUI = FindObjectOfType<BloodSplatUI>();
        UpdateHealthUI();
    }

    public void TakeDamage(float damage, Vector3 hitPoint)
    {
        if (isDead || damage <= 0f) return;

        currentHealth = Mathf.Max(currentHealth - damage, 0f);
        UpdateHealthUI();

        OnDamaged?.Invoke(damage, hitPoint);
        bloodSplatUI?.ShowDamage(damage, currentHealth / Mathf.Max(0.001f, maxHealth));

        if (currentHealth <= 0f && !isDead)
        {
            Die();
        }
    }

    public bool TryHeal(float amount)
    {
        if (isDead || amount <= 0f || currentHealth >= maxHealth) return false;
        Heal(amount);
        return true;
    }

    public void Heal(float amount)
    {
        if (isDead || amount <= 0f) return;

        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        UpdateHealthUI();
    }

    private void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        isDead = true;
        if (healthSlider != null) healthSlider.value = 0f;

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
