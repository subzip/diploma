using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class WeaponSystem : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private float damage = 25f;
    [SerializeField] private float maxRange = 100f;
    [SerializeField] private float fireRate = 0.15f;
    [SerializeField] private int magazineSize = 30;
    [SerializeField] private float reloadTime = 2f;

    private PlayerInputActions inputActions;
    
    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private GameObject impactDecalPrefab;
    [SerializeField] [Range(0.001f, 10f)] private float impactDecalSize = 1f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip reloadSound;
    [SerializeField] private AudioClip emptyClipSound;
    
    [Header("Optimization")]
    [SerializeField] private LayerMask hitLayers; // Настройте в инспекторе (Environment, Enemies)
    
    private float nextFireTime;
    private int currentAmmo;
    private bool isReloading;
    private Camera mainCamera;
    private AudioSource audioSource;
    
    // Для Object Pooling визуальных эффектов
    private const int MAX_HIT_EFFECTS = 20;
    private GameObject[] hitEffectPool;
    private int hitEffectIndex = 0;
    
    private void Awake()
    {
        mainCamera = Camera.main;
        audioSource = GetComponent<AudioSource>();
        currentAmmo = magazineSize;

        inputActions = new PlayerInputActions();
        inputActions.Player.Enable();
        
        // Настройка слоев для оптимизации
        if (hitLayers.value == 0)
        {
            hitLayers = LayerMask.GetMask("Environment", "Enemies", "Interactive");
        }
        
        // Инициализация пула эффектов
        InitializeHitEffectPool();
        
        // Проверка наличия audioSource
        if (audioSource == null)
        {
            Debug.LogError("AudioSource не найден! Добавьте компонент AudioSource к объекту оружия.");
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    private void InitializeHitEffectPool()
    {
        if (hitEffectPrefab == null) 
        {
            Debug.LogWarning("Префаб эффекта попадания не назначен. Визуальные эффекты не будут работать.");
            return;
        }
        
        hitEffectPool = new GameObject[MAX_HIT_EFFECTS];
        for (int i = 0; i < MAX_HIT_EFFECTS; i++)
        {
            GameObject effect = Instantiate(hitEffectPrefab);
            effect.SetActive(false);
            hitEffectPool[i] = effect;
        }
    }
    
    private void Update()
    {
        // Обработка стрельбы
        if (inputActions.Player.Reload.triggered && currentAmmo < magazineSize && !isReloading)
        {
            StartCoroutine(Reload());
        }
        else if (Mouse.current.leftButton.wasPressedThisFrame && !isReloading)
        {
            Shoot();
        }
    }
    
    public void Shoot()
    {
        // Проверка на скорострельность
        if (Time.time < nextFireTime) return;
        
        // Проверка наличия патронов
        if (currentAmmo <= 0)
        {
            if (!isReloading)
            {
                if (audioSource != null && emptyClipSound != null)
                    audioSource.PlayOneShot(emptyClipSound);
                StartCoroutine(Reload());
                
                // Добавлено логирование в консоль
                Debug.Log("Патроны закончились! Начата перезарядка.");
            }
            return;
        }
        
        nextFireTime = Time.time + fireRate;
        currentAmmo--;
        
        // Воспроизведение звука выстрела (с проверкой)
        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }
        else
        {
            Debug.LogWarning("Звук выстрела не назначен или AudioSource отсутствует.");
        }
        
        // Визуальные эффекты
        if (muzzleFlash != null) 
            muzzleFlash.Play();
        
        // Оптимизированный Raycast
        if (Physics.Raycast(
            mainCamera.transform.position, 
            mainCamera.transform.forward, 
            out RaycastHit hit, 
            maxRange,
            hitLayers,
            QueryTriggerInteraction.Ignore))
        {
            // ДОБАВЛЕНО: Логирование в консоль при попадании
            Debug.Log($"🎯 ПОПАДАНИЕ! Объект: {hit.collider.gameObject.name} | Дистанция: {hit.distance:F2} м");
            Debug.Log($"📍 Позиция попадания: X:{hit.point.x:F2}, Y:{hit.point.y:F2}, Z:{hit.point.z:F2}");

            // Обработка урона
            if (hit.collider.TryGetComponent<IDamageable>(out IDamageable damageable))
            {
                damageable.TakeDamage(damage, hit.point);
                // ДОБАВЛЕНО: Логирование урона
                Debug.Log($"💥 Нанесено {damage} урона объекту {hit.collider.gameObject.name}");
                
                if (damageable is Enemy enemy && enemy.IsDead())
                {
                    Debug.Log($"☠️ Враг [{hit.collider.gameObject.name}] уничтожен с одного выстрела!");
                }
            }
            else
            {
                Debug.Log($"🛡️ Объект {hit.collider.gameObject.name} не принимает урон");
            }
            
            // Визуальный эффект попадания
            SpawnHitEffects(hit);
        }
        else
        {
            // ДОБАВЛЕНО: Логирование промаха
            Debug.Log($"❌ ПРОМАХ! Выстрел в направлении {mainCamera.transform.forward}");
        }
    }
    
    private void SpawnHitEffects(RaycastHit hit)
    {
        // Эффект попадания (дым, искры)
        if (hitEffectPrefab != null)
        {
            GameObject effect = GetPooledHitEffect();
            if (effect != null)
            {
                effect.transform.position = hit.point;
                effect.transform.rotation = Quaternion.LookRotation(hit.normal);
                effect.SetActive(true);
                
                // Авто-деактивация через время
                StartCoroutine(DeactivateAfterDelay(effect, 2f));
            }
        }
        
        // Накладываем декаль на поверхность
        if (impactDecalPrefab != null)
        {
            GameObject decal = Instantiate(impactDecalPrefab, hit.point, Quaternion.LookRotation(hit.normal));
            decal.transform.localScale = Vector3.one * impactDecalSize;
            
            // Авто-уничтожение декаля
            Destroy(decal, 10f);
        }
    }
    
    private GameObject GetPooledHitEffect()
    {
        if (hitEffectPool == null || hitEffectPool.Length == 0) return null;
        
        GameObject effect = hitEffectPool[hitEffectIndex];
        hitEffectIndex = (hitEffectIndex + 1) % hitEffectPool.Length;
        
        return effect;
    }
    
    private System.Collections.IEnumerator DeactivateAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        obj.SetActive(false);
    }
    
    private System.Collections.IEnumerator Reload()
    {
        isReloading = true;
        
        if (audioSource != null && reloadSound != null)
            audioSource.PlayOneShot(reloadSound);
        
        yield return new WaitForSeconds(reloadTime);
        
        currentAmmo = magazineSize;
        isReloading = false;
        
        // ДОБАВЛЕНО: Логирование завершения перезарядки
        Debug.Log($"🔄 Перезарядка завершена. Патроны: {currentAmmo}/{magazineSize}");
    }
    
    // Метод для отображения текущего состояния оружия (можно использовать в UI)
    public string GetAmmoText()
    {
        return $"{currentAmmo}/{magazineSize}" + (isReloading ? " [Перезарядка...]" : "");
    }
    
    // Для отладки: отображение линии выстрела
    private void OnDrawGizmosSelected()
    {
        if (mainCamera == null) return;
        
        Gizmos.color = Color.red;
        Gizmos.DrawLine(mainCamera.transform.position, mainCamera.transform.position + mainCamera.transform.forward * maxRange);
    }
}