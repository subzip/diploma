
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;
using TMPro;

public class WeaponManager : MonoBehaviour
{
    [Header("Weapons")]
    [SerializeField] private Transform[] weaponSlots;
    [SerializeField] private int currentWeaponIndex = 0;
    [SerializeField] private float switchCooldown = 0.25f;

    [Header("Code Swap Animation")]
    [SerializeField] private bool useCodeSwapAnimation = true;
    [SerializeField] private Vector3 swapHideOffset = new Vector3(0.28f, -0.35f, 0.22f);
    [SerializeField] private Vector3 swapHideEuler = new Vector3(12f, 0f, -10f);
    [SerializeField] private float swapHideDuration = 0.09f;
    [SerializeField] private float swapDrawDuration = 0.12f;

    [Header("IK Setup")]
    [SerializeField] private TwoBoneIKConstraint leftHandIK;
    [SerializeField] private TwoBoneIKConstraint rightHandIK;
    [SerializeField] private RigBuilder rigBuilder;

    [Header("Ammo HUD Binding")]
    [SerializeField] private TMP_Text ammoCurrentText;
    [SerializeField] private string ammoCurrentObjectName = "BulletsCurrent";
    [SerializeField] private TMP_Text ammoReserveText;
    [SerializeField] private string ammoReserveObjectName = "BulletsReserve";

    private readonly List<BaseWeapon> weapons = new();
    public BaseWeapon CurrentWeapon => (currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Count) ? weapons[currentWeaponIndex] : null;
    private PlayerInputActions inputActions;
    private bool isSwitching = false;
    private bool fireHeld = false;
    private Vector3[] slotDefaultLocalPos;
    private Quaternion[] slotDefaultLocalRot;
    private int lastHudCurrentAmmo = int.MinValue;
    private int lastHudReserveAmmo = int.MinValue;
    private float nextHudResolveTime;

    private void Awake()
    {
        ResolveInputActions();
        CacheSlotDefaultTransforms();
        ResolveAmmoHudTextIfNeeded(force: true);

        
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
        ResolveInputActions();
        if (inputActions == null) return;

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
        if (inputActions == null && GameInput.Instance != null)
            ResolveInputActions();
        ResolveAmmoHudTextIfNeeded(force: false);

        if (IsGameplayInputBlocked())
        {
            fireHeld = false;
            CurrentWeapon?.UpdateRecoil(Time.deltaTime);
            SyncAmmoHudFromCurrentWeapon();
            return;
        }

        if (!isSwitching && fireHeld && CurrentWeapon?.stats.fireMode == FireMode.Auto)
        {
            CurrentWeapon.Shoot();
        }

        CurrentWeapon?.UpdateRecoil(Time.deltaTime);
        SyncAmmoHudFromCurrentWeapon();
    }

    private void OnShoot(InputAction.CallbackContext ctx)
    {
        if (IsGameplayInputBlocked() || IsIgnoredShootControl(ctx))
        {
            fireHeld = false;
            return;
        }

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
    private void OnReload(InputAction.CallbackContext ctx)
    {
        if (IsGameplayInputBlocked()) return;
        CurrentWeapon?.Reload();
    }
    private void OnSlot1(InputAction.CallbackContext ctx)
    {
        if (IsGameplayInputBlocked()) return;
        SwitchWeapon(0);
    }
    private void OnSlot2(InputAction.CallbackContext ctx)
    {
        if (IsGameplayInputBlocked()) return;
        SwitchWeapon(1);
    }
    private void OnSlot3(InputAction.CallbackContext ctx)
    {
        if (IsGameplayInputBlocked()) return;
        SwitchWeapon(2);
    }

    private static bool IsGameplayInputBlocked()
    {
        return DeathScreen.GlobalDeathActive || CycleTransitionScreen.IsTransitionActive || PauseManager.IsPaused || PauseManager.IsInputGuardActive;
    }

    private static bool IsIgnoredShootControl(InputAction.CallbackContext ctx)
    {
        string path = ctx.control?.path ?? string.Empty;
        string name = ctx.control?.name ?? string.Empty;
        string displayName = ctx.control?.displayName ?? string.Empty;

        if (ContainsIgnoreCase(path, "printScreen") ||
            ContainsIgnoreCase(name, "printScreen") ||
            ContainsIgnoreCase(displayName, "Print Screen"))
        {
            return true;
        }

        return path.StartsWith("/Keyboard", StringComparison.OrdinalIgnoreCase) ||
               ContainsIgnoreCase(path, "<Keyboard>");
    }

    private static bool ContainsIgnoreCase(string source, string value)
    {
        return !string.IsNullOrEmpty(source) &&
               source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }

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
        fireHeld = false;

        int oldIndex = currentWeaponIndex;
        BaseWeapon oldWeapon = (oldIndex >= 0 && oldIndex < weapons.Count) ? weapons[oldIndex] : null;
        Transform oldSlot = (oldIndex >= 0 && oldIndex < weaponSlots.Length) ? weaponSlots[oldIndex] : null;

        if (useCodeSwapAnimation && oldWeapon != null && oldSlot != null && oldWeapon.gameObject.activeSelf)
        {
            yield return AnimateSlotToHidden(oldIndex, oldSlot);
        }

        if (currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Count && weapons[currentWeaponIndex] != null)
        {
            weapons[currentWeaponIndex].CancelReload();
            weapons[currentWeaponIndex].gameObject.SetActive(false);
        }

        if (oldIndex >= 0 && oldIndex < weaponSlots.Length)
        {
            ResetSlotToDefault(oldIndex);
        }

        currentWeaponIndex = slotIndex;
        BaseWeapon newWeapon = weapons[currentWeaponIndex];
        Transform newSlot = weaponSlots[Mathf.Clamp(currentWeaponIndex, 0, weaponSlots.Length - 1)];

        newWeapon.gameObject.SetActive(true);
        newWeapon.transform.SetParent(newSlot);
        newWeapon.transform.localPosition = Vector3.zero;
        newWeapon.transform.localRotation = Quaternion.identity;

        if (useCodeSwapAnimation)
        {
            SetSlotHiddenPose(currentWeaponIndex, newSlot);
        }

        UpdateIKTargets(newWeapon.transform);

        if (useCodeSwapAnimation)
        {
            yield return AnimateSlotToDefault(currentWeaponIndex, newSlot, swapDrawDuration);
        }

        if (switchCooldown > 0f)
            yield return new WaitForSeconds(switchCooldown);

        isSwitching = false;
        lastHudCurrentAmmo = int.MinValue;
        lastHudReserveAmmo = int.MinValue;
        SyncAmmoHudFromCurrentWeapon();
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
            Transform slot = weaponSlots[Mathf.Clamp(currentWeaponIndex, 0, weaponSlots.Length - 1)];
            ResetSlotToDefault(currentWeaponIndex);
            w.transform.SetParent(slot);
            w.transform.localPosition = Vector3.zero;
            w.transform.localRotation = Quaternion.identity;
            UpdateIKTargets(w.transform);
        }

