// Assets/Scripts/Weapons/WeaponManager.cs
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging;

public class WeaponManager : MonoBehaviour
{
    [Header("Weapons")]
    [SerializeField] private Transform[] weaponSlots; // слоты для оружия на руках
    [SerializeField] private int currentWeaponIndex = 0;
    [SerializeField] private float switchCooldown = 0.25f;

    [Header("IK Setup")]
    [SerializeField] private TwoBoneIKConstraint leftHandIK;
    [SerializeField] private TwoBoneIKConstraint rightHandIK;
    [SerializeField] private RigBuilder rigBuilder;

    private readonly System.Collections.Generic.List<BaseWeapon> weapons = new();
    private BaseWeapon CurrentWeapon => (currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Count) ? weapons[currentWeaponIndex] : null;
    private PlayerInputActions inputActions;
    private bool isSwitching = false;

    private void Awake()
    {
        inputActions = new PlayerInputActions();

        // Собираем оружие из детей слотов (никаких сериализованных массивов, чтобы не путать префабы и инстансы)
        for (int i = 0; i < weaponSlots.Length; i++)
        {
            var existing = weaponSlots[i].GetComponentInChildren<BaseWeapon>(true);
            weapons.Add(existing);
            if (existing == null) continue;

            existing.Initialize();
            existing.transform.SetParent(weaponSlots[i]);
            existing.transform.localPosition = Vector3.zero;
            existing.transform.localRotation = Quaternion.identity;
            existing.gameObject.SetActive(false);

            Debug.Log($"[WeaponManager] Slot {i} scene weapon: {existing.name} (id {existing.GetInstanceID()})");
        }

        // Стартуем с пистолета (если найден), иначе первое доступное
        int startIndex = FindWeaponIndexByName("Pistol");
        if (startIndex == -1) startIndex = FindFirstExistingWeapon();
        if (startIndex != -1) EquipImmediate(startIndex);
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Shoot.performed += OnShoot;
        inputActions.Player.Reload.performed += OnReload;
        inputActions.Player.Slot1.performed += OnSlot1;
        inputActions.Player.Slot2.performed += OnSlot2;
        inputActions.Player.Slot3.performed += OnSlot3;
    }

