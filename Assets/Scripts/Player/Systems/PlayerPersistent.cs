// PlayerPersistent.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerPersistent : MonoBehaviour
{
    private static PlayerPersistent instance;

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
            Destroy(gameObject); // Удаляем дубликаты
        }
    }

    // Вызывается при выходе из триггера (необязательно)
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
        // Можно добавить логику пост-загрузки (например, позиционирование)
    }
}