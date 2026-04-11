using UnityEngine;

public class AmmoPickup : PulsingPickupHighlight
{
    [Header("Ammo")]
    [SerializeField] private AmmoType ammoType = AmmoType.Rifle;
    [SerializeField] private int ammoAmount = 30;

    protected override void Awake()
    {
        emissionColor = new Color(0.8f, 0.7f, 0.1f);
        base.Awake();
    }

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

        ConsumeAndDisableHighlight();
        Destroy(gameObject);
    }
}
