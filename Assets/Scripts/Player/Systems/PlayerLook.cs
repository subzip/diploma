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

    [Header("Headbob")]
    [SerializeField] private float bobAmplitudeWalk = 0.02f;
    [SerializeField] private float bobAmplitudeSprint = 0.035f;
    [SerializeField] private float bobFrequencyWalk = 8f;
    [SerializeField] private float bobFrequencySprint = 11f;
    private float bobTimer = 0f;

    private void Awake()
    {
        inputActions = GameInput.Instance.Actions;
        if (movement == null) movement = GetComponent<PlayerMovement>();
        if (neuroresist == null) neuroresist = GetComponent<PlayerNeuroresist>();
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

        Vector3 recoil = weaponManager?.GetRecoilOffset() ?? Vector3.zero;
        float neuroJitter = 0f;
        if (neuroresist != null && neuroresist.CurrentValue > 0f)
        {
            float jitterAmp = 0.3f;
            float jitterFreq = 18f;
            neuroJitter = Mathf.Sin(Time.time * jitterFreq) * jitterAmp;
        }
        cameraPivot.localRotation = Quaternion.Euler(xRotation + recoil.y + neuroJitter, recoil.x + bobOffsetX * 30f, 0);
    }
}
