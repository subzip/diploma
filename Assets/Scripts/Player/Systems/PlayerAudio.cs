using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAudio : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioCue footstepCue;
    [SerializeField] private AudioCue breathRunCue;
    [SerializeField] private AudioCue sighCue;
    [SerializeField, Range(0f, 2f)] private float footstepVolumeScale = 0.8f;
    [SerializeField, Range(0f, 2f)] private float breathRunVolumeScale = 0.45f;
    [SerializeField, Range(0f, 2f)] private float sighVolumeScale = 0.5f;
    [SerializeField] private float breathRunInterval = 1.1f;
    [SerializeField] private float idleSighInterval = 10f;

    private PlayerInputActions inputActions;
    private Vector2 moveInput;
    private float lastFootstepTime = 0f;
    private float footstepInterval = 0.5f;
    private float lastRunBreathTime = 0f;
    private float lastSighTime = -999f;
    private AudioSource runBreathSource;
    private AudioSource sighSource;
    private PlayerMovement movement;

    private void Awake()
    {
        ResolveInputActions();
        movement = GetComponent<PlayerMovement>();
    }

    private void OnEnable()
    {
        ResolveInputActions();
        if (inputActions == null) return;
        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMoveCancel;
    }

    private void OnDisable()
    {
        if (inputActions == null) return;
        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Move.canceled -= OnMoveCancel;
    }

    private void OnMove(InputAction.CallbackContext ctx) => moveInput = ctx.ReadValue<Vector2>();
    private void OnMoveCancel(InputAction.CallbackContext ctx) => moveInput = Vector2.zero;

    private void Update()
    {
        if (movement == null || !movement.IsGrounded) return;

        bool isMoving = moveInput.magnitude > 0.1f;
        bool isSprinting = movement.IsSprinting;

        if (isMoving)
        {
            if (Time.time - lastFootstepTime > footstepInterval)
            {
                AudioService.PlayAt(footstepCue, transform.position, footstepVolumeScale);
                lastFootstepTime = Time.time;
            }
        }

        if (isSprinting && isMoving)
        {
            bool runBreathBusy = runBreathSource != null && runBreathSource.isPlaying;
            if (!runBreathBusy && Time.time - lastRunBreathTime > Mathf.Max(0.2f, breathRunInterval))
            {
                runBreathSource = AudioService.PlayAt(breathRunCue, transform.position, breathRunVolumeScale);
                lastRunBreathTime = Time.time;
            }
            return;
        }

        if (!isMoving && Time.time - lastSighTime > Mathf.Max(1f, idleSighInterval))
        {
            bool sighBusy = sighSource != null && sighSource.isPlaying;
            if (!sighBusy)
            {
                sighSource = AudioService.PlayAt(sighCue, transform.position, sighVolumeScale);
                lastSighTime = Time.time;
            }
        }
    }

    private void ResolveInputActions()
    {
        if (inputActions != null) return;
        if (GameInput.Instance == null) return;
        inputActions = GameInput.Instance.Actions;
    }
}
