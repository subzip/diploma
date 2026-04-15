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

    [Header("Bobbing Rotation")]
    [SerializeField] private Vector3 multiplier = new Vector3(1f, 1f, 1f);
    private Vector3 bobEulerRotation;

    [Header("Aim Tuning")]
    [SerializeField] private float aimSwayMultiplier = 0.25f;
    [SerializeField] private float aimBobMultiplier = 0.15f;

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

        speedCurve += Time.deltaTime * ((grounded ? movementAmount : 1f) * bobExaggeration + 0.01f);

        bobPosition.x = (CurveCos * bobLimit.x * (grounded ? 1f : 0f)) - (walkInput.x * travelLimit.x);
        bobPosition.y = (CurveSin * bobLimit.y) - (walkInput.y * travelLimit.y);
        bobPosition.z = -(walkInput.y * travelLimit.z);
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
        Vector3 recoil = weaponManager != null ? weaponManager.GetRecoilOffset() : Vector3.zero;
        Vector3 recoilPos = new Vector3(
            0f,
            0f,
            -Mathf.Abs(recoil.y) * recoilPosZ
        );
        Vector3 recoilRot = new Vector3(
            -Mathf.Abs(recoil.y) * recoilRotPitch,
            0f,
            0f
        );
        targetPos += recoilPos;

        Quaternion targetRot = initialLocalRot *
                               Quaternion.Euler(swayEulerRot * swayMul) *
                               Quaternion.Euler(bobEulerRotation * bobMul) *
                               Quaternion.Euler(recoilRot);

        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * smooth);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * smoothRot);
    }
}
