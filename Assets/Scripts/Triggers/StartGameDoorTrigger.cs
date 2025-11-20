using UnityEngine;

public class StartGameDoorTrigger : MonoBehaviour
{
   public GameObject ambienceObject; // Сюда перетащите LabAmbience
   public QuestSystem questSystem;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ambienceObject.SetActive(true);
            questSystem.CompleteCurrentQuest();
            Destroy(gameObject); // Удаляем триггер, чтобы не срабатывал дважды
        }
    }
}
