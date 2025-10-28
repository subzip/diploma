using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [SerializeField] private string weaponName = "Assault Rifle";
    [SerializeField] public GameObject weaponPrefab; // Префаб самого оружия

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.PickUpWeapon(weaponPrefab);
                Destroy(gameObject); // Удаляем лежащее оружие
            }
        }
    }

    // Для отладки: отображение подсказки
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1.5f);
    }
}