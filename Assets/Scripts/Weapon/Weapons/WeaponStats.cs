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
    public float fireRate = 0.1f; // 0.1 = 10 выстрелов/сек
    public FireMode fireMode = FireMode.Auto;
    public float impactForce = 500f; 

    [Header("Recoil")]
    public Vector2 recoilAngle = new Vector2(0.5f, 0.2f); // вверх, вправо
    public float recoilRecoverySpeed = 5f;

    [Header("Effects")]
    public GameObject muzzlePrefab;
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