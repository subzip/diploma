using TMPro;
using UnityEngine;

public abstract class BaseWeapon : MonoBehaviour
{
    [SerializeField] public WeaponStats stats;

    [Header("UI")]
    [SerializeField] private TMP_Text ammoText;

    [Header("Effects")]
    [SerializeField] private float tracerDuration = 1f;
    [SerializeField] private GameObject bulletHolePrefab;
    [SerializeField] private Transform casingEjectPoint;

    [Header("Muzzle Flash")]
    [SerializeField] private Transform muzzlePoint;

    protected int currentAmmo;
    protected float nextFireTime;
    protected bool isReloading;
    protected Vector3 recoilOffset = Vector3.zero;

    private Vector2 recoilCurrent = Vector2.zero;
    private Vector2 recoilVelocity = Vector2.zero;
    private float currentBloom;

    private AudioSource audioSource;
    private PlayerMovement movement;
    private AimController aimController;

    public bool CanShoot => !isReloading && currentAmmo > 0 && Time.time >= nextFireTime;
    public bool IsReloading => isReloading;
    public int CurrentAmmo => currentAmmo;
    protected virtual float SpreadMultiplier => 1f;

    public virtual void Initialize()
    {
        currentAmmo = stats != null ? stats.magazineSize : 0;
        currentBloom = 0f;
    }

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.enabled = true;

