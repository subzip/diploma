using UnityEngine;

[DisallowMultipleComponent]
public class GuideOnAim : MonoBehaviour
{
    [SerializeField] private string guideId;
    [SerializeField] private bool requireWeaponWithAimOverlay = true;
    [SerializeField] private bool completeAfterShow = true;

    private AimController aimController;
    private WeaponManager weaponManager;
    private bool shown;

    private void Start()
    {
        aimController = FindObjectOfType<AimController>(true);
        weaponManager = FindObjectOfType<WeaponManager>(true);
    }

    private void Update()
    {
        if (shown) return;
        if (aimController == null) aimController = FindObjectOfType<AimController>(true);
        if (weaponManager == null) weaponManager = FindObjectOfType<WeaponManager>(true);
        if (aimController == null || weaponManager == null) return;
        if (!aimController.IsAiming) return;

        BaseWeapon current = weaponManager.CurrentWeapon;
        if (current == null) return;
        if (requireWeaponWithAimOverlay && (current.stats == null || !current.stats.useAimOverlay))
            return;

        GuideManager manager = GuideManager.Instance;
        if (manager == null) return;

        shown = true;
        manager.ShowGuide(guideId);
        if (completeAfterShow) manager.CompleteGuide(guideId, hide: false);
    }
}

