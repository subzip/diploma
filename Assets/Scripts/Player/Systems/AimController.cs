using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Reflection;




public class AimController : MonoBehaviour
{
    [SerializeField] private Transform weaponHolder; 
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private float aimLerpSpeed = 10f;

    [Header("Fallback ADS (РєРѕРіРґР° РЅРµС‚ WeaponStats)")]
    [SerializeField] private Vector3 fallbackAimOffset = new Vector3(0.05f, -0.05f, 0.1f);

    [Header("Camera")]
    [SerializeField] private Camera playerCamera;
    [Tooltip("Assign your active Cinemachine virtual camera component here (CinemachineVirtualCamera/CinemachineCamera/FreeLook).")]
    [SerializeField] private Behaviour cinemachineVirtualCamera;
    [SerializeField] private float defaultAimFov = 55f;

    [Header("Gameplay")]
    [SerializeField] private GameObject aimOverlay;
    [SerializeField] private GameObject hipCrosshair;
    [SerializeField] private float aimSlowMultiplier = 0.7f;
    [SerializeField] private float aimOverlayFadeSpeed = 8f;
    [SerializeField] private float overlayPositionTolerance = 0.015f;
    [SerializeField] private float overlayRotationTolerance = 4f;

    private Vector3 defaultHolderLocalPos;
    private Quaternion defaultHolderLocalRot;
    private float defaultFov;
    private PlayerMovement movement;
    private bool isAiming;
    private PlayerInputActions inputActions;
    private CanvasGroup aimOverlayGroup;

    public bool IsAiming => isAiming;

    private void Awake()
    {
        inputActions = GameInput.Instance != null ? GameInput.Instance.Actions : null;

        if (weaponManager == null) weaponManager = GetComponentInChildren<WeaponManager>();
        if (weaponHolder == null && weaponManager != null) weaponHolder = weaponManager.transform;

        ResolvePlayerCamera();
        if (!TryGetCinemachineFov(out defaultFov))
        {
            if (playerCamera != null) defaultFov = playerCamera.fieldOfView;
        }

        if (weaponHolder != null)
        {
            defaultHolderLocalPos = weaponHolder.localPosition;
            defaultHolderLocalRot = weaponHolder.localRotation;
        }

        if (aimOverlay != null)
        {
            aimOverlayGroup = aimOverlay.GetComponent<CanvasGroup>();
            if (aimOverlayGroup != null)
            {
                aimOverlayGroup.alpha = 0f;
                aimOverlayGroup.interactable = false;
                aimOverlayGroup.blocksRaycasts = false;
                aimOverlay.SetActive(true);
            }
            else
            {
                aimOverlay.SetActive(false);
            }
        }

        movement = GetComponent<PlayerMovement>();
    }

    private void OnEnable()
    {
        if (inputActions == null && GameInput.Instance != null)
            inputActions = GameInput.Instance.Actions;
    }

    private void OnDisable()
    {
        if (isAiming && movement != null)
            movement.SetAimMultiplier(1f);
        isAiming = false;
    }

    private void Update()
    {
        bool aimPressed = IsAimPressed();
        if (aimPressed != isAiming)
        {
            isAiming = aimPressed;
            if (movement != null) movement.SetAimMultiplier(isAiming ? aimSlowMultiplier : 1f);
        }

        UpdateAimOverlay();
        UpdateCrosshairVisibility();
        UpdateTransforms();
        UpdateFov();
    }

    private bool IsAimPressed()
    {
        if (inputActions == null && GameInput.Instance != null)
            inputActions = GameInput.Instance.Actions;
        if (inputActions == null) return false;

        return inputActions.UI.RightClick.IsPressed();
    }

    private void UpdateAimOverlay()
    {
        if (aimOverlay == null) return;

        BaseWeapon currentWeapon = weaponManager != null ? weaponManager.CurrentWeapon : null;
        WeaponStats stats = currentWeapon != null ? currentWeapon.stats : null;
        bool allowOverlay = stats == null ? isAiming : stats.useAimOverlay && isAiming;

        if (allowOverlay && currentWeapon != null && stats != null)
        {
            float posDelta = Vector3.Distance(currentWeapon.transform.localPosition, stats.aimLocalPosition);
            float rotDelta = Quaternion.Angle(currentWeapon.transform.localRotation, Quaternion.Euler(stats.aimLocalEuler));
            allowOverlay = posDelta <= overlayPositionTolerance && rotDelta <= overlayRotationTolerance;
        }

        if (aimOverlayGroup != null)
        {
            float target = allowOverlay ? 1f : 0f;
            aimOverlayGroup.alpha = Mathf.MoveTowards(
                aimOverlayGroup.alpha,
                target,
                aimOverlayFadeSpeed * Time.unscaledDeltaTime
            );
        }
        else
        {
            aimOverlay.SetActive(allowOverlay);
        }
    }

