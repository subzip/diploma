// Assets/Scripts/Weapons/WeaponStats.cs
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponStats", menuName = "Weapons/Weapon Stats", order = 1)]
public class WeaponStats : ScriptableObject
{
    [Header("Ammo")]
    public AmmoType ammoType = AmmoType.Rifle;
    [Min(0)] public int startReserveAmmo = 90;
    [Min(0)] public int maxReserveAmmo = 180;

    [Header("General")]
    public string weaponName = "Assault Rifle";
    public float damage = 25f;
    public float range = 100f;
    public int magazineSize = 30;
    public float reloadTime = 2f;

    [Header("Firing")]
    public float fireRate = 0.1f;
    public FireMode fireMode = FireMode.Auto;
    public float impactForce = 500f;

    [Header("Spread (COD-like Dynamic)")]
    [Min(0f)] public float hipSpread = 1.5f;
    [Min(0f)] public float aimSpread = 0.35f;
    [Min(0f)] public float moveSpreadPenalty = 0.8f;
    [Min(0f)] public float sprintSpreadPenalty = 1.3f;
    [Min(0f)] public float airSpreadPenalty = 1.7f;
    [Min(0f)] public float crouchSpreadReduction = 0.2f;
    [Min(0f)] public float bloomPerShot = 0.2f;
    [Min(0f)] public float maxBloom = 2.0f;
    [Min(0f)] public float bloomRecovery = 5f;

    [Header("Recoil")]
    public float recoilKickMin = 0.35f;
    public float recoilKickMax = 0.6f;
    public float recoilHorizontal = 0.25f;
    public float recoilRecoverySpeed = 6f;    // чем выше — быстрее возврат
    public float recoilSnap = 10f;            // чем выше — резче дергает камеру

    [Header("Effects")]
    public GameObject muzzlePrefab;
    public GameObject tracerEffectPrefab;
    public GameObject impactDefaultPrefab;
    public GameObject impactFleshPrefab;
    public GameObject casingPrefab;
    public LayerMask hitLayers = -1;
    [Header("Audio")]
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public AudioClip emptyClipSound;

    [Header("ADS Pose")]
    public Vector3 hipLocalPosition = Vector3.zero;
    public Vector3 hipLocalEuler = Vector3.zero;
    public Vector3 aimLocalPosition = new Vector3(0.015f, -0.02f, 0.08f);
    public Vector3 aimLocalEuler = Vector3.zero;
    public bool useAdsFovOverride = false;
    [Min(1f)] public float adsFovOverride = 55f;

    [Header("ADS Overlay")]
    public bool useAimOverlay = true;
}

public enum FireMode
{
    SemiAuto,
    Auto
}

public enum AmmoType
{
    Pistol,
    Rifle
}
