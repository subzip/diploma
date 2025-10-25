using UnityEngine;

public class StartGameDoorTrigger : MonoBehaviour
{
   public GameObject ambienceObject; // Сюда перетащите LabAmbience

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ambienceObject.SetActive(true);
            Destroy(gameObject); // Удаляем триггер, чтобы не срабатывал дважды
        }
    }
}
