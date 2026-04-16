using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject camera1;
    private bool isPaused = false;
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

    private void OnPausePerformed(UnityEngine.InputSystem.InputAction.CallbackContext _) => TogglePause();

    public void Continue()
    {
        Debug.Log("Pause");
        pausePanel.SetActive(false);
        Time.timeScale = 1;
        isPaused = !isPaused;
    }

    public void Exit()
    {
        SceneManager.LoadScene("StartGame");
        Destroy(player);
        Destroy(camera1);
    }

    public void TogglePause()
    {
        if (DeathScreen.GlobalDeathActive) return;

        // If timeScale is already 0 but this pause panel is not active,
        // it means game is paused by another system (e.g. death). Do not unpause from here.
        if (Time.timeScale == 0f && (pausePanel == null || !pausePanel.activeSelf)) return;

        DeathScreen deathScreen = DeathScreen.Instance;
        if (deathScreen != null && deathScreen.IsDeathScreenActive) return;

        if(pausePanel != null)
        {
            if(isPaused == false)
            {
                pausePanel.SetActive(true);
                Time.timeScale = 0;
            }

            else
            {
                pausePanel.SetActive(false);
                Time.timeScale = 1;
            }
                
            isPaused = !isPaused;

        }
    }

    public void ForceCloseForDeath()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;
    }

    private void ResolveInputActions()
    {
        if (inputActions != null) return;
        if (GameInput.Instance == null) return;
        inputActions = GameInput.Instance.Actions;
    }
}
