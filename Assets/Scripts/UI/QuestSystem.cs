
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
        if (questUI != null)
            questUI.ShowCompleted();
        UpdateUI();
    }

    public bool IsQuestCompletedByTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return false;

        for (int i = 0; i < quests.Count; i++)
        {
            QuestItem q = quests[i];
            if (q == null) continue;
            if (!string.Equals(q.title, title, System.StringComparison.Ordinal)) continue;
            return q.isCompleted;
        }

        return false;
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

    public void SetSingleActiveQuest(string title, string description, string objective = "")
    {
        QuestItem item = new QuestItem
        {
            title = title,
            description = description,
            objective = objective,
            isCompleted = false,
            nextQuestID = string.Empty
        };

        quests.Clear();
        quests.Add(item);
        currentQuestIndex = 0;
        UpdateUI();
    }
}
