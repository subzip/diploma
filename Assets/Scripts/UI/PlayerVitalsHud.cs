using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerVitalsHud : MonoBehaviour
{
    private enum NeuroMode
    {
        ActiveDurationOnly,
        ReadinessAndDuration
    }

    [System.Serializable]
    private class HudBar
    {
        public string label = "Bar";
        public Image fillImage;
        public RectTransform fillTransform;
        public TMP_Text valueText;
        public Image iconImage;
    }

    [Header("Sources")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerNeuroresist playerNeuroresist;

    [Header("Bars (Left Bottom)")]
    [SerializeField] private HudBar healthBar = new HudBar { label = "Health" };
    [SerializeField] private HudBar staminaBar = new HudBar { label = "Stamina" };
    [SerializeField] private HudBar neuroBar = new HudBar { label = "Neuro" };

    [Header("Neuro Display")]
    [SerializeField] private NeuroMode neuroMode = NeuroMode.ReadinessAndDuration;
    [SerializeField] private bool hideNeuroWhenNotAvailable = false;

    [Header("Behavior")]
    [SerializeField] private bool useImageFillAmount = true;
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private bool updateNumericText = true;
    [SerializeField] private bool percentText = false;
    [SerializeField] private bool hideTextForFullBars = false;

    private float currentHealth01 = 1f;
    private float targetHealth01 = 1f;
    private float currentStamina01 = 1f;
    private float targetStamina01 = 1f;
    private float currentNeuro01 = 1f;
    private float targetNeuro01 = 1f;
    private float nextResolveAttemptTime;
    private PlayerHealth boundHealth;
    private int lastHealthText = int.MinValue;
    private int lastStaminaText = int.MinValue;
    private int lastNeuroText = int.MinValue;
    private int lastHealthPercent = int.MinValue;
    private int lastStaminaPercent = int.MinValue;
    private int lastNeuroPercent = int.MinValue;
    private bool lastNeuroHideState;

    private void Awake()
    {
        ResolveSources();
        BindHealthEvents();
        SyncInstantValues();
        ApplyAllBarsInstant();
        InvalidateTextCache();
        UpdateAllTexts(force: true);
    }

    private void OnEnable()
    {
        ResolveSources();
        BindHealthEvents();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnbindHealthEvents();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (playerHealth == null || playerMovement == null || playerNeuroresist == null)
        {
            if (Time.unscaledTime >= nextResolveAttemptTime)
            {
                ResolveSources();
                BindHealthEvents();
                nextResolveAttemptTime = Time.unscaledTime + 0.5f;
            }
        }

        if (playerMovement != null)
            targetStamina01 = Mathf.Clamp01(playerMovement.CurrentStamina / Mathf.Max(0.001f, playerMovement.MaxStamina));

        if (playerNeuroresist != null)
            targetNeuro01 = GetNeuroNormalized();

        float t = Mathf.Max(0f, smoothSpeed) * Time.unscaledDeltaTime;
        currentHealth01 = Mathf.Lerp(currentHealth01, targetHealth01, t);
        currentStamina01 = Mathf.Lerp(currentStamina01, targetStamina01, t);
        currentNeuro01 = Mathf.Lerp(currentNeuro01, targetNeuro01, t);

        ApplyBar(healthBar, currentHealth01);
        ApplyBar(staminaBar, currentStamina01);
        ApplyBar(neuroBar, currentNeuro01);
        UpdateAllTexts(force: false);
    }

    private void OnHealthChanged(float current, float max)
    {
        targetHealth01 = Mathf.Clamp01(current / Mathf.Max(0.001f, max));
    }

    private void ResolveSources()
    {
        if (playerHealth == null) playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerMovement == null) playerMovement = FindObjectOfType<PlayerMovement>();
        if (playerNeuroresist == null) playerNeuroresist = FindObjectOfType<PlayerNeuroresist>();
    }

    private void BindHealthEvents()
    {
        if (boundHealth != null && boundHealth != playerHealth)
            boundHealth.OnHealthChanged -= OnHealthChanged;

        if (playerHealth == null)
        {
            boundHealth = null;
            return;
        }

        playerHealth.OnHealthChanged -= OnHealthChanged;
        playerHealth.OnHealthChanged += OnHealthChanged;
        boundHealth = playerHealth;
    }

    private void UnbindHealthEvents()
    {
        if (boundHealth == null) return;
        boundHealth.OnHealthChanged -= OnHealthChanged;
        boundHealth = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResolveSources();
        BindHealthEvents();
        SyncInstantValues();
        ApplyAllBarsInstant();
        InvalidateTextCache();
        UpdateAllTexts(force: true);
    }

    private void SyncInstantValues()
    {
        if (playerHealth != null)
            targetHealth01 = currentHealth01 = Mathf.Clamp01(playerHealth.CurrentHealth / Mathf.Max(0.001f, playerHealth.MaxHealth));

        if (playerMovement != null)
            targetStamina01 = currentStamina01 = Mathf.Clamp01(playerMovement.CurrentStamina / Mathf.Max(0.001f, playerMovement.MaxStamina));

        if (playerNeuroresist != null)
            targetNeuro01 = currentNeuro01 = GetNeuroNormalized();
    }

    private float GetNeuroNormalized()
    {
        if (playerNeuroresist == null) return 0f;
        return neuroMode == NeuroMode.ActiveDurationOnly
            ? Mathf.Clamp01(playerNeuroresist.CurrentValue / Mathf.Max(0.001f, playerNeuroresist.MaxValue))
            : Mathf.Clamp01(playerNeuroresist.Readiness01);
    }

    private void ApplyAllBarsInstant()
    {
        ApplyBar(healthBar, currentHealth01);
        ApplyBar(staminaBar, currentStamina01);
        ApplyBar(neuroBar, currentNeuro01);
    }

    private void ApplyBar(HudBar bar, float normalized)
    {
        if (bar == null) return;
        normalized = Mathf.Clamp01(normalized);

        if (useImageFillAmount && bar.fillImage != null)
        {
            bar.fillImage.fillAmount = normalized;
        }
        else if (bar.fillTransform != null)
        {
            Vector3 scale = bar.fillTransform.localScale;
            scale.x = normalized;
            bar.fillTransform.localScale = scale;
        }

        if (bar.iconImage != null)
        {
            float alpha = Mathf.Lerp(0.45f, 1f, normalized);
            Color c = bar.iconImage.color;
            c.a = alpha;
            bar.iconImage.color = c;
        }
    }

    private void UpdateAllTexts(bool force)
    {
        if (!updateNumericText) return;

        int healthRaw = playerHealth != null ? Mathf.RoundToInt(playerHealth.CurrentHealth) : 0;
        int staminaRaw = playerMovement != null ? Mathf.RoundToInt(playerMovement.CurrentStamina) : 0;
        int healthPercent = Mathf.RoundToInt(targetHealth01 * 100f);
        int staminaPercent = Mathf.RoundToInt(targetStamina01 * 100f);

        UpdateBarText(healthBar, targetHealth01, healthRaw, healthPercent, ref lastHealthText, ref lastHealthPercent, force);
        UpdateBarText(staminaBar, targetStamina01, staminaRaw, staminaPercent, ref lastStaminaText, ref lastStaminaPercent, force);

        if (neuroBar != null && neuroBar.valueText != null)
        {
            bool hideNeuro = hideNeuroWhenNotAvailable && playerNeuroresist != null && !playerNeuroresist.IsActive && playerNeuroresist.IsReady;
            if (hideNeuro)
            {
                if (force || !lastNeuroHideState || !string.IsNullOrEmpty(neuroBar.valueText.text))
                    neuroBar.valueText.text = string.Empty;
            }
            else
            {
                int neuroRaw = playerNeuroresist != null
                    ? Mathf.RoundToInt(playerNeuroresist.IsActive ? playerNeuroresist.CurrentValue : playerNeuroresist.CooldownRemaining)
                    : 0;
                int neuroPercent = Mathf.RoundToInt(targetNeuro01 * 100f);
                UpdateBarText(neuroBar, targetNeuro01, neuroRaw, neuroPercent, ref lastNeuroText, ref lastNeuroPercent, force);
            }

            lastNeuroHideState = hideNeuro;
        }
    }

    private void UpdateBarText(HudBar bar, float normalized, int rawValue, int percentValue, ref int lastRaw, ref int lastPercent, bool force)
    {
        if (bar == null || bar.valueText == null) return;

        if (hideTextForFullBars && normalized >= 0.999f)
        {
            if (force || !string.IsNullOrEmpty(bar.valueText.text))
                bar.valueText.text = string.Empty;
            return;
        }

        if (percentText)
        {
            if (!force && lastPercent == percentValue) return;
            bar.valueText.text = $"{percentValue}%";
            lastPercent = percentValue;
            return;
        }

        if (!force && lastRaw == rawValue) return;
        bar.valueText.text = rawValue.ToString();
        lastRaw = rawValue;
    }

    private void InvalidateTextCache()
    {
        lastHealthText = int.MinValue;
        lastStaminaText = int.MinValue;
        lastNeuroText = int.MinValue;
        lastHealthPercent = int.MinValue;
        lastStaminaPercent = int.MinValue;
        lastNeuroPercent = int.MinValue;
        lastNeuroHideState = false;
    }
}
