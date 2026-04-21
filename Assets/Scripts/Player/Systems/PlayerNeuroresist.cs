
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerNeuroresist : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float duration = 15f;
    [SerializeField] private float cooldown = 90f;
    [SerializeField] private float detectionRadius = 100f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField, Min(16)] private int detectionBufferSize = 256;

    [Header("Rendering")]
    [SerializeField] private string xrayLayerName = "XRay";
    [SerializeField] private NeuroresistPostProcess postProcess;
    [SerializeField] private Material xrayMaterialOverride;
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private bool disableOcclusionCullingWhileActive = true;

    private int xrayLayer;
    private bool isActive;
    private float endTime;

    private Collider[] detectedEnemies;
    private readonly List<Renderer> cachedRenderers = new();
    private readonly Dictionary<Renderer, int> cachedOriginalLayers = new();
    private readonly Dictionary<Renderer, Material[]> cachedOriginalMats = new();

    private PlayerInputActions input;
    private PlayerMovement movement;
    private float nextReadyTime = 0f;
    private bool cachedOcclusionValue;
    private bool cachedOcclusionInitialized;

    public float CurrentValue => isActive ? Mathf.Max(0f, endTime - Time.time) : 0f;
    public float MaxValue => duration;
    public bool IsActive => isActive;
    public float Cooldown => cooldown;
    public float CooldownRemaining => Mathf.Max(0f, nextReadyTime - Time.time);
    public bool IsReady => !isActive && Time.time >= nextReadyTime;
    public float Readiness01
    {
        get
        {
            if (isActive)
                return Mathf.Clamp01(CurrentValue / Mathf.Max(0.001f, duration));

            if (Time.time >= nextReadyTime)
                return 1f;

            float remaining = Mathf.Max(0f, nextReadyTime - Time.time);
            return Mathf.Clamp01(1f - remaining / Mathf.Max(0.001f, cooldown));
        }
    }

    private void Awake()
    {
        input = GameInput.Instance != null ? GameInput.Instance.Actions : null;
        movement = GetComponent<PlayerMovement>();
        detectedEnemies = new Collider[Mathf.Max(16, detectionBufferSize)];
        if (gameplayCamera == null) gameplayCamera = Camera.main;
        xrayLayer = LayerMask.NameToLayer(xrayLayerName);
        if (xrayLayer == -1)
        {
            Debug.LogWarning($"РЎР»РѕР№ '{xrayLayerName}' РЅРµ РЅР°Р№РґРµРЅ. РЎРѕР·РґР°Р№ СЃР»РѕР№ Рё РїСЂРёРІСЏР¶Рё РµРіРѕ РІ Render Feature.");
        }

        if (postProcess == null)
        {
            postProcess = GetComponentInChildren<NeuroresistPostProcess>();
            if (postProcess == null) postProcess = FindObjectOfType<NeuroresistPostProcess>();
        }
    }

    private void OnEnable()
    {
        if (input == null && GameInput.Instance != null)
            input = GameInput.Instance.Actions;
        if (input == null) return;

        input.Player.Neuroresist.performed += OnNeuroresist;
    }

    private void OnDisable()
    {
        if (input != null)
            input.Player.Neuroresist.performed -= OnNeuroresist;
        if (isActive) Deactivate();
        else SetOcclusionCullingDuringNeuro(active: false);
    }

    private void Update()
    {
        if (!isActive) return;
        if (Time.time >= endTime)
        {
            Deactivate();
        }
    }

    private void OnNeuroresist(InputAction.CallbackContext ctx)
    {
        if (!isActive && Time.time >= nextReadyTime) Activate();
    }

    private void Activate()
    {
        if (isActive || xrayLayer == -1) return;

        isActive = true;
        endTime = Time.time + duration;
        if (movement != null) movement.SetNeuroMultiplier(0.7f);
        SetOcclusionCullingDuringNeuro(active: true);

        int count = Physics.OverlapSphereNonAlloc(transform.position, detectionRadius, detectedEnemies, enemyLayer);
        if (count >= detectedEnemies.Length)
        {
            Debug.LogWarning($"PlayerNeuroresist: detection buffer is full ({detectedEnemies.Length}). Increase Detection Buffer Size.");
        }

        cachedRenderers.Clear();
        cachedOriginalLayers.Clear();
        cachedOriginalMats.Clear();

        for (int i = 0; i < count; i++)
        {
            if (detectedEnemies[i] == null) continue;

            var rends = detectedEnemies[i].GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends)
            {
                if (r == null) continue;
                if (cachedOriginalLayers.ContainsKey(r)) continue;

                cachedRenderers.Add(r);
                cachedOriginalLayers[r] = r.gameObject.layer;
                cachedOriginalMats[r] = r.sharedMaterials;
                r.gameObject.layer = xrayLayer;

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

        for (int i = 0; i < cachedRenderers.Count; i++)
        {
            Renderer renderer = cachedRenderers[i];
            if (renderer != null)
            {
                if (cachedOriginalLayers.TryGetValue(renderer, out int layer))
                    renderer.gameObject.layer = layer;

                if (cachedOriginalMats.TryGetValue(renderer, out Material[] mats) && mats != null)
                    renderer.sharedMaterials = mats;
            }
        }

        cachedRenderers.Clear();
        cachedOriginalLayers.Clear();
        cachedOriginalMats.Clear();

        if (postProcess != null) postProcess.EnableEffects(false);
        if (movement != null) movement.SetNeuroMultiplier(1f);
        SetOcclusionCullingDuringNeuro(active: false);

        nextReadyTime = Time.time + cooldown;
    }

    private void SetOcclusionCullingDuringNeuro(bool active)
    {
        if (!disableOcclusionCullingWhileActive) return;
        if (gameplayCamera == null) gameplayCamera = Camera.main;
        if (gameplayCamera == null) return;

        if (active)
        {
            if (!cachedOcclusionInitialized)
            {
                cachedOcclusionValue = gameplayCamera.useOcclusionCulling;
                cachedOcclusionInitialized = true;
            }
            gameplayCamera.useOcclusionCulling = false;
        }
        else if (cachedOcclusionInitialized)
        {
            gameplayCamera.useOcclusionCulling = cachedOcclusionValue;
            cachedOcclusionInitialized = false;
        }
    }
}
