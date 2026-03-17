using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Простая механика прицеливания: ПКМ приближает оружие, сужает FOV, включает прицельную сетку и замедляет игрока.
/// </summary>
public class AimController : MonoBehaviour
{
    [SerializeField] private Transform weaponHolder;      // WeaponSlots
    [SerializeField] private Vector3 aimOffset = new Vector3(0.05f, -0.05f, 0.1f);
    [SerializeField] private float aimLerpSpeed = 10f;

    [SerializeField] private Camera playerCamera;
    [SerializeField] private float aimFov = 55f;
    private float defaultFov;

    [SerializeField] private GameObject aimOverlay;       // UI прицел (включается при Aim)
    [SerializeField] private float aimSlowMultiplier = 0.7f;

    private Vector3 defaultWeaponLocalPos;
    private PlayerMovement movement;
    private bool isAiming;

    private void Awake()
    {
        if (weaponHolder == null)
        {
            var wm = GetComponentInChildren<WeaponManager>();
            if (wm != null) weaponHolder = wm.transform;
        }
        if (playerCamera == null) playerCamera = Camera.main;
        if (playerCamera != null) defaultFov = playerCamera.fieldOfView;
        if (weaponHolder != null) defaultWeaponLocalPos = weaponHolder.localPosition;
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
    }

    private void UpdateTransforms()
    {
        if (weaponHolder != null)
        {
            Vector3 targetPos = isAiming ? defaultWeaponLocalPos + aimOffset : defaultWeaponLocalPos;
            weaponHolder.localPosition = Vector3.Lerp(weaponHolder.localPosition, targetPos, Time.deltaTime * aimLerpSpeed);
        }

        if (playerCamera != null)
        {
            float targetFov = isAiming ? aimFov : defaultFov;
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, Time.deltaTime * aimLerpSpeed);
        }
    }
}