        lastHudCurrentAmmo = int.MinValue;
        lastHudReserveAmmo = int.MinValue;
        SyncAmmoHudFromCurrentWeapon();
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

        
        for (int i = 0; i < weapons.Count; i++)
        {
            BaseWeapon weapon = weapons[i];
            if (weapon == null) continue;
            weapon.RefreshAmmoUiBindingAndValue();
        }

        lastHudCurrentAmmo = int.MinValue;
        lastHudReserveAmmo = int.MinValue;
        SyncAmmoHudFromCurrentWeapon();
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

    private void ResolveInputActions()
    {
        if (inputActions != null) return;
        if (GameInput.Instance == null) return;
        inputActions = GameInput.Instance.Actions;
    }

    private void ResolveAmmoHudTextIfNeeded(bool force)
    {
        if (!force && Time.unscaledTime < nextHudResolveTime) return;

        if (force || ammoCurrentText == null)
            ammoCurrentText = ResolveTextByName(ammoCurrentObjectName);
        if (force || ammoReserveText == null)
            ammoReserveText = ResolveTextByName(ammoReserveObjectName);

        if (ammoCurrentText == null || ammoReserveText == null)
            nextHudResolveTime = Time.unscaledTime + 0.5f;
    }

    private TMP_Text ResolveTextByName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName)) return null;
        GameObject go = GameObject.Find(objectName);
        return go != null ? go.GetComponent<TMP_Text>() : null;
    }

    private void SyncAmmoHudFromCurrentWeapon()
    {
        if (ammoCurrentText == null || ammoReserveText == null) return;
        BaseWeapon weapon = GetHudWeapon();
        if (weapon == null) return;

        int current = weapon.CurrentAmmo;
        int reserve = weapon.ReserveAmmo;
        ammoCurrentText.text = current.ToString();
        ammoReserveText.text = reserve.ToString();
        lastHudCurrentAmmo = current;
        lastHudReserveAmmo = reserve;
    }

    private BaseWeapon GetHudWeapon()
    {
        BaseWeapon active = GetActiveWeaponFromSlots();
        if (active != null) return active;
        if (CurrentWeapon != null) return CurrentWeapon;
        return null;
    }

    private BaseWeapon GetActiveWeaponFromSlots()
    {
        if (weaponSlots == null) return null;

        for (int i = 0; i < weaponSlots.Length; i++)
        {
            Transform slot = weaponSlots[i];
            if (slot == null) continue;

            BaseWeapon weapon = (i < weapons.Count) ? weapons[i] : null;
            if (weapon == null)
                weapon = slot.GetComponentInChildren<BaseWeapon>(true);

            if (weapon != null && weapon.gameObject.activeInHierarchy)
                return weapon;
        }

        return null;
    }

    private void CacheSlotDefaultTransforms()
    {
        if (weaponSlots == null)
        {
            slotDefaultLocalPos = System.Array.Empty<Vector3>();
            slotDefaultLocalRot = System.Array.Empty<Quaternion>();
            return;
        }

        slotDefaultLocalPos = new Vector3[weaponSlots.Length];
        slotDefaultLocalRot = new Quaternion[weaponSlots.Length];

        for (int i = 0; i < weaponSlots.Length; i++)
        {
            Transform slot = weaponSlots[i];
            if (slot == null) continue;
            slotDefaultLocalPos[i] = slot.localPosition;
            slotDefaultLocalRot[i] = slot.localRotation;
        }
    }

    private void ResetSlotToDefault(int index)
    {
        if (!IsValidSlotIndex(index)) return;
        Transform slot = weaponSlots[index];
        if (slot == null) return;
        slot.localPosition = slotDefaultLocalPos[index];
        slot.localRotation = slotDefaultLocalRot[index];
    }

    private void SetSlotHiddenPose(int index, Transform slot)
    {
        if (slot == null || !IsValidSlotIndex(index)) return;
        slot.localPosition = slotDefaultLocalPos[index] + swapHideOffset;
        slot.localRotation = slotDefaultLocalRot[index] * Quaternion.Euler(swapHideEuler);
    }

    private IEnumerator AnimateSlotToHidden(int index, Transform slot)
    {
        if (slot == null || !IsValidSlotIndex(index)) yield break;

        Vector3 startPos = slot.localPosition;
        Quaternion startRot = slot.localRotation;
        Vector3 endPos = slotDefaultLocalPos[index] + swapHideOffset;
        Quaternion endRot = slotDefaultLocalRot[index] * Quaternion.Euler(swapHideEuler);

        float duration = Mathf.Max(0.01f, swapHideDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            slot.localPosition = Vector3.Lerp(startPos, endPos, t);
            slot.localRotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        slot.localPosition = endPos;
        slot.localRotation = endRot;
    }

    private IEnumerator AnimateSlotToDefault(int index, Transform slot, float duration)
    {
        if (slot == null || !IsValidSlotIndex(index)) yield break;

        Vector3 startPos = slot.localPosition;
        Quaternion startRot = slot.localRotation;
        Vector3 endPos = slotDefaultLocalPos[index];
        Quaternion endRot = slotDefaultLocalRot[index];

        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;
        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            slot.localPosition = Vector3.Lerp(startPos, endPos, t);
            slot.localRotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        slot.localPosition = endPos;
        slot.localRotation = endRot;
    }

    private bool IsValidSlotIndex(int index)
    {
        return index >= 0 &&
               weaponSlots != null &&
               index < weaponSlots.Length &&
               slotDefaultLocalPos != null &&
               slotDefaultLocalRot != null &&
               index < slotDefaultLocalPos.Length &&
               index < slotDefaultLocalRot.Length;
    }

    public bool PickupWeapon(GameObject weaponPrefab)
    {
        if (weaponPrefab == null || weaponSlots == null || weaponSlots.Length == 0) return false;

        BaseWeapon incomingWeapon = weaponPrefab.GetComponent<BaseWeapon>();
        if (incomingWeapon == null)
            incomingWeapon = weaponPrefab.GetComponentInChildren<BaseWeapon>(true);

        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i] == null) continue;
            if (IsSameWeapon(weapons[i], incomingWeapon, weaponPrefab.name))
            {
                SwitchWeapon(i);
                return true;
            }
        }

        int freeSlot = FindFirstEmptySlot();
        if (freeSlot == -1) freeSlot = currentWeaponIndex; 
        freeSlot = Mathf.Clamp(freeSlot, 0, weaponSlots.Length - 1);

        if (freeSlot < weapons.Count && weapons[freeSlot] != null)
        {
            Destroy(weapons[freeSlot].gameObject);
            weapons[freeSlot] = null;
        }

        Transform slot = weaponSlots[freeSlot];

        GameObject weaponObj;
        if (weaponPrefab.scene.IsValid() && weaponPrefab.scene.rootCount != 0)
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
            weapon = weaponObj.GetComponentInChildren<BaseWeapon>(true);
        if (weapon == null)
        {
            if (weaponObj != weaponPrefab) Destroy(weaponObj);
            return false;
        }

        weapon.Initialize();
        weaponObj.tag = "Weapon";
        if (freeSlot < weapons.Count) weapons[freeSlot] = weapon;
        else weapons.Add(weapon);

        SwitchWeapon(freeSlot);
        lastHudCurrentAmmo = int.MinValue;
        lastHudReserveAmmo = int.MinValue;
        SyncAmmoHudFromCurrentWeapon();
        return true;
    }

    private static bool IsSameWeapon(BaseWeapon existing, BaseWeapon incoming, string fallbackName)
    {
        if (existing == null) return false;

        if (incoming != null)
        {
            if (existing.GetType() == incoming.GetType()) return true;
            if (existing.stats != null && incoming.stats != null && existing.stats == incoming.stats) return true;

            string existingStatsName = existing.stats != null ? existing.stats.weaponName : string.Empty;
            string incomingStatsName = incoming.stats != null ? incoming.stats.weaponName : string.Empty;
            if (!string.IsNullOrWhiteSpace(existingStatsName) &&
                string.Equals(existingStatsName, incomingStatsName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        string existingName = existing.name.Replace("(Clone)", string.Empty);
        return !string.IsNullOrWhiteSpace(fallbackName) &&
               existingName.StartsWith(fallbackName, StringComparison.OrdinalIgnoreCase);
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

