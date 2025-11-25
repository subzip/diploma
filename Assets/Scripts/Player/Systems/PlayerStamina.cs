
using UnityEngine;

public class PlayerStamina : MonoBehaviour
{
    [SerializeField] private PlayerData playerData;
    [SerializeField] private PlayerStateManager stateManager;

    private float lastSprintTime = 0f;
    private bool canRegenStamina = true;

    public void SetSprinting(bool isSprinting)
    {
        stateManager.isSprinting = isSprinting;
    }

    public void UpdateStamina()
    {
        if (playerData == null || stateManager == null) return;

        if (stateManager.isSprinting && stateManager.moveInput.magnitude > 0.1f)
        {
            stateManager.currentStamina -= playerData.staminaDrainRate * Time.deltaTime;
            stateManager.currentStamina = Mathf.Clamp(stateManager.currentStamina, 0, playerData.staminaMax);
            lastSprintTime = Time.time;
            canRegenStamina = false;
        }
        else
        {
            if (!canRegenStamina && Time.time - lastSprintTime > playerData.staminaRegenDelay)
                canRegenStamina = true;
            if (canRegenStamina)
            {
                stateManager.currentStamina += playerData.staminaRegenRate * Time.deltaTime;
                stateManager.currentStamina = Mathf.Clamp(stateManager.currentStamina, 0, playerData.staminaMax);
            }
        }
    }
}