using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string targetSceneName = "Level-1";
    [SerializeField] private QuestSystem questSystem;
    [SerializeField] private string requiredQuestTitle = "\u0410\u043a\u0442\u0438\u0432\u0438\u0440\u043e\u0432\u0430\u0442\u044c \u0434\u0432\u0435\u0440\u044c";
    [SerializeField] private Vector3 spawnPosition = new Vector3(27.331f, 7.086f, -23.299f);
    [SerializeField] private Vector3 spawnRotationEuler = Vector3.zero;
    [SerializeField] private bool registerEntryCheckpoint = true;
    [SerializeField] private string entryCheckpointId = "scene_entry";

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
        if (questSystem == null)
            questSystem = FindObjectOfType<QuestSystem>(true);
        if (questSystem == null) return;

        bool canOpenByCurrentQuest =
            questSystem.GetCurrentQuest() != null &&
            string.Equals(questSystem.GetCurrentQuest().title, requiredQuestTitle, System.StringComparison.Ordinal);
        bool canOpenByCompleted = questSystem.IsQuestCompletedByTitle(requiredQuestTitle);

        if (canOpenByCurrentQuest || canOpenByCompleted)
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

        Quaternion spawnRotation = Quaternion.Euler(spawnRotationEuler);
        SceneEntrySpawnState.SetPending(
            targetSceneName,
            spawnPosition,
            spawnRotation,
            registerEntryCheckpoint,
            entryCheckpointId
        );

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
        asyncLoad.allowSceneActivation = true;
        yield return asyncLoad;

        yield return null;
        yield return new WaitForEndOfFrame();
        ApplySpawnAndCheckpoint();

        if (fadeImage != null)
        {
            fadeImage.CrossFadeAlpha(0f, fadeDuration * 0.5f, true);
            yield return new WaitForSeconds(fadeDuration * 0.5f);
        }

        isTransitioning = false;
    }

    private void ApplySpawnAndCheckpoint()
    {
        Transform player = PlayerLocator.GetPlayerTransform(forceRefresh: true);
        if (player == null) return;

        Quaternion spawnRotation = Quaternion.Euler(spawnRotationEuler);
        string activeScene = SceneManager.GetActiveScene().name;

        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        if (movement != null)
            movement.ResetForRespawnAt(spawnPosition, spawnRotation, resetStaminaToMax: false);
        else
            player.SetPositionAndRotation(spawnPosition, spawnRotation);

        if (registerEntryCheckpoint)
        {
            RespawnCheckpointState.SetCheckpoint(
                activeScene,
                spawnPosition,
                spawnRotation,
                string.IsNullOrWhiteSpace(entryCheckpointId) ? "scene_entry" : entryCheckpointId
            );
        }

        Debug.Log($"[SceneLoader] Applied spawn in '{activeScene}' at {spawnPosition}, rot={spawnRotation.eulerAngles}");
    }
}