        movement = GetComponentInParent<PlayerMovement>();
        aimController = GetComponentInParent<AimController>();
    }

    private void Update()
    {
        if (ammoText != null) ammoText.text = currentAmmo.ToString();
        RecoverBloom(Time.deltaTime);
    }

    public virtual void Shoot()
    {
        if (!CanShoot || stats == null) return;

        currentAmmo--;
        nextFireTime = Time.time + stats.fireRate;

        if (stats.shootSound != null) audioSource.PlayOneShot(stats.shootSound);

        Camera playerCamera = Camera.main;
        if (playerCamera == null) return;

        Ray centerRay = playerCamera.ViewportPointToRay(Vector2.one * 0.5f);
        Vector3 shotDirection = ApplySpread(centerRay.direction, playerCamera.transform);

        bool hit = Physics.Raycast(centerRay.origin, shotDirection, out RaycastHit hitInfo, stats.range, stats.hitLayers, QueryTriggerInteraction.Ignore);
        Vector3 tracerEnd = hit ? hitInfo.point : centerRay.origin + shotDirection * stats.range;

        if (stats.tracerEffectPrefab != null)
        {
            Transform origin = muzzlePoint != null ? muzzlePoint : transform;
            GameObject tracer = Instantiate(stats.tracerEffectPrefab, origin.position, Quaternion.LookRotation(tracerEnd - origin.position));
            Destroy(tracer, tracerDuration);
        }

        if (hit)
        {
            SpawnImpact(hitInfo);
            TrySpawnBulletHole(hitInfo);
            TryApplyPhysicsImpact(hitInfo, shotDirection);

            if (hitInfo.collider.TryGetComponent<IDamageable>(out IDamageable target))
            {
                target.TakeDamage(stats.damage, hitInfo.point);
            }
        }

        if (muzzlePoint != null && stats.muzzlePrefab != null)
        {
            GameObject flash = Instantiate(stats.muzzlePrefab, muzzlePoint.position, muzzlePoint.rotation);
            Destroy(flash, 2f);
        }

        if (currentAmmo == 0)
        {
            if (stats.emptyClipSound != null) audioSource.PlayOneShot(stats.emptyClipSound);
            Reload();
        }

        currentBloom = Mathf.Min(currentBloom + stats.bloomPerShot, stats.maxBloom);
        ApplyRecoil();
        EjectCasing();
    }

    protected virtual void ApplyRecoil()
    {
        float kick = Random.Range(stats.recoilKickMin, stats.recoilKickMax);
        float horiz = Random.Range(-stats.recoilHorizontal, stats.recoilHorizontal);
        recoilCurrent += new Vector2(horiz, -kick);
        recoilOffset = new Vector3(recoilCurrent.x, recoilCurrent.y, 0f);
    }

    public virtual void UpdateRecoil(float deltaTime)
    {
        if (stats == null) return;

        recoilCurrent = Vector2.SmoothDamp(
            recoilCurrent,
            Vector2.zero,
            ref recoilVelocity,
            1f / Mathf.Max(0.01f, stats.recoilRecoverySpeed),
            Mathf.Infinity,
            deltaTime
        );
        recoilOffset = new Vector3(recoilCurrent.x * stats.recoilSnap, recoilCurrent.y * stats.recoilSnap, 0f);
    }

    public virtual void Reload()
    {
        if (stats == null) return;
        if (isReloading || currentAmmo >= stats.magazineSize) return;
        isReloading = true;

        if (stats.reloadSound != null) audioSource.PlayOneShot(stats.reloadSound);

        Invoke(nameof(FinishReload), stats.reloadTime);
    }

    protected virtual void FinishReload()
    {
        if (stats == null)
        {
            isReloading = false;
            return;
        }

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

    private Vector3 ApplySpread(Vector3 forward, Transform cameraTransform)
    {
        float spreadDeg = GetCurrentSpread();
        if (spreadDeg <= 0.001f) return forward.normalized;

        float spreadRadius = Mathf.Tan(spreadDeg * Mathf.Deg2Rad);
        Vector2 random = Random.insideUnitCircle * spreadRadius;

        Vector3 direction = forward +
                            cameraTransform.right * random.x +
                            cameraTransform.up * random.y;

        return direction.normalized;
    }

    private float GetCurrentSpread()
    {
        float spread = aimController != null && aimController.IsAiming ? stats.aimSpread : stats.hipSpread;

        if (movement != null)
        {
            if (!movement.IsGrounded)
            {
                spread += stats.airSpreadPenalty;
            }
            else
            {
                if (movement.IsSprinting)
                {
                    spread += stats.sprintSpreadPenalty;
                }
                else if (movement.IsMoving)
                {
                    spread += stats.moveSpreadPenalty;
                }

                if (movement.IsCrouching)
                {
                    spread = Mathf.Max(0.01f, spread - stats.crouchSpreadReduction);
                }
            }
        }

        return (spread + currentBloom) * SpreadMultiplier;
    }

    private void RecoverBloom(float deltaTime)
    {
        if (stats == null) return;
        currentBloom = Mathf.MoveTowards(currentBloom, 0f, stats.bloomRecovery * deltaTime);
    }

    private void TrySpawnBulletHole(RaycastHit hitInfo)
    {
        if (bulletHolePrefab == null) return;
        if (hitInfo.collider.gameObject.layer != LayerMask.NameToLayer("Walls")) return;

        Rigidbody rb = hitInfo.collider.attachedRigidbody;
        if (rb != null) return;

        GameObject hole = Instantiate(
            bulletHolePrefab,
            hitInfo.point,
            Quaternion.FromToRotation(-Vector3.forward, hitInfo.normal)
        );
        Destroy(hole, 10f);
    }

    private void TryApplyPhysicsImpact(RaycastHit hitInfo, Vector3 shotDirection)
    {
        Rigidbody rb = hitInfo.collider.attachedRigidbody;
        if (rb == null || rb.isKinematic) return;
        rb.AddForceAtPosition(shotDirection * stats.impactForce, hitInfo.point, ForceMode.Impulse);
    }

    private void EjectCasing()
    {
        if (stats.casingPrefab == null || casingEjectPoint == null) return;

        GameObject casing = Instantiate(stats.casingPrefab, casingEjectPoint.position, casingEjectPoint.rotation);
        if (casing.TryGetComponent<Rigidbody>(out Rigidbody rb))
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

        if (prefab == null) return;

        GameObject impact = Instantiate(prefab, hitInfo.point, Quaternion.LookRotation(hitInfo.normal));
        Destroy(impact, 5f);
    }
}
