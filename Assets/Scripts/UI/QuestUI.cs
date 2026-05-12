
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;


public class QuestUI : MonoBehaviour
{
    private const string LegacyHintGuideId = "legacy_hint_runtime";

    [Header("References")]
    [SerializeField] private GameObject questPanel;   
    [SerializeField] private TMP_Text questTitleText; 
    [SerializeField] private TMP_Text questDescriptionText;
    [SerializeField] private TMP_Text hintText;  

    [Header("Settings")]
    [SerializeField] private float hintDuration = 3f;

    [Header("Guide Icons")]
    [SerializeField] private Sprite iconW;
    [SerializeField] private Sprite iconA;
    [SerializeField] private Sprite iconS;
    [SerializeField] private Sprite iconD;
    [SerializeField] private Sprite iconTab;
    [SerializeField] private Sprite iconC;
    [SerializeField] private Sprite iconQ;
    [SerializeField] private Sprite iconF;
    [SerializeField] private Sprite iconE;
    [SerializeField] private Sprite iconLmb;
    [SerializeField] private Sprite iconRmb;
    [SerializeField] private Sprite icon1;
    [SerializeField] private Sprite icon2;

    private PlayerInputActions inputActions;

    private void Start()
    {
        questPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked; 
        Cursor.visible = false; 
    }

    public void ShowInitialHint()
    {
        GuideManager guideManager = GuideManager.Instance;
        if (guideManager != null)
        {
            string text = "Список задач";
            Sprite[] icons = iconTab != null ? new[] { iconTab } : null;
            guideManager.ShowCustomHint(LegacyHintGuideId, text, icons, hintDuration);
            return;
        }

        if (hintText != null)
        {
            hintText.gameObject.SetActive(true);
            hintText.text = "Список задач";
            Invoke("HideHint", hintDuration);
        }
    }

    public void ShowCompleted()
    {
        GuideManager guideManager = GuideManager.Instance;
        if (guideManager != null)
        {
            guideManager.ShowCustomHint(LegacyHintGuideId, "Задание выполнено!", null, hintDuration);
            return;
        }

        if (hintText != null)
        {
            hintText.gameObject.SetActive(true);
            hintText.text = "Задание выполнено!";
            Invoke("HideHint", hintDuration);
        }
    }

    public void ShowHint(string text)
    {
        GuideManager guideManager = GuideManager.Instance;
        if (guideManager != null)
        {
            guideManager.ShowCustomHint(LegacyHintGuideId, text, BuildIconsForText(text), hintDuration);
            return;
        }

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

    private Sprite[] BuildIconsForText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        string t = text.ToUpperInvariant();
        List<Sprite> icons = new List<Sprite>(6);

        void Add(Sprite s) { if (s != null) icons.Add(s); }

        if (t.Contains("W") && t.Contains("A") && t.Contains("S") && t.Contains("D"))
        {
            Add(iconW); Add(iconA); Add(iconS); Add(iconD);
        }

        if (t.Contains("TAB")) Add(iconTab);
        if (t.Contains("КАРТ")) Add(iconE);
        if (t.Contains("ГЕНЕРАТОР")) Add(iconE);
        if (t.Contains("ФОНАРИК")) Add(iconF);
        if (t.Contains(" ЛКМ") || t.Contains("ЛКМ")) Add(iconLmb);
        if (t.Contains(" ПКМ") || t.Contains("ПКМ")) Add(iconRmb);
        if (t.Contains(" C ") || t.EndsWith(" C") || t.Contains(" C,")) Add(iconC);
        if (t.Contains(" Q ") || t.EndsWith(" Q") || t.Contains(" Q,")) Add(iconQ);
        if (t.Contains(" F ") || t.EndsWith(" F") || t.Contains(" F,")) Add(iconF);
        if (t.Contains(" E ") || t.EndsWith(" E") || t.Contains(" E,")) Add(iconE);
        if (t.Contains("1")) Add(icon1);
        if (t.Contains("2")) Add(icon2);

        return icons.Count > 0 ? icons.ToArray() : null;
    }
}
