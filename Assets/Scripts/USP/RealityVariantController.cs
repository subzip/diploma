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

    }

    private void ValidateVariantCountAgainstConfig()
    {
        if (manager == null || manager.Config == null) return;
        int configured = Mathf.Max(2, manager.Config.variantCount);
        if (configured == variants.Length) return;
        if (variantCountWarningShown) return;
        variantCountWarningShown = true;

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
            binding.respawnPoint.rotation,
            $"variant_{index}"
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
            if (binding == null) continue;

            bool shouldBeActive = i == activeIndex;
            CollectVariantNavMeshSurfaces(binding, cachedNavSurfaces);
            for (int s = 0; s < cachedNavSurfaces.Count; s++)
            {
                Component surface = cachedNavSurfaces[s];
                if (surface == null) continue;

                if (!shouldBeActive)
                {
                    InvokeNavSurface(surface, "RemoveData");
                    continue;
                }

                InvokeNavSurface(surface, "RemoveData");
                if (forceRebuildNavMeshOnSwitch)
                    InvokeNavSurface(surface, "BuildNavMesh");
                else
                    InvokeNavSurface(surface, "AddData");
            }
        }
    }

    private static void CollectVariantNavMeshSurfaces(RealityVariantBinding binding, List<Component> output)
    {
        output.Clear();
        if (binding == null) return;

        if (binding.navMeshRoot != null)
        {
            CollectNavMeshSurfaces(binding.navMeshRoot, output);
            return;
        }

        if (binding.layoutRoot != null)
            CollectNavMeshSurfacesAppend(binding.layoutRoot, output);

        if (binding.enemiesRoot != null)
            CollectNavMeshSurfacesAppend(binding.enemiesRoot, output);
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

    private static void CollectNavMeshSurfacesAppend(GameObject root, List<Component> output)
    {
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
