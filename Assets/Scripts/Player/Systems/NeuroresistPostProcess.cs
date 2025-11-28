
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


[RequireComponent(typeof(Volume))]
public class NeuroresistPostProcess : MonoBehaviour
{
    private Volume volume;
    //private Grayscale grayscale;
    private Vignette vignette;
    private ChromaticAberration chromatic;
    private FilmGrain grain;

    private void Awake()
    {
        volume = GetComponent<Volume>();
        //volume.profile.TryGet(out grayscale);
        volume.profile.TryGet(out vignette);
        volume.profile.TryGet(out chromatic);
        volume.profile.TryGet(out grain);
    }

    public void EnableEffects(bool enabled)
    {
        //if (grayscale != null) grayscale.active = enabled;
        if (vignette != null) vignette.active = enabled;
        if (chromatic != null) chromatic.active = enabled;
        if (grain != null) grain.active = enabled;

       
        if (enabled)
        {
            //if (grayscale != null) grayscale.intensity.value = 1f;
            if (vignette != null) vignette.intensity.value = 0.6f;
            if (chromatic != null) chromatic.intensity.value = 0.1f;
            if (grain != null) grain.intensity.value = 0.3f;
        }
    }
}