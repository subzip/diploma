using UnityEngine;

[DisallowMultipleComponent]
public class MedkitPickup : MonoBehaviour
{
    [Header("Heal")]
    [SerializeField] private float healAmount = 50f;
    private bool consumed;

    private void OnTriggerEnter(Collider other) => TryConsume(other);
    private void OnTriggerStay(Collider other) => TryConsume(other);

    private void TryConsume(Component other)
    {
        if (consumed) return;
        if (other == null) return;
        if (!ComponentSearch.IsPlayer(other)) return;

        PlayerHealth health = ComponentSearch.FindInHierarchy<PlayerHealth>(other);
        if (health == null) return;
        if (!health.TryHeal(healAmount)) return;

        consumed = true;
        Destroy(gameObject);
    }
}