    private void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.Player.Shoot.performed -= OnShoot;
            inputActions.Player.Reload.performed -= OnReload;
            inputActions.Player.Slot1.performed -= OnSlot1;
            inputActions.Player.Slot2.performed -= OnSlot2;
            inputActions.Player.Slot3.performed -= OnSlot3;
            inputActions.Player.Disable();
        }
    }

    private void OnDestroy()
    {
        if (inputActions != null)
        {
            inputActions.Dispose();
            inputActions = null;
        }
    }

    private void Update()
    {
        if (!isSwitching && CurrentWeapon?.stats.fireMode == FireMode.Auto &&
            inputActions.Player.Shoot.ReadValue<float>() > 0.1f)
        {
            CurrentWeapon.Shoot();
        }

        CurrentWeapon?.UpdateRecoil(Time.deltaTime);

        // Прямое чтение клавиш 1-3, чтобы не лезть в InputActions-ассет
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) SwitchWeapon(0);
            if (Keyboard.current.digit2Key.wasPressedThisFrame) SwitchWeapon(1);
            if (Keyboard.current.digit3Key.wasPressedThisFrame) SwitchWeapon(2);
        }
    }

    private void OnShoot(InputAction.CallbackContext ctx)
    {
        if (isSwitching) return;
        if (CurrentWeapon == null) return;

        if (CurrentWeapon.stats.fireMode == FireMode.SemiAuto)
        {
            CurrentWeapon.Shoot();
        }
    }

    private void OnReload(InputAction.CallbackContext ctx) => CurrentWeapon?.Reload();
    private void OnSlot1(InputAction.CallbackContext ctx) => SwitchWeapon(0);
    private void OnSlot2(InputAction.CallbackContext ctx) => SwitchWeapon(1);
    private void OnSlot3(InputAction.CallbackContext ctx) => SwitchWeapon(2);

    private void SwitchWeapon(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= weapons.Count) return;
        if (weapons[slotIndex] == null) return;
        if (currentWeaponIndex == slotIndex) return;
        if (isSwitching) return;

        Debug.Log($"[WeaponManager] Request switch {currentWeaponIndex} -> {slotIndex}");
        StartCoroutine(SwitchRoutine(slotIndex));
    }

    private IEnumerator SwitchRoutine(int slotIndex)
    {
        isSwitching = true;

        if (currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Count && weapons[currentWeaponIndex] != null)
        {
            weapons[currentWeaponIndex].gameObject.SetActive(false);
            Debug.Log($"[WeaponManager] Deactivate slot {currentWeaponIndex}: {weapons[currentWeaponIndex].name}");
        }

        currentWeaponIndex = slotIndex;
        BaseWeapon newWeapon = weapons[currentWeaponIndex];
        newWeapon.gameObject.SetActive(true);
        newWeapon.transform.SetParent(weaponSlots[Mathf.Clamp(currentWeaponIndex, 0, weaponSlots.Length - 1)]);
        newWeapon.transform.localPosition = Vector3.zero;
        newWeapon.transform.localRotation = Quaternion.identity;
        Debug.Log($"[WeaponManager] Switched to slot {currentWeaponIndex}: {newWeapon.name} (instanceID {newWeapon.GetInstanceID()})");

        UpdateIKTargets(newWeapon.transform);

        yield return new WaitForSeconds(switchCooldown);
        isSwitching = false;
    }

    private void EquipImmediate(int slotIndex)
    {
        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i] == null) continue;
            weapons[i].gameObject.SetActive(false);
        }

        currentWeaponIndex = slotIndex;
        var w = weapons[currentWeaponIndex];
        if (w != null)
        {
            w.gameObject.SetActive(true);
            w.transform.SetParent(weaponSlots[Mathf.Clamp(currentWeaponIndex, 0, weaponSlots.Length - 1)]);
            w.transform.localPosition = Vector3.zero;
            w.transform.localRotation = Quaternion.identity;
            UpdateIKTargets(w.transform);
        }
    }

    private void UpdateIKTargets(Transform weaponRoot)
    {
        if (leftHandIK != null)
        {
            Transform leftGrip = weaponRoot.Find("LeftHandP");
            if (leftGrip != null) leftHandIK.data.target = leftGrip;
        }

        if (rightHandIK != null)
        {
            Transform rightGrip = weaponRoot.Find("RightHandP");
            if (rightGrip != null) rightHandIK.data.target = rightGrip;
        }

        if (rigBuilder != null)
        {
            rigBuilder.enabled = false;
            rigBuilder.enabled = true;
        }
    }

    public Vector3 GetRecoilOffset() => CurrentWeapon?.GetRecoilOffset() ?? Vector3.zero;

    public void PickupWeapon(GameObject weaponPrefab)
    {
        if (weaponPrefab == null || weaponSlots.Length == 0) return;

        // Уже есть такое оружие? просто переключаемся на него
        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i] == null) continue;
            var baseName = weapons[i].name.Replace("(Clone)", "");
            if (baseName.StartsWith(weaponPrefab.name))
            {
                SwitchWeapon(i);
                return;
            }
        }

        int freeSlot = FindFirstEmptySlot();
        if (freeSlot == -1) freeSlot = currentWeaponIndex; // заменяем текущее, если нет свободных
        freeSlot = Mathf.Clamp(freeSlot, 0, weaponSlots.Length - 1);

        // Удаляем старое оружие в слоте, чтобы не копить объекты
        if (freeSlot < weapons.Count && weapons[freeSlot] != null)
        {
            Destroy(weapons[freeSlot].gameObject);
            weapons[freeSlot] = null;
        }

        Transform slot = weaponSlots[freeSlot];

        // Если нам дали сценовый объект (дропнутое оружие), просто перемещаем его в слот без клонирования
        GameObject weaponObj;
        if (weaponPrefab.scene.rootCount != 0)
        {
            weaponObj = weaponPrefab;
            var pickup = weaponObj.GetComponent<WeaponPickup>();
            if (pickup != null) Destroy(pickup); // чтобы не триггерилось снова
            weaponObj.transform.SetParent(slot);
            weaponObj.transform.SetPositionAndRotation(slot.position, slot.rotation);
        }
        else
        {
            weaponObj = Instantiate(weaponPrefab, slot.position, slot.rotation, slot);
        }

        var weapon = weaponObj.GetComponent<BaseWeapon>();
        if (weapon == null)
        {
            Debug.LogError($"Weapon prefab {weaponPrefab.name} не содержит BaseWeapon");
            if (weaponObj != weaponPrefab) Destroy(weaponObj);
            return;
        }

        weapon.Initialize();
        weaponObj.tag = "Weapon";
        if (freeSlot < weapons.Count) weapons[freeSlot] = weapon;
        else weapons.Add(weapon);

        SwitchWeapon(freeSlot);
    }

    private int FindWeaponIndexByName(string key)
    {
        if (string.IsNullOrEmpty(key)) return -1;
        key = key.ToLowerInvariant();
        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i] == null) continue;
            if (weapons[i].name.ToLowerInvariant().Contains(key) ||
                weapons[i].stats.weaponName.ToLowerInvariant().Contains(key))
                return i;
        }
        return -1;
    }

    private int FindFirstExistingWeapon()
    {
        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i] != null) return i;
        }
        return -1;
    }

    private int FindFirstEmptySlot()
    {
        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i] == null) return i;
        }
        return weapons.Count < weaponSlots.Length ? weapons.Count : -1;
    }
}
