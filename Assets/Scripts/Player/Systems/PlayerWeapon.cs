using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging;

public class PlayerWeapon : MonoBehaviour
{
    [Header("IK & Weapon")]
    [SerializeField] private TwoBoneIKConstraint leftHandIK;
    [SerializeField] private Transform rightHandGripPoint;
    [SerializeField] private WeaponManager weaponManager;

    private PlayerInputActions inputActions;
    private Transform cameraPivot;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
        cameraPivot = GetComponent<PlayerLook>().GetComponent<Transform>();
        if (weaponManager == null) weaponManager = GetComponent<WeaponManager>();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.PickUp.performed += _ => TryPickUpWeapon();
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

    private void TryPickUpWeapon()
    {
        if (Physics.Raycast(cameraPivot.position, cameraPivot.forward, out RaycastHit hit, 3f))
        {
            if (hit.collider.TryGetComponent<WeaponPickup>(out var pickup))
            {
                if (weaponManager != null)
                {
                    // Если в пикапе лежит сценовый экземпляр оружия, отдаём его; иначе — prefab
                    GameObject toGive = hit.collider.GetComponent<BaseWeapon>() != null
                        ? hit.collider.gameObject
                        : pickup.weaponPrefab;

                    weaponManager.PickupWeapon(toGive);
                }
                pickup.Consume();
            }
        }
    }

    // Подбор теперь через WeaponManager; оставлено для совместимости
    public void PickUpWeapon(GameObject weaponPrefab) => weaponManager?.PickupWeapon(weaponPrefab);
}
