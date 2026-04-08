
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerPersistent : MonoBehaviour
{
    private static PlayerPersistent instance;
    [SerializeField] private string mainMenuSceneName = "StartGame";

    public static PlayerPersistent Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<PlayerPersistent>();
                if (instance == null)
                {
                    Debug.LogError("PlayerPersistent: объект не найден на сцене!");
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!DeathScreen.GlobalDeathActive)
        {
            EnableGameplayComponents();
        }

        if (DeathScreen.ConsumePendingHardRespawn())
        {
            ApplyHardRespawn(scene.name);
        }

        if (scene.name == mainMenuSceneName)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void ApplyHardRespawn(string sceneName)
    {
        DeathScreen.SetGlobalDeathActive(false);
        EnableGameplayComponents();

        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.ResetForRespawn();
        }

        PlayerCrouch crouch = GetComponent<PlayerCrouch>();
        if (crouch != null)
        {
            crouch.ForceStand();
        }

        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.ResetForRespawnAtSceneCheckpoint(sceneName);
        }

        WeaponManager weaponManager = GetComponentInChildren<WeaponManager>(true);
        if (weaponManager != null)
        {
            weaponManager.ResetForRespawn(refillAmmoToDefaults: true);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void EnableGameplayComponents()
    {
        SetEnabled<PlayerMovement>(true);
        SetEnabled<PlayerLook>(true);
        SetEnabled<PlayerCrouch>(true);
        SetEnabled<PlayerNeuroresist>(true);
        SetEnabled<AimController>(true);
        SetEnabled<SwayNBobScript>(true);

        WeaponManager weaponManager = GetComponentInChildren<WeaponManager>(true);
        if (weaponManager != null) weaponManager.enabled = true;
    }

    private void SetEnabled<T>(bool value) where T : Behaviour
    {
        T component = GetComponentInChildren<T>(true);
        if (component != null) component.enabled = value;
    }

    // Intentionally no runtime duplicate-pruning here.
    // DontDestroyObj handles duplicate prevention in Awake before persisting objects.
}
