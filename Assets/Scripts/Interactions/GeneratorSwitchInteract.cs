using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class GeneratorSwitchInteract : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float interactRange = 2.2f;
    [SerializeField] private float viewAngle = 35f;
    [SerializeField] private string promptText = "Включить генератор";
    [SerializeField] private float promptRefreshSeconds = 0.12f;

    [Header("Target")]
    [SerializeField] private PowerOutageScenario scenario;

    [Header("Lever Animation")]
    [SerializeField] private Transform leverTransform;
    [SerializeField] private float leverOffX = 0f;
    [SerializeField] private float leverOnX = -90f;
    [SerializeField] private float leverAnimDuration = 0.35f;

    [Header("Audio")]
    [SerializeField] private AudioCue leverSwitchCue;
    [SerializeField] private AudioCue generatorPowerOnCue;
    [SerializeField, Range(0f, 2f)] private float leverSwitchVolume = 1f;
    [SerializeField, Range(0f, 2f)] private float generatorPowerOnVolume = 1f;

    private PlayerInputActions inputActions;
    private Transform playerCamera;
    private Transform playerTransform;
    private bool isInteractEnabled;
    private bool inRange;
    private bool lookingAt;
    private bool used;
    private bool activating;
    private float nextPromptTime;
    private Quaternion leverInitialRotation;

    private void Start()
    {
        playerTransform = PlayerLocator.GetPlayerTransform(forceRefresh: true);
        playerCamera = Camera.main != null ? Camera.main.transform : null;
        if (scenario == null)
            scenario = FindObjectOfType<PowerOutageScenario>(true);

        if (leverTransform != null)
            leverInitialRotation = leverTransform.localRotation;
    }

    private void Update()
    {
        if (!isInteractEnabled || used) return;
        RefreshPlayerRefsIfNeeded();
        UpdateRangeAndView();
        ShowPromptIfNeeded();
    }

    private void OnEnable()
    {
        ResolveInputActions();
        if (inputActions == null) return;
        inputActions.Player.PickUp.performed += OnInteract;
    }

    private void OnDisable()
    {
        if (inputActions == null) return;
        inputActions.Player.PickUp.performed -= OnInteract;
    }

    public void SetInteractEnabled(bool enabled)
    {
        isInteractEnabled = enabled;
        if (!enabled)
        {
            inRange = false;
            lookingAt = false;
        }
    }

    private void OnInteract(InputAction.CallbackContext ctx)
    {
        if (!isInteractEnabled || used || activating) return;
        if (!inRange || !lookingAt) return;

        StartCoroutine(ActivateGeneratorRoutine());
    }

    private void UpdateRangeAndView()
    {
        if (playerTransform == null || playerCamera == null)
        {
            inRange = false;
            lookingAt = false;
            return;
        }

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        inRange = distance <= interactRange;

        Vector3 toObj = transform.position - playerCamera.position;
        float angle = Vector3.Angle(playerCamera.forward, toObj);
        lookingAt = angle <= viewAngle;
    }

    private void ShowPromptIfNeeded()
    {
        if (!inRange || !lookingAt) return;
        if (Time.unscaledTime < nextPromptTime) return;
        nextPromptTime = Time.unscaledTime + Mathf.Max(0.05f, promptRefreshSeconds);

        QuestUI questUi = FindObjectOfType<QuestUI>(true);
        if (questUi != null)
            questUi.ShowHint(promptText);
    }

    private void ResolveInputActions()
    {
        if (inputActions != null) return;
        if (GameInput.Instance == null) return;
        inputActions = GameInput.Instance.Actions;
    }

    private void RefreshPlayerRefsIfNeeded()
    {
        if (playerTransform == null)
            playerTransform = PlayerLocator.GetPlayerTransform(forceRefresh: true);
        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main.transform;
    }

    private System.Collections.IEnumerator ActivateGeneratorRoutine()
    {
        activating = true;
        used = true;
        AudioService.PlayAt(leverSwitchCue, transform.position, leverSwitchVolume, ambience: false);

        if (leverTransform != null)
            yield return AnimateLeverToOn();

        AudioService.PlayAt(generatorPowerOnCue, transform.position, generatorPowerOnVolume, ambience: true);
        if (scenario != null)
            scenario.RestorePower();

        activating = false;
    }

    private System.Collections.IEnumerator AnimateLeverToOn()
    {
        Vector3 startEuler = leverTransform.localEulerAngles;
        float startX = Normalize180(startEuler.x);
        float endX = leverOnX;
        float y = startEuler.y;
        float z = startEuler.z;

        float duration = Mathf.Max(0.01f, leverAnimDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float x = Mathf.Lerp(startX, endX, t);
            leverTransform.localRotation = Quaternion.Euler(x, y, z);
            yield return null;
        }

        leverTransform.localRotation = Quaternion.Euler(endX, y, z);
    }

    private static float Normalize180(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        return angle;
    }
}

