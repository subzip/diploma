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

    private void Awake()
    {
        inputActions = new PlayerInputActions();
    }
    

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Pause.performed += _ => TogglePause();
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

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
}
