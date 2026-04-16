using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Volume))]
public class NeuroresistPostProcess : MonoBehaviour
{
    [Header("Effect Tuning")]
    [SerializeField, Range(0f, 1f)] private float activeVignette = 0.35f;
    [SerializeField, Range(0f, 1f)] private float activeVignetteSmoothness = 0.45f;
    [SerializeField, Range(0f, 1f)] private float activeChromatic = 0.12f;
    [SerializeField, Range(0f, 1f)] private float activeGrain = 0.14f;
    [SerializeField, Range(0f, 1f)] private float activeGrainResponse = 0.8f;
    [SerializeField, Range(-100f, 100f)] private float activeSaturation = -20f;
    [SerializeField, Range(-100f, 100f)] private float activeContrast = 10f;
    [SerializeField, Range(-5f, 5f)] private float activePostExposure = 0f;
    [SerializeField] private bool useColorFilter = false;
    [SerializeField] private Color activeColorFilter = Color.white;

    private Volume volume;
    private Vignette vignette;
    private ChromaticAberration chromatic;
    private FilmGrain grain;
    private ColorAdjustments colorAdjust;

    private void Awake()
    {
        volume = GetComponent<Volume>();
        if (volume == null || volume.profile == null)
        {
            Debug.LogWarning("NeuroresistPostProcess: Volume/Profile is missing.");
            enabled = false;
            return;
        }

        volume.isGlobal = true;
        volume.weight = 1f;

        EnsureOverride(ref vignette);
        EnsureOverride(ref chromatic);
        EnsureOverride(ref grain);
        EnsureOverride(ref colorAdjust);
    }

    private void EnsureOverride<T>(ref T setting) where T : VolumeComponent, new()
    {
        if (volume.profile.TryGet(out setting)) return;
        setting = volume.profile.Add<T>(true);
        setting.active = false;
    }

    public void EnableEffects(bool enabled)
    {
        if (vignette != null)
        {
            vignette.active = enabled;
            vignette.intensity.value = enabled ? activeVignette : 0f;
            vignette.smoothness.value = enabled ? activeVignetteSmoothness : 0.2f;
        }

        if (chromatic != null)
        {
            chromatic.active = enabled;
            chromatic.intensity.value = enabled ? activeChromatic : 0f;
        }

        if (grain != null)
        {
            grain.active = enabled;
            grain.intensity.value = enabled ? activeGrain : 0f;
            grain.response.value = enabled ? activeGrainResponse : 0f;
        }

        if (colorAdjust != null)
        {
            colorAdjust.active = enabled;
            colorAdjust.saturation.value = enabled ? activeSaturation : 0f;
            colorAdjust.postExposure.value = enabled ? activePostExposure : 0f;
            colorAdjust.contrast.value = enabled ? activeContrast : 0f;
            colorAdjust.colorFilter.value = enabled && useColorFilter ? activeColorFilter : Color.white;
        }
    }
}
