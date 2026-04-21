
using UnityEngine;

[System.Serializable]
public class QuestItem
{
    public string title = "РќРѕРІР°СЏ Р·Р°РґР°С‡Р°";
    public string description = "РћРїРёСЃР°РЅРёРµ Р·Р°РґР°С‡Рё...";
    [TextArea] public string objective = "Р¦РµР»СЊ: РЅР°Р№С‚Рё С‡С‚Рѕ-С‚Рѕ";
    public bool isCompleted = false;
    public string nextQuestID = "";

    public void Complete()
    {
        isCompleted = true;
    }
}