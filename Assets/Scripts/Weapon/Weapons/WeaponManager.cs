// Assets/Scripts/Weapons/WeaponManager.cs
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponManager : MonoBehaviour
{
    [Header("Weapons")]
    [SerializeField] private BaseWeapon[] weapons;
    [SerializeField] private int currentWeaponIndex = 0;
    

    private BaseWeapon CurrentWeapon => weapons[currentWeaponIndex];
    private PlayerInputActions inputActions;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
        if (weapons.Length > 0)
        {
            CurrentWeapon.Initialize();
        }
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Shoot.performed += OnShoot;
        inputActions.Player.Reload.performed += _ => CurrentWeapon?.Reload();
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

    private void OnShoot(InputAction.CallbackContext ctx)
    {
        if (CurrentWeapon == null) return;

        if (CurrentWeapon.stats.fireMode == FireMode.SemiAuto)
        {
            CurrentWeapon.Shoot();
        }
        // Auto обрабатывается в Update
    }

    private void Update()
    {
        // Автоматическая стрельба
        if (CurrentWeapon?.stats.fireMode == FireMode.Auto && 
            inputActions.Player.Shoot.ReadValue<float>() > 0.1f)
        {
            CurrentWeapon.Shoot();
        }

        // Обновление отдачи (для передачи в камеру)
        CurrentWeapon?.UpdateRecoil(Time.deltaTime);
        
    }

    // Для передачи отдачи игроку (в PlayerLook или отдельный компонент)
    public Vector3 GetRecoilOffset() => CurrentWeapon?.GetRecoilOffset() ?? Vector3.zero;
}