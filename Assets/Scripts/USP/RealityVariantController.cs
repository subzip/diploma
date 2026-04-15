using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

[Serializable]
public class RealityVariantBinding
{
    [Tooltip("Root object of this layout variant (environment geometry, blockers, props).")]
    public GameObject layoutRoot;

    [Tooltip("Optional root object with enemy setup for this variant.")]
    public GameObject enemiesRoot;

    [Tooltip("Optional root object that contains NavMeshSurface for this variant.")]
    public GameObject navMeshRoot;

    [Tooltip("Optional checkpoint spawn for this variant.")]
    public Transform respawnPoint;
}

public class RealityVariantController : MonoBehaviour
{
    [Header("Bindings")]
    [SerializeField] private RealityVariantBinding[] variants;

    [Header("Respawn Integration")]
    [SerializeField] private bool setCheckpointFromVariantSpawn = true;

    [Header("Navigation")]
    [SerializeField] private bool refreshNavAgentsAfterSwitch = true;
    [SerializeField] private float navRefreshDelay = 0.05f;
    [SerializeField] private bool forceRebuildNavMeshOnSwitch = false;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private DeathCycleManager manager;
    private bool subscribed;
    private bool variantCountWarningShown;
    private readonly List<Component> cachedNavSurfaces = new();

    private void Awake()
    {
        ResolveManager();
        ApplyCurrentVariant();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SubscribeToManager();
        ApplyCurrentVariant();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnsubscribeFromManager();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResolveManager();
        SubscribeToManager();
        ApplyCurrentVariant();
    }

    private void OnCycleStateChanged(int cycle, int variantIndex, int entropy, int tier)
    {
        ApplyVariantByIndex(variantIndex);
    }

    private void ResolveManager()
    {
        manager = DeathCycleManager.Instance;
        if (manager == null)
            manager = FindObjectOfType<DeathCycleManager>();
    }

    private void SubscribeToManager()
    {
        if (manager == null || subscribed) return;
        manager.OnCycleStateChanged += OnCycleStateChanged;
        subscribed = true;
    }

    private void UnsubscribeFromManager()
    {
        if (manager == null || !subscribed) return;
        manager.OnCycleStateChanged -= OnCycleStateChanged;
        subscribed = false;
    }

    private void ApplyCurrentVariant()
    {
        int index = manager != null ? manager.CurrentVariantIndex : 0;
        ApplyVariantByIndex(index);
    }

    private void ApplyVariantByIndex(int rawIndex)
    {
        if (variants == null || variants.Length == 0) return;
        ValidateVariantCountAgainstConfig();

        int index = NormalizeIndex(rawIndex, variants.Length);

        for (int i = 0; i < variants.Length; i++)
        {
            bool active = i == index;
            SetVariantActive(variants[i], active);
        }

        ApplyVariantCheckpoint(index);
        RefreshVariantNavMesh(index);
        if (refreshNavAgentsAfterSwitch)
        {
            StopAllCoroutines();
            StartCoroutine(RefreshAgentsAfterDelay());
        }

        if (debugLogs)
        {
            Debug.Log($"[RealityVariant] Applied variant {index} (raw={rawIndex}) in scene '{SceneManager.GetActiveScene().name}'.");
        }
    }

    private void ValidateVariantCountAgainstConfig()
    {
        if (manager == null || manager.Config == null) return;
        int configured = Mathf.Max(2, manager.Config.variantCount);
        if (configured == variants.Length) return;
        if (variantCountWarningShown) return;
        variantCountWarningShown = true;

        if (debugLogs)
        {
            Debug.LogWarning($"[RealityVariant] variants.Length ({variants.Length}) != config.variantCount ({configured}). " +
                             $"Controller will use bindings length.");
        }
    }

    private void SetVariantActive(RealityVariantBinding binding, bool active)
    {
        if (binding == null) return;
        if (binding.layoutRoot != null) binding.layoutRoot.SetActive(active);
        if (binding.enemiesRoot != null) binding.enemiesRoot.SetActive(active);
        if (binding.navMeshRoot != null) binding.navMeshRoot.SetActive(active);
    }

    private void ApplyVariantCheckpoint(int index)
    {
        if (!setCheckpointFromVariantSpawn) return;
        if (index < 0 || index >= variants.Length) return;

        RealityVariantBinding binding = variants[index];
        if (binding == null || binding.respawnPoint == null) return;

        string scene = SceneManager.GetActiveScene().name;
        RespawnCheckpointState.SetCheckpoint(
            scene,
            binding.respawnPoint.position,
            binding.respawnPoint.rotation
        );
    }

    private static int NormalizeIndex(int index, int count)
    {
        if (count <= 0) return 0;
        int mod = index % count;
        return mod < 0 ? mod + count : mod;
    }

    private void RefreshVariantNavMesh(int activeIndex)
    {
        for (int i = 0; i < variants.Length; i++)
        {
            RealityVariantBinding binding = variants[i];
            if (binding == null || binding.navMeshRoot == null) continue;

            bool shouldBeActive = i == activeIndex;
            CollectNavMeshSurfaces(binding.navMeshRoot, cachedNavSurfaces);
            for (int s = 0; s < cachedNavSurfaces.Count; s++)
            {
                Component surface = cachedNavSurfaces[s];
                if (surface == null) continue;

                if (!shouldBeActive)
                {
                    InvokeNavSurface(surface, "RemoveData");
                    continue;
                }

                // Ensure only current variant contributes navmesh data.
                InvokeNavSurface(surface, "RemoveData");
                if (forceRebuildNavMeshOnSwitch)
                    InvokeNavSurface(surface, "BuildNavMesh");
                else
                    InvokeNavSurface(surface, "AddData");
            }
        }
    }

    private static void CollectNavMeshSurfaces(GameObject root, List<Component> output)
    {
        output.Clear();
        if (root == null) return;

        Component[] components = root.GetComponentsInChildren<Component>(true);
        for (int i = 0; i < components.Length; i++)
        {
            Component c = components[i];
            if (c == null) continue;
            string typeName = c.GetType().Name;
            if (typeName == "NavMeshSurface")
            {
                output.Add(c);
            }
        }
    }

    private void InvokeNavSurface(Component surface, string methodName)
    {
        MethodInfo method = surface.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (method == null)
        {
            if (debugLogs)
                Debug.LogWarning($"[RealityVariant] NavMeshSurface method '{methodName}' not found on {surface.GetType().FullName}");
            return;
        }

        method.Invoke(surface, null);
    }

    private IEnumerator RefreshAgentsAfterDelay()
    {
        float wait = Mathf.Max(0f, navRefreshDelay);
        if (wait > 0f)
            yield return new WaitForSeconds(wait);

        NavMeshAgent[] agents = FindObjectsOfType<NavMeshAgent>(true);
        for (int i = 0; i < agents.Length; i++)
        {
            NavMeshAgent agent = agents[i];
            if (agent == null || !agent.enabled || !agent.isOnNavMesh) continue;
            agent.ResetPath();
        }
    }
}
