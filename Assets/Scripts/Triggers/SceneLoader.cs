using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string targetSceneName = "Level-1";
    [SerializeField] private QuestSystem questSystem;
    [SerializeField] private string requiredQuestTitle = "Активировать дверь";
    [SerializeField] private Vector3 spawnPosition = new Vector3(27.331f, 7.086f, -23.299f);

    [Header("Transition")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1f;

    private bool isTransitioning;

    private void Start()
    {
        if (fadeImage != null)
        {
            fadeImage.canvasRenderer.SetAlpha(0f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isTransitioning) return;
        if (!ComponentSearch.IsPlayer(other)) return;
        if (questSystem == null) return;

        QuestItem current = questSystem.GetCurrentQuest();
        if (current != null && string.Equals(current.title, requiredQuestTitle, System.StringComparison.Ordinal))
        {
            StartCoroutine(LoadSceneWithFade());
        }
        else
        {
            Debug.Log("Card!");
        }
    }

    private IEnumerator LoadSceneWithFade()
    {
        isTransitioning = true;

        if (fadeImage != null)
        {
            fadeImage.CrossFadeAlpha(1f, fadeDuration * 0.5f, true);
            yield return new WaitForSeconds(fadeDuration * 0.5f);
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
        asyncLoad.allowSceneActivation = true;
        yield return asyncLoad;

        Transform player = PlayerLocator.GetPlayerTransform(forceRefresh: true);
        if (player != null)
        {
            player.position = spawnPosition;
        }

        if (fadeImage != null)
        {
            fadeImage.CrossFadeAlpha(0f, fadeDuration * 0.5f, true);
            yield return new WaitForSeconds(fadeDuration * 0.5f);
        }

        isTransitioning = false;
    }
}
