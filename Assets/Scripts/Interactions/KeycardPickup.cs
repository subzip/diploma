// KeycardPickup.cs
using UnityEngine;
using UnityEngine.InputSystem;

public class KeycardPickup : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float pickupRange = 2f;
    [SerializeField] private float viewAngle = 30f;
    [SerializeField] private string promptText = "Подберите карту";

    [Header("Quest System")]
    [SerializeField] private QuestSystem questSystem;
    [SerializeField] private QuestUI questUI;
    private bool isInRange = false;
    private bool isLookingAt = false;
    private Transform playerCamera;
    private Transform playerTransform;
    private PlayerInputActions inputActions;

    [Header("Highlight")]
    [SerializeField] private MeshRenderer[] renderersToHighlight;
    [SerializeField] private Color emissionColor = new Color(0.3f, 0.8f, 0.2f);
    [SerializeField] private float pulseSpeed = 3f;
    private MaterialPropertyBlock mpb;
    private bool consumed = false;
    private float nextResolveRefTime;

    private void Start()
    {
        playerCamera = Camera.main?.transform;
        if (playerCamera == null)
        {
            Debug.LogError("Camera");
        }
        playerTransform = PlayerLocator.GetPlayerTransform(forceRefresh: true);
        if (renderersToHighlight == null || renderersToHighlight.Length == 0)
            renderersToHighlight = GetComponentsInChildren<MeshRenderer>(true);
        mpb = new MaterialPropertyBlock();
        EnableEmission();
    }

    private void Update()
    {
        if (consumed) return;
        CheckProximity();
        CheckViewDirection();

        if (isInRange && isLookingAt)
        {
            ShowPrompt();
        }
        
    }

    private void CheckProximity()
    {
        if (playerTransform == null && Time.time >= nextResolveRefTime)
        {
            playerTransform = PlayerLocator.GetPlayerTransform(forceRefresh: true);
            if (playerCamera == null) playerCamera = Camera.main?.transform;
            nextResolveRefTime = Time.time + 0.5f;
        }

        if (playerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            isInRange = distance <= pickupRange;
        }
        else
        {
            isInRange = false;
        }
    }

    private void CheckViewDirection()
    {
        if (playerCamera == null) return;

        Vector3 directionToKeycard = transform.position - playerCamera.position;
        float angle = Vector3.Angle(playerCamera.forward, directionToKeycard);

        isLookingAt = angle <= viewAngle;
    }

    private void ShowPrompt()
    {
        questUI.ShowHint(promptText);
    }

    private void OnPickup(InputAction.CallbackContext ctx)
    {
        if (isInRange && isLookingAt)
        {
            Pickup();
        }
    }

    private void Pickup()
    {
        consumed = true;
        DisableEmission();

        if (questSystem != null)
        {
            questSystem.CompleteCurrentQuest();
        }

        Destroy(gameObject);
    }

    private void OnEnable()
    {
        ResolveInputActions();
        if (inputActions == null) return;
        inputActions.Player.PickUp.performed += OnPickup;
    }

    private void OnDisable()
    {
        if (inputActions == null) return;
        inputActions.Player.PickUp.performed -= OnPickup;
    }

    private void LateUpdate()
    {
        if (consumed || renderersToHighlight == null) return;
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed);
        foreach (var r in renderersToHighlight)
        {
            if (r == null) continue;
            r.GetPropertyBlock(mpb);
            mpb.SetColor("_EmissionColor", emissionColor * pulse);
            r.SetPropertyBlock(mpb);
        }
    }

    private void EnableEmission()
    {
        if (renderersToHighlight == null) return;
        foreach (var r in renderersToHighlight)
        {
            if (r == null) continue;
            foreach (var mat in r.sharedMaterials)
            {
                if (mat != null && !mat.IsKeywordEnabled("_EMISSION"))
                    mat.EnableKeyword("_EMISSION");
            }
        }
    }

    private void DisableEmission()
    {
        if (renderersToHighlight == null) return;
        foreach (var r in renderersToHighlight)
        {
            if (r == null) continue;
            r.GetPropertyBlock(mpb);
            mpb.SetColor("_EmissionColor", Color.black);
            r.SetPropertyBlock(mpb);
        }
    }

    private void ResolveInputActions()
    {
        if (inputActions != null) return;
        if (GameInput.Instance == null) return;
        inputActions = GameInput.Instance.Actions;
    }
}
