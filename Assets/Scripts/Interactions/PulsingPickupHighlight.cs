using UnityEngine;

public abstract class PulsingPickupHighlight : MonoBehaviour
{
    [Header("Highlight (Optional)")]
    [SerializeField] protected MeshRenderer[] renderersToHighlight;
    [SerializeField] protected Color emissionColor = Color.white;
    [SerializeField] protected float pulseSpeed = 3f;

    protected MaterialPropertyBlock mpb;
    protected bool consumed;

    protected virtual void Awake()
    {
        if (renderersToHighlight == null || renderersToHighlight.Length == 0)
            renderersToHighlight = GetComponentsInChildren<MeshRenderer>(true);

        mpb = new MaterialPropertyBlock();
        EnableEmissionKeyword();
    }

    protected virtual void Update()
    {
        if (consumed) return;
        ApplyHighlightPulse();
    }

    protected void ConsumeAndDisableHighlight()
    {
        consumed = true;
        SetEmission(Color.black);
    }

    private void EnableEmissionKeyword()
    {
        if (renderersToHighlight == null) return;

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

    private void ApplyHighlightPulse()
    {
        if (renderersToHighlight == null) return;
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed);
        SetEmission(emissionColor * pulse);
    }

    private void SetEmission(Color color)
    {
        if (renderersToHighlight == null) return;

        for (int i = 0; i < renderersToHighlight.Length; i++)
        {
            MeshRenderer renderer = renderersToHighlight[i];
            if (renderer == null) continue;
            renderer.GetPropertyBlock(mpb);
            mpb.SetColor("_EmissionColor", color);
            renderer.SetPropertyBlock(mpb);
        }
    }
}
