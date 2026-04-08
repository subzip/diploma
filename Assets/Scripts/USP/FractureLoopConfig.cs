using UnityEngine;

[CreateAssetMenu(fileName = "FractureLoopConfig", menuName = "USP/Fracture Loop Config", order = 1)]
public class FractureLoopConfig : ScriptableObject
{
    [Header("Core")]
    [Min(2)] public int variantCount = 3;
    [Min(1)] public int entropyCap = 100;

    [Header("Entropy Gains")]
    [Min(0)] public int deathPenalty = 20;
    [Min(0)] public int repeatZoneDeathPenalty = 25;
    [Min(0)] public int suicidePenalty = 30;
    [Min(0)] public int majorProgressReward = 10;

    [Header("Recovery (Anti-Stuck)")]
    public bool enableRecovery = true;
    [Min(1)] public int stuckDeathsRequired = 2;
    [Min(30f)] public float stuckWindowSeconds = 240f;
    [Min(5f)] public float recoveryWindowSeconds = 12f;
    [Range(0f, 1f)] public float recoveryDamageMultiplier = 0.85f;

    [Header("Run Collapse")]
    public bool enableRunCollapse = false;
    [Min(1)] public int runCollapseThreshold = 95;

    [Header("Respawn Resource Rules")]
    [Range(0f, 1f)] public float reserveAmmoCarryPercent = 0.75f;
    [Range(0f, 1f)] public float medkitCarryPercent = 0.50f;

    [Header("Difficulty Tiers")]
    [Min(0)] public int tier1MaxEntropy = 24;
    [Min(0)] public int tier2MaxEntropy = 49;
    [Min(0)] public int tier3MaxEntropy = 74;

    public int GetTier(int entropy)
    {
        int value = Mathf.Clamp(entropy, 0, entropyCap);
        if (value <= tier1MaxEntropy) return 1;
        if (value <= tier2MaxEntropy) return 2;
        if (value <= tier3MaxEntropy) return 3;
        return 4;
    }

    private void OnValidate()
    {
        variantCount = Mathf.Max(2, variantCount);
        entropyCap = Mathf.Max(1, entropyCap);

        tier1MaxEntropy = Mathf.Clamp(tier1MaxEntropy, 0, entropyCap);
        tier2MaxEntropy = Mathf.Clamp(tier2MaxEntropy, tier1MaxEntropy, entropyCap);
        tier3MaxEntropy = Mathf.Clamp(tier3MaxEntropy, tier2MaxEntropy, entropyCap);

        runCollapseThreshold = Mathf.Clamp(runCollapseThreshold, 1, entropyCap);
    }
}
