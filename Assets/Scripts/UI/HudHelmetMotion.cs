using UnityEngine;
using UnityEngine.SceneManagement;





public class HudHelmetMotion : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform hudRoot;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Tilt From Look")]
    [SerializeField] private float maxPitchTilt = 1.1f;
    [SerializeField] private float maxRollTilt = 1.3f;
    [SerializeField] private float lookSensitivity = 2.2f;

    [Header("Move Sway (Pixels)")]
    [SerializeField] private float moveSwayX = 6f;
    [SerializeField] private float moveSwayY = 4f;
    [SerializeField] private float moveSwaySmooth = 10f;

    [Header("Sprint Micro Shake")]
    [SerializeField] private float sprintShakeAmount = 1.5f;
    [SerializeField] private float sprintShakeFrequency = 8f;

    [Header("Smoothing")]
    [SerializeField] private float rotationSmooth = 12f;

    private Vector2 baseAnchoredPos;
    private Quaternion baseLocalRotation;
    private Vector3 lastCameraEuler;
    private bool cameraInit;
    private float shakeTime;
    private Vector2 currentOffset;
    private Quaternion currentRotation;
    private float nextResolveAttemptTime;

    private void Awake()
    {
        if (hudRoot == null) hudRoot = transform as RectTransform;
        ResolveReferences();

        if (hudRoot != null)
        {
            baseAnchoredPos = hudRoot.anchoredPosition;
            baseLocalRotation = hudRoot.localRotation;
            currentOffset = Vector2.zero;
            currentRotation = baseLocalRotation;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResolveReferences(force: true);
    }

    private void LateUpdate()
    {
        if (hudRoot == null) return;
        if ((cameraTransform == null || playerMovement == null) && Time.unscaledTime >= nextResolveAttemptTime)
        {
            ResolveReferences();
            nextResolveAttemptTime = Time.unscaledTime + 0.5f;
        }

        Vector2 lookDelta = GetCameraLookDelta();
        float move01 = 0f;
        if (playerMovement != null)
        {
            float speed = playerMovement.GetMoveSpeed();
            float refSpeed = playerMovement.IsSprinting ? 8f : 5f;
            move01 = Mathf.Clamp01(speed / Mathf.Max(0.01f, refSpeed));
        }

        Vector2 targetOffset = new Vector2(
            -lookDelta.x * moveSwayX,
            -lookDelta.y * moveSwayY + Mathf.Sin(Time.unscaledTime * 6.5f) * (moveSwayY * 0.22f * move01)
        );

        if (playerMovement != null && playerMovement.IsSprinting && playerMovement.IsGrounded)
        {
            shakeTime += Time.unscaledDeltaTime * sprintShakeFrequency;
            targetOffset.x += Mathf.Sin(shakeTime * 1.73f) * sprintShakeAmount;
            targetOffset.y += Mathf.Cos(shakeTime * 2.11f) * sprintShakeAmount * 0.6f;
        }
        else
        {
            shakeTime = 0f;
        }

        currentOffset = Vector2.Lerp(currentOffset, targetOffset, Time.unscaledDeltaTime * moveSwaySmooth);
        hudRoot.anchoredPosition = baseAnchoredPos + currentOffset;

        float pitch = Mathf.Clamp(-lookDelta.y * maxPitchTilt * lookSensitivity, -maxPitchTilt, maxPitchTilt);
        float roll = Mathf.Clamp(-lookDelta.x * maxRollTilt * lookSensitivity, -maxRollTilt, maxRollTilt);
        Quaternion targetRotation = baseLocalRotation * Quaternion.Euler(pitch, 0f, roll);
        currentRotation = Quaternion.Slerp(currentRotation, targetRotation, Time.unscaledDeltaTime * rotationSmooth);
        hudRoot.localRotation = currentRotation;
    }

    private Vector2 GetCameraLookDelta()
    {
        if (cameraTransform == null) return Vector2.zero;

        Vector3 currentEuler = cameraTransform.eulerAngles;
        if (!cameraInit)
        {
            lastCameraEuler = currentEuler;
            cameraInit = true;
            return Vector2.zero;
        }

        float deltaYaw = Mathf.DeltaAngle(lastCameraEuler.y, currentEuler.y);
        float deltaPitch = Mathf.DeltaAngle(lastCameraEuler.x, currentEuler.x);
        lastCameraEuler = currentEuler;

        return new Vector2(deltaYaw, deltaPitch) * 0.12f;
    }

    private void ResolveReferences(bool force = false)
    {
        if ((force || cameraTransform == null) && Camera.main != null) cameraTransform = Camera.main.transform;
        if (force || playerMovement == null) playerMovement = FindObjectOfType<PlayerMovement>();
    }
}
