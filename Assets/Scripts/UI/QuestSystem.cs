// QuestSystem.cs
using UnityEngine;
using System.Collections.Generic;

public class QuestSystem : MonoBehaviour
{
    [Header("Quests")]
    [SerializeField] private List<QuestItem> quests = new List<QuestItem>();

    [Header("UI")]
    public QuestUI questUI;

    private int currentQuestIndex = 0;

    private void Start()
    {
        if (questUI != null)
        {
            questUI.ShowInitialHint();
            UpdateUI();
        }
    }

    public void CompleteCurrentQuest()
    {
        if (currentQuestIndex >= quests.Count || quests[currentQuestIndex].isCompleted) return;

        quests[currentQuestIndex].Complete();

        currentQuestIndex++;
        questUI.ShowCompleted();
        UpdateUI();
    }

    public QuestItem GetCurrentQuest()
    {
        if (currentQuestIndex < quests.Count)
            return quests[currentQuestIndex];
        return null;
    }

    public void UpdateUI()
    {
        if (questUI != null)
            questUI.UpdateQuestDisplay(GetCurrentQuest());
    }
}