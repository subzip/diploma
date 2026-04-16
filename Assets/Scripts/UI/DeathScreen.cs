// DeathScreen.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

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

    [Header("UI")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    private bool isDead = false;
    private bool isRestarting;
    private bool isReturningToMenu;
    private readonly List<Behaviour> disabledPlayerBehaviours = new();

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

        PauseManager[] pauseManagers = FindObjectsOfType<PauseManager>(true);
        for (int i = 0; i < pauseManagers.Length; i++)
        {
            if (pauseManagers[i] != null)
                pauseManagers[i].ForceCloseForDeath();
        }

        LockPlayerControlsForDeath();
        GlobalDeathActive = true;
        isDead = true;
        Time.timeScale = 1f; // death no longer relies on global timescale pause
        if (deathPanel != null) deathPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartLevel()
    {
        if (isRestarting) return;

        Debug.Log("[DeathScreen] RestartLevel pressed");
        isRestarting = true;
        RestartCurrentScene();
    }

    private void RestartCurrentScene()
    {
        GlobalDeathActive = false;
        Time.timeScale = 1f;
        RestorePlayerControlsIfNeeded();
        PendingHardRespawn = true;

        if (deathPanel != null) deathPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        isDead = false;
        isRestarting = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
}
