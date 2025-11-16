
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInputActions))]
public class PlayerNeuroresist : MonoBehaviour
{
    [Header("Neuroresist Settings")]
    [SerializeField] private float duration = 15f;   
    [SerializeField] private float radius = 100f;     
    [SerializeField] private LayerMask enemyLayer;     
    [SerializeField] private Material enemySilhouetteMat;   

    [Header("Post-processing")]
    [SerializeField] private Volume postProcessVolume;
    [SerializeField] private float grayscaleIntensity = 1f;
    [SerializeField] private float opacity = 0.7f;

    private bool isActive = false;
    private float endTime;
    private PlayerInputActions inputActions;
    private Renderer[] originalRenderers;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Neuroresist.performed += _ => ActivateNeuroresist();
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
        DeactivateNeuroresist();
    }

    private void Update()
    {
        if (isActive && Time.time >= endTime)
        {
            DeactivateNeuroresist();
        }

        // Обновление силуэтов врагов (каждый кадр)
        if (isActive)
        {
            UpdateEnemySilhouettes();
        }
    }

    private void ActivateNeuroresist()
    {
        if (isActive) return;

        isActive = true;
        endTime = Time.time + duration;

        // Включить постобработку
        EnablePostProcessing(true);

        Debug.Log("Нейрорезист активирован!");
    }

    private void DeactivateNeuroresist()
    {
        if (!isActive) return;

        isActive = false;

        // Выключить постобработку
        EnablePostProcessing(false);

        // Скрыть все силуэты
        HideAllSilhouettes();

        Debug.Log("Нейрорезист деактивирован.");
    }

    private void EnablePostProcessing(bool enabled)
    {
        if (postProcessVolume == null) return;

        // Включаем/выключаем эффекты через Volume
        // Нужно добавить Grayscale и Chromatic Aberration через Volume Profile
        // Но для простоты — просто меняем цвет фона через камеру
        Camera.main.clearFlags = enabled ? CameraClearFlags.SolidColor : CameraClearFlags.Skybox;
        Camera.main.backgroundColor = enabled ? new Color(0.2f, 0.2f, 0.2f, opacity) : Color.black;
    }

    private void UpdateEnemySilhouettes()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, radius, enemyLayer);
        foreach (Collider col in colliders)
        {
            Renderer[] renderers = col.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                // Показываем силуэт через стены
                if (r.material != enemySilhouetteMat)
                {
                    r.material = enemySilhouetteMat;
                }
            }
        }
    }

    private void HideAllSilhouettes()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, radius, enemyLayer);
        foreach (Collider col in colliders)
        {
            Renderer[] renderers = col.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                // Восстанавливаем оригинальный материал (если нужно)
                // Или просто убираем силуэт
                if (r.material == enemySilhouetteMat)
                {
                    r.material = null; // или оригинальный материал
                }
            }
        }
    }
}