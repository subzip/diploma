// Assets/Scripts/Weapons/BaseWeapon.cs
using UnityEngine;

public abstract class BaseWeapon : MonoBehaviour
{
    [SerializeField] public WeaponStats stats;
    protected int currentAmmo;
    protected float nextFireTime;
    protected bool isReloading = false;
    protected Vector3 recoilOffset = Vector3.zero;
    [SerializeField] protected GameObject tracerPrefab;
    [SerializeField] private float tracerDuration = 0.1f;

    public bool CanShoot => !isReloading && currentAmmo > 0 && Time.time >= nextFireTime;

    public virtual void Initialize()
    {
        currentAmmo = stats.magazineSize;
    }

    public virtual void Shoot()
    {
        if (!CanShoot) return;

        currentAmmo--;
        nextFireTime = Time.time + stats.fireRate;

        // Воспроизведение звука и вспышки (по-прежнему от оружия)
        if (stats.shootSound != null)
            AudioSource.PlayClipAtPoint(stats.shootSound, transform.position);
        if (stats.muzzleFlash != null)
            stats.muzzleFlash.Play();

        // 🔥 Raycast от ЦЕНТРА КАМЕРЫ, а не от оружия!
        Camera playerCamera = Camera.main;
        if (playerCamera == null) return;

        Ray ray = playerCamera.ViewportPointToRay(Vector2.one * 0.5f);
        bool hit = Physics.Raycast(ray, out RaycastHit hitInfo, stats.range, stats.hitLayers);

        // Определяем конечную точку трассера
        Vector3 tracerEnd = hit ? hitInfo.point : ray.GetPoint(stats.range);

        // Создаём трассер ОТ ОРУЖИЯ до точки попадания
        if (tracerPrefab != null)
        {
            GameObject tracerObj = Instantiate(tracerPrefab, transform.position, Quaternion.identity);
            LineRenderer lr = tracerObj.GetComponent<LineRenderer>();
            if (lr != null)
            {
                lr.SetPosition(0, transform.position); // Начало — из оружия
                lr.SetPosition(1, tracerEnd);         // Конец — в точку попадания
            }

            // Уничтожаем после задержки
            Destroy(tracerObj, tracerDuration);
        }

        // Наносим урон ТОЛЬКО если попали
        if (hit && hitInfo.collider.TryGetComponent<IDamageable>(out IDamageable target))
        {
            target.TakeDamage(stats.damage, hitInfo.point);
        }

        if(currentAmmo == 0)
            Reload();

        ApplyRecoil();
    }

    protected virtual void ApplyRecoil()
    {
        recoilOffset += new Vector3(
            Random.Range(-stats.recoilAngle.y, stats.recoilAngle.y),
            stats.recoilAngle.x,
            0
        );
    }

    public virtual void UpdateRecoil(float deltaTime)
    {
        if (recoilOffset != Vector3.zero)
        {
            recoilOffset = Vector3.Lerp(recoilOffset, Vector3.zero, stats.recoilRecoverySpeed * deltaTime);
        }
    }

    public virtual void Reload()
    {
        if (isReloading || currentAmmo == stats.magazineSize) return;
        isReloading = true;
        Invoke(nameof(FinishReload), stats.reloadTime);
    }

    protected virtual void FinishReload()
    {
        currentAmmo = stats.magazineSize;
        isReloading = false;
    }

    // Для передачи отдачи игроку
    public Vector3 GetRecoilOffset() => recoilOffset;
}