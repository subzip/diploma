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

    private PlayerInputActions inputActions;
    private Vector2 lookInput;
    private float xRotation = 0f;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += ctx => lookInput = Vector2.zero;
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

    private void Update()
    {
        // Поворот тела
        float yRotation = lookInput.x * lookSensitivity;
        transform.Rotate(Vector3.up * yRotation);

        // Поворот камеры вверх-вниз
        xRotation -= lookInput.y * lookSensitivity;
        xRotation = Mathf.Clamp(xRotation, -maxLookUp, maxLookDown);
    }

    private void LateUpdate()
    {
        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(xRotation, 0, 0);
    }
}