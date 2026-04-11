using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerVitalsHud : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Health Bar")]
    [SerializeField] private Image healthFillImage;
    [SerializeField] private RectTransform healthFillTransform;
    [SerializeField] private TMP_Text healthText;

    [Header("Stamina Bar")]
    [SerializeField] private Image staminaFillImage;
    [SerializeField] private RectTransform staminaFillTransform;
    [SerializeField] private TMP_Text staminaText;

    [Header("Behavior")]
    [SerializeField] private bool useImageFillAmount = false;
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private bool updateNumericText = true;
    [SerializeField] private bool percentText = false;

    private float currentHealthNormalized = 1f;
    private float targetHealthNormalized = 1f;
    private float currentStaminaNormalized = 1f;
    private float targetStaminaNormalized = 1f;

    private void Awake()
    {
        ResolveSources();

        if (playerHealth != null)
        {
            targetHealthNormalized = Mathf.Clamp01(playerHealth.CurrentHealth / Mathf.Max(0.001f, playerHealth.MaxHealth));
            currentHealthNormalized = targetHealthNormalized;
        }

        if (playerMovement != null)
        {
            targetStaminaNormalized = Mathf.Clamp01(playerMovement.CurrentStamina / Mathf.Max(0.001f, playerMovement.MaxStamina));
            currentStaminaNormalized = targetStaminaNormalized;
        }

        ApplyBarFill(healthFillImage, healthFillTransform, currentHealthNormalized);
        ApplyBarFill(staminaFillImage, staminaFillTransform, currentStaminaNormalized);
        UpdateTexts();
    }

    private void OnEnable()
    {
        ResolveSources();
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += OnHealthChanged;
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= OnHealthChanged;
        }
    }

    private void Update()
    {
        if (playerHealth == null || playerMovement == null)
        {
            ResolveSources();
        }

        if (playerMovement != null)
        {
            targetStaminaNormalized = Mathf.Clamp01(playerMovement.CurrentStamina / Mathf.Max(0.001f, playerMovement.MaxStamina));
        }

        float t = Mathf.Max(0f, smoothSpeed) * Time.unscaledDeltaTime;
        currentHealthNormalized = Mathf.Lerp(currentHealthNormalized, targetHealthNormalized, t);
        currentStaminaNormalized = Mathf.Lerp(currentStaminaNormalized, targetStaminaNormalized, t);

        ApplyBarFill(healthFillImage, healthFillTransform, currentHealthNormalized);
        ApplyBarFill(staminaFillImage, staminaFillTransform, currentStaminaNormalized);
        UpdateTexts();
    }

    private void OnHealthChanged(float current, float max)
    {
        targetHealthNormalized = Mathf.Clamp01(current / Mathf.Max(0.001f, max));
    }

    private void ApplyBarFill(Image fillImage, RectTransform fillTransform, float normalized)
    {
        normalized = Mathf.Clamp01(normalized);

        if (useImageFillAmount && fillImage != null)
        {
            fillImage.fillAmount = normalized;
            return;
        }

        if (fillTransform != null)
        {
            Vector3 scale = fillTransform.localScale;
            scale.x = normalized;
            fillTransform.localScale = scale;
        }
    }

    private void UpdateTexts()
    {
        if (!updateNumericText) return;

        if (healthText != null && playerHealth != null)
        {
            healthText.text = percentText
                ? $"{Mathf.RoundToInt(targetHealthNormalized * 100f)}%"
                : $"{Mathf.RoundToInt(playerHealth.CurrentHealth)}";
        }

        if (staminaText != null && playerMovement != null)
        {
            staminaText.text = percentText
                ? $"{Mathf.RoundToInt(targetStaminaNormalized * 100f)}%"
                : $"{Mathf.RoundToInt(playerMovement.CurrentStamina)}";
        }
    }

    private void ResolveSources()
    {
        if (playerHealth == null) playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerMovement == null) playerMovement = FindObjectOfType<PlayerMovement>();
    }
}
