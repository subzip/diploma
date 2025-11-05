using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "ScriptableObjects/PlayerData", order = 1)]
public class PlayerData : ScriptableObject
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public float crouchSpeed = 2f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;

    [Header("Stamina")]
    public float staminaMax = 100f;
    public float staminaDrainRate = 20f;
    public float staminaRegenRate = 15f;
    public float staminaRegenDelay = 2f;

    [Header("Stamina UI")]
    public float staminaSliderUpdateInterval = 0.1f;

    [Header("Look")]
    public float lookSensitivity = 2f;
    public float maxLookUp = 80f;
    public float maxLookDown = 45f;

    [Header("Crouch")]
    public float standHeight = 2f;
    public float crouchHeight = 1f;
    public float crouchSmoothTime = 0.22f;

    [Header("Weapon")]
    public int magazineSize = 30;
    public float reloadTime = 2f;
    public float fireRate = 0.15f;
    public float damage = 25f;

    
}