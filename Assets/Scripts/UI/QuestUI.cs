
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
        Cursor.lockState = CursorLockMode.Locked; 
        Cursor.visible = false; 
    }

    public void ShowInitialHint()
    {
        if (hintText != null)
        {
            hintText.gameObject.SetActive(true);
            hintText.text = "\u041d\u0430\u0436\u043c\u0438\u0442\u0435 Tab, \u0447\u0442\u043e\u0431\u044b \u043e\u0442\u043a\u0440\u044b\u0442\u044c \u0437\u0430\u0434\u0430\u0447\u0438";
            Invoke("HideHint", hintDuration);
        }
    }

    public void ShowCompleted()
    {
        if (hintText != null)
        {
            hintText.gameObject.SetActive(true);
            hintText.text = "\u0417\u0430\u0434\u0430\u043d\u0438\u0435 \u0432\u044b\u043f\u043e\u043b\u043d\u0435\u043d\u043e!";
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
