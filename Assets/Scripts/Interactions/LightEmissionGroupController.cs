using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class LightEmissionGroupController : MonoBehaviour
{
    public enum InitialEmissionState
    {
        None,
        On,
        Off
    }

    [Header("Targets (Optional)")]
    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private Material[] targetMaterials;
    [SerializeField] private bool autoCollectRenderersByMaterial = true;
    [SerializeField] private Transform searchRoot;
    [SerializeField] private bool includeInactive = true;

    [Header("Emission")]
    [SerializeField] private string emissionPropertyName = "_EmissionColor";
    [SerializeField] private string secondaryEmissionPropertyName = "_EmissiveColor";
    [SerializeField] private Color emissionOnColor = Color.white;
    [SerializeField] private Color emissionOffColor = Color.black;
    [SerializeField] private bool useSharedMaterials = true;
    [SerializeField] private bool cloneTargetMaterialsOnAwake = true;
    [SerializeField] private InitialEmissionState applyInitialStateOnStart = InitialEmissionState.None;

    private Material[] runtimeMaterialClones;

    private void Awake()
    {
        AutoCollectRenderersIfNeeded();

        if (!cloneTargetMaterialsOnAwake) return;
        if (targetMaterials == null || targetMaterials.Length == 0) return;
        if (targetRenderers == null || targetRenderers.Length == 0) return;

        runtimeMaterialClones = new Material[targetMaterials.Length];
        Dictionary<Material, Material> cloneMap = new Dictionary<Material, Material>(targetMaterials.Length);
        for (int i = 0; i < targetMaterials.Length; i++)
        {
            Material src = targetMaterials[i];
            if (src == null) continue;
            Material clone = new Material(src);
            runtimeMaterialClones[i] = clone;
            cloneMap[src] = clone;
        }
        targetMaterials = runtimeMaterialClones;

        for (int r = 0; r < targetRenderers.Length; r++)
        {
            Renderer renderer = targetRenderers[r];
            if (renderer == null) continue;

            Material[] rendererMats = useSharedMaterials ? renderer.sharedMaterials : renderer.materials;
            bool changed = false;
            for (int m = 0; m < rendererMats.Length; m++)
            {
                Material original = rendererMats[m];
                if (original == null) continue;
                if (!cloneMap.TryGetValue(original, out Material clone)) continue;
                rendererMats[m] = clone;
                changed = true;
            }

            if (changed)
            {
                if (useSharedMaterials) renderer.sharedMaterials = rendererMats;
                else renderer.materials = rendererMats;
            }
        }
    }

    private void Start()
    {
        if (applyInitialStateOnStart == InitialEmissionState.On)
            SetEmissionEnabled(true);
        else if (applyInitialStateOnStart == InitialEmissionState.Off)
            SetEmissionEnabled(false);
    }

    public void SetEmissionEnabled(bool enabled)
    {
        Color targetColor = enabled ? emissionOnColor : emissionOffColor;

        ApplyToDirectMaterials(targetColor, enabled);
        ApplyToRenderers(targetColor, enabled);
    }

    private void ApplyToDirectMaterials(Color color, bool enabled)
    {
        if (targetMaterials == null || targetMaterials.Length == 0) return;
        for (int i = 0; i < targetMaterials.Length; i++)
        {
            ApplyToMaterial(targetMaterials[i], color, enabled);
        }
    }
    
    private void ApplyToRenderers(Color color, bool enabled)
    {
        if (targetRenderers == null || targetRenderers.Length == 0) return;
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            Renderer renderer = targetRenderers[i];
            if (renderer == null) continue;

            Material[] materials = useSharedMaterials ? renderer.sharedMaterials : renderer.materials;
            if (materials == null) continue;

            for (int m = 0; m < materials.Length; m++)
            {
                ApplyToMaterial(materials[m], color, enabled);
            }
        }
    }

    private void ApplyToMaterial(Material mat, Color color, bool enabled)
    {
        if (mat == null) return;

        bool hasPrimary = !string.IsNullOrWhiteSpace(emissionPropertyName) && mat.HasProperty(emissionPropertyName);
        bool hasSecondary = !string.IsNullOrWhiteSpace(secondaryEmissionPropertyName) && mat.HasProperty(secondaryEmissionPropertyName);
        if (!hasPrimary && !hasSecondary) return;

        if (enabled) mat.EnableKeyword("_EMISSION");
        if (hasPrimary) mat.SetColor(emissionPropertyName, color);
        if (hasSecondary) mat.SetColor(secondaryEmissionPropertyName, color);
        if (!enabled) mat.DisableKeyword("_EMISSION");
    }

    private void OnDestroy()
    {
        if (runtimeMaterialClones == null) return;
        for (int i = 0; i < runtimeMaterialClones.Length; i++)
        {
            if (runtimeMaterialClones[i] != null)
                Destroy(runtimeMaterialClones[i]);
        }
    }

    private void AutoCollectRenderersIfNeeded()
    {
        if (!autoCollectRenderersByMaterial) return;
        if (targetMaterials == null || targetMaterials.Length == 0) return;

        Transform root = searchRoot != null ? searchRoot : transform.root;
        if (root == null) return;

        Renderer[] allRenderers = root.GetComponentsInChildren<Renderer>(includeInactive);
        List<Renderer> matched = new List<Renderer>();

        for (int i = 0; i < allRenderers.Length; i++)
        {
            Renderer r = allRenderers[i];
            if (r == null) continue;

            Material[] mats = r.sharedMaterials;
            if (mats == null || mats.Length == 0) continue;

            bool hasMatch = false;
            for (int m = 0; m < mats.Length && !hasMatch; m++)
            {
                Material rm = mats[m];
                if (rm == null) continue;
                for (int t = 0; t < targetMaterials.Length; t++)
                {
                    Material tm = targetMaterials[t];
                    if (tm == null) continue;
                    if (rm == tm)
                    {
                        hasMatch = true;
                        break;
                    }
                }
            }

            if (hasMatch)
                matched.Add(r);
        }

        targetRenderers = matched.ToArray();
    }
}
