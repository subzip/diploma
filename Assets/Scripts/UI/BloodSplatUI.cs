using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BloodSplatUI : MonoBehaviour
{
    [SerializeField] private RectTransform container;
    [SerializeField] private Image splatPrefab;
    [SerializeField] private Sprite[] splatSprites;

    [Header("Behavior")]
    [SerializeField] private int maxActiveSplats = 8;
    [SerializeField] private float baseLifetime = 2f;
    [SerializeField] private float fadeInTime = 0.08f;
    [SerializeField] private Vector2 sizeRange = new Vector2(110f, 240f);
    [SerializeField] private Vector2 edgePadding = new Vector2(80f, 80f);
    [SerializeField] private float damageToAlpha = 0.015f;

    private readonly List<SplatState> activeSplats = new();

    private sealed class SplatState
    {
        public Image image;
        public float age;
        public float life;
        public float peakAlpha;
    }

    private void Awake()
    {
        if (container == null) container = transform as RectTransform;
    }

    private void Update()
    {
        for (int i = activeSplats.Count - 1; i >= 0; i--)
        {
            SplatState splat = activeSplats[i];
            if (splat.image == null)
            {
                activeSplats.RemoveAt(i);
                continue;
            }

            splat.age += Time.unscaledDeltaTime;
            float alpha = EvaluateAlpha(splat);
            Color c = splat.image.color;
            c.a = alpha;
            splat.image.color = c;

            if (splat.age >= splat.life)
            {
                Destroy(splat.image.gameObject);
                activeSplats.RemoveAt(i);
            }
        }
    }

    public void ShowDamage(float damage, float healthNormalized)
    {
        if (container == null || splatPrefab == null || splatSprites == null || splatSprites.Length == 0) return;

        int splatCount = damage >= 35f ? 2 : 1;
        float dangerBoost = 1f + Mathf.Clamp01(1f - healthNormalized) * 0.35f;
        for (int i = 0; i < splatCount; i++)
        {
            SpawnSplat(damage * dangerBoost);
        }
    }

    private void SpawnSplat(float damage)
    {
        while (activeSplats.Count >= Mathf.Max(1, maxActiveSplats))
        {
            if (activeSplats[0].image != null) Destroy(activeSplats[0].image.gameObject);
            activeSplats.RemoveAt(0);
        }

        Image image = Instantiate(splatPrefab, container);
        image.gameObject.SetActive(true);
        image.raycastTarget = false;
        image.sprite = splatSprites[Random.Range(0, splatSprites.Length)];

        RectTransform rt = image.rectTransform;
        rt.anchoredPosition = GetEdgePosition(container.rect);
        float size = Random.Range(sizeRange.x, sizeRange.y) * Mathf.Lerp(0.85f, 1.25f, Mathf.Clamp01(damage / 50f));
        rt.sizeDelta = new Vector2(size, size);
        rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        Color c = image.color;
        c.a = 0f;
        image.color = c;

        activeSplats.Add(new SplatState
        {
            image = image,
            age = 0f,
            life = baseLifetime + Random.Range(-0.25f, 0.35f),
            peakAlpha = Mathf.Clamp01(0.18f + damage * damageToAlpha)
        });
    }

    private float EvaluateAlpha(SplatState state)
    {
        if (state.age <= fadeInTime)
        {
            return Mathf.Lerp(0f, state.peakAlpha, state.age / Mathf.Max(0.001f, fadeInTime));
        }

        float fadeDuration = Mathf.Max(0.001f, state.life - fadeInTime);
        float t = Mathf.Clamp01((state.age - fadeInTime) / fadeDuration);
        return Mathf.Lerp(state.peakAlpha, 0f, t);
    }

    private Vector2 GetEdgePosition(Rect rect)
    {
        float halfW = rect.width * 0.5f - edgePadding.x;
        float halfH = rect.height * 0.5f - edgePadding.y;
        float edgePick = Random.value;

        if (edgePick < 0.25f) return new Vector2(Random.Range(-halfW, halfW), halfH);          // top
        if (edgePick < 0.5f) return new Vector2(Random.Range(-halfW, halfW), -halfH);          // bottom
        if (edgePick < 0.75f) return new Vector2(-halfW, Random.Range(-halfH, halfH));         // left
        return new Vector2(halfW, Random.Range(-halfH, halfH));                                  // right
    }
}
