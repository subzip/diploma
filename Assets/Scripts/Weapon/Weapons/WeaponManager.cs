// Assets/Scripts/Weapons/WeaponManager.cs
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging;

public class WeaponManager : MonoBehaviour
{
    [Header("Weapons")]
    [SerializeField] private BaseWeapon[] weapons;
    [SerializeField] private Transform[] weaponSlots; // Слоты для оружия
    [SerializeField] private int currentWeaponIndex = 0;
    
    [Header("IK Setup")]
    [SerializeField] private TwoBoneIKConstraint leftHandIK;
    [SerializeField] private TwoBoneIKConstraint rightHandIK;
    //[SerializeField] private Transform rightHandGripPoint;

    private BaseWeapon CurrentWeapon => weapons[currentWeaponIndex];
    [SerializeField] private RigBuilder rigBuilder;
    private PlayerInputActions inputActions;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
        
        // Инициализация всех оружий
        for (int i = 0; i < weapons.Length; i++)
        {
            weapons[i].Initialize();
            weapons[i].gameObject.SetActive(false); // Изначально все отключены
        }
        
        // Активируем первое оружие
        if (weapons.Length > 0 && weaponSlots.Length > 0)
        {
            weapons[0].gameObject.SetActive(true);
            weapons[0].transform.SetParent(weaponSlots[0]);
            UpdateIKTargets(weapons[0].transform);
        }
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Shoot.performed += OnShoot;
        inputActions.Player.Reload.performed += OnReload;
        inputActions.Player.Slot1.performed += OnSlot1;
        inputActions.Player.Slot2.performed += OnSlot2;
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
        inputActions.Player.Shoot.performed -= OnShoot;
        inputActions.Player.Reload.performed -= OnReload;
        inputActions.Player.Slot1.performed -= OnSlot1;
        inputActions.Player.Slot2.performed -= OnSlot2;
    }

    private void OnShoot(InputAction.CallbackContext ctx)
    {
        if (CurrentWeapon == null) return;

        if (CurrentWeapon.stats.fireMode == FireMode.SemiAuto)
        {
            CurrentWeapon.Shoot();
        }
    }

    private void OnReload(InputAction.CallbackContext ctx) => CurrentWeapon?.Reload();
    private void OnSlot1(InputAction.CallbackContext ctx) => SwitchWeapon(0);
    private void OnSlot2(InputAction.CallbackContext ctx) => SwitchWeapon(1);

    private void Update()
    {
        if (CurrentWeapon?.stats.fireMode == FireMode.Auto && 
            inputActions.Player.Shoot.ReadValue<float>() > 0.1f)
        {
            CurrentWeapon.Shoot();
        }

        CurrentWeapon?.UpdateRecoil(Time.deltaTime);
    }

    private void SwitchWeapon(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= weapons.Length || weapons[slotIndex] == null)
            return;

        if (currentWeaponIndex == slotIndex)
            return;

        // Деактивируем старое оружие
        if (weapons[currentWeaponIndex] != null)
        {
            weapons[currentWeaponIndex].gameObject.SetActive(false);
        }

        // Активируем новое
        currentWeaponIndex = slotIndex;
        BaseWeapon newWeapon = weapons[currentWeaponIndex];
        newWeapon.gameObject.SetActive(true);
        
        // Помещаем оружие в слот
        newWeapon.transform.SetParent(weaponSlots[currentWeaponIndex]);
        // newWeapon.transform.localPosition = Vector3.zero;
        // newWeapon.transform.localRotation = Quaternion.identity;

        // 🔥 ОБНОВЛЯЕМ IK-ТАРГЕТЫ
        UpdateIKTargets(newWeapon.transform);
    }

    private void UpdateIKTargets(Transform weaponRoot)
    {
        // Обновляем цель для левой руки
        if (leftHandIK != null)
        {
            Transform leftGrip = weaponRoot.Find("LeftHandP");
            if (leftGrip != null)
            {
                leftHandIK.data.target = leftGrip;
            }
            else
            {
                Debug.LogWarning($"Не найдена точка 'LeftHandP' на оружии {weaponRoot.name}");
            }
        }

        // Обновляем позицию правой руки (если используется)
        if (rightHandIK != null)
        {
            Transform rightGrip = weaponRoot.Find("RightHandP");
            if (rightGrip != null)
            {
                rightHandIK.data.target = rightGrip;
            }
            else
            {
                Debug.LogWarning($"Не найдена точка 'RightHandP' на оружии {weaponRoot.name}");
            }
        }

        if (rigBuilder != null)
        {
            rigBuilder.enabled = false;
            rigBuilder.enabled = true;
        }
    }

    public Vector3 GetRecoilOffset() => CurrentWeapon?.GetRecoilOffset() ?? Vector3.zero;
}
