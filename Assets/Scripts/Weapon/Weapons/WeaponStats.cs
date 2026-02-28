// Assets/Scripts/Weapons/WeaponStats.cs
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponStats", menuName = "Weapons/Weapon Stats", order = 1)]
public class WeaponStats : ScriptableObject
{
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
}

public enum FireMode
{
    SemiAuto,
    Auto
}
