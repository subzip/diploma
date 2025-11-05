using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging;

public class PlayerWeapon : MonoBehaviour
{
    [Header("IK & Weapon")]
    [SerializeField] private TwoBoneIKConstraint leftHandIK;
    [SerializeField] private Transform rightHandGripPoint;

    private PlayerInputActions inputActions;
    private Transform cameraPivot;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
        cameraPivot = GetComponent<PlayerLook>().GetComponent<Transform>();
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
                pickup.gameObject.SetActive(false);
                PickUpWeapon(pickup.weaponPrefab);
                Destroy(pickup.gameObject);
            }
        }
    }

    public void PickUpWeapon(GameObject weaponPrefab)
    {
        foreach (Transform child in transform)
        {
            if (child.CompareTag("Weapon")) Destroy(child.gameObject);
        }

        GameObject weapon = Instantiate(weaponPrefab, rightHandGripPoint.position, rightHandGripPoint.rotation);
        weapon.transform.SetParent(transform);
        weapon.tag = "Weapon";

        if (leftHandIK != null)
        {
            Transform leftGrip = weapon.transform.Find("LeftHandP");
            if (leftGrip != null) leftHandIK.data.target = leftGrip;
        }
    }
}