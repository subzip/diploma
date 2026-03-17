// QuestUI.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class QuestUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject questPanel;   
    [SerializeField] private TMP_Text questTitleText; 
    [SerializeField] private TMP_Text questDescriptionText;
    [SerializeField] private TMP_Text hintText;  

    [Header("Settings")]
    [SerializeField] private float hintDuration = 3f;

    private PlayerInputActions inputActions;

    private void Start()
    {
        questPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked; // Блокирует курсор в центре
        Cursor.visible = false; // Скрывает курсор
    }

    public void ShowInitialHint()
    {
        if (hintText != null)
        {
            hintText.gameObject.SetActive(true);
            hintText.text = "Нажмите Tab, чтобы открыть задачи";
            Invoke("HideHint", hintDuration);
        }
    }

    public void ShowCompleted()
    {
        if (hintText != null)
        {
            hintText.gameObject.SetActive(true);
            hintText.text = "Задание выполнено!";
            Invoke("HideHint", hintDuration);
        }
    }

    public void ShowHint(string text)
    {
        if (hintText != null)
        {
            hintText.gameObject.SetActive(true);
            hintText.text = text;
            Invoke("HideHint", hintDuration);
        }
    }

    private void OnEnable()
    {
        inputActions = GameInput.Instance.Actions;
        inputActions.Player.OpenQuests.performed += OnOpenQuests;
    }

    private void OnDisable()
    {
        if (inputActions == null) return;
        inputActions.Player.OpenQuests.performed -= OnOpenQuests;
    }

    private void OnOpenQuests(UnityEngine.InputSystem.InputAction.CallbackContext _) => ToggleQuestPanel();

    private void HideHint()
    {
        if (hintText != null)
            hintText.gameObject.SetActive(false);
    }

    public void UpdateQuestDisplay(QuestItem quest)
    {
        if (questPanel == null) return;

        if (quest != null)
        {
            questTitleText.text = quest.title;
            questDescriptionText.text = quest.description;
            //questPanel.SetActive(true);
        }
        else
        {
            questPanel.SetActive(false);
        }
    }

    public void ToggleQuestPanel()
    {
        questPanel.SetActive(!questPanel.activeSelf);
    }
}
