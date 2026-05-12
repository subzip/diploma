using UnityEngine;

[DisallowMultipleComponent]
public class GuideOnWeaponPickup : MonoBehaviour
{
    [SerializeField] private string guideId;
    [SerializeField] private string weaponNameContains = "assault";
    [SerializeField] private bool completeAfterShow = true;

    private void OnEnable()
    {
        WeaponPickup.OnWeaponPickedUp += OnWeaponPicked;
    }

    private void OnDisable()
    {
        WeaponPickup.OnWeaponPickedUp -= OnWeaponPicked;
    }

    private void OnWeaponPicked(string pickedName)
    {
        if (string.IsNullOrWhiteSpace(guideId)) return;
        if (!string.IsNullOrWhiteSpace(weaponNameContains))
        {
            string n = pickedName ?? string.Empty;
            if (n.IndexOf(weaponNameContains, System.StringComparison.OrdinalIgnoreCase) < 0)
                return;
        }

        GuideManager manager = GuideManager.Instance;
        if (manager == null) return;
        manager.ShowGuide(guideId);
        if (completeAfterShow) manager.CompleteGuide(guideId, hide: false);
    }
}

