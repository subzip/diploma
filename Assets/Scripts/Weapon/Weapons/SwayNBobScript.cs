using UnityEngine;
using UnityEngine.InputSystem;

public class SwayNBobScript : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement mover;
    [SerializeField] private AimController aimController;
    [SerializeField] private WeaponManager weaponManager;

    [Header("Sway Position")]
    [SerializeField] private float step = 0.01f;
    [SerializeField] private float maxStepDistance = 0.06f;
    private Vector3 swayPos;

    [Header("Sway Rotation")]
    [SerializeField] private float rotationStep = 4f;
    [SerializeField] private float maxRotationStep = 5f;
    private Vector3 swayEulerRot;

    [Header("Smoothing")]
    [SerializeField] private float smooth = 10f;
    [SerializeField] private float smoothRot = 12f;

    [Header("Bobbing Position")]
    [SerializeField] private float speedCurve = 0f;
    [SerializeField] private Vector3 travelLimit = Vector3.one * 0.025f;
    [SerializeField] private Vector3 bobLimit = Vector3.one * 0.01f;
    [SerializeField] private float bobExaggeration = 1f;
    private Vector3 bobPosition;

    [Header("Vertical Weapon Bob (Walk/Sprint)")]
    [SerializeField] private float walkVerticalBobAmplitude = 0.008f;
    [SerializeField] private float sprintVerticalBobAmplitude = 0.014f;
    [SerializeField] private float walkVerticalBobFrequency = 8.5f;
    [SerializeField] private float sprintVerticalBobFrequency = 11.5f;
    [SerializeField] private float verticalBobBlendInSpeed = 8f;
    [SerializeField] private float verticalBobBlendOutSpeed = 10f;
    private float verticalBobTimer;
    private float verticalBobWeight;
    
    private float verticalBobOffsetY;

    [Header("Bobbing Rotation")]
    [SerializeField] private Vector3 multiplier = new Vector3(1f, 1f, 1f);
    private Vector3 bobEulerRotation;

    [Header("Aim Tuning")]
    [SerializeField] private float aimSwayMultiplier = 0.25f;
    [SerializeField] private float aimBobMultiplier = 0.15f;
    [SerializeField] private bool lockRifleHipfireRoll = true;

    [Header("Visual Recoil")]
    [SerializeField] private float recoilPosZ = 0.035f;
    [SerializeField] private float recoilRotPitch = 6f;

    private PlayerInputActions inputActions;
    private Vector2 walkInput;
    private Vector2 lookInput;
    private Vector3 initialLocalPos;
    private Quaternion initialLocalRot;

    private float CurveSin => Mathf.Sin(speedCurve);
    private float CurveCos => Mathf.Cos(speedCurve);

    private void Awake()
    {
        if (mover == null) mover = GetComponentInParent<PlayerMovement>();
        if (aimController == null) aimController = GetComponentInParent<AimController>();
        if (weaponManager == null) weaponManager = GetComponentInParent<WeaponManager>();

        initialLocalPos = transform.localPosition;
        initialLocalRot = transform.localRotation;
        inputActions = GameInput.Instance != null ? GameInput.Instance.Actions : null;
    }

    private void OnEnable()
    {
        if (inputActions == null && GameInput.Instance != null)
            inputActions = GameInput.Instance.Actions;
        if (inputActions == null) return;

        inputActions.Player.Move.performed += OnMovePerformed;
        inputActions.Player.Move.canceled += OnMoveCanceled;
        inputActions.Player.Look.performed += OnLookPerformed;
        inputActions.Player.Look.canceled += OnLookCanceled;
    }

    private void OnDisable()
    {
        if (inputActions == null) return;

        inputActions.Player.Move.performed -= OnMovePerformed;
        inputActions.Player.Move.canceled -= OnMoveCanceled;
        inputActions.Player.Look.performed -= OnLookPerformed;
        inputActions.Player.Look.canceled -= OnLookCanceled;
    }

    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        walkInput = ctx.ReadValue<Vector2>().normalized;
    }

    private void OnMoveCanceled(InputAction.CallbackContext _)
    {
        walkInput = Vector2.zero;
    }

    private void OnLookPerformed(InputAction.CallbackContext ctx)
    {
        lookInput = ctx.ReadValue<Vector2>();
    }

    private void OnLookCanceled(InputAction.CallbackContext _)
    {
        lookInput = Vector2.zero;
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) return;

        Sway();
        SwayRotation();
        BobOffset();
        BobRotation();
        CompositePositionRotation();
    }

    private void Sway()
    {
        Vector2 invertLook = lookInput * -step * 0.01f;
        invertLook.x = Mathf.Clamp(invertLook.x, -maxStepDistance, maxStepDistance);
        invertLook.y = Mathf.Clamp(invertLook.y, -maxStepDistance, maxStepDistance);

        swayPos = new Vector3(invertLook.x, invertLook.y, 0f);
    }

    private void SwayRotation()
    {
        Vector2 invertLook = lookInput * -rotationStep * 0.01f;
        invertLook.x = Mathf.Clamp(invertLook.x, -maxRotationStep, maxRotationStep);
        invertLook.y = Mathf.Clamp(invertLook.y, -maxRotationStep, maxRotationStep);
        swayEulerRot = new Vector3(invertLook.y, invertLook.x, invertLook.x);
    }

    private void BobOffset()
    {
        bool grounded = mover != null && mover.IsGrounded;
        float movementAmount = Mathf.Clamp01(Mathf.Abs(walkInput.x) + Mathf.Abs(walkInput.y));
        bool sprinting = mover != null && mover.IsSprinting;

        speedCurve += Time.deltaTime * ((grounded ? movementAmount : 1f) * bobExaggeration + 0.01f);

        bobPosition.x = (CurveCos * bobLimit.x * (grounded ? 1f : 0f)) - (walkInput.x * travelLimit.x);
        bobPosition.y = (CurveSin * bobLimit.y) - (walkInput.y * travelLimit.y);
        bobPosition.z = -(walkInput.y * travelLimit.z);

        
        float targetWeight = grounded ? movementAmount : 0f;
        float blend = (targetWeight > verticalBobWeight ? verticalBobBlendInSpeed : verticalBobBlendOutSpeed) * Time.deltaTime;
        verticalBobWeight = Mathf.MoveTowards(verticalBobWeight, targetWeight, blend);

        float freq = sprinting ? sprintVerticalBobFrequency : walkVerticalBobFrequency;
        float amp = sprinting ? sprintVerticalBobAmplitude : walkVerticalBobAmplitude;
        verticalBobTimer += Time.deltaTime * Mathf.Lerp(0.6f, freq, verticalBobWeight);
        verticalBobOffsetY = Mathf.Sin(verticalBobTimer) * amp * verticalBobWeight;
    }

    private void BobRotation()
    {
        bool moving = walkInput.sqrMagnitude > 0.0001f;

        bobEulerRotation.x = moving ? multiplier.x * Mathf.Sin(2f * speedCurve) : multiplier.x * (Mathf.Sin(2f * speedCurve) * 0.5f);
        bobEulerRotation.y = moving ? multiplier.y * CurveCos : 0f;
        bobEulerRotation.z = moving ? multiplier.z * CurveCos * walkInput.x : 0f;
    }

    private void CompositePositionRotation()
    {
        float swayMul = 1f;
        float bobMul = 1f;
        if (aimController != null && aimController.IsAiming)
        {
            swayMul = aimSwayMultiplier;
            bobMul = aimBobMultiplier;
        }

        Vector3 targetPos = initialLocalPos + swayPos * swayMul + bobPosition * bobMul;
        targetPos.y += verticalBobOffsetY * bobMul;
        Vector3 recoil = weaponManager != null ? weaponManager.GetRecoilOffset() : Vector3.zero;
        bool aimingRifleStyle = aimController != null &&
                                aimController.IsAiming &&
                                weaponManager != null &&
                                weaponManager.CurrentWeapon != null &&
                                weaponManager.CurrentWeapon.stats != null &&
                                weaponManager.CurrentWeapon.stats.useAimOverlay;
        bool rifleHipfireStyle = !aimingRifleStyle &&
                                 weaponManager != null &&
                                 weaponManager.CurrentWeapon != null &&
                                 weaponManager.CurrentWeapon.stats != null &&
                                 weaponManager.CurrentWeapon.stats.useAimOverlay;
        float rifleHipTiltComp = 1f;
        if (rifleHipfireStyle)
        {
            Vector3 hipEuler = weaponManager.CurrentWeapon.stats.hipLocalEuler;
            float yaw = Mathf.Abs(Mathf.DeltaAngle(0f, hipEuler.y));
            float roll = Mathf.Abs(Mathf.DeltaAngle(0f, hipEuler.z));
            float tilt = yaw * 0.6f + roll;
            float t = Mathf.Clamp01(tilt / 28f);
            rifleHipTiltComp = Mathf.Lerp(1f, 0.65f, t);
        }

        float recoilZScale = aimingRifleStyle ? 0f : (rifleHipfireStyle ? 0.45f * rifleHipTiltComp : 1f);
        Vector3 recoilPos = new Vector3(
            0f,
            0f,
            -Mathf.Abs(recoil.y) * recoilPosZ * recoilZScale
        );
        Vector3 recoilRot = new Vector3(
            aimingRifleStyle ? 0f : -Mathf.Abs(recoil.y) * recoilRotPitch,
            0f,
            0f
        );
        targetPos += recoilPos;

        Vector3 swayRot = swayEulerRot * swayMul;
        Vector3 bobRot = bobEulerRotation * bobMul;
        if (rifleHipfireStyle)
        {
            
            swayRot.y = 0f;
            swayRot.z = 0f;
            bobRot.y = 0f;
            bobRot.z = 0f;
        }

        Quaternion targetRot = initialLocalRot *
                               Quaternion.Euler(swayRot) *
                               Quaternion.Euler(bobRot) *
                               Quaternion.Euler(recoilRot);

        if (lockRifleHipfireRoll && rifleHipfireStyle)
        {
            Vector3 lockedEuler = targetRot.eulerAngles;
            lockedEuler.z = initialLocalRot.eulerAngles.z;
            targetRot = Quaternion.Euler(lockedEuler);
        }

        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * smooth);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * smoothRot);
    }
}
