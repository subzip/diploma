using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public abstract class BaseWeapon : MonoBehaviour
{
    [SerializeField] public WeaponStats stats;

    [Header("UI")]
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private string ammoTextObjectName = "Bullets";

    [Header("Effects")]
    [SerializeField] private float tracerFadeOut = 0.04f;
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
    protected int reserveAmmo;
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
    public int ReserveAmmo => reserveAmmo;
    protected virtual float SpreadMultiplier => 1f;

    public virtual void Initialize()
    {
        currentAmmo = stats != null ? stats.magazineSize : 0;
        reserveAmmo = stats != null ? Mathf.Max(0, stats.startReserveAmmo) : 0;
        if (stats != null && stats.maxReserveAmmo > 0)
            reserveAmmo = Mathf.Min(reserveAmmo, stats.maxReserveAmmo);
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
        ResolveAmmoTextIfNeeded();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        ResolveAmmoTextIfNeeded();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResolveAmmoTextIfNeeded(force: true);
    }

    private void Update()
    {
        ResolveAmmoTextIfNeeded();
        UpdateAmmoUi();
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

        SpawnTracer(tracerEnd);

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

        SpawnMuzzleFlash();

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
        if (reserveAmmo <= 0) return;
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

        int needed = Mathf.Max(0, stats.magazineSize - currentAmmo);
        int toLoad = Mathf.Min(needed, reserveAmmo);
        currentAmmo += toLoad;
        reserveAmmo -= toLoad;
        isReloading = false;
    }

    public virtual void CancelReload()
    {
        if (!isReloading) return;
        isReloading = false;
        CancelInvoke(nameof(FinishReload));
    }

    public Vector3 GetRecoilOffset() => recoilOffset;

    public void ResetForRespawn(bool refillAmmoToDefaults)
    {
        CancelReload();
        nextFireTime = 0f;
        recoilOffset = Vector3.zero;
        recoilCurrent = Vector2.zero;
        recoilVelocity = Vector2.zero;
        currentBloom = 0f;

        if (refillAmmoToDefaults)
        {
            Initialize();
        }

        RefreshAmmoUiBindingAndValue();
    }

    public int AddReserveAmmo(int amount)
    {
        if (stats == null || amount <= 0) return 0;

        int maxReserve = Mathf.Max(0, stats.maxReserveAmmo);
        if (maxReserve == 0) return 0;

        int before = reserveAmmo;
        reserveAmmo = Mathf.Clamp(reserveAmmo + amount, 0, maxReserve);
        return reserveAmmo - before;
    }

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

    private void SpawnMuzzleFlash()
    {
        if (stats == null || stats.muzzlePrefab == null) return;
        Transform origin = muzzlePoint != null ? muzzlePoint : transform;

        GameObject flash = Instantiate(stats.muzzlePrefab, origin.position, origin.rotation, origin);
        MuzzleFlashOneShot oneShot = flash.GetComponent<MuzzleFlashOneShot>();
        if (oneShot == null) oneShot = flash.AddComponent<MuzzleFlashOneShot>();
        oneShot.PlayAndAutoDestroy(stats.muzzleLifetime);
    }

    private void SpawnTracer(Vector3 tracerEnd)
    {
        if (stats == null) return;

        Transform origin = muzzlePoint != null ? muzzlePoint : transform;
        GameObject tracerObj = stats.tracerEffectPrefab != null
            ? Instantiate(stats.tracerEffectPrefab, origin.position, Quaternion.identity)
            : new GameObject("BulletTracer");

        TracerVFX tracer = tracerObj.GetComponent<TracerVFX>();
        if (tracer == null) tracer = tracerObj.AddComponent<TracerVFX>();

        tracer.Initialize(
            origin.position,
            tracerEnd,
            stats.tracerSpeed,
            stats.tracerWidth,
            stats.tracerLength,
            stats.tracerColor,
            stats.tracerMaterial,
            tracerFadeOut
        );
    }

    private void UpdateAmmoUi()
    {
        if (ammoText != null) ammoText.text = $"{currentAmmo} / {reserveAmmo}";
    }

    public void RefreshAmmoUiBindingAndValue()
    {
        ResolveAmmoTextIfNeeded(force: true);
        UpdateAmmoUi();
    }

    private void ResolveAmmoTextIfNeeded(bool force = false)
    {
        if (!force && ammoText != null) return;

        TMP_Text[] allTexts = FindObjectsOfType<TMP_Text>(true);
        TMP_Text exactActiveDd = null;
        TMP_Text exactActive = null;
        TMP_Text exactAnyDd = null;
        TMP_Text exactAny = null;
        TMP_Text ammoActive = null;
        TMP_Text bulletActive = null;
        TMP_Text fallback = null;

        for (int i = 0; i < allTexts.Length; i++)
        {
            TMP_Text text = allTexts[i];
            if (text == null) continue;

            string lower = text.name.ToLowerInvariant();
            bool active = text.gameObject.activeInHierarchy;
            bool inDdol = text.gameObject.scene.IsValid() && text.gameObject.scene.name == "DontDestroyOnLoad";

            if (lower == "bullets")
            {
                if (active && inDdol && exactActiveDd == null) exactActiveDd = text;
                if (active && exactActive == null) exactActive = text;
                if (inDdol && exactAnyDd == null) exactAnyDd = text;
                if (exactAny == null) exactAny = text;
                continue;
            }

            if (lower.Contains("ammo"))
            {
                if (active && ammoActive == null) ammoActive = text;
                if (fallback == null) fallback = text;
                continue;
            }

            if (lower.Contains("bullet"))
            {
                if (active && bulletActive == null) bulletActive = text;
                if (fallback == null) fallback = text;
            }
        }

        if (exactActiveDd != null)
        {
            ammoText = exactActiveDd;
            return;
        }

        if (exactActive != null)
        {
            ammoText = exactActive;
            return;
        }

        if (exactAnyDd != null)
        {
            ammoText = exactAnyDd;
            return;
        }

        if (exactAny != null)
        {
            ammoText = exactAny;
            return;
        }

        if (ammoActive != null)
        {
            ammoText = ammoActive;
            return;
        }

        if (bulletActive != null)
        {
            ammoText = bulletActive;
            return;
        }

        if (fallback != null)
        {
            ammoText = fallback;
            return;
        }

        if (!string.IsNullOrWhiteSpace(ammoTextObjectName))
        {
            GameObject named = GameObject.Find(ammoTextObjectName);
            if (named != null)
            {
                ammoText = named.GetComponent<TMP_Text>();
            }
        }
    }
}
