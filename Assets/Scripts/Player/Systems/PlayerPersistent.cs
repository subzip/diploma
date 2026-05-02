
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerPersistent : MonoBehaviour
{
    private static PlayerPersistent instance;
    [SerializeField] private string mainMenuSceneName = "StartGame";
    [SerializeField] private float invalidSpawnYThreshold = -100f;
    [SerializeField] private float maxDistanceFromFallbackSpawn = 80f;

    public static PlayerPersistent Instance
    { 
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<PlayerPersistent>();
                if (instance == null)
                {
                    Debug.LogError("PlayerPersistent: object was not found in scene!");
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
            EnsureSingleAudioListener();
            DisableLegacyStaminaUiIfVitalsHudExists();
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

    private void Start()
    {
        StartCoroutine(ApplyPendingSceneEntrySpawn(SceneManager.GetActiveScene().name));
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureSingleAudioListener();
        DisableLegacyStaminaUiIfVitalsHudExists();

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

        StartCoroutine(ApplyPendingSceneEntrySpawn(scene.name));
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

    private void EnsureSingleAudioListener()
    {
        AudioListener[] listeners = FindObjectsOfType<AudioListener>(true);
        if (listeners == null || listeners.Length <= 1) return;

        Camera mainCamera = Camera.main;
        AudioListener preferred = null;
        if (mainCamera != null)
            preferred = mainCamera.GetComponent<AudioListener>();

        if (preferred == null)
            preferred = listeners[0];

        for (int i = 0; i < listeners.Length; i++)
        {
            AudioListener listener = listeners[i];
            if (listener == null) continue;
            listener.enabled = listener == preferred;
        }
    }

    private void DisableLegacyStaminaUiIfVitalsHudExists()
    {
        PlayerVitalsHud vitalsHud = FindObjectOfType<PlayerVitalsHud>();
        if (vitalsHud == null) return;

        PlayerStaminaUI staminaUi = FindObjectOfType<PlayerStaminaUI>();
        if (staminaUi != null)
            staminaUi.enabled = false;
    }

    private IEnumerator ApplyPendingSceneEntrySpawn(string sceneName)
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        if (!SceneEntrySpawnState.TryConsume(sceneName, out Vector3 spawnPos, out Quaternion spawnRot, out bool shouldRegisterCheckpoint, out string checkpointId))
        {
            RecoverInvalidScenePositionIfNeeded(sceneName);
            yield break;
        }

        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
            movement.ResetForRespawnAt(spawnPos, spawnRot, resetStaminaToMax: false);
        else
            transform.SetPositionAndRotation(spawnPos, spawnRot);

        if (shouldRegisterCheckpoint)
            RespawnCheckpointState.SetCheckpoint(sceneName, spawnPos, spawnRot, checkpointId);

        Debug.Log($"[PlayerPersistent] Consumed pending scene-entry spawn for '{sceneName}' at {spawnPos}");
    }

    private void RecoverInvalidScenePositionIfNeeded(string sceneName)
    {
        if (sceneName == mainMenuSceneName) return;
        bool belowLevel = transform.position.y <= invalidSpawnYThreshold;
        bool hasCheckpoint = RespawnCheckpointState.TryGetCheckpoint(sceneName, out Vector3 checkpointPos, out Quaternion checkpointRot);
        string checkpointId = RespawnCheckpointState.GetCheckpointId(sceneName);
        bool checkpointIsSceneStart = string.Equals(checkpointId, "scene_start", System.StringComparison.Ordinal);

        if (hasCheckpoint && !checkpointIsSceneStart)
        {
            if (belowLevel && checkpointPos.y > invalidSpawnYThreshold)
                ApplyRecoveredSpawn(sceneName, checkpointPos, checkpointRot, "checkpoint");
            return;
        }

        if (TryFindFallbackSpawnInScene(out Vector3 fallbackPos, out Quaternion fallbackRot))
        {
            bool tooFarFromFallback = Vector3.Distance(transform.position, fallbackPos) > maxDistanceFromFallbackSpawn;
            if (belowLevel || tooFarFromFallback)
            {
                RespawnCheckpointState.SetCheckpoint(sceneName, fallbackPos, fallbackRot, "scene_fallback");
                ApplyRecoveredSpawn(sceneName, fallbackPos, fallbackRot, "scene_fallback");
            }
            else if (hasCheckpoint && checkpointIsSceneStart && checkpointPos.y <= invalidSpawnYThreshold)
            {
                RespawnCheckpointState.SetCheckpoint(sceneName, fallbackPos, fallbackRot, "scene_fallback");
            }
        }
    }

    private void ApplyRecoveredSpawn(string sceneName, Vector3 position, Quaternion rotation, string source)
    {
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
            movement.ResetForRespawnAt(position, rotation, resetStaminaToMax: false);
        else
            transform.SetPositionAndRotation(position, rotation);

        Debug.LogWarning($"[PlayerPersistent] Recovered invalid spawn in '{sceneName}' from {source} at {position}");
    }

    private bool TryFindFallbackSpawnInScene(out Vector3 position, out Quaternion rotation)
    {
        Scene activeScene = SceneManager.GetActiveScene();

        CheckpointTrigger[] checkpoints = FindObjectsOfType<CheckpointTrigger>(true);
        for (int i = 0; i < checkpoints.Length; i++)
        {
            CheckpointTrigger checkpoint = checkpoints[i];
            if (checkpoint == null || checkpoint.gameObject.scene != activeScene) continue;
            if (!checkpoint.gameObject.activeInHierarchy) continue;
            position = checkpoint.transform.position;
            rotation = checkpoint.transform.rotation;
            return true;
        }

        Transform[] transforms = FindObjectsOfType<Transform>(true);
        Transform best = null;
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform candidate = transforms[i];
            if (candidate == null || candidate.gameObject.scene != activeScene) continue;
            if (!candidate.gameObject.activeInHierarchy) continue;
            if (candidate == transform || candidate.IsChildOf(transform) || transform.IsChildOf(candidate)) continue;

            string lower = candidate.name.ToLowerInvariant();
            bool isSpawn =
                lower.Contains("spawn") ||
                lower.Contains("respawn") ||
                lower.Contains("checkpoint") ||
                lower.Contains("start");

            if (!isSpawn) continue;
            if (best == null || IsBetterFallbackSpawnName(candidate.name, best.name))
                best = candidate;
        }

        if (best != null)
        {
            position = best.position;
            rotation = best.rotation;
            return true;
        }

        position = default;
        rotation = Quaternion.identity;
        return false;
    }

    private static bool IsBetterFallbackSpawnName(string candidate, string current)
    {
        int candidateScore = GetFallbackSpawnScore(candidate);
        int currentScore = GetFallbackSpawnScore(current);
        return candidateScore > currentScore;
    }

    private static int GetFallbackSpawnScore(string name)
    {
        if (string.IsNullOrEmpty(name)) return 0;
        name = name.ToLowerInvariant();

        int score = 0;
        if (name.Contains("scene")) score += 4;
        if (name.Contains("entry")) score += 4;
        if (name.Contains("player")) score += 3;
        if (name.Contains("spawn")) score += 3;
        if (name.Contains("respawn")) score += 2;
        if (name.Contains("checkpoint")) score += 1;
        return score;
    }
}
