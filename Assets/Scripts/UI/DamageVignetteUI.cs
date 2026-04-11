using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DamageVignetteUI : MonoBehaviour
{
    [SerializeField] private Image vignette;
    [SerializeField] private Color vignetteColor = new Color(0.6f, 0f, 0f, 0.75f);
    [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    [SerializeField] private float fadeInDuration = 0.15f;
    [SerializeField] private float fadeOutDuration = 0.45f;

    private Coroutine currentPulse;
    private PlayerHealth cachedHealth;

    private void Awake()
    {
        if (vignette != null)
        {
            vignette.color = new Color(vignetteColor.r, vignetteColor.g, vignetteColor.b, 0f);
            vignette.raycastTarget = false;
        }
    }

    private void OnEnable()
    {
        cachedHealth = FindObjectOfType<PlayerHealth>();
        if (cachedHealth != null)
            cachedHealth.OnDamaged += OnPlayerDamaged;
    }

    private void OnDisable()
    {
        if (cachedHealth != null)
            cachedHealth.OnDamaged -= OnPlayerDamaged;
        cachedHealth = null;
    }

    private void OnPlayerDamaged(float damage, Vector3 hitPoint)
    {
        if (vignette == null) return;

        if (currentPulse != null)
            StopCoroutine(currentPulse);
        currentPulse = StartCoroutine(PulseVignette());
    }

    private IEnumerator PulseVignette()
    {
        float elapsed = 0f;
        float inDuration = Mathf.Max(0.001f, fadeInDuration);
        float outDuration = Mathf.Max(0.001f, fadeOutDuration);

        while (elapsed < inDuration)
        {
            float t = fadeInCurve.Evaluate(elapsed / inDuration);
            SetAlpha(t * vignetteColor.a);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < outDuration)
        {
            float t = fadeOutCurve.Evaluate(elapsed / outDuration);
            SetAlpha(t * vignetteColor.a);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        SetAlpha(0f);
        currentPulse = null;
    }

    private void SetAlpha(float alpha)
    {
        if (vignette == null) return;
        Color c = vignette.color;
        c.a = Mathf.Clamp01(alpha);
        vignette.color = c;
    }
}
