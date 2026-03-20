using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ADS-контроллер: включает режим прицеливания, замедляет игрока, подводит текущее оружие в per-weapon позу.
/// </summary>
public class AimController : MonoBehaviour
{
    [SerializeField] private Transform weaponHolder; // WeaponSlots
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private float aimLerpSpeed = 10f;

    [Header("Fallback ADS (когда нет WeaponStats)")]
    [SerializeField] private Vector3 fallbackAimOffset = new Vector3(0.05f, -0.05f, 0.1f);

    [Header("Camera")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float defaultAimFov = 55f;

    [Header("Gameplay")]
    [SerializeField] private GameObject aimOverlay;
    [SerializeField] private float aimSlowMultiplier = 0.7f;

    private Vector3 defaultHolderLocalPos;
    private Quaternion defaultHolderLocalRot;
    private float defaultFov;
    private PlayerMovement movement;
    private bool isAiming;

    public bool IsAiming => isAiming;

    private void Awake()
    {
        if (weaponManager == null) weaponManager = GetComponentInChildren<WeaponManager>();
        if (weaponHolder == null && weaponManager != null) weaponHolder = weaponManager.transform;

        if (playerCamera == null) playerCamera = Camera.main;
        if (playerCamera != null) defaultFov = playerCamera.fieldOfView;

        if (weaponHolder != null)
        {
            defaultHolderLocalPos = weaponHolder.localPosition;
            defaultHolderLocalRot = weaponHolder.localRotation;
        }

        movement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        bool aimPressed = Mouse.current.rightButton.isPressed;
        if (aimPressed != isAiming)
        {
            isAiming = aimPressed;
            if (movement != null) movement.SetAimMultiplier(isAiming ? aimSlowMultiplier : 1f);
            if (aimOverlay != null) aimOverlay.SetActive(isAiming);
        }

        UpdateTransforms();
        UpdateFov();
    }

    private void UpdateTransforms()
    {
        BaseWeapon currentWeapon = weaponManager != null ? weaponManager.CurrentWeapon : null;
        WeaponStats stats = currentWeapon != null ? currentWeapon.stats : null;

        if (currentWeapon != null && stats != null)
        {
            Vector3 targetPos = isAiming ? stats.aimLocalPosition : stats.hipLocalPosition;
            Quaternion targetRot = Quaternion.Euler(isAiming ? stats.aimLocalEuler : stats.hipLocalEuler);

            currentWeapon.transform.localPosition = Vector3.Lerp(
                currentWeapon.transform.localPosition,
                targetPos,
                Time.deltaTime * aimLerpSpeed
            );
            currentWeapon.transform.localRotation = Quaternion.Slerp(
                currentWeapon.transform.localRotation,
                targetRot,
                Time.deltaTime * aimLerpSpeed
            );
            return;
        }

        if (weaponHolder == null) return;

        Vector3 targetHolderPos = isAiming ? defaultHolderLocalPos + fallbackAimOffset : defaultHolderLocalPos;
        Quaternion targetHolderRot = defaultHolderLocalRot;
        weaponHolder.localPosition = Vector3.Lerp(weaponHolder.localPosition, targetHolderPos, Time.deltaTime * aimLerpSpeed);
        weaponHolder.localRotation = Quaternion.Slerp(weaponHolder.localRotation, targetHolderRot, Time.deltaTime * aimLerpSpeed);
    }

    private void UpdateFov()
    {
        if (playerCamera == null) return;

        float targetFov = defaultFov;
        if (isAiming)
        {
            BaseWeapon currentWeapon = weaponManager != null ? weaponManager.CurrentWeapon : null;
            WeaponStats stats = currentWeapon != null ? currentWeapon.stats : null;
            if (stats != null && stats.useAdsFovOverride)
                targetFov = stats.adsFovOverride;
            else
                targetFov = defaultAimFov;
        }

        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, Time.deltaTime * aimLerpSpeed);
    }
}
