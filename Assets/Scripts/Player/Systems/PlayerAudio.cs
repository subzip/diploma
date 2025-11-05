using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAudio : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] private AudioClip breathIdle;
    [SerializeField] private AudioClip sighSound;

    private AudioSource audioSource;
    private PlayerInputActions inputActions;
    private Vector2 moveInput;
    private float lastFootstepTime = 0f;
    private float footstepInterval = 0.5f;
    private float lastSighTime = 0f;
    private float sighInterval = 20f;
    private bool wasMoving = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.maxDistance = 15f;
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

    private void Update()
    {
        if (!GetComponent<PlayerMovement>().IsGrounded) return;

        bool isMoving = moveInput.magnitude > 0.1f;

        if (isMoving)
        {
            if (Time.time - lastFootstepTime > footstepInterval && footstepSounds.Length > 0)
            {
                AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
                audioSource.PlayOneShot(clip, 0.7f);
                lastFootstepTime = Time.time;
            }
            wasMoving = true;
        }
        else if (wasMoving)
        {
            audioSource.PlayOneShot(breathIdle, 0.3f);
            wasMoving = false;
        }

        if (Time.time - lastSighTime > sighInterval && Random.value < 0.15f)
        {
            audioSource.PlayOneShot(sighSound, 0.5f);
            lastSighTime = Time.time;
        }
    }
}