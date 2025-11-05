// Assets/Scripts/Player/Core/PlayerStateManager.cs
using UnityEngine;

public class PlayerStateManager : MonoBehaviour
{
    // Состояния
    public bool isSprinting { get; set; } = false;
    public bool isCrouching { get; set; } = false;
    public bool isReloading { get; set; } = false;
    public bool isGrounded { get; set; } = false;
    public float currentStamina { get; set; } = 100f;
    public int currentAmmo { get; set; } = 30;
    public Vector2 moveInput { get; set; } = Vector2.zero;
    public Vector2 lookInput { get; set; } = Vector2.zero;

    [SerializeField] private PlayerData playerData;

    private void Start()
    {
        currentStamina = playerData?.staminaMax ?? 100f;
        currentAmmo = playerData?.magazineSize ?? 30;
    }

    public void SetGrounded(bool grounded) => isGrounded = grounded;
}