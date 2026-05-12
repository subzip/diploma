using UnityEngine;

[DisallowMultipleComponent]
public class GuideOnWeaponShot : MonoBehaviour
{
    [SerializeField] private string guideId;
    [SerializeField] private bool onlyWeaponWithAimOverlay = true;
    [SerializeField] private string weaponNameContains = "assault";
    [SerializeField] private bool completeAfterShow = true;

    private void OnEnable()
    {
        BaseWeapon.OnAnyShotFired += OnAnyShot;
    }

    private void OnDisable()
    {
        BaseWeapon.OnAnyShotFired -= OnAnyShot;
    }

    private void OnAnyShot(BaseWeapon weapon)
    {
        if (weapon == null) return;
        if (string.IsNullOrWhiteSpace(guideId)) return;

        if (onlyWeaponWithAimOverlay && (weapon.stats == null || !weapon.stats.useAimOverlay))
            return;

        if (!string.IsNullOrWhiteSpace(weaponNameContains))
        {
            string n = weapon.stats != null ? weapon.stats.weaponName : weapon.name;
            if (n.IndexOf(weaponNameContains, System.StringComparison.OrdinalIgnoreCase) < 0)
                return;
        }

        GuideManager manager = GuideManager.Instance;
        if (manager == null) return;
        manager.ShowGuide(guideId);
        if (completeAfterShow) manager.CompleteGuide(guideId, hide: false);
    }
}

