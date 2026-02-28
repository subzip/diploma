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

    private void Awake()
    {
        inputActions = GameInput.Instance.Actions;
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

        cameraPivot.localPosition = new Vector3(0f, currentHeight, cameraPivot.localPosition.z);
        cameraPivot.localRotation = Quaternion.Euler(xRotation, 0, 0);

        Vector3 recoil = weaponManager?.GetRecoilOffset() ?? Vector3.zero;
        cameraPivot.localRotation = Quaternion.Euler(xRotation + recoil.y, recoil.x, 0);
    }
}
