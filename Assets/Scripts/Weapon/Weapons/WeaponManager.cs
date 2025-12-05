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
            UpdateIKTarget();
        }
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Shoot.performed += OnShoot;
        inputActions.Player.Reload.performed += _ => CurrentWeapon?.Reload();
        inputActions.Player.Slot1.performed += _ => SwitchWeapon(0);
        inputActions.Player.Slot2.performed += _ => SwitchWeapon(1);
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
        inputActions.Player.Shoot.performed -= OnShoot;
        inputActions.Player.Slot1.performed -= _ => SwitchWeapon(0);
        inputActions.Player.Slot2.performed -= _ => SwitchWeapon(1);
    }

    private void OnShoot(InputAction.CallbackContext ctx)
    {
        if (CurrentWeapon == null) return;

        if (CurrentWeapon.stats.fireMode == FireMode.SemiAuto)
        {
            CurrentWeapon.Shoot();
        }
    }

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
        // Проверка корректности слота
        if (slotIndex < 0 || slotIndex >= weapons.Length || weapons[slotIndex] == null)
            return;

        // Если уже стоит это оружие — не переключаем
        if (currentWeaponIndex == slotIndex)
            return;

        // Деактивируем текущее оружие
        weapons[currentWeaponIndex].gameObject.SetActive(false);

        // Активируем новое оружие
        currentWeaponIndex = slotIndex;
        weapons[currentWeaponIndex].gameObject.SetActive(true);
        
        // Перемещаем оружие в правильный слот
        weapons[currentWeaponIndex].transform.SetParent(weaponSlots[currentWeaponIndex]);
        weapons[currentWeaponIndex].transform.localPosition = Vector3.zero;
        weapons[currentWeaponIndex].transform.localRotation = Quaternion.identity;

        // Обновляем IK-привязку
        UpdateIKTarget();
        
        Debug.Log($"Оружие изменено на слот {currentWeaponIndex + 1}");
    }

    private void UpdateIKTarget()
    {
        if (leftHandIK != null && CurrentWeapon != null && rightHandIK != null)
        {
            // Ищем точки привязки на оружии
            Transform leftGrip = CurrentWeapon.transform.Find("LeftHandP");
            Transform rightGrip = CurrentWeapon.transform.Find("RightHandP");
            
            if (leftGrip != null)
            {
                leftHandIK.data.target = leftGrip;
            }
            
            if (rightGrip != null)
            {
                rightHandIK.data.target = rightGrip;
            }
        }
    }

    public Vector3 GetRecoilOffset() => CurrentWeapon?.GetRecoilOffset() ?? Vector3.zero;
}