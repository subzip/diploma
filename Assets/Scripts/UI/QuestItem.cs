// QuestItem.cs
using UnityEngine;

[System.Serializable]
public class QuestItem
{
    public string title = "Новая задача";
    public string description = "Описание задачи...";
    [TextArea] public string objective = "Цель: найти что-то";
    public bool isCompleted = false;
    public string nextQuestID = "";

    public void Complete()
    {
        isCompleted = true;
    }
}