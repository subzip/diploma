using UnityEngine;

[DisallowMultipleComponent]
public class AmmoPickup : MonoBehaviour
{
    [Header("Ammo")]
    [SerializeField] private AmmoType ammoType = AmmoType.Rifle;
    [SerializeField] private int ammoAmount = 30;
    private bool consumed;

    private void OnTriggerEnter(Collider other) => TryConsume(other);
    private void OnTriggerStay(Collider other) => TryConsume(other);

    private void TryConsume(Component other)
    {
        if (consumed) return;
        if (other == null) return;
        if (!ComponentSearch.IsPlayer(other)) return;

        WeaponManager manager = ComponentSearch.FindInHierarchy<WeaponManager>(other);
        if (manager == null) return;

        if (!manager.AddAmmo(ammoType, ammoAmount)) return;

        consumed = true;
        Destroy(gameObject);
    }
}
