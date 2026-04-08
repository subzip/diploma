using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum DeathKind
{
    Combat,
    Suicide,
    Scripted
}

public class DeathCycleManager : MonoBehaviour
{
    private static DeathCycleManager instance;
    public static DeathCycleManager Instance => instance;

    [Header("Config")]
    [SerializeField] private FractureLoopConfig config;

    [Header("Runtime")]
    [SerializeField] private int cycleCount;
    [SerializeField] private int currentVariantIndex;
    [SerializeField] private int entropy;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;
    [SerializeField] private string mainMenuSceneName = "StartGame";

    private string lastDeathZoneId;
    private float lastDeathTime = -999f;

    public int CycleCount => cycleCount;
    public int CurrentVariantIndex => currentVariantIndex;
    public int Entropy => entropy;
    public int EntropyTier => GetTier(entropy);
    public FractureLoopConfig Config => config;

    public event Action<int, int, int, int> OnCycleStateChanged;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            if (instance.config == null && config != null)
            {
                instance.config = config;
            }
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (config == null)
        {
            Debug.LogWarning("DeathCycleManager: FractureLoopConfig is not assigned. Using built-in defaults.");
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == mainMenuSceneName)
        {
            ResetRunState();
        }
    }

    public void RegisterDeath(DeathKind deathKind, string zoneId = "")
    {
        int cap = config != null ? Mathf.Max(1, config.entropyCap) : 100;
        int variantCount = config != null ? Mathf.Max(2, config.variantCount) : 3;

        int penalty = GetBasePenalty(deathKind);

        bool sameZoneRepeat = !string.IsNullOrEmpty(zoneId) &&
                              zoneId == lastDeathZoneId &&
                              Time.time - lastDeathTime <= GetStuckWindowSeconds();

        if (sameZoneRepeat)
        {
            int repeatPenalty = config != null ? config.repeatZoneDeathPenalty : 25;
            penalty = Mathf.Max(penalty, repeatPenalty);
        }

        entropy = Mathf.Clamp(entropy + Mathf.Max(0, penalty), 0, cap);
        cycleCount += 1;
        currentVariantIndex = (currentVariantIndex + 1) % variantCount;

        lastDeathZoneId = zoneId;
        lastDeathTime = Time.time;

        if (debugLogs)
        {
            Debug.Log($"[DeathCycle] death={deathKind}, zone='{zoneId}', +entropy={penalty}, entropy={entropy}, tier={EntropyTier}, variant={currentVariantIndex}, cycles={cycleCount}");
        }

        OnCycleStateChanged?.Invoke(cycleCount, currentVariantIndex, entropy, EntropyTier);
    }

    public void RegisterMajorProgress()
    {
        int reward = config != null ? Mathf.Max(0, config.majorProgressReward) : 10;
        entropy = Mathf.Max(0, entropy - reward);

        if (debugLogs)
        {
            Debug.Log($"[DeathCycle] major progress: -{reward} entropy => {entropy}");
        }

        OnCycleStateChanged?.Invoke(cycleCount, currentVariantIndex, entropy, EntropyTier);
    }

    public void ResetRunState()
    {
        cycleCount = 0;
        currentVariantIndex = 0;
        entropy = 0;
        lastDeathZoneId = string.Empty;
        lastDeathTime = -999f;

        if (debugLogs)
        {
            Debug.Log("[DeathCycle] run state reset");
        }

        OnCycleStateChanged?.Invoke(cycleCount, currentVariantIndex, entropy, EntropyTier);
    }

    public bool IsRunCollapseReached()
    {
        if (config == null || !config.enableRunCollapse) return false;
        return entropy >= config.runCollapseThreshold;
    }

    private int GetBasePenalty(DeathKind deathKind)
    {
        if (config == null)
        {
            return deathKind == DeathKind.Suicide ? 30 : 20;
        }

        return deathKind switch
        {
            DeathKind.Suicide => config.suicidePenalty,
            _ => config.deathPenalty
        };
    }

    private float GetStuckWindowSeconds()
    {
        if (config == null) return 240f;
        return Mathf.Max(30f, config.stuckWindowSeconds);
    }

    private int GetTier(int entropyValue)
    {
        if (config == null)
        {
            if (entropyValue <= 24) return 1;
            if (entropyValue <= 49) return 2;
            if (entropyValue <= 74) return 3;
            return 4;
        }

        return config.GetTier(entropyValue);
    }
}
