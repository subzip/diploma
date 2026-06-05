using UnityEngine;

public abstract class PulsingPickupHighlight : MonoBehaviour
{
    [Header("Highlight (Optional)")]
    [SerializeField] protected MeshRenderer[] renderersToHighlight;
    [SerializeField] protected Color emissionColor = Color.white;
    [SerializeField] protected float pulseSpeed = 3f;
    [SerializeField] private float rendererRefreshInterval = 1.5f;

    protected MaterialPropertyBlock mpb;
    protected bool consumed;
    private float nextRefreshTime;

    protected virtual void Awake()
    {
        mpb = new MaterialPropertyBlock();
        EnsureRenderers();
        EnableEmissionKeyword();
    }

    protected virtual void OnEnable()
    {
        EnsureRenderers();
        EnableEmissionKeyword();
    }

    protected virtual void Update()
    {
        if (consumed) return;
        if (Time.unscaledTime >= nextRefreshTime)
        {
            EnsureRenderers();
            EnableEmissionKeyword();
            nextRefreshTime = Time.unscaledTime + Mathf.Max(0.25f, rendererRefreshInterval);
        }
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
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseSpeed);
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
            mpb.SetColor("_EmissiveColor", color);
            renderer.SetPropertyBlock(mpb);
        }
    }

    private void EnsureRenderers()
    {
        bool needRefresh = renderersToHighlight == null || renderersToHighlight.Length == 0;
        if (!needRefresh)
        {
            for (int i = 0; i < renderersToHighlight.Length; i++)
            {
                if (renderersToHighlight[i] == null)
                {
                    needRefresh = true;
                    break;
                }
            }
        }

        if (needRefresh)
        {
            renderersToHighlight = GetComponentsInChildren<MeshRenderer>(true);
        }
    }
}
