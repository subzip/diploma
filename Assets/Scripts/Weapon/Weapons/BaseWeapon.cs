
using UnityEngine;
using TMPro;
using Unity.VisualScripting;

public abstract class BaseWeapon : MonoBehaviour
{
    [SerializeField] public WeaponStats stats;
    protected int currentAmmo;
    protected float nextFireTime;
    protected bool isReloading = false;
    protected Vector3 recoilOffset = Vector3.zero;
    private Vector2 recoilCurrent = Vector2.zero;
    private Vector2 recoilVelocity = Vector2.zero;
    
    [SerializeField] private float tracerDuration = 0.1f;
    [SerializeField] private TMP_Text ammoText;

    public bool CanShoot => !isReloading && currentAmmo > 0 && Time.time >= nextFireTime;
    public bool IsReloading => isReloading;
    public int CurrentAmmo => currentAmmo;


    [Header("Effects")]
    [SerializeField] private GameObject bulletHolePrefab;
    [SerializeField] private Transform casingEjectPoint;

    [Header("Muzzle Flash")]
    [SerializeField] private Transform muzzlePoint;

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
        if (ammoText != null) ammoText.text = currentAmmo.ToString();
    }

    public virtual void Shoot()
    {
        if (!CanShoot) return;

        currentAmmo--;
        nextFireTime = Time.time + stats.fireRate;

        if (stats.shootSound != null)
            audioSource.PlayOneShot(stats.shootSound);
        

        Camera playerCamera = Camera.main;
        if (playerCamera == null) return;

        Ray ray = playerCamera.ViewportPointToRay(Vector2.one * 0.5f);
        bool hit = Physics.Raycast(ray, out RaycastHit hitInfo, stats.range, stats.hitLayers, QueryTriggerInteraction.Ignore);
        Vector3 tracerEnd = hit ? hitInfo.point : ray.GetPoint(stats.range);


        if (stats.tracerEffectPrefab != null)
        {
            GameObject tracer = Instantiate(stats.tracerEffectPrefab, muzzlePoint != null ? muzzlePoint.position : transform.position, Quaternion.identity);
            tracer.transform.LookAt(tracerEnd);
            Destroy(tracer, 1f);
        }

        // Impact visuals & decals
        if (hit)
        {
            SpawnImpact(hitInfo);

            if (bulletHolePrefab != null && hitInfo.collider.gameObject.layer == LayerMask.NameToLayer("Walls"))
            {
                Rigidbody rb = hitInfo.collider.attachedRigidbody;
                if (rb == null)
                {
                    GameObject hole = Instantiate(
                        bulletHolePrefab,
                        hitInfo.point,
                        Quaternion.FromToRotation(-Vector3.forward, hitInfo.normal)
                    );
                    Destroy(hole, 10f);
                }
            }

            // Physics impact
            Rigidbody rb_ = hitInfo.collider.attachedRigidbody;
            if (rb_ != null && !rb_.isKinematic)
            {
                Vector3 forceDirection = (hitInfo.point - transform.position).normalized;
                rb_.AddForceAtPosition(forceDirection * stats.impactForce, hitInfo.point, ForceMode.Impulse);
            }
        }

        if (muzzlePoint != null && stats.muzzlePrefab != null)
        {
            GameObject flash = Instantiate(
                stats.muzzlePrefab,
                muzzlePoint.position,   
                muzzlePoint.rotation    
            );
            Destroy(flash, 2f);
        }


        if (hit && hitInfo.collider.TryGetComponent<IDamageable>(out IDamageable target))
        {
            target.TakeDamage(stats.damage, hitInfo.point);
        }

        if(currentAmmo == 0){
            if (stats.emptyClipSound != null)
                audioSource.PlayOneShot(stats.emptyClipSound);
            Reload();
        }

            

        ApplyRecoil();

        EjectCasing();
    }

    protected virtual void ApplyRecoil()
    {
        float kick = Random.Range(stats.recoilKickMin, stats.recoilKickMax);
        float horiz = Random.Range(-stats.recoilHorizontal, stats.recoilHorizontal);

        // Камера смотрит вверх при отрицательном pitch, поэтому вертикальный импульс инвертируем
        recoilCurrent += new Vector2(horiz, -kick);
        recoilOffset = new Vector3(recoilCurrent.x, recoilCurrent.y, 0);
    }

    public virtual void UpdateRecoil(float deltaTime)
    {
        recoilCurrent = Vector2.SmoothDamp(recoilCurrent, Vector2.zero, ref recoilVelocity, 1f / Mathf.Max(0.01f, stats.recoilRecoverySpeed), Mathf.Infinity, deltaTime);
        recoilOffset = new Vector3(recoilCurrent.x * stats.recoilSnap, recoilCurrent.y * stats.recoilSnap, 0);
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

    public virtual void CancelReload()
    {
        if (!isReloading) return;
        isReloading = false;
        CancelInvoke(nameof(FinishReload));
    }

    public Vector3 GetRecoilOffset() => recoilOffset;

    private void EjectCasing()
    {
        if (stats.casingPrefab == null || casingEjectPoint == null) return;
        var casing = Instantiate(stats.casingPrefab, casingEjectPoint.position, casingEjectPoint.rotation);
        if (casing.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.AddForce(casingEjectPoint.right * Random.Range(1.5f, 2.5f) + Vector3.up * Random.Range(0.5f, 1f), ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);
        }
        Destroy(casing, 6f);
    }

    private void SpawnImpact(RaycastHit hitInfo)
    {
        GameObject prefab = stats.impactDefaultPrefab;
        if (hitInfo.collider.CompareTag("Enemy") && stats.impactFleshPrefab != null)
            prefab = stats.impactFleshPrefab;

        if (prefab != null)
        {
            var impact = Instantiate(prefab, hitInfo.point, Quaternion.LookRotation(hitInfo.normal));
            Destroy(impact, 5f);
        }
    }
}
