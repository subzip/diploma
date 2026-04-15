// Assets/Scripts/Weapons/WeaponManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;

public class WeaponManager : MonoBehaviour
{
    [Header("Weapons")]
    [SerializeField] private Transform[] weaponSlots;
    [SerializeField] private int currentWeaponIndex = 0;
    [SerializeField] private float switchCooldown = 0.25f;

    [Header("IK Setup")]
    [SerializeField] private TwoBoneIKConstraint leftHandIK;
    [SerializeField] private TwoBoneIKConstraint rightHandIK;
    [SerializeField] private RigBuilder rigBuilder;

    private readonly List<BaseWeapon> weapons = new();
    public BaseWeapon CurrentWeapon => (currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Count) ? weapons[currentWeaponIndex] : null;
    private PlayerInputActions inputActions;
    private bool isSwitching = false;
    private bool fireHeld = false;

    private void Awake()
    {
        inputActions = GameInput.Instance.Actions;

        // Подхватываем оружие из детей слотов
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
        }

        int startIndex = FindWeaponIndexByName("pistol");
        if (startIndex == -1) startIndex = FindFirstExistingWeapon();
        if (startIndex != -1) EquipImmediate(startIndex);
    }

    private void OnEnable()
    {
        inputActions.Player.Shoot.performed += OnShoot;
        inputActions.Player.Shoot.canceled += OnShootCanceled;
        inputActions.Player.Reload.performed += OnReload;
        inputActions.Player.Slot1.performed += OnSlot1;
        inputActions.Player.Slot2.performed += OnSlot2;
        inputActions.Player.Slot3.performed += OnSlot3;
    }

    private void OnDisable()
    {
        if (inputActions == null) return;
        inputActions.Player.Shoot.performed -= OnShoot;
        inputActions.Player.Shoot.canceled -= OnShootCanceled;
        inputActions.Player.Reload.performed -= OnReload;
        inputActions.Player.Slot1.performed -= OnSlot1;
        inputActions.Player.Slot2.performed -= OnSlot2;
        inputActions.Player.Slot3.performed -= OnSlot3;
    }

    private void Update()
    {
        if (!isSwitching && fireHeld && CurrentWeapon?.stats.fireMode == FireMode.Auto)
        {
            CurrentWeapon.Shoot();
        }

        CurrentWeapon?.UpdateRecoil(Time.deltaTime);
    }

    private void OnShoot(InputAction.CallbackContext ctx)
    {
        fireHeld = true;
        if (isSwitching) return;
        if (CurrentWeapon == null) return;
        if (CurrentWeapon.IsReloading) return;

        if (CurrentWeapon.stats.fireMode == FireMode.SemiAuto)
        {
            CurrentWeapon.Shoot();
        }
    }

    private void OnShootCanceled(InputAction.CallbackContext ctx) => fireHeld = false;
    private void OnReload(InputAction.CallbackContext ctx) => CurrentWeapon?.Reload();
    private void OnSlot1(InputAction.CallbackContext ctx) => SwitchWeapon(0);
    private void OnSlot2(InputAction.CallbackContext ctx) => SwitchWeapon(1);
    private void OnSlot3(InputAction.CallbackContext ctx) => SwitchWeapon(2);

    private void SwitchWeapon(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= weapons.Count) return;
        if (weapons[slotIndex] == null) return;
        if (currentWeaponIndex == slotIndex) return;
        if (isSwitching || fireHeld) return;

        StartCoroutine(SwitchRoutine(slotIndex));
    }

    private IEnumerator SwitchRoutine(int slotIndex)
    {
        isSwitching = true;

        if (currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Count && weapons[currentWeaponIndex] != null)
        {
            weapons[currentWeaponIndex].CancelReload();
            weapons[currentWeaponIndex].gameObject.SetActive(false);
        }

        currentWeaponIndex = slotIndex;
        BaseWeapon newWeapon = weapons[currentWeaponIndex];
        newWeapon.gameObject.SetActive(true);
        newWeapon.transform.SetParent(weaponSlots[Mathf.Clamp(currentWeaponIndex, 0, weaponSlots.Length - 1)]);
        newWeapon.transform.localPosition = Vector3.zero;
        newWeapon.transform.localRotation = Quaternion.identity;

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
    public Vector2 ConsumeLookRecoil() => CurrentWeapon?.ConsumeLookRecoil() ?? Vector2.zero;

    public void ResetForRespawn(bool refillAmmoToDefaults)
    {
        if (weaponSlots == null || weaponSlots.Length == 0) return;

        fireHeld = false;
        isSwitching = false;

        for (int i = 0; i < weapons.Count; i++)
        {
            BaseWeapon weapon = weapons[i];
            if (weapon == null) continue;

            weapon.CancelReload();
            weapon.ResetForRespawn(refillAmmoToDefaults);
            weapon.gameObject.SetActive(false);
        }

        int targetIndex = Mathf.Clamp(currentWeaponIndex, 0, weapons.Count - 1);
        if (targetIndex < 0 || targetIndex >= weapons.Count || weapons[targetIndex] == null)
        {
            targetIndex = FindFirstExistingWeapon();
        }

        if (targetIndex >= 0 && targetIndex < weapons.Count && weapons[targetIndex] != null)
        {
            currentWeaponIndex = targetIndex;
            BaseWeapon current = weapons[currentWeaponIndex];
            current.gameObject.SetActive(true);
            current.transform.SetParent(weaponSlots[Mathf.Clamp(currentWeaponIndex, 0, weaponSlots.Length - 1)]);
            current.transform.localPosition = Vector3.zero;
            current.transform.localRotation = Quaternion.identity;
            UpdateIKTargets(current.transform);
            current.RefreshAmmoUiBindingAndValue();
        }

        // One extra pass to guarantee HUD sync after scene reload.
        for (int i = 0; i < weapons.Count; i++)
        {
            BaseWeapon weapon = weapons[i];
            if (weapon == null) continue;
            weapon.RefreshAmmoUiBindingAndValue();
        }
    }

    public bool AddAmmo(AmmoType ammoType, int amount)
    {
        if (amount <= 0) return false;

        bool addedAny = false;
        for (int i = 0; i < weapons.Count; i++)
        {
            BaseWeapon weapon = weapons[i];
            if (weapon == null || weapon.stats == null) continue;
            if (weapon.stats.ammoType != ammoType) continue;

            int added = weapon.AddReserveAmmo(amount);
            if (added > 0) addedAny = true;
        }

        return addedAny;
    }

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

        if (freeSlot < weapons.Count && weapons[freeSlot] != null)
        {
            Destroy(weapons[freeSlot].gameObject);
            weapons[freeSlot] = null;
        }

        Transform slot = weaponSlots[freeSlot];

        GameObject weaponObj;
        if (weaponPrefab.scene.rootCount != 0)
        {
            weaponObj = weaponPrefab;
            var pickup = weaponObj.GetComponent<WeaponPickup>();
            if (pickup != null) Destroy(pickup);
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
