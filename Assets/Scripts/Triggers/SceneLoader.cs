// SceneLoader.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string targetSceneName = "Level-1";
    [SerializeField] private Vector3 spawnPosition = Vector3.zero; // Позиция за дверью

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
        if (other.CompareTag("Player") && !isTransitioning)
        {
            StartCoroutine(LoadSceneWithFade());
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