using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    public static bool IsPaused { get; private set; }
    public static bool IsInputGuardActive => Time.unscaledTime < inputGuardUntilUnscaledTime;

    private static float inputGuardUntilUnscaledTime;

    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject camera1;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private AudioCue hoverCue;
    [SerializeField] private AudioCue clickCue;
    [SerializeField, Range(0f, 1f)] private float hoverVolume = 0.2f;
    [SerializeField, Range(0f, 1f)] private float clickVolume = 0.35f;
    [SerializeField] private float unpauseInputGuardSeconds = 0.12f;

    private PlayerInputActions inputActions;

    private void OnEnable()
    {
        ResolveInputActions();
        if (inputActions == null) return;
        inputActions.Player.Pause.performed += OnPausePerformed;
    }

    private void OnDisable()
    {
        if (inputActions == null) return;
        inputActions.Player.Pause.performed -= OnPausePerformed;
    }

    private void Start()
    {
        WirePauseButtons();
        EnsureClosedState();
    }

    private void OnPausePerformed(UnityEngine.InputSystem.InputAction.CallbackContext _) => TogglePause();

    public void Continue()
    {
        ClosePause();
    }

    public void Exit()
    {
        ClosePause();
        SceneManager.LoadScene("StartGame");
        Destroy(player);
        Destroy(camera1);
    }

    public void TogglePause()
    {
        if (DeathScreen.GlobalDeathActive) return;
        if (Time.timeScale == 0f && (pausePanel == null || !pausePanel.activeSelf)) return;

        DeathScreen deathScreen = DeathScreen.Instance;
        if (deathScreen != null && deathScreen.IsDeathScreenActive) return;

        if (pausePanel == null) return;

        if (!IsPaused) OpenPause();
        else ClosePause();
    }

    public void ForceCloseForDeath()
    {
        EnsureClosedState();
    }

    private void ResolveInputActions()
    {
        if (inputActions != null) return;
        if (GameInput.Instance == null) return;
        inputActions = GameInput.Instance.Actions;
    }

    private void OpenPause()
    {
        pausePanel.SetActive(true);
        IsPaused = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ClosePause()
    {
        if (pausePanel != null) pausePanel.SetActive(false);

        IsPaused = false;
        Time.timeScale = 1f;
        inputGuardUntilUnscaledTime = Time.unscaledTime + Mathf.Max(0f, unpauseInputGuardSeconds);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void EnsureClosedState()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        IsPaused = false;
        Time.timeScale = 1f;
    }

    private void WirePauseButtons()
    {
        if (continueButton == null && pausePanel != null)
        {
            Transform t = pausePanel.transform.Find("Continue");
            if (t != null) continueButton = t.GetComponent<Button>();
        }

        if (exitButton == null && pausePanel != null)
        {
            Transform t = pausePanel.transform.Find("Exit");
            if (t != null) exitButton = t.GetComponent<Button>();
        }

        AttachSfx(continueButton);
        AttachSfx(exitButton);
    }

    private void AttachSfx(Button button)
    {
        if (button == null) return;

        UiButtonSfx sfx = button.GetComponent<UiButtonSfx>();
        if (sfx == null) sfx = button.gameObject.AddComponent<UiButtonSfx>();

        sfx.SetCues(hoverCue, clickCue, hoverVolume, clickVolume);
    }
}
