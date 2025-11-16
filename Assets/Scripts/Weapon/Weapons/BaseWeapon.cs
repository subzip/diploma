// Assets/Scripts/Weapons/BaseWeapon.cs
using UnityEngine;
using TMPro;

public abstract class BaseWeapon : MonoBehaviour
{
    [SerializeField] public WeaponStats stats;
    protected int currentAmmo;
    protected float nextFireTime;
    protected bool isReloading = false;
    protected Vector3 recoilOffset = Vector3.zero;
    
    [SerializeField] private float tracerDuration = 0.1f;
    [SerializeField] private TMP_Text ammoText;

    public bool CanShoot => !isReloading && currentAmmo > 0 && Time.time >= nextFireTime;


    [Header("Effects")]
    [SerializeField] private GameObject bulletHolePrefab;
    [SerializeField] protected GameObject tracerEffectPrefab;

    private AudioSource audioSource;

    public virtual void Initialize()
    {
        currentAmmo = stats.magazineSize;
    }

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.enabled = true;
    }

    void Update()
    {
        ammoText.text = currentAmmo.ToString();
    }

    public virtual void Shoot()
    {
        if (!CanShoot) return;

        currentAmmo--;
        nextFireTime = Time.time + stats.fireRate;

        if (stats.shootSound != null)
            audioSource.PlayOneShot(stats.shootSound);
        if (stats.muzzleFlash != null)
            stats.muzzleFlash.Play();

       
        Camera playerCamera = Camera.main;
        if (playerCamera == null) return;

        Ray ray = playerCamera.ViewportPointToRay(Vector2.one * 0.5f);
        bool hit = Physics.Raycast(ray, out RaycastHit hitInfo, stats.range, stats.hitLayers);
        Vector3 tracerEnd = hit ? hitInfo.point : ray.GetPoint(stats.range);


        if (tracerEffectPrefab != null)
        {
            GameObject tracer = Instantiate(tracerEffectPrefab, transform.position, Quaternion.identity);
            tracer.transform.LookAt(tracerEnd);
            Destroy(tracer, 1f);
        }

        if (hit && bulletHolePrefab != null)
        {
            GameObject hole = Instantiate(bulletHolePrefab, hitInfo.point, Quaternion.FromToRotation(-Vector3.forward, hitInfo.normal));
            Destroy(hole, 10f);
        }


        if (hit && hitInfo.collider.TryGetComponent<IDamageable>(out IDamageable target))
        {
            target.TakeDamage(stats.damage, hitInfo.point);
        }

        if (stats.shootSound != null && currentAmmo != 0) audioSource.PlayOneShot(stats.shootSound);

        if(currentAmmo == 0){
            if (stats.emptyClipSound != null)
                audioSource.PlayOneShot(stats.emptyClipSound);
            Reload();
        }

            

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
        if (isReloading || currentAmmo >= stats.magazineSize) return;
        isReloading = true;
        
        if (stats.reloadSound != null)
            audioSource.PlayOneShot(stats.reloadSound);
        
        Invoke(nameof(FinishReload), stats.reloadTime);
    }

    protected virtual void FinishReload()
    {
        currentAmmo = stats.magazineSize;
        isReloading = false;
    }

    public Vector3 GetRecoilOffset() => recoilOffset;
}