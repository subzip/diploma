using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerStaminaUI : MonoBehaviour
{
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private string staminaSliderObjectName = "Stamina";

    private PlayerMovement playerMovement;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        ResolveSliderIfNeeded();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        ResolveSliderIfNeeded();

        if (staminaSlider != null && playerMovement != null)
        {
            staminaSlider.value = playerMovement.CurrentStamina / Mathf.Max(0.001f, playerMovement.MaxStamina);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResolveSliderIfNeeded(force: true);
    }

    private void ResolveSliderIfNeeded(bool force = false)
    {
        if (!force && staminaSlider != null) return;

        Slider[] sliders = FindObjectsOfType<Slider>(true);
        Slider fallback = null;

        for (int i = 0; i < sliders.Length; i++)
        {
            Slider slider = sliders[i];
            if (slider == null) continue;

            string lower = slider.name.ToLowerInvariant();
            if (lower == "stamina" || lower.Contains("stamina"))
            {
                staminaSlider = slider;
                return;
            }

            if (fallback == null && lower.Contains("energy"))
            {
                fallback = slider;
            }
        }

        if (fallback != null)
        {
            staminaSlider = fallback;
            return;
        }

        if (!string.IsNullOrWhiteSpace(staminaSliderObjectName))
        {
            GameObject byName = GameObject.Find(staminaSliderObjectName);
            if (byName != null)
            {
                staminaSlider = byName.GetComponent<Slider>();
            }
        }
    }
}
