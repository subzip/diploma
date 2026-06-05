using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Combat Difficulty Profile", fileName = "CombatDifficultyProfile")]
public class CombatDifficultyProfile : ScriptableObject
{
    [Header("Global")]
    [SerializeField, Range(0.5f, 2f)] private float healthMultiplier = 1f;
    [SerializeField, Range(0.5f, 2f)] private float damageMultiplier = 1f;
    [SerializeField, Range(0.5f, 2f)] private float fireIntervalMultiplier = 1f;
    [SerializeField, Range(0.5f, 2f)] private float reactionDelayMultiplier = 1f;
    [SerializeField, Range(0.5f, 2f)] private float searchDurationMultiplier = 1f;
    [SerializeField, Range(0.5f, 2f)] private float aimSpreadMultiplier = 1f;

    [Header("Ground Shooter")]
    [SerializeField, Range(0.5f, 2f)] private float groundBurstDurationMultiplier = 1f;
    [SerializeField, Range(0.5f, 2f)] private float groundRepositionDistanceMultiplier = 1f;

    [Header("Drone Shooter")]
    [SerializeField, Range(0.5f, 2f)] private float droneMoveSpeedMultiplier = 1f;
    [SerializeField, Range(0.5f, 2f)] private float droneRotateSpeedMultiplier = 1f;
    [SerializeField, Range(0.5f, 2f)] private float droneWeaveMultiplier = 1f;
    [SerializeField, Range(0.5f, 2f)] private float droneAttackRangeMultiplier = 1f;

    public float HealthMultiplier => healthMultiplier;
    public float DamageMultiplier => damageMultiplier;
    public float FireIntervalMultiplier => fireIntervalMultiplier;
    public float ReactionDelayMultiplier => reactionDelayMultiplier;
    public float SearchDurationMultiplier => searchDurationMultiplier;
    public float AimSpreadMultiplier => aimSpreadMultiplier;
    public float GroundBurstDurationMultiplier => groundBurstDurationMultiplier;
    public float GroundRepositionDistanceMultiplier => groundRepositionDistanceMultiplier;
    public float DroneMoveSpeedMultiplier => droneMoveSpeedMultiplier;
    public float DroneRotateSpeedMultiplier => droneRotateSpeedMultiplier;
    public float DroneWeaveMultiplier => droneWeaveMultiplier;
    public float DroneAttackRangeMultiplier => droneAttackRangeMultiplier;
}