    private void UpdateCrosshairVisibility()
    {
        if (hipCrosshair == null) return;

        BaseWeapon currentWeapon = weaponManager != null ? weaponManager.CurrentWeapon : null;
        WeaponStats stats = currentWeapon != null ? currentWeapon.stats : null;

        
        bool hideHipCrosshair = isAiming && stats != null && stats.useAimOverlay;
        hipCrosshair.SetActive(!hideHipCrosshair);
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

        bool fovApplied = false;
        if (TryGetCinemachineFov(out float currentCmFov))
        {
            float smoothed = Mathf.Lerp(currentCmFov, targetFov, Time.deltaTime * aimLerpSpeed);
            fovApplied = TrySetCinemachineFov(smoothed);
        }

        if (fovApplied) return;

        if (playerCamera == null) ResolvePlayerCamera();
        if (playerCamera == null) return;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, Time.deltaTime * aimLerpSpeed);
    }

    private void ResolvePlayerCamera()
    {
        if (playerCamera != null) return;

        Camera[] cameras = GetComponentsInChildren<Camera>(true);
        Camera fallback = null;
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera cam = cameras[i];
            if (cam == null) continue;

            string lowerName = cam.name.ToLowerInvariant();
            if (lowerName.Contains("fp camera") || lowerName == "fpcamera" || lowerName.Contains("first"))
            {
                playerCamera = cam;
                return;
            }

            if (fallback == null) fallback = cam;
        }

        if (fallback != null)
        {
            playerCamera = fallback;
            return;
        }

        playerCamera = Camera.main;
    }

    private bool TryGetCinemachineFov(out float fov)
    {
        fov = 0f;
        if (cinemachineVirtualCamera == null) return false;

        object cameraObject = cinemachineVirtualCamera;
        if (TryGetLensFieldOfView(cameraObject, out fov)) return true;

        return false;
    }

    private bool TrySetCinemachineFov(float fov)
    {
        if (cinemachineVirtualCamera == null) return false;
        object cameraObject = cinemachineVirtualCamera;
        return TrySetLensFieldOfView(cameraObject, fov);
    }

    private bool TryGetLensFieldOfView(object target, out float fov)
    {
        fov = 0f;
        if (target == null) return false;

        Type t = target.GetType();

        
        if (TryReadLensMember(t, target, "Lens", out fov)) return true;
        if (TryReadLensMember(t, target, "m_Lens", out fov)) return true;

        PropertyInfo lensProp = t.GetProperty("Lens");
        if (lensProp != null)
        {
            object lens = lensProp.GetValue(target);
            if (TryReadFieldOfViewFromLens(lens, out fov)) return true;
        }

        FieldInfo lensField = t.GetField("m_Lens", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (lensField != null)
        {
            object lens = lensField.GetValue(target);
            if (TryReadFieldOfViewFromLens(lens, out fov)) return true;
        }

        return false;
    }

    private bool TrySetLensFieldOfView(object target, float fov)
    {
        if (target == null) return false;

        Type t = target.GetType();

        
        if (TryWriteLensMember(t, target, "Lens", fov)) return true;
        if (TryWriteLensMember(t, target, "m_Lens", fov)) return true;

        PropertyInfo lensProp = t.GetProperty("Lens");
        if (lensProp != null && lensProp.CanRead && lensProp.CanWrite)
        {
            object lens = lensProp.GetValue(target);
            if (TryWriteFieldOfViewToLens(ref lens, fov))
            {
                lensProp.SetValue(target, lens);
                return true;
            }
        }

        FieldInfo lensField = t.GetField("m_Lens", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (lensField != null)
        {
            object lens = lensField.GetValue(target);
            if (TryWriteFieldOfViewToLens(ref lens, fov))
            {
                lensField.SetValue(target, lens);
                return true;
            }
        }

        return false;
    }

    private bool TryReadFieldOfViewFromLens(object lens, out float fov)
    {
        fov = 0f;
        if (lens == null) return false;

        Type lensType = lens.GetType();
        PropertyInfo fovProp = lensType.GetProperty("FieldOfView");
        if (fovProp != null && fovProp.CanRead)
        {
            object value = fovProp.GetValue(lens);
            if (value is float floatValue)
            {
                fov = floatValue;
                return true;
            }
        }

        FieldInfo fovField = lensType.GetField("FieldOfView", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (fovField != null)
        {
            object value = fovField.GetValue(lens);
            if (value is float floatFieldValue)
            {
                fov = floatFieldValue;
                return true;
            }
        }

        
        FieldInfo altField = lensType.GetField("fieldOfView", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (altField != null)
        {
            object value = altField.GetValue(lens);
            if (value is float floatAltValue)
            {
                fov = floatAltValue;
                return true;
            }
        }

        return false;
    }

    private bool TryWriteFieldOfViewToLens(ref object lens, float fov)
    {
        if (lens == null) return false;

        Type lensType = lens.GetType();
        PropertyInfo fovProp = lensType.GetProperty("FieldOfView");
        if (fovProp != null && fovProp.CanWrite)
        {
            fovProp.SetValue(lens, fov);
            return true;
        }

        FieldInfo fovField = lensType.GetField("FieldOfView", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (fovField != null)
        {
            fovField.SetValue(lens, fov);
            return true;
        }

        FieldInfo altField = lensType.GetField("fieldOfView", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (altField != null)
        {
            altField.SetValue(lens, fov);
            return true;
        }

        return false;
    }

    private bool TryReadLensMember(Type targetType, object target, string memberName, out float fov)
    {
        fov = 0f;

        PropertyInfo prop = targetType.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop != null && prop.CanRead)
        {
            object lens = prop.GetValue(target);
            if (TryReadFieldOfViewFromLens(lens, out fov)) return true;
        }

        FieldInfo field = targetType.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null)
        {
            object lens = field.GetValue(target);
            if (TryReadFieldOfViewFromLens(lens, out fov)) return true;
        }

        return false;
    }

    private bool TryWriteLensMember(Type targetType, object target, string memberName, float fov)
    {
        PropertyInfo prop = targetType.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop != null && prop.CanRead && prop.CanWrite)
        {
            object lens = prop.GetValue(target);
            if (TryWriteFieldOfViewToLens(ref lens, fov))
            {
                prop.SetValue(target, lens);
                return true;
            }
        }

        FieldInfo field = targetType.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null)
        {
            object lens = field.GetValue(target);
            if (TryWriteFieldOfViewToLens(ref lens, fov))
            {
                field.SetValue(target, lens);
                return true;
            }
        }

        return false;
    }
}
