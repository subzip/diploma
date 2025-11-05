using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class WeaponSystem : MonoBehaviour
{
    [Header("Weapons")]
    public Weapon[] weapons; // Массив всех оружий
    [SerializeField] private int currentWeaponIndex = 0;

    private PlayerInputActions inputActions;
    private Camera mainCamera;
    private AudioSource audioSource;
    private bool isAiming = false;

    private void Awake()
    {
        mainCamera = Camera.main;
        audioSource = GetComponent<AudioSource>();
        inputActions = new PlayerInputActions();

        if (weapons.Length > 0)
        {
            weapons[currentWeaponIndex].Initialize();
        }
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void Update()
    {
        if (weapons.Length == 0) return;

        Weapon currentWeapon = weapons[currentWeaponIndex];

        // Обновление состояния оружия (рассеивание, отдача)
        currentWeapon.Update(Time.deltaTime);

        // Стрельба
        if (Mouse.current.leftButton.wasPressedThisFrame && currentWeapon.shootingMode == ShootingMode.SemiAuto)
        {
            TryShoot();
        }
        else if (Mouse.current.leftButton.isPressed && currentWeapon.shootingMode == ShootingMode.Auto)
        {
            TryShoot();
        }

        // Перезарядка
        if (inputActions.Player.Reload.triggered && currentWeapon.currentAmmo < currentWeapon.magazineSize && !currentWeapon.isReloading)
        {
            StartCoroutine(Reload(currentWeapon));
        }

        // Прицеливание
        isAiming = Mouse.current.rightButton.isPressed;
    }

    private void TryShoot()
    {
        Weapon currentWeapon = weapons[currentWeaponIndex];
        if (currentWeapon.CanShoot(Time.time))
        {
            Shoot(currentWeapon);
        }
    }

    private void Shoot(Weapon weapon)
    {
        weapon.OnShoot();

        // Воспроизвести звук
        if (weapon.shootSound != null && audioSource != null)
            audioSource.PlayOneShot(weapon.shootSound);

        // Визуальные эффекты
        if (weapon.muzzleFlash != null)
            weapon.muzzleFlash.Play();

        // Рассчитать угол выстрела с учётом отдачи и рассеивания
        Vector3 spreadOffset = CalculateSpread(weapon);
        Vector3 direction = mainCamera.transform.forward + spreadOffset;

        // Raycast
        if (Physics.Raycast(mainCamera.transform.position, direction, out RaycastHit hit, weapon.maxRange, weapon.hitLayers, QueryTriggerInteraction.Ignore))
        {
            Debug.Log($"🎯 Попадание в {hit.collider.gameObject.name}");
            if (hit.collider.TryGetComponent<IDamageable>(out IDamageable damageable))
            {
                damageable.TakeDamage(weapon.damage, hit.point);
            }

            SpawnHitEffects(weapon, hit);
        }
    }

    private Vector3 CalculateSpread(Weapon weapon)
    {
        float spread = isAiming ? weapon.currentSpread : weapon.hipSpread;
        float x = Random.Range(-spread, spread);
        float y = Random.Range(-spread, spread);
        return new Vector3(x, y, 0);
    }

    private void SpawnHitEffects(Weapon weapon, RaycastHit hit)
    {
        if (weapon.hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(weapon.hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            Destroy(effect, 2f);
        }
        if (weapon.impactDecalPrefab != null)
        {
            GameObject decal = Instantiate(weapon.impactDecalPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            decal.transform.localScale = Vector3.one * weapon.impactDecalSize;
            Destroy(decal, 10f);
        }
    }

    private System.Collections.IEnumerator Reload(Weapon weapon)
    {
        weapon.isReloading = true;
        if (weapon.reloadSound != null && audioSource != null)
            audioSource.PlayOneShot(weapon.reloadSound);

        yield return new WaitForSeconds(weapon.reloadTime);

        weapon.currentAmmo = weapon.magazineSize;
        weapon.isReloading = false;
    }

    // Метод для переключения оружия
    public void SwitchWeapon(int index)
    {
        if (index >= 0 && index < weapons.Length)
        {
            currentWeaponIndex = index;
            weapons[currentWeaponIndex].Initialize();
        }
    }

    public string GetCurrentAmmoText()
    {
        if (weapons.Length == 0) return "No Weapon";
        Weapon w = weapons[currentWeaponIndex];
        return $"{w.currentAmmo}/{w.magazineSize}";
    }
}