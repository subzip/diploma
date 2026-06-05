using UnityEngine;

public class WeaponPickup : PulsingPickupHighlight
{
    public static event System.Action<string> OnWeaponPickedUp;

    [SerializeField] private string weaponName = "Weapon";
    [SerializeField] public GameObject weaponPrefab;

    protected override void Awake()
    {
        emissionColor = new Color(0.1f, 0.6f, 1f);
        base.Awake();
    }

    private void OnTriggerEnter(Collider other) => TryGiveWeapon(other);
    private void OnTriggerStay(Collider other) => TryGiveWeapon(other);

    public bool TryPickupFrom(Component playerComponent) => TryGiveWeapon(playerComponent);

    public void Consume()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        ConsumeAndDisableHighlight();
    }

    private bool TryGiveWeapon(Component other)
    {
        if (consumed) return false;
        if (other == null) return false;
        if (!ComponentSearch.IsPlayer(other)) return false;

        WeaponManager manager = ComponentSearch.FindInHierarchy<WeaponManager>(other);
        if (manager == null) return false;

        GameObject weaponToGive = ResolveWeaponObject();
        if (weaponToGive == null) return false;

        bool isSceneObject = weaponToGive.scene.IsValid() && weaponToGive.scene.rootCount != 0;
        bool pickedUp = manager.PickupWeapon(weaponToGive);
        if (!pickedUp) return false;
        OnWeaponPickedUp?.Invoke(string.IsNullOrWhiteSpace(weaponName) ? weaponToGive.name : weaponName);

        ConsumeAndDisableHighlight();

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

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1.5f);
    }

    private GameObject ResolveWeaponObject()
    {
        if (weaponPrefab != null)
        {
            bool isAsset = !weaponPrefab.scene.IsValid() || weaponPrefab.scene.rootCount == 0;
            bool belongsToThisPickup = weaponPrefab.transform == transform ||
                                       weaponPrefab.transform.IsChildOf(transform) ||
                                       transform.IsChildOf(weaponPrefab.transform);

            if (isAsset || belongsToThisPickup)
                return weaponPrefab;
        }

        BaseWeapon localWeapon = GetComponent<BaseWeapon>();
        if (localWeapon == null)
            localWeapon = GetComponentInChildren<BaseWeapon>(true);
        if (localWeapon == null)
            localWeapon = GetComponentInParent<BaseWeapon>();

        return localWeapon != null ? localWeapon.gameObject : weaponPrefab;
    }
}
