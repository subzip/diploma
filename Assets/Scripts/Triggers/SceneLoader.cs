// SceneLoader.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string targetSceneName = "Level-1";
    [SerializeField] private QuestSystem questSystem;
    [SerializeField] private Vector3 spawnPosition = new Vector3(27.331f, 7.086f, -23.299f); // Позиция за дверью
    //27.331 7.086 -23.299
    [Header("Transition")]
    [SerializeField] private Image fadeImage; // Чёрный UI Image на Canvas
    [SerializeField] private float fadeDuration = 1f;
   

    private bool isTransitioning = false;

    private void Start()
    {
        if (fadeImage != null)
        {
            fadeImage.canvasRenderer.SetAlpha(0f); // Скрыть в начале
        }
    }

    private void OnTriggerEnter(Collider other)
{
    if (other.CompareTag("Player"))
    {
        // Проверяем, выполнена ли задача "Активировать дверь"
        QuestItem current = questSystem.GetCurrentQuest();
        if (current != null && current.title == "Активировать дверь")
        {
            StartCoroutine(LoadSceneWithFade());
        }
        else
        {
            Debug.Log("Нужна ключ-карта!");
        }
    }
}

    private IEnumerator LoadSceneWithFade()
    {
        isTransitioning = true;

        // 1. Затемнение экрана
        if (fadeImage != null)
        {
            fadeImage.CrossFadeAlpha(1f, fadeDuration * 0.5f, true);
            yield return new WaitForSeconds(fadeDuration * 0.5f);
        }

        // 2. Загрузка новой сцены
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
        asyncLoad.allowSceneActivation = true;
        yield return asyncLoad;

        // 3. Переместить игрока за дверь
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = spawnPosition;
        }

        // 4. Осветление экрана
        if (fadeImage != null)
        {
            fadeImage.CrossFadeAlpha(0f, fadeDuration * 0.5f, true);
            yield return new WaitForSeconds(fadeDuration * 0.5f);
        }

        isTransitioning = false;
    }
}