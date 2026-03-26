// DeathScreen.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DeathScreen : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "StartGame";

    [Header("UI")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    private bool isDead = false;

    private void Awake()
    {
        if (deathPanel == null)
            deathPanel = GameObject.Find("DeathPanel");

        if (deathPanel != null)
            deathPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (restartButton != null) restartButton.onClick.AddListener(RestartLevel);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
    }

    private void OnDisable()
    {
        if (restartButton != null) restartButton.onClick.RemoveListener(RestartLevel);
        if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
        if (quitButton != null) quitButton.onClick.RemoveListener(QuitGame);
    }

    public void ShowDeathScreen()
    {
        if (deathPanel == null) return;

        isDead = true;
        Time.timeScale = 0f;
        deathPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartLevel()
    {
        if (!isDead) return;

        Time.timeScale = 1f;
        DontDestroyMarker[] markers = FindObjectsOfType<DontDestroyMarker>();
        foreach (DontDestroyMarker marker in markers)
        {
            Destroy(marker.gameObject);
        }

        
        SceneManager.LoadScene("level0");
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ReturnToMainMenu()
    {
        if (!isDead) return;

        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void QuitGame()
    {
        if (!isDead) return;
        Time.timeScale = 1f;
        Application.Quit();
    }
}
