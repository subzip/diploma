// DeathScreen.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathScreen : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "StartGame";

    private GameObject deathPanel;
    private bool isDead = false;

    private void Start()
    {
        deathPanel = GameObject.Find("DeathPanel");
        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("DeathPanel not");
        }
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
    }

    public void ReturnToMainMenu()
    {
        if (!isDead) return;

        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}