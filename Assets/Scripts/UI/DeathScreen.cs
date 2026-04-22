
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class DeathScreen : MonoBehaviour
{
    public static DeathScreen Instance { get; private set; }
    public static bool GlobalDeathActive { get; private set; }
    public static void SetGlobalDeathActive(bool active) => GlobalDeathActive = active;

    public static bool PendingHardRespawn { get; private set; }
    public static bool ConsumePendingHardRespawn()
    {
        if (!PendingHardRespawn) return false;
        PendingHardRespawn = false;
        return true;
    }

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "StartGame";
    [SerializeField] private bool useVariantSceneMapping = true;
    [SerializeField] private string[] variantSceneNames = { "level0_var0", "level0_var1", "level0_var2" };
    [SerializeField] private string[] variantManagedScenePrefixes = { "level0" };

    [Header("Cycle Transition Screen")]
    [SerializeField] private bool useCycleTransitionScreen = true;
    [SerializeField] private float transitionTypewriterCharsPerSecond = 46f;
    [SerializeField] private float transitionMinBlackSeconds = 1.6f;
    [SerializeField] private string[] cycleNarrativeLines =
    {
        "\u0421\u0418\u041d\u0425\u0420\u041e\u041d\u0418\u0417\u0410\u0426\u0418\u042f \u0426\u0418\u041a\u041b\u0410...\n\u041f\u0440\u043e\u0441\u0442\u0440\u0430\u043d\u0441\u0442\u0432\u0435\u043d\u043d\u0430\u044f \u0444\u0440\u0430\u043a\u0442\u0443\u0440\u0430 \u0441\u043c\u0435\u0449\u0430\u0435\u0442 \u043a\u043e\u043d\u0444\u0438\u0433\u0443\u0440\u0430\u0446\u0438\u044e \u043a\u043e\u043c\u043f\u043b\u0435\u043a\u0441\u0430.",
        "\u041f\u0410\u041c\u042f\u0422\u042c \u0426\u0418\u041a\u041b\u0410 \u041e\u0411\u041d\u041e\u0412\u041b\u0415\u041d\u0410.\n\u041c\u0430\u0440\u0448\u0440\u0443\u0442\u044b \u0438 \u0431\u043e\u0435\u0432\u044b\u0435 \u043f\u043e\u0437\u0438\u0446\u0438\u0438 \u043f\u0440\u043e\u0442\u0438\u0432\u043d\u0438\u043a\u0430 \u043f\u0435\u0440\u0435\u0441\u0442\u0440\u043e\u0435\u043d\u044b.",
        "\u042d\u041d\u0422\u0420\u041e\u041f\u0418\u042f \u0420\u0415\u0410\u041b\u042c\u041d\u041e\u0421\u0422\u0418 \u0420\u0410\u0421\u0422\u0415\u0422.\n\u0410\u0440\u0445\u0438\u0442\u0435\u043a\u0442\u0443\u0440\u0430 \u0441\u0435\u043a\u0442\u043e\u0440\u0430 \u0438\u0437\u043c\u0435\u043d\u0435\u043d\u0430. \u0411\u0443\u0434\u044c\u0442\u0435 \u0433\u043e\u0442\u043e\u0432\u044b."
    };

    [Header("UI")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    private bool isDead = false;
    private bool isRestarting;
    private bool isReturningToMenu;
    private readonly List<Behaviour> disabledPlayerBehaviours = new();
    private readonly List<PauseManager> cachedPauseManagers = new();
    private float nextPauseManagersRefreshTime;

    public bool IsDeathScreenActive => (deathPanel != null && deathPanel.activeSelf) || isDead;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        GlobalDeathActive = false;

        ResolveDeathPanelReference(force: true);
        if (deathPanel != null) deathPanel.SetActive(false);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (restartButton != null) restartButton.onClick.AddListener(RestartLevel);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (restartButton != null) restartButton.onClick.RemoveListener(RestartLevel);
        if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
        if (quitButton != null) quitButton.onClick.RemoveListener(QuitGame);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResolveDeathPanelReference(force: true);
        if (deathPanel != null && deathPanel.activeSelf)
        {
            EnsureDeathPanelVisible();
            RebindButtons();
        }
    }

    public void ShowDeathScreen()
    {
        if (isDead) return;
        ResolveDeathPanelReference(force: false);

        if (deathPanel != null)
        {
            RebindButtons();
            NormalizeDeathPanelRaycastTargets();
            deathPanel.transform.SetAsLastSibling();
            EnsureDeathPanelVisible();
        }

        RefreshPauseManagersIfNeeded();
        for (int i = 0; i < cachedPauseManagers.Count; i++)
        {
            PauseManager manager = cachedPauseManagers[i];
            if (manager != null)
                manager.ForceCloseForDeath();
        }

        LockPlayerControlsForDeath();
        GlobalDeathActive = true;
        isDead = true;
        Time.timeScale = 1f; 
        if (deathPanel != null) deathPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartLevel()
    {
        if (isRestarting) return;

        Debug.Log("[DeathScreen] RestartLevel pressed");
        isRestarting = true;
        StartCoroutine(RestartCurrentSceneRoutine());
    }

    private IEnumerator RestartCurrentSceneRoutine()
    {
        GlobalDeathActive = false;
        Time.timeScale = 1f;
        RestorePlayerControlsIfNeeded();
        PendingHardRespawn = true;

        if (deathPanel != null) deathPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        isDead = false;

        string targetSceneName = ResolveRestartSceneName();
        if (useCycleTransitionScreen)
        {
            CycleTransitionScreen transition = CycleTransitionScreen.Instance;
            if (transition != null)
            {
                yield return transition.PlayTransitionAndLoad(
                    targetSceneName,
                    BuildCycleTransitionText(),
                    transitionTypewriterCharsPerSecond,
                    transitionMinBlackSeconds
                );
            }
            else
            {
                SceneManager.LoadScene(targetSceneName);
            }
        }
        else
        {
            SceneManager.LoadScene(targetSceneName);
        }

        isRestarting = false;
    }

    public void ReturnToMainMenu()
    {
        if (isReturningToMenu) return;

        isReturningToMenu = true;
        GlobalDeathActive = false;
        PendingHardRespawn = false;
        Time.timeScale = 1f;
        RestorePlayerControlsIfNeeded();
        RespawnCheckpointState.Clear();
        SceneManager.LoadScene(mainMenuSceneName);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        isDead = false;
        isReturningToMenu = false;
    }

    public void QuitGame()
    {
        GlobalDeathActive = false;
        PendingHardRespawn = false;
        Time.timeScale = 1f;
        RestorePlayerControlsIfNeeded();
        Application.Quit();
    }

    private void RebindButtons()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartLevel);
            restartButton.interactable = true;
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
            mainMenuButton.interactable = true;
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(QuitGame);
            quitButton.interactable = true;
        }
    }

    private void NormalizeDeathPanelRaycastTargets()
    {
        if (deathPanel == null) return;

        HashSet<Graphic> clickableGraphics = new();
        Button[] buttons = deathPanel.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].targetGraphic != null)
            {
                clickableGraphics.Add(buttons[i].targetGraphic);
            }
        }

        Graphic[] graphics = deathPanel.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic g = graphics[i];
            if (g == null) continue;
            g.raycastTarget = clickableGraphics.Contains(g);
        }
    }

    private void LockPlayerControlsForDeath()
    {
        disabledPlayerBehaviours.Clear();

        Transform player = PlayerLocator.GetPlayerTransform(forceRefresh: true);
        if (player == null) return;

        DisableBehaviours<PlayerMovement>(player.gameObject);
        DisableBehaviours<PlayerLook>(player.gameObject);
        DisableBehaviours<PlayerCrouch>(player.gameObject);
        DisableBehaviours<PlayerNeuroresist>(player.gameObject);
        DisableBehaviours<WeaponManager>(player.gameObject);
        DisableBehaviours<AimController>(player.gameObject);
        DisableBehaviours<SwayNBobScript>(player.gameObject);
    }

    private void RestorePlayerControlsIfNeeded()
    {
        for (int i = 0; i < disabledPlayerBehaviours.Count; i++)
        {
            Behaviour b = disabledPlayerBehaviours[i];
            if (b != null) b.enabled = true;
        }
        disabledPlayerBehaviours.Clear();
    }

    private void DisableBehaviours<T>(GameObject root) where T : Behaviour
    {
        T[] components = root.GetComponentsInChildren<T>(true);
        for (int i = 0; i < components.Length; i++)
        {
            T c = components[i];
            if (c == null || !c.enabled) continue;
            c.enabled = false;
            disabledPlayerBehaviours.Add(c);
        }
    }

    private void RefreshPauseManagersIfNeeded()
    {
        if (Time.unscaledTime < nextPauseManagersRefreshTime && cachedPauseManagers.Count > 0)
            return;

        cachedPauseManagers.Clear();
        PauseManager[] pauseManagers = FindObjectsOfType<PauseManager>(true);
        for (int i = 0; i < pauseManagers.Length; i++)
        {
            if (pauseManagers[i] != null)
                cachedPauseManagers.Add(pauseManagers[i]);
        }

        nextPauseManagersRefreshTime = Time.unscaledTime + 1f;
    }

    private void ResolveDeathPanelReference(bool force = false)
    {
        if (!force && deathPanel != null) return;

        deathPanel = GameObject.Find("DeathPanel");
        if (deathPanel == null)
        {
            Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas c = canvases[i];
                if (c == null) continue;
                if (!c.gameObject.scene.IsValid()) continue;
                Transform t = c.transform.Find("DeathPanel");
                if (t != null)
                {
                    deathPanel = t.gameObject;
                    break;
                }
            }
        }

        if (deathPanel != null)
        {
            if (restartButton == null)
            {
                Transform t = deathPanel.transform.Find("Restart");
                if (t != null) restartButton = t.GetComponent<Button>();
            }

            if (mainMenuButton == null)
            {
                Transform t = deathPanel.transform.Find("Exit");
                if (t != null) mainMenuButton = t.GetComponent<Button>();
            }
        }
    }

    private void EnsureDeathPanelVisible()
    {
        if (deathPanel == null) return;

        deathPanel.SetActive(true);
        CanvasGroup[] groups = deathPanel.GetComponentsInChildren<CanvasGroup>(true);
        for (int i = 0; i < groups.Length; i++)
        {
            CanvasGroup g = groups[i];
            if (g == null) continue;
            g.alpha = 1f;
            g.interactable = true;
            g.blocksRaycasts = true;
        }
    }

    private string ResolveRestartSceneName()
    {
        string current = SceneManager.GetActiveScene().name;
        if (!useVariantSceneMapping || variantSceneNames == null || variantSceneNames.Length == 0)
            return current;

        bool isManaged = false;
        for (int i = 0; i < variantSceneNames.Length; i++)
        {
            string scene = variantSceneNames[i];
            if (!string.IsNullOrWhiteSpace(scene) && scene == current)
            {
                isManaged = true;
                break;
            }
        }

        if (!isManaged && variantManagedScenePrefixes != null)
        {
            for (int i = 0; i < variantManagedScenePrefixes.Length; i++)
            {
                string prefix = variantManagedScenePrefixes[i];
                if (string.IsNullOrWhiteSpace(prefix)) continue;
                if (!current.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase)) continue;
                isManaged = true;
                break;
            }
        }

        if (!isManaged) return current;

        DeathCycleManager manager = DeathCycleManager.Instance;
        if (manager == null) return current;

        int variant = manager.CurrentVariantIndex;
        int idx = variant % variantSceneNames.Length;
        if (idx < 0) idx += variantSceneNames.Length;

        string mapped = variantSceneNames[idx];
        if (string.IsNullOrWhiteSpace(mapped)) return current;
        return mapped;
    }

    private string BuildCycleTransitionText()
    {
        DeathCycleManager manager = DeathCycleManager.Instance;
        int cycle = manager != null ? manager.CycleCount : 0;
        int tier = manager != null ? manager.EntropyTier : 1;
        int variant = manager != null ? manager.CurrentVariantIndex : 0;

        if (cycleNarrativeLines != null && cycleNarrativeLines.Length > 0)
        {
            int idx = variant % cycleNarrativeLines.Length;
            if (idx < 0) idx += cycleNarrativeLines.Length;
            string baseLine = cycleNarrativeLines[idx];
            return $"{baseLine}\n\n\u0426\u0438\u043a\u043b: {cycle}   |   \u042d\u043d\u0442\u0440\u043e\u043f\u0438\u044f: T{tier}";
        }

        return $"\u041f\u0435\u0440\u0435\u0441\u0442\u0440\u043e\u0439\u043a\u0430 \u0440\u0435\u0430\u043b\u044c\u043d\u043e\u0441\u0442\u0438 \u0437\u0430\u0432\u0435\u0440\u0448\u0435\u043d\u0430.\n\u0426\u0438\u043a\u043b: {cycle}   |   \u042d\u043d\u0442\u0440\u043e\u043f\u0438\u044f: T{tier}";
    }
}
