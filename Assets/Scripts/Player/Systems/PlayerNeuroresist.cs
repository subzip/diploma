// PlayerNeuroresist.cs
// Активирует «скан через стены»: переводит видимых врагов во временный XRay-слой,
// который рендерится отдельным Render Feature (силуэт/подсветка), параллельно включает постэффекты.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInputActions))]
public class PlayerNeuroresist : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float duration = 15f;
    [SerializeField] private float detectionRadius = 100f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Rendering")]
    [Tooltip("Имя слоя, который попадает в RenderObjects feature для подсветки.")]
    [SerializeField] private string xrayLayerName = "XRay";
    [SerializeField] private NeuroresistPostProcess postProcess;
    [Tooltip("Опционально: временно применять override-материал вместо материала врага (если RenderObjects не ставит свой).")]
    [SerializeField] private Material xrayMaterialOverride;

    private int xrayLayer;
    private bool isActive;
    private float endTime;

    private readonly Collider[] detectedEnemies = new Collider[50];
    private readonly List<Renderer> cachedRenderers = new();
    private readonly List<int> cachedOriginalLayers = new();
    private readonly List<Material[]> cachedOriginalMats = new();

    private PlayerInputActions input;

    private void Awake()
    {
        input = new PlayerInputActions();
        xrayLayer = LayerMask.NameToLayer(xrayLayerName);
        if (xrayLayer == -1)
        {
            Debug.LogWarning($"Слой '{xrayLayerName}' не найден. Создай слой и привяжи его в Render Feature.");
        }

        // Автоподхват постпроцесса, если поле не выставлено в инспекторе.
        if (postProcess == null)
        {
            postProcess = GetComponentInChildren<NeuroresistPostProcess>();
            if (postProcess == null) postProcess = FindObjectOfType<NeuroresistPostProcess>();
        }
        if (postProcess == null)
        {
            Debug.LogWarning("NeuroresistPostProcess не назначен и не найден в сцене — экранные эффекты не включатся.");
        }
    }

    private void OnEnable()
    {
        input.Player.Enable();
        input.Player.Neuroresist.performed += OnNeuroresist;
    }

    private void OnDisable()
    {
        input.Player.Neuroresist.performed -= OnNeuroresist;
        input.Player.Disable();
        if (isActive) Deactivate(); // на всякий случай возвращаем слои
    }

    private void Update()
    {
        if (!isActive) return;

        if (Time.time >= endTime)
        {
            Deactivate();
            return;
        }
    }

    private void OnNeuroresist(InputAction.CallbackContext ctx)
    {
        if (!isActive) Activate();
    }

    private void Activate()
    {
        if (isActive || xrayLayer == -1) return;

        isActive = true;
        endTime = Time.time + duration;

        // Находим врагов вокруг игрока
        int count = Physics.OverlapSphereNonAlloc(transform.position, detectionRadius, detectedEnemies, enemyLayer);

        cachedRenderers.Clear();
        cachedOriginalLayers.Clear();
        cachedOriginalMats.Clear();

        for (int i = 0; i < count; i++)
        {
            if (detectedEnemies[i] == null) continue;

            var rends = detectedEnemies[i].GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends)
            {
                cachedRenderers.Add(r);
                cachedOriginalLayers.Add(r.gameObject.layer);
                cachedOriginalMats.Add(r.sharedMaterials);
                r.gameObject.layer = xrayLayer;

                // Если рендер-фича не переопределяет материал, можно временно поставить свой.
                if (xrayMaterialOverride != null)
                {
                    var mats = new Material[r.sharedMaterials.Length];
                    for (int m = 0; m < mats.Length; m++) mats[m] = xrayMaterialOverride;
                    r.sharedMaterials = mats;
                }
            }
        }

        if (postProcess != null) postProcess.EnableEffects(true);
    }

    private void Deactivate()
    {
        isActive = false;

        // Вернуть слои
        for (int i = 0; i < cachedRenderers.Count; i++)
        {
            if (cachedRenderers[i] != null)
            {
                cachedRenderers[i].gameObject.layer = cachedOriginalLayers[i];

                // Вернём материалы, если меняли.
                if (cachedOriginalMats.Count == cachedRenderers.Count && cachedOriginalMats[i] != null)
                {
                    cachedRenderers[i].sharedMaterials = cachedOriginalMats[i];
                }
            }
        }

        cachedRenderers.Clear();
        cachedOriginalLayers.Clear();
        cachedOriginalMats.Clear();

        if (postProcess != null) postProcess.EnableEffects(false);
    }
}
