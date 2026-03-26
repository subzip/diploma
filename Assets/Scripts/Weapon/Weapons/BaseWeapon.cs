using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public abstract class BaseWeapon : MonoBehaviour
{
    [SerializeField] public WeaponStats stats;

    [Header("UI")]
    [SerializeField] private TMP_Text ammoText;

    [Header("Effects")]
    [SerializeField] private float tracerDuration = 1f;
    [SerializeField] private GameObject bulletHolePrefab;
    [SerializeField] private Transform casingEjectPoint;
    [SerializeField] private bool useDecalProjector = true;
    [SerializeField] private Material decalMaterial;
    [SerializeField] private Vector2 decalSize = new Vector2(0.08f, 0.08f);
    [SerializeField] private float decalDepth = 0.02f;
    [SerializeField] private float decalPush = 0.002f;
    [SerializeField] private float decalLifetime = 12f;
    [SerializeField] private float decalTextureScale = 2.5f;
    [SerializeField] private bool decalUseNormalForward = true;
    [SerializeField] private bool decalParentToHit = false;
    [SerializeField] private Transform decalParentOverride;
    [Header("Decal Debug")]
    [SerializeField] private bool debugDecals = false;
    [SerializeField] private bool debugDecalsVerbose = false;
    [SerializeField] private Vector2 debugDecalSize = new Vector2(0.5f, 0.5f);
    [SerializeField] private float debugDecalDepth = 0.5f;
    [SerializeField] private Color debugDecalColor = new Color(1f, 0f, 0f, 1f);

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
    private Material runtimeDecalMaterial;

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

    private void OnDestroy()
    {
        if (runtimeDecalMaterial != null)
        {
            Destroy(runtimeDecalMaterial);
        }
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
            if (debugDecalsVerbose)
            {
                Debug.Log($"[DecalDebug] HIT {hitInfo.collider.name} layer {hitInfo.collider.gameObject.layer} point {hitInfo.point} normal {hitInfo.normal}");
            }
            SpawnImpact(hitInfo);
            TrySpawnBulletHole(hitInfo);
            TryApplyPhysicsImpact(hitInfo, shotDirection);

            if (hitInfo.collider.TryGetComponent<IDamageable>(out IDamageable target))
            {
                target.TakeDamage(stats.damage, hitInfo.point);
            }
        }
        else if (debugDecalsVerbose)
        {
            Debug.Log("[DecalDebug] Raycast MISS");
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
        if (useDecalProjector && decalMaterial != null)
        {
            SpawnDecalProjector(hitInfo);
            return;
        }

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

    private void SpawnDecalProjector(RaycastHit hitInfo)
    {
        Vector3 normal = hitInfo.normal;
        Vector3 forward = decalUseNormalForward ? normal : -normal;

        Vector2 size = debugDecals ? debugDecalSize : decalSize;
        float depth = debugDecals ? debugDecalDepth : decalDepth;
        float halfDepth = Mathf.Max(0.001f, depth) * 0.5f;

        Vector3 position = hitInfo.point + forward * (decalPush + halfDepth);

        // Stabilize orientation around the normal.
        Vector3 up = Vector3.up;
        if (Mathf.Abs(Vector3.Dot(up, forward)) > 0.98f)
            up = Vector3.right;
        Quaternion rotation = Quaternion.LookRotation(forward, up);

        GameObject decalObj = new GameObject("BulletDecal");
        decalObj.transform.SetPositionAndRotation(position, rotation);
        if (decalParentOverride != null)
        {
            decalObj.transform.SetParent(decalParentOverride, true);
        }
        else if (decalParentToHit)
        {
            decalObj.transform.SetParent(hitInfo.collider.transform, true);
        }

        DecalProjector projector = decalObj.AddComponent<DecalProjector>();
        projector.material = GetDecalMaterial();

        projector.size = new Vector3(size.x, size.y, Mathf.Max(0.001f, depth));
        projector.pivot = new Vector3(0f, 0f, -0.5f);
        projector.fadeFactor = 1f;
        projector.drawDistance = 50f;
        projector.startAngleFade = 180f;
        projector.endAngleFade = 180f;

        if (debugDecals && decalMaterial != null)
        {
            Material debugMat = new Material(decalMaterial);
            ApplyDecalTextureScale(debugMat);
            if (debugMat.HasProperty("_BaseColor")) debugMat.SetColor("_BaseColor", debugDecalColor);
            if (debugMat.HasProperty("_Color")) debugMat.SetColor("_Color", debugDecalColor);
            projector.material = debugMat;
        }

        if (debugDecals)
        {
            Debug.Log($"[DecalDebug] Spawned at {hitInfo.point}, normal {hitInfo.normal}, layer {hitInfo.collider.gameObject.layer}");
            Debug.DrawRay(hitInfo.point, hitInfo.normal * 0.3f, Color.red, 2f);
            Debug.DrawRay(hitInfo.point, forward * 0.3f, Color.green, 2f);
        }

        Destroy(decalObj, decalLifetime);
    }

    private Material GetDecalMaterial()
    {
        if (decalMaterial == null) return null;

        if (runtimeDecalMaterial == null)
        {
            runtimeDecalMaterial = new Material(decalMaterial);
            ApplyDecalTextureScale(runtimeDecalMaterial);
        }

        return runtimeDecalMaterial;
    }

    private void ApplyDecalTextureScale(Material mat)
    {
        if (mat == null) return;

        float scale = Mathf.Max(0.05f, decalTextureScale);
        Vector2 tiling = new Vector2(scale, scale);

        if (mat.HasProperty("_BaseMap")) mat.SetTextureScale("_BaseMap", tiling);
        if (mat.HasProperty("_MainTex")) mat.SetTextureScale("_MainTex", tiling);
        if (mat.HasProperty("_DecalTex")) mat.SetTextureScale("_DecalTex", tiling);
        if (mat.HasProperty("Base_Map")) mat.SetTextureScale("Base_Map", tiling);

        if (mat.HasProperty("_BaseMap_ST")) mat.SetVector("_BaseMap_ST", new Vector4(scale, scale, 0f, 0f));
        if (mat.HasProperty("_MainTex_ST")) mat.SetVector("_MainTex_ST", new Vector4(scale, scale, 0f, 0f));
        if (mat.HasProperty("_DecalTex_ST")) mat.SetVector("_DecalTex_ST", new Vector4(scale, scale, 0f, 0f));
        if (mat.HasProperty("Base_Map_ST")) mat.SetVector("Base_Map_ST", new Vector4(scale, scale, 0f, 0f));
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
