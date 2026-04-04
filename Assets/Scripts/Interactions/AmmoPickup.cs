using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    [Header("Ammo")]
    [SerializeField] private AmmoType ammoType = AmmoType.Rifle;
    [SerializeField] private int ammoAmount = 30;

    [Header("Highlight (Optional)")]
    [SerializeField] private MeshRenderer[] renderersToHighlight;
    [SerializeField] private Color emissionColor = new Color(0.8f, 0.7f, 0.1f);
    [SerializeField] private float pulseSpeed = 3f;

    private MaterialPropertyBlock mpb;
    private bool consumed;

    private void Awake()
    {
        if (renderersToHighlight == null || renderersToHighlight.Length == 0)
            renderersToHighlight = GetComponentsInChildren<MeshRenderer>(true);

        mpb = new MaterialPropertyBlock();

        for (int i = 0; i < renderersToHighlight.Length; i++)
        {
            MeshRenderer renderer = renderersToHighlight[i];
            if (renderer == null) continue;
            foreach (Material mat in renderer.sharedMaterials)
            {
                if (mat != null && !mat.IsKeywordEnabled("_EMISSION"))
                    mat.EnableKeyword("_EMISSION");
            }
        }
    }

    private void Update()
    {
        if (consumed || renderersToHighlight == null) return;

        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed);
        for (int i = 0; i < renderersToHighlight.Length; i++)
        {
            MeshRenderer renderer = renderersToHighlight[i];
            if (renderer == null) continue;
            renderer.GetPropertyBlock(mpb);
            mpb.SetColor("_EmissionColor", emissionColor * pulse);
            renderer.SetPropertyBlock(mpb);
        }
    }

    private void OnTriggerEnter(Collider other) => TryConsume(other);
    private void OnTriggerStay(Collider other) => TryConsume(other);

    private void TryConsume(Component other)
    {
        if (consumed) return;
        if (other == null) return;
        if (!other.CompareTag("Player") && !other.transform.root.CompareTag("Player")) return;

        WeaponManager manager = other.GetComponent<WeaponManager>() ??
                                other.GetComponentInParent<WeaponManager>() ??
                                other.GetComponentInChildren<WeaponManager>();
        if (manager == null) return;

        if (!manager.AddAmmo(ammoType, ammoAmount)) return;

        consumed = true;
        DisableHighlight();
        Destroy(gameObject);
    }

    private void DisableHighlight()
    {
        if (renderersToHighlight == null) return;
        for (int i = 0; i < renderersToHighlight.Length; i++)
        {
            MeshRenderer renderer = renderersToHighlight[i];
            if (renderer == null) continue;
            renderer.GetPropertyBlock(mpb);
            mpb.SetColor("_EmissionColor", Color.black);
            renderer.SetPropertyBlock(mpb);
        }
    }
}
