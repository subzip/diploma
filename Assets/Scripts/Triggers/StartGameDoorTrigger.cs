using UnityEngine;

public class StartGameDoorTrigger : MonoBehaviour
{
   public GameObject ambienceObject;


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ambienceObject.SetActive(true);
            Destroy(gameObject);
        }
    }
}
