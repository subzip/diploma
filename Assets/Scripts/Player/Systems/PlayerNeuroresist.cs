// PlayerNeuroresist.cs
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(PlayerInputActions))]
public class PlayerNeuroresist : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float duration = 15f;
    [SerializeField] private float detectionRadius = 100f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("UI")]
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private GameObject indicatorPrefab; // Префаб с белым кружком/крестом

    [Header("Post-processing")]
    [SerializeField] private Volume volume; // Global Volume на сцене

    private bool isActive = false;
    private float endTime;
    private GameObject[] indicators = new GameObject[50];
    private Collider[] detectedEnemies = new Collider[50];
    private int enemyCount = 0;
    private VolumeProfile runtimeProfile; // Важно: клонируемый профиль

    private void OnEnable()
    {
        var input = new PlayerInputActions();
        input.Player.Enable();
        input.Player.Neuroresist.performed += _ => Activate();
    }

    private void Start()
    {
        // 🔥 КЛЮЧЕВОЕ ИСПРАВЛЕНИЕ: Клонируем Volume Profile
        if (volume != null && volume.profile != null)
        {
            runtimeProfile = Instantiate(volume.profile);
            volume.profile = runtimeProfile;
        }
    }

    private void Update()
    {
        if (isActive)
        {
            if (Time.time >= endTime)
            {
                Deactivate();
                return;
            }

            // Обновление позиций подсветок
            for (int i = 0; i < enemyCount; i++)
            {
                if (detectedEnemies[i] != null && indicators[i] != null)
                {
                    Vector3 screenPoint = Camera.main.WorldToScreenPoint(detectedEnemies[i].transform.position);
                    indicators[i].transform.position = screenPoint;
                }
            }
        }
    }

    private void Activate()
    {
        if (isActive) return;

        isActive = true;
        endTime = Time.time + duration;

        // Поиск врагов в радиусе
        enemyCount = Physics.OverlapSphereNonAlloc(transform.position, detectionRadius, detectedEnemies, enemyLayer);

        // Создание подсветок
        for (int i = 0; i < enemyCount; i++)
        {
            if (detectedEnemies[i] != null)
            {
                GameObject ind = Instantiate(indicatorPrefab, uiCanvas.transform);
                indicators[i] = ind;
                ind.SetActive(true);
            }
        }

        // 🔥 ПРИМЕНЕНИЕ ЭФФЕКТОВ (работает в URP 2022+)
        ApplyNeuroresistEffects(true);
    }

    private void Deactivate()
    {
        isActive = false;

        // Удаление подсветок
        for (int i = 0; i < enemyCount; i++)
        {
            if (indicators[i] != null)
            {
                Destroy(indicators[i]);
            }
        }
        enemyCount = 0;

        // 🔥 СБРОС ЭФФЕКТОВ
        ApplyNeuroresistEffects(false);
    }

    private void ApplyNeuroresistEffects(bool enabled)
    {
        if (runtimeProfile == null) return;

        // Получаем настройки
        if (runtimeProfile.TryGet<ColorAdjustments>(out var colorAdjust))
        {
            colorAdjust.active = enabled;
            if (enabled)
            {
                colorAdjust.saturation.value = -100f;   // Полностью ч/б
                colorAdjust.postExposure.value = -0.3f; // Затемнение
                colorAdjust.contrast.value = 20f;       // Повышение контраста
            }
            else
            {
                colorAdjust.saturation.value = 0f;
                colorAdjust.postExposure.value = 0f;
                colorAdjust.contrast.value = 0f;
            }
        }

        if (runtimeProfile.TryGet<Vignette>(out var vignette))
        {
            vignette.active = enabled;
            if (enabled)
            {
                vignette.intensity.value = 0.6f;
            }
            else
            {
                vignette.intensity.value = 0f;
            }
        }
    }
}