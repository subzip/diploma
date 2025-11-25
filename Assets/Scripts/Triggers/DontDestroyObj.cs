// DontDestroyObj.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class DontDestroyObj : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "StartGame";

    void Awake()
    {
        if (SceneManager.GetActiveScene().name == mainMenuSceneName)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        gameObject.AddComponent<DontDestroyMarker>();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == mainMenuSceneName)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}