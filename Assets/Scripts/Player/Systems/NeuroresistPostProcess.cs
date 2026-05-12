using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Volume))]
public class NeuroresistPostProcess : MonoBehaviour
{
    [Header("Base Effect")]
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

    [Header("Glitch Dynamics")]
    [SerializeField] private bool useDynamicGlitch = true;
    [SerializeField, Min(0.1f)] private float blendInSpeed = 4f;
    [SerializeField, Min(0.1f)] private float blendOutSpeed = 3f;
    [SerializeField, Range(0f, 1f)] private float chromaticPulse = 0.08f;
    [SerializeField, Range(0f, 1f)] private float grainPulse = 0.12f;
    [SerializeField, Range(0f, 1f)] private float vignettePulse = 0.07f;
    [SerializeField, Min(0.1f)] private float pulseFrequency = 5f;
    [SerializeField, Min(0f)] private float jitterPositionAmplitude = 0.02f;
    [SerializeField, Min(0f)] private float jitterRotationAmplitude = 0.3f;

    [Header("Glitch Spikes")]
    [SerializeField] private bool useRandomSpikes = true;
    [SerializeField, Min(0.05f)] private float spikeIntervalMin = 0.35f;
    [SerializeField, Min(0.05f)] private float spikeIntervalMax = 1.1f;
    [SerializeField, Min(0.03f)] private float spikeDuration = 0.08f;
    [SerializeField, Range(0f, 1f)] private float spikeStrength = 0.75f;

    [Header("Optional UI Overlay")]
    [SerializeField] private CanvasGroup glitchOverlay;
    [SerializeField] private RectTransform glitchOverlayRect;
    [SerializeField, Range(0f, 1f)] private float overlayBaseAlpha = 0.08f;
    [SerializeField, Range(0f, 1f)] private float overlayPulseAlpha = 0.14f;
    [SerializeField, Min(0f)] private float overlayJitterPixels = 4f;

    private Volume volume;
    private Vignette vignette;
    private ChromaticAberration chromatic;
    private FilmGrain grain;
    private ColorAdjustments colorAdjust;

    private bool targetEnabled;
    private bool initialized;
    private float blend;
    private float nextSpikeTime;
    private float spikeEndTime;
    private Vector2 overlayBasePos;

    private void Awake()
    {
        TryInitialize(force: true);
    }

    private void OnEnable()
    {
        TryInitialize(force: false);
        ApplySnapshot(blend, 0f, 0f);
    }

    private void Update()
    {
        if (!initialized)
        {
            TryInitialize(force: false);
            if (!initialized) return;
        }

        float dt = Time.unscaledDeltaTime;
        float speed = targetEnabled ? blendInSpeed : blendOutSpeed;
        blend = Mathf.MoveTowards(blend, targetEnabled ? 1f : 0f, Mathf.Max(0.01f, speed) * dt);

        if (!useDynamicGlitch)
        {
            ApplySnapshot(blend, 0f, 0f);
            if (blend <= 0f) SetOverlayVisible(false, 0f);
            return;
        }

        float t = Time.unscaledTime;
        float pulse = 0.5f + 0.5f * Mathf.Sin(t * pulseFrequency);
        float spike = ComputeSpike(t);

        ApplySnapshot(blend, pulse, spike);
        UpdateCameraJitter(blend, pulse, spike);
        UpdateOverlay(blend, pulse, spike);
    }

    private float ComputeSpike(float time)
    {
        if (!useRandomSpikes || !targetEnabled)
            return 0f;

        if (time >= nextSpikeTime && time > spikeEndTime)
        {
            spikeEndTime = time + spikeDuration;
            ScheduleNextSpike();
        }

        if (time <= spikeEndTime)
        {
            float k = 1f - Mathf.Clamp01((spikeEndTime - time) / Mathf.Max(0.001f, spikeDuration));
            return (1f - k) * spikeStrength;
        }

        return 0f;
    }

    private void ScheduleNextSpike()
    {
        float minV = Mathf.Min(spikeIntervalMin, spikeIntervalMax);
        float maxV = Mathf.Max(spikeIntervalMin, spikeIntervalMax);
        nextSpikeTime = Time.unscaledTime + Random.Range(minV, maxV);
    }

    private void UpdateCameraJitter(float blend01, float pulse, float spike)
    {
        float amount = blend01 * (0.35f + pulse * 0.35f + spike * 0.9f);
        if (amount <= 0.0001f)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            return;
        }

        float px = (Mathf.PerlinNoise(Time.unscaledTime * 13.7f, 0.1f) - 0.5f) * 2f;
        float py = (Mathf.PerlinNoise(0.2f, Time.unscaledTime * 12.1f) - 0.5f) * 2f;
        float rz = (Mathf.PerlinNoise(Time.unscaledTime * 9.3f, 0.7f) - 0.5f) * 2f;

        transform.localPosition = new Vector3(
            px * jitterPositionAmplitude * amount,
            py * jitterPositionAmplitude * amount,
            0f
        );

        transform.localRotation = Quaternion.Euler(0f, 0f, rz * jitterRotationAmplitude * amount);
    }

    private void UpdateOverlay(float blend01, float pulse, float spike)
    {
        if (glitchOverlay == null)
            return;

        float alpha = blend01 * (overlayBaseAlpha + overlayPulseAlpha * pulse + spike * 0.25f);
        SetOverlayVisible(alpha > 0.001f, alpha);

        if (glitchOverlayRect == null)
            return;

        float j = blend01 * (overlayJitterPixels + spike * overlayJitterPixels * 2f);
        float ox = Random.Range(-j, j);
        float oy = Random.Range(-j, j);
        glitchOverlayRect.anchoredPosition = overlayBasePos + new Vector2(ox, oy);
    }

    private void SetOverlayVisible(bool visible, float alpha)
    {
        if (glitchOverlay == null) return;
        glitchOverlay.alpha = Mathf.Clamp01(alpha);
        glitchOverlay.blocksRaycasts = false;
        glitchOverlay.interactable = false;
        if (glitchOverlay.gameObject.activeSelf != visible)
            glitchOverlay.gameObject.SetActive(visible);

        if (!visible && glitchOverlayRect != null)
            glitchOverlayRect.anchoredPosition = overlayBasePos;
    }

    private void ApplySnapshot(float blend01, float pulse, float spike)
    {
        float pChrom = chromaticPulse * pulse + spike * 0.45f;
        float pGrain = grainPulse * pulse + spike * 0.55f;
        float pVignette = vignettePulse * pulse + spike * 0.2f;

        if (vignette != null)
        {
            vignette.active = blend01 > 0f;
            vignette.intensity.value = Mathf.Lerp(0f, activeVignette + pVignette, blend01);
            vignette.smoothness.value = Mathf.Lerp(0.2f, activeVignetteSmoothness, blend01);
        }

        if (chromatic != null)
        {
            chromatic.active = blend01 > 0f;
            chromatic.intensity.value = Mathf.Lerp(0f, activeChromatic + pChrom, blend01);
        }

        if (grain != null)
        {
            grain.active = blend01 > 0f;
            grain.intensity.value = Mathf.Lerp(0f, activeGrain + pGrain, blend01);
            grain.response.value = Mathf.Lerp(0f, activeGrainResponse, blend01);
        }

        if (colorAdjust != null)
        {
            colorAdjust.active = blend01 > 0f;
            colorAdjust.saturation.value = Mathf.Lerp(0f, activeSaturation - spike * 20f, blend01);
            colorAdjust.postExposure.value = Mathf.Lerp(0f, activePostExposure - spike * 0.2f, blend01);
            colorAdjust.contrast.value = Mathf.Lerp(0f, activeContrast + spike * 8f, blend01);
            colorAdjust.colorFilter.value = blend01 > 0f && useColorFilter ? activeColorFilter : Color.white;
        }
    }

    private void EnsureOverride<T>(ref T setting) where T : VolumeComponent, new()
    {
        if (volume.profile.TryGet(out setting)) return;
        setting = volume.profile.Add<T>(true);
        setting.active = false;
    }

    public void EnableEffects(bool enabled)
    {
        if (!initialized)
            TryInitialize(force: false);

        targetEnabled = enabled;
        if (!enabled)
            spikeEndTime = 0f;
        else
            ScheduleNextSpike();

        if (initialized)
        {
            if (enabled)
                ApplySnapshot(Mathf.Max(blend, 0.01f), 0.2f, 0f);
            else
                ApplySnapshot(0f, 0f, 0f);
        }
    }

    private void TryInitialize(bool force)
    {
        if (initialized && !force) return;

        if (volume == null)
            volume = GetComponent<Volume>();
        if (volume == null)
        {
            if (force) Debug.LogWarning("NeuroresistPostProcess: Volume component is missing.");
            initialized = false;
            return;
        }

        if (volume.profile == null && volume.sharedProfile != null)
            volume.profile = volume.sharedProfile;

        if (volume.profile == null)
        {
            if (force) Debug.LogWarning("NeuroresistPostProcess: Volume Profile is missing.");
            initialized = false;
            return;
        }

        volume.isGlobal = true;
        volume.weight = 1f;

        EnsureOverride(ref vignette);
        EnsureOverride(ref chromatic);
        EnsureOverride(ref grain);
        EnsureOverride(ref colorAdjust);

        if (glitchOverlayRect != null)
            overlayBasePos = glitchOverlayRect.anchoredPosition;

        SetOverlayVisible(false, 0f);
        initialized = true;
    }
}
