using UnityEngine;
using UnityEngine.UI;

public class PlayerStaminaUI : MonoBehaviour
{
    [SerializeField] private Slider staminaSlider;

    private PlayerMovement playerMovement;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (staminaSlider != null && playerMovement != null)
        {
            staminaSlider.value = playerMovement.CurrentStamina / Mathf.Max(0.001f, playerMovement.MaxStamina);
        }
    }
}
