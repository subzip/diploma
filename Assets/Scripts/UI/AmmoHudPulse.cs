using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;





public class AmmoHudPulse : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private RectTransform currentAmmoRoot;
    [SerializeField] private RectTransform reserveAmmoRoot;
    [SerializeField] private TMP_Text currentAmmoText;
    [SerializeField] private TMP_Text reserveAmmoText;

    [Header("Pulse")]
    [SerializeField] private float pulseDuration = 0.1f;
    [SerializeField] private float pulseScale = 1.09f;
    [SerializeField] private float jitterPixels = 1.2f;
    [SerializeField] private Color idleCurrentColor = new Color(0.9f, 0.95f, 1f, 0.9f);
    [SerializeField] private Color fireCurrentColor = new Color(0.45f, 0.92f, 1f, 1f);
    [SerializeField] private Color idleReserveColor = new Color(0.86f, 0.9f, 1f, 0.55f);
    [SerializeField] private bool affectReserveLightly = true;

    private Vector2 currentBasePos;
    private Vector2 reserveBasePos;
    private Vector3 currentBaseScale;
    private Vector3 reserveBaseScale;
    private float pulseTimer;
    private int lastAmmoInMag = -1;
    private BaseWeapon lastWeapon;
    private float nextResolveAttemptTime;

    private void Awake()
    {
        ResolveReferences();

        if (currentAmmoRoot != null)
        {
            currentBasePos = currentAmmoRoot.anchoredPosition;
            currentBaseScale = currentAmmoRoot.localScale;
        }
        if (reserveAmmoRoot != null)
        {
            reserveBasePos = reserveAmmoRoot.anchoredPosition;
            reserveBaseScale = reserveAmmoRoot.localScale;
        }

        ApplyIdleVisual();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResolveReferences(force: true);
        lastWeapon = null;
        lastAmmoInMag = -1;
        pulseTimer = 0f;
        ApplyIdleVisual();
    }

    private void Update()
    {
        if (weaponManager == null && Time.unscaledTime >= nextResolveAttemptTime)
        {
            ResolveReferences();
            nextResolveAttemptTime = Time.unscaledTime + 0.5f;
        }
        BaseWeapon weapon = weaponManager != null ? weaponManager.CurrentWeapon : null;

        if (weapon != lastWeapon)
        {
            lastWeapon = weapon;
            lastAmmoInMag = weapon != null ? weapon.CurrentAmmo : -1;
            pulseTimer = 0f;
            ApplyIdleVisual();
            return;
        }

        if (weapon != null)
        {
            if (lastAmmoInMag >= 0 && weapon.CurrentAmmo < lastAmmoInMag)
            {
                TriggerPulse();
            }
            lastAmmoInMag = weapon.CurrentAmmo;
        }

        UpdatePulseVisual();
    }

    private void TriggerPulse()
    {
        pulseTimer = pulseDuration;
    }

    private void UpdatePulseVisual()
    {
        if (currentAmmoRoot == null && currentAmmoText == null) return;

        if (pulseTimer > 0f)
        {
            pulseTimer -= Time.unscaledDeltaTime;
            float t = 1f - Mathf.Clamp01(pulseTimer / Mathf.Max(0.0001f, pulseDuration));
            float arc = Mathf.Sin(t * Mathf.PI);

            if (currentAmmoRoot != null)
            {
                currentAmmoRoot.localScale = Vector3.Lerp(currentBaseScale, currentBaseScale * pulseScale, arc);
                currentAmmoRoot.anchoredPosition = currentBasePos + Random.insideUnitCircle * jitterPixels * arc;
            }

            if (currentAmmoText != null)
            {
                currentAmmoText.color = Color.Lerp(idleCurrentColor, fireCurrentColor, arc);
            }

            if (affectReserveLightly && reserveAmmoRoot != null)
            {
                reserveAmmoRoot.localScale = Vector3.Lerp(reserveBaseScale, reserveBaseScale * (1f + (pulseScale - 1f) * 0.3f), arc);
                reserveAmmoRoot.anchoredPosition = reserveBasePos + Random.insideUnitCircle * jitterPixels * 0.4f * arc;
            }
        }
        else
        {
            ApplyIdleVisual();
        }
    }

    private void ApplyIdleVisual()
    {
        if (currentAmmoRoot != null)
        {
            currentAmmoRoot.localScale = currentBaseScale;
            currentAmmoRoot.anchoredPosition = currentBasePos;
        }
        if (reserveAmmoRoot != null)
        {
            reserveAmmoRoot.localScale = reserveBaseScale;
            reserveAmmoRoot.anchoredPosition = reserveBasePos;
        }

        if (currentAmmoText != null) currentAmmoText.color = idleCurrentColor;
        if (reserveAmmoText != null) reserveAmmoText.color = idleReserveColor;
    }

    private void ResolveReferences(bool force = false)
    {
        if (!force && weaponManager != null) return;
        weaponManager = FindObjectOfType<WeaponManager>();
    }
}
