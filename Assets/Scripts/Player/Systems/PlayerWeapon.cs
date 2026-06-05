using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWeapon : MonoBehaviour
{
    [Header("Weapon")]
    [SerializeField] private WeaponManager weaponManager;
    [SerializeField] private Transform interactionOrigin;

    private PlayerInputActions inputActions;

    private void Awake()
    {
        inputActions = GameInput.Instance.Actions;
        if (weaponManager == null) weaponManager = GetComponentInChildren<WeaponManager>();
        if (interactionOrigin == null) interactionOrigin = Camera.main != null ? Camera.main.transform : transform;
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
        if (interactionOrigin == null && Camera.main != null)
            interactionOrigin = Camera.main.transform;

        if (interactionOrigin == null) return;

        if (Physics.Raycast(interactionOrigin.position, interactionOrigin.forward, out RaycastHit hit, 3f))
        {
            if (hit.collider.TryGetComponent<WeaponPickup>(out var pickup))
            {
                pickup.TryPickupFrom(this);
            }
        }
    }

    
    public void PickUpWeapon(GameObject weaponPrefab) => weaponManager?.PickupWeapon(weaponPrefab);
}
