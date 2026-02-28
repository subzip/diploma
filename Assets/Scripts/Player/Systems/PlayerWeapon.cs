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
        inputActions = GameInput.Instance.Actions;
        cameraPivot = GetComponent<PlayerLook>().GetComponent<Transform>();
        if (weaponManager == null) weaponManager = GetComponent<WeaponManager>();
    }

    private void OnEnable()
    {
        inputActions.Player.PickUp.performed += OnPickUp;
    }

    private void OnDisable()
    {
        if (inputActions == null) return;
        inputActions.Player.PickUp.performed -= OnPickUp;
    }

    private void OnPickUp(InputAction.CallbackContext _) => TryPickUpWeapon();

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
