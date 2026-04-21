
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
            hintText.text = "РќР°Р¶РјРёС‚Рµ Tab, С‡С‚РѕР±С‹ РѕС‚РєСЂС‹С‚СЊ Р·Р°РґР°С‡Рё";
            Invoke("HideHint", hintDuration);
        }
    }

    public void ShowCompleted()
    {
        if (hintText != null)
        {
            hintText.gameObject.SetActive(true);
            hintText.text = "Р—Р°РґР°РЅРёРµ РІС‹РїРѕР»РЅРµРЅРѕ!";
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
