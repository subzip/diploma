// PlayerLook.cs
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{
    [Header("Look")]
    [SerializeField] private float lookSensitivity = 2f;
    [SerializeField] private float maxLookUp = 80f;
    [SerializeField] private float maxLookDown = 45f;

    [Header("Camera")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private float standCameraHeight = 1.65f;
    [SerializeField] private float crouchCameraHeight = 1.0f;
    [SerializeField] private float cameraSmoothTime = 0.2f;

    [SerializeField] private WeaponManager weaponManager;

    private PlayerInputActions inputActions;
    private Vector2 lookInput;
    private float xRotation = 0f;
    private float cameraHeightVelocity = 0f;

    [SerializeField] private PlayerCrouch crouch;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerNeuroresist neuroresist;
    [SerializeField] private AimController aimController;

    [Header("Headbob")]
    [SerializeField] private float bobAmplitudeWalk = 0.02f;
    [SerializeField] private float bobAmplitudeSprint = 0.035f;
    [SerializeField] private float bobFrequencyWalk = 8f;
    [SerializeField] private float bobFrequencySprint = 11f;
    private float bobTimer = 0f;

    [Header("Recoil (Shooter Style)")]
    [SerializeField] private float recoilKickSnappiness = 22f;
    [SerializeField] private float recoilReturnSpeed = 12f;
    [SerializeField] private float maxRecoilPitch = 18f;
    [SerializeField] private float maxRecoilYaw = 2.5f;
    [SerializeField] private float adsRecoilMultiplier = 0.8f;
    [SerializeField] private float recoilPitchMultiplier = 3.1f;
    [SerializeField] private float recoilYawMultiplier = 0.45f;

    private Vector2 recoilTarget;
    private Vector2 recoilCurrent;

    private void Awake()
    {
        inputActions = GameInput.Instance.Actions;
        if (movement == null) movement = GetComponent<PlayerMovement>();
        if (neuroresist == null) neuroresist = GetComponent<PlayerNeuroresist>();
        if (aimController == null) aimController = GetComponentInParent<AimController>();
    }

    private void OnEnable()
    {
        inputActions.Player.Look.performed += OnLookPerformed;
        inputActions.Player.Look.canceled += OnLookCanceled;
    }

    private void OnDisable()
    {
        if (inputActions == null) return;
        inputActions.Player.Look.performed -= OnLookPerformed;
        inputActions.Player.Look.canceled -= OnLookCanceled;
    }

    private void OnLookPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx) => lookInput = ctx.ReadValue<Vector2>();
    private void OnLookCanceled(UnityEngine.InputSystem.InputAction.CallbackContext ctx) => lookInput = Vector2.zero;

    private void Update()
    {
        if(Time.timeScale == 0) return;
        
        float yRotation = lookInput.x * lookSensitivity;
        transform.Rotate(Vector3.up * yRotation);

        xRotation -= lookInput.y * lookSensitivity;
        xRotation = Mathf.Clamp(xRotation, -maxLookUp, maxLookDown);
    }

    private void LateUpdate()
    {
        if (cameraPivot == null) return;

        float targetHeight = crouch != null && crouch.IsCrouching 
            ? crouchCameraHeight 
            : standCameraHeight;

        float currentHeight = Mathf.SmoothDamp(
            cameraPivot.localPosition.y,
            targetHeight,
            ref cameraHeightVelocity,
            cameraSmoothTime
        );

        // headbob
        float speed = movement != null ? movement.GetMoveSpeed() : 0f;
        bool grounded = movement != null ? movement.IsGrounded : true;
        float bobOffsetY = 0f;
        float bobOffsetX = 0f;
        if (grounded && speed > 0.1f)
        {
            bool sprinting = movement.IsSprinting;
            bobTimer += Time.deltaTime * (sprinting ? bobFrequencySprint : bobFrequencyWalk);
            float amp = sprinting ? bobAmplitudeSprint : bobAmplitudeWalk;
            bobOffsetY = Mathf.Sin(bobTimer) * amp;
            bobOffsetX = Mathf.Cos(bobTimer * 0.5f) * amp * 0.5f;
        }
        else
        {
            bobTimer = 0f;
        }

        cameraPivot.localPosition = new Vector3(bobOffsetX, currentHeight + bobOffsetY, cameraPivot.localPosition.z);
        cameraPivot.localRotation = Quaternion.Euler(xRotation, 0, 0);

        ApplyLookRecoil(Time.deltaTime);
        float neuroJitter = 0f;
        if (neuroresist != null && neuroresist.CurrentValue > 0f)
        {
            float jitterAmp = 0.3f;
            float jitterFreq = 18f;
            neuroJitter = Mathf.Sin(Time.time * jitterFreq) * jitterAmp;
        }
        cameraPivot.localRotation = Quaternion.Euler(
            xRotation - recoilCurrent.y + neuroJitter,
            recoilCurrent.x + bobOffsetX * 30f,
            0f
        );
    }

    private void ApplyLookRecoil(float deltaTime)
    {
        if (weaponManager == null) return;

        float effectivePitchMul = Mathf.Max(2.8f, recoilPitchMultiplier);
        float effectiveYawMul = Mathf.Max(0.25f, recoilYawMultiplier);
        float effectiveMaxPitch = Mathf.Max(10f, maxRecoilPitch);
        float effectiveMaxYaw = Mathf.Max(1.5f, maxRecoilYaw);
        float effectiveReturn = Mathf.Max(8f, recoilReturnSpeed);
        float effectiveSnap = Mathf.Max(18f, recoilKickSnappiness);

        Vector2 impulse = weaponManager.ConsumeLookRecoil();
        if (impulse.sqrMagnitude > 0f)
        {
            bool aiming = weaponManager.CurrentWeapon != null && aimController != null && aimController.IsAiming;

            float adsMul = aiming ? adsRecoilMultiplier : 1f;
            recoilTarget.x += impulse.x * effectiveYawMul * adsMul;
            recoilTarget.y += impulse.y * effectivePitchMul * adsMul;
        }

        recoilTarget.x = Mathf.Clamp(recoilTarget.x, -effectiveMaxYaw, effectiveMaxYaw);
        recoilTarget.y = Mathf.Clamp(recoilTarget.y, 0f, effectiveMaxPitch);

        float returnT = 1f - Mathf.Exp(-effectiveReturn * deltaTime);
        recoilTarget = Vector2.Lerp(recoilTarget, Vector2.zero, returnT);

        float kickT = 1f - Mathf.Exp(-effectiveSnap * deltaTime);
        recoilCurrent = Vector2.Lerp(recoilCurrent, recoilTarget, kickT);
    }
}
