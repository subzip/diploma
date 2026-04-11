using UnityEngine;

public class WeaponPickup : PulsingPickupHighlight
{
    [SerializeField] private string weaponName = "Weapon";
    [SerializeField] public GameObject weaponPrefab;

    protected override void Awake()
    {
        emissionColor = new Color(0.1f, 0.6f, 1f);
        base.Awake();
    }

    private void OnTriggerEnter(Collider other) => TryGiveWeapon(other);
    private void OnTriggerStay(Collider other) => TryGiveWeapon(other);

    public void Consume() => TryGiveWeapon(GetComponent<Collider>());

    private void TryGiveWeapon(Component other)
    {
        if (consumed) return;
        if (other == null) return;
        if (!ComponentSearch.IsPlayer(other)) return;

        WeaponManager manager = ComponentSearch.FindInHierarchy<WeaponManager>(other);
        if (manager == null) return;

        ConsumeAndDisableHighlight();

        bool isSceneObject = weaponPrefab != null && weaponPrefab.scene.rootCount != 0;
        manager.PickupWeapon(weaponPrefab);

        if (isSceneObject)
        {
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
            Destroy(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1.5f);
    }
}
