using UnityEngine;

[System.Serializable]
public class Weapon
{
    [Header("General")]
    public string weaponName = "Assault Rifle";
    public float damage = 25f;
    public float fireRate = 0.1f; // Скорострельность (1 выстрел в 0.1 сек)
    public int magazineSize = 30;
    public int maxAmmo = 120;
    public float reloadTime = 2f;
    public float maxRange = 100f;

    [Header("Spread")]
    public float aimSpread = 0.1f;      // Точность при прицеливании
    public float hipSpread = 0.5f;      // Рассеивание при стрельбе с бедра
    public float spreadRecoveryTime = 0.2f; // Время возврата к базовому рассеиванию

    [Header("Recoil")]
    public float recoilKickback = 0.1f; // Отдача вверх
    public float recoilRecoverySpeed = 5f;
    public float maxRecoil = 1f;        // Максимальный угол отдачи

    [Header("Shooting")]
    public ShootingMode shootingMode = ShootingMode.Auto;
    public LayerMask hitLayers = -1; // Слои, в которые можно стрелять

    [Header("Effects")]
    public ParticleSystem muzzleFlash;
    public GameObject hitEffectPrefab;
    public GameObject impactDecalPrefab;
    [Range(0.001f, 10f)] public float impactDecalSize = 1f;

    [Header("Audio")]
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public AudioClip emptyClipSound;

    // Внутренние состояния
    [HideInInspector] public float nextFireTime;
    [HideInInspector] public int currentAmmo;
    [HideInInspector] public bool isReloading;
    [HideInInspector] public float currentSpread;
    [HideInInspector] public float currentRecoil;

    public virtual void Initialize()
    {
        currentAmmo = magazineSize;
        currentSpread = aimSpread;
        currentRecoil = 0f;
    }

    public virtual bool CanShoot(float currentTime)
    {
        return !isReloading && currentAmmo > 0 && currentTime >= nextFireTime;
    }

    public virtual void OnShoot()
    {
        currentAmmo--;
        nextFireTime = Time.time + fireRate;
        currentSpread = Mathf.Min(currentSpread + aimSpread * 0.5f, hipSpread);
        currentRecoil = Mathf.Clamp(currentRecoil + recoilKickback, 0, maxRecoil);
    }

    public virtual void Update(float deltaTime)
    {
        // Восстановление рассеивания
        if (currentSpread > aimSpread)
        {
            currentSpread = Mathf.Max(aimSpread, currentSpread - (deltaTime / spreadRecoveryTime));
        }

        // Восстановление отдачи
        if (currentRecoil > 0)
        {
            currentRecoil = Mathf.Max(0, currentRecoil - (recoilRecoverySpeed * deltaTime));
        }
    }
}

public enum ShootingMode
{
    SemiAuto,  // Одиночный
    Auto       // Автомат
}