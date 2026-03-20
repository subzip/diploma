using UnityEngine;

public class MedkitPickup : MonoBehaviour
{
    [Header("Heal")]
    [SerializeField] private float healAmount = 50f;

    [Header("Highlight (Optional)")]
    [SerializeField] private MeshRenderer[] renderersToHighlight;
    [SerializeField] private Color emissionColor = new Color(0.8f, 0.1f, 0.1f);
    [SerializeField] private float pulseSpeed = 3f;

    private MaterialPropertyBlock mpb;
    private bool consumed;

    private void Awake()
    {
        if (renderersToHighlight == null || renderersToHighlight.Length == 0)
            renderersToHighlight = GetComponentsInChildren<MeshRenderer>(true);

        mpb = new MaterialPropertyBlock();

        foreach (MeshRenderer renderer in renderersToHighlight)
        {
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
        foreach (MeshRenderer renderer in renderersToHighlight)
        {
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
        if (!other.CompareTag("Player") && !other.transform.root.CompareTag("Player")) return;

        PlayerHealth health = other.GetComponent<PlayerHealth>() ??
                              other.GetComponentInParent<PlayerHealth>() ??
                              other.GetComponentInChildren<PlayerHealth>();
        if (health == null) return;
        if (!health.TryHeal(healAmount)) return;

        consumed = true;
        DisableHighlight();
        Destroy(gameObject);
    }

    private void DisableHighlight()
    {
        if (renderersToHighlight == null) return;
        foreach (MeshRenderer renderer in renderersToHighlight)
        {
            if (renderer == null) continue;
            renderer.GetPropertyBlock(mpb);
            mpb.SetColor("_EmissionColor", Color.black);
            renderer.SetPropertyBlock(mpb);
        }
    }
}
