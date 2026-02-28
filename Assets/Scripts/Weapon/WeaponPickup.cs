using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [SerializeField] private string weaponName = "Weapon";
    [SerializeField] public GameObject weaponPrefab;
    private bool consumed = false;

    [Header("Highlight")]
    [SerializeField] private MeshRenderer[] renderersToHighlight;
    [SerializeField] private Color emissionColor = new Color(0.1f, 0.6f, 1f);
    [SerializeField] private float pulseSpeed = 3f;
    private MaterialPropertyBlock mpb;

    private void Awake()
    {
        if (renderersToHighlight == null || renderersToHighlight.Length == 0)
            renderersToHighlight = GetComponentsInChildren<MeshRenderer>(true);
        mpb = new MaterialPropertyBlock();

        // Включаем глобально поддержку эмиссии на материалах (на случай, если выключена)
        foreach (var r in renderersToHighlight)
        {
            if (r == null) continue;
            foreach (var mat in r.sharedMaterials)
            {
                if (mat != null && !mat.IsKeywordEnabled("_EMISSION"))
                {
                    mat.EnableKeyword("_EMISSION");
                }
            }
        }
    }

    private void Update()
    {
        if (consumed || renderersToHighlight == null) return;
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed);
        foreach (var r in renderersToHighlight)
        {
            if (r == null) continue;
            r.GetPropertyBlock(mpb);
            mpb.SetColor("_EmissionColor", emissionColor * pulse);
            r.SetPropertyBlock(mpb);
        }
    }

    private void OnTriggerEnter(Collider other) => TryGiveWeapon(other);
    private void OnTriggerStay(Collider other) => TryGiveWeapon(other);

    public void Consume() => TryGiveWeapon(GetComponent<Collider>());

    private void TryGiveWeapon(Component other)
    {
        if (consumed) return;
        if (!other.CompareTag("Player")) return;

        var manager = other.GetComponent<WeaponManager>() ?? other.GetComponentInChildren<WeaponManager>();
        if (manager == null) return;

        consumed = true;
        DisableHighlight();

        // Определяем, сценовый ли объект
        bool isSceneObject = weaponPrefab != null && weaponPrefab.scene.rootCount != 0;
        manager.PickupWeapon(weaponPrefab);

        if (isSceneObject)
        {
            var col = GetComponent<Collider>();
            if (col) col.enabled = false;
            var rb = GetComponent<Rigidbody>();
            if (rb) rb.isKinematic = true;
            Destroy(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void DisableHighlight()
    {
        if (renderersToHighlight == null) return;
        foreach (var r in renderersToHighlight)
        {
            if (r == null) continue;
            r.GetPropertyBlock(mpb);
            mpb.SetColor("_EmissionColor", Color.black);
            r.SetPropertyBlock(mpb);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1.5f);
    }
}
