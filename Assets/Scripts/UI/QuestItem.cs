
using UnityEngine;

[System.Serializable]
public class QuestItem
{
    public string title = "\u041d\u043e\u0432\u0430\u044f \u0437\u0430\u0434\u0430\u0447\u0430";
    public string description = "\u041e\u043f\u0438\u0441\u0430\u043d\u0438\u0435 \u0437\u0430\u0434\u0430\u0447\u0438...";
    [TextArea] public string objective = "\u0426\u0435\u043b\u044c: \u043d\u0430\u0439\u0442\u0438 \u0447\u0442\u043e-\u0442\u043e";
    public bool isCompleted = false;
    public string nextQuestID = "";

    public void Complete()
    {
        isCompleted = true;
    }
}
