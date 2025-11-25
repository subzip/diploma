// KeycardPickup.cs
using UnityEngine;
using UnityEngine.InputSystem;

public class KeycardPickup : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float pickupRange = 2f;        // Дистанция подбора
    [SerializeField] private float viewAngle = 30f;         // Угол обзора (в градусах)
    [SerializeField] private string promptText = "Подберите карту"; // Текст подсказки

    [Header("Quest System")]
    [SerializeField] private QuestSystem questSystem;       // Ссылка на систему задач
    [SerializeField] private QuestUI questUI;
    private bool isInRange = false;
    private bool isLookingAt = false;
    private Transform playerCamera;

    private void Start()
    {
        // Находим камеру игрока
        playerCamera = Camera.main?.transform;
        if (playerCamera == null)
        {
            Debug.LogError("Камера не найдена! Убедитесь, что у камеры тег 'MainCamera'");
        }
    }

    private void Update()
    {
        CheckProximity();
        CheckViewDirection();

        // Показ подсказки
        if (isInRange && isLookingAt)
        {
            ShowPrompt();
        }
        
    }

    private void CheckProximity()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
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
        if (questSystem != null)
        {
            questSystem.CompleteCurrentQuest();
        }

        Destroy(gameObject);
    }

    private void OnEnable()
    {
        var input = new PlayerInputActions();
        input.Player.Enable();
        input.Player.PickUp.performed += OnPickup;
    }

    private void OnDisable()
    {
        var input = new PlayerInputActions();
        input.Player.Disable();
        input.Player.PickUp.performed -= OnPickup;
    }
}