
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Volume))]
public class NeuroresistPostProcess : MonoBehaviour
{
    private Volume volume;
    private Vignette vignette;
    private ChromaticAberration chromatic;
    private FilmGrain grain;
    private ColorAdjustments colorAdjust;

    private void Awake()
    {
        volume = GetComponent<Volume>();
        // Убедимся, что volume глобальный и вес полный
        volume.isGlobal = true;
        volume.weight = 1f;
        // Если каких‑то override нет в профиле — добавляем в рантайме, чтобы TryGet не проваливался.
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
            vignette.intensity.value = enabled ? 0.6f : 0f;
        }

        if (chromatic != null)
        {
            chromatic.active = enabled;
            chromatic.intensity.value = enabled ? 0.25f : 0f;
        }

        if (grain != null)
        {
            grain.active = enabled;
            grain.intensity.value = enabled ? 0.35f : 0f;
            grain.response.value = enabled ? 0.8f : 0f;
        }

        if (colorAdjust != null)
        {
            colorAdjust.active = enabled;
            colorAdjust.saturation.value = enabled ? -100f : 0f;  // ч/б
            colorAdjust.postExposure.value = enabled ? -0.3f : 0f;
            colorAdjust.contrast.value = enabled ? 20f : 0f;
        }
    }
}
