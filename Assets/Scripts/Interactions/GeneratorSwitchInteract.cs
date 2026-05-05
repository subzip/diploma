using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class GeneratorSwitchInteract : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float interactRange = 2.2f;
    [SerializeField] private float viewAngle = 35f;
    [SerializeField] private string promptText = "Нажмите E, чтобы включить генератор";
    [SerializeField] private float promptRefreshSeconds = 0.12f;

    [Header("Target")]
    [SerializeField] private PowerOutageScenario scenario;

    private PlayerInputActions inputActions;
    private Transform playerCamera;
    private Transform playerTransform;
    private bool isInteractEnabled;
    private bool inRange;
    private bool lookingAt;
    private bool used;
    private float nextPromptTime;

    private void Start()
    {
        playerTransform = PlayerLocator.GetPlayerTransform(forceRefresh: true);
        playerCamera = Camera.main != null ? Camera.main.transform : null;
        if (scenario == null)
            scenario = FindObjectOfType<PowerOutageScenario>(true);
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
        if (!isInteractEnabled || used) return;
        if (!inRange || !lookingAt) return;

        used = true;
        if (scenario != null)
            scenario.RestorePower();
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
}

