using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 150f;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private string healthSliderObjectName = "Health";

    [Header("Damage Feedback")]
    [SerializeField] private BloodSplatUI bloodSplatUI;

    private float currentHealth;
    private bool isDead;
    private DeathCycleManager cycleManager;
    private DeathScreen deathScreen;
    private float nextReferenceResolveTime;
    private float nextHealthSliderResolveTime;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;

    public event Action<float, float> OnHealthChanged;
    public event Action<float, Vector3> OnDamaged;

    private void Awake()
    {
        currentHealth = maxHealth;
        ResolveReferences(force: true);
        ResolveHealthSliderIfNeeded();
        UpdateHealthUI();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
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

    public void ResetForRespawn()
    {
        isDead = false;
        currentHealth = maxHealth;
        ResolveHealthSliderIfNeeded(force: true);
        UpdateHealthUI();
    }

    private void UpdateHealthUI()
    {
        ResolveHealthSliderIfNeeded();

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

        ResolveReferences(force: true);
        if (cycleManager != null)
        {
            cycleManager.RegisterDeath(DeathKind.Combat, GetDeathZoneId());
        }

        if (deathScreen != null)
        {
            deathScreen.ShowDeathScreen();
        }
        else
        {
            ForceDeathFallbackWithoutUi();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResolveReferences(force: true);
        ResolveHealthSliderIfNeeded(force: true);
        UpdateHealthUI();
    }

    private void ResolveReferences(bool force = false)
    {
        if (!force && Time.unscaledTime < nextReferenceResolveTime) return;

        if (bloodSplatUI == null || force) bloodSplatUI = FindObjectOfType<BloodSplatUI>();
        if (cycleManager == null || force) cycleManager = DeathCycleManager.Instance != null ? DeathCycleManager.Instance : FindObjectOfType<DeathCycleManager>();
        if (deathScreen == null || force) deathScreen = DeathScreen.Instance != null ? DeathScreen.Instance : FindObjectOfType<DeathScreen>();

        nextReferenceResolveTime = Time.unscaledTime + 0.5f;
    }

    private void ResolveHealthSliderIfNeeded(bool force = false)
    {
        if (!force && healthSlider == null && Time.unscaledTime < nextHealthSliderResolveTime) return;
        if (!force && healthSlider != null) return;

        Slider[] sliders = FindObjectsOfType<Slider>(true);
        Slider fallback = null;

        for (int i = 0; i < sliders.Length; i++)
        {
            Slider slider = sliders[i];
            if (slider == null) continue;

            string lower = slider.name.ToLowerInvariant();
            if (lower == "health" || lower.Contains("health"))
            {
                healthSlider = slider;
                nextHealthSliderResolveTime = 0f;
                return;
            }

            if (fallback == null && lower.Contains("hp"))
            {
                fallback = slider;
            }
        }

        if (fallback != null)
        {
            healthSlider = fallback;
            nextHealthSliderResolveTime = 0f;
            return;
        }

        if (!string.IsNullOrWhiteSpace(healthSliderObjectName))
        {
            GameObject byName = GameObject.Find(healthSliderObjectName);
            if (byName != null)
            {
                healthSlider = byName.GetComponent<Slider>();
            }
        }

        if (healthSlider == null)
            nextHealthSliderResolveTime = Time.unscaledTime + 0.5f;
    }

    private void ForceDeathFallbackWithoutUi()
    {
        DeathScreen.SetGlobalDeathActive(true);
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        DisableBehaviour<PlayerMovement>();
        DisableBehaviour<PlayerLook>();
        DisableBehaviour<PlayerCrouch>();
        DisableBehaviour<PlayerNeuroresist>();
        DisableBehaviour<AimController>();
        DisableBehaviour<SwayNBobScript>();

        WeaponManager wm = GetComponentInChildren<WeaponManager>(true);
        if (wm != null) wm.enabled = false;
    }

    private void DisableBehaviour<T>() where T : Behaviour
    {
        T component = GetComponentInChildren<T>(true);
        if (component != null) component.enabled = false;
    }

    private string GetDeathZoneId()
    {
        string scene = SceneManager.GetActiveScene().name;
        string id = RespawnCheckpointState.GetCheckpointId(scene);
        if (!string.IsNullOrWhiteSpace(id)) return id;
        return scene;
    }
}
