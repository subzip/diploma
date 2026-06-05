
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class DontDestroyObj : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "StartGame";
    
    
    [SerializeField] private bool persistAcrossScenes = true;
    [SerializeField] private string uniqueId;

    private static readonly HashSet<string> ActiveIds = new();
    private string registeredId;
    private bool ownsRegistration;

    void Awake()
    {
        registeredId = string.IsNullOrWhiteSpace(uniqueId) ? gameObject.name : uniqueId;
        if (ActiveIds.Contains(registeredId))
        {
            Destroy(gameObject);
            return;
        }

        ActiveIds.Add(registeredId);
        ownsRegistration = true;

        if (SceneManager.GetActiveScene().name == mainMenuSceneName)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        gameObject.AddComponent<DontDestroyMarker>();
        RemoveDuplicateInstancesForRegisteredId();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RemoveDuplicateInstancesForRegisteredId();

        if (scene.name == mainMenuSceneName)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (ownsRegistration && !string.IsNullOrWhiteSpace(registeredId))
        {
            ActiveIds.Remove(registeredId);
        }
    }

    private void RemoveDuplicateInstancesForRegisteredId()
    {
        if (!ownsRegistration || string.IsNullOrWhiteSpace(registeredId)) return;

        DontDestroyObj[] all = Resources.FindObjectsOfTypeAll<DontDestroyObj>();
        for (int i = 0; i < all.Length; i++)
        {
            DontDestroyObj other = all[i];
            if (other == null || other == this) continue;
            if (!other.gameObject.scene.IsValid()) continue;
            if (other.gameObject.scene.name != "DontDestroyOnLoad") continue;
            if (other.registeredId != registeredId) continue;
            Destroy(other.gameObject);
        }
    }
}
