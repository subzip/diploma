using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class PowerOutageScenario : MonoBehaviour
{
    [Header("Flow")]
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private float blackoutSeconds = 1.5f;

    [Header("Lights")]
    [SerializeField] private GameObject[] normalLightGroups;
    [SerializeField] private GameObject[] emergencyLightGroups;
    [SerializeField] private LightEmissionGroupController[] normalEmissionGroups;
    [SerializeField] private LightEmissionGroupController[] emergencyEmissionGroups;

    [Header("Enemies")]
    [SerializeField] private GameObject[] enemyGroupsBeforeOutage;
    [SerializeField] private GameObject[] enemyGroupsAfterOutage;

    [Header("Generator")]
    [SerializeField] private GeneratorSwitchInteract generatorSwitch;
    [SerializeField] private GameObject generatorRoomHintMarker;
    [SerializeField] private QuestUI questUI;
    [SerializeField] private string outageStartHint = "Задание обновлено!";
    [SerializeField] private string powerRestoredHint = "Питание восстановлено";
    [SerializeField] private bool showFlashlightHintAfterOutage = true;
    [SerializeField, Min(0f)] private float flashlightHintDelaySeconds = 2.5f;
    [SerializeField] private string flashlightHint = "Фонарик";

    [Header("Audio")]
    [SerializeField] private AudioCue powerDownCue;
    [SerializeField] private AudioCue emergencyOnCue;
    [SerializeField] private AudioCue powerRestoreCue;
    [SerializeField] private AudioCue emergencyAlarmLoopCue;
    [SerializeField, Range(0f, 2f)] private float powerDownVolume = 1f;
    [SerializeField, Range(0f, 2f)] private float emergencyOnVolume = 1f;
    [SerializeField, Range(0f, 2f)] private float powerRestoreVolume = 1f;
    [SerializeField, Range(0f, 2f)] private float alarmLoopVolume = 0.18f;

    [Header("Optional Quest Hook")]
    [SerializeField] private QuestSystem questSystem;
    [SerializeField] private bool completeCurrentQuestOnOutageStart;
    [SerializeField] private bool completeCurrentQuestOnPowerRestore;

    [Header("Level-1 Quest Flow")]
    [SerializeField] private bool useLevel1QuestFlow = true;
    [SerializeField] private string level1SceneName = "Level-1";
    [SerializeField] private string findElevatorTitle = "Найти Лифт";
    [SerializeField] private string findElevatorDescription = "Найдите лифт и покиньте лабораторию";
    [SerializeField] private string restorePowerTitle = "Восстановить электричество";
    [SerializeField] private string restorePowerDescription = "Найдите генератор и включите электричество";
    [SerializeField] private string leaveLabTitle = "Покинуть лабораторию";
    [SerializeField] private string leaveLabDescription = "Электричество восстановлено, найдите лифт и покиньте лабораторию";

    public bool HasOutageStarted { get; private set; }
    public bool IsPowerRestored { get; private set; }

    private bool transitionRunning;
    private AudioSource alarmLoopSource;

    private void Start()
    {
        ResolveUiRefs();
        SetGroupsActive(normalLightGroups, true);
        SetGroupsActive(emergencyLightGroups, false);
        SetEmissionGroups(normalEmissionGroups, true);
        SetEmissionGroups(emergencyEmissionGroups, false);
        SetGroupsActive(enemyGroupsAfterOutage, false);

        if (generatorSwitch != null)
            generatorSwitch.SetInteractEnabled(false);

        if (generatorRoomHintMarker != null)
            generatorRoomHintMarker.SetActive(false);

        EnsureAlarmLoopSource();
        StopAlarmLoop();
        ApplyInitialLevelQuest();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!ComponentSearch.IsPlayer(other)) return;
        if (transitionRunning) return;
        if (triggerOnce && HasOutageStarted) return;
        StartCoroutine(BeginOutageRoutine());
    }

    private IEnumerator BeginOutageRoutine()
    {
        transitionRunning = true;
        HasOutageStarted = true;
        IsPowerRestored = false;

        if (completeCurrentQuestOnOutageStart && questSystem != null)
            questSystem.CompleteCurrentQuest();

        AudioService.PlayAt(powerDownCue, transform.position, powerDownVolume, ambience: false);
        SetGroupsActive(normalLightGroups, false);
        SetGroupsActive(enemyGroupsBeforeOutage, false);
        SetGroupsActive(emergencyLightGroups, false);
        SetEmissionGroups(normalEmissionGroups, false);
        SetEmissionGroups(emergencyEmissionGroups, false);

        yield return new WaitForSeconds(Mathf.Max(0.1f, blackoutSeconds));

        SetGroupsActive(emergencyLightGroups, true);
        SetEmissionGroups(emergencyEmissionGroups, true);
        SetGroupsActive(enemyGroupsAfterOutage, true);
        AudioService.PlayAt(emergencyOnCue, transform.position, emergencyOnVolume, ambience: true);
        StartAlarmLoop();

        if (generatorSwitch != null)
            generatorSwitch.SetInteractEnabled(true);

        if (generatorRoomHintMarker != null)
            generatorRoomHintMarker.SetActive(true);

        if (questUI != null && !string.IsNullOrWhiteSpace(outageStartHint))
            questUI.ShowHint(outageStartHint);

        SetSingleQuest(restorePowerTitle, restorePowerDescription);
        if (showFlashlightHintAfterOutage && questUI != null && !string.IsNullOrWhiteSpace(flashlightHint))
            StartCoroutine(ShowFlashlightHintRoutine());
        transitionRunning = false;
    }

    public void RestorePower()
    {
        if (!HasOutageStarted) return;
        if (IsPowerRestored) return;

        IsPowerRestored = true;
        StopAlarmLoop();
        AudioService.PlayAt(powerRestoreCue, transform.position, powerRestoreVolume, ambience: true);
        SetGroupsActive(normalLightGroups, true);
        SetGroupsActive(emergencyLightGroups, false);
        SetEmissionGroups(normalEmissionGroups, true);
        SetEmissionGroups(emergencyEmissionGroups, false);

        if (generatorRoomHintMarker != null)
            generatorRoomHintMarker.SetActive(false);

        if (generatorSwitch != null)
            generatorSwitch.SetInteractEnabled(false);

        if (completeCurrentQuestOnPowerRestore && questSystem != null)
            questSystem.CompleteCurrentQuest();

        if (questUI != null && !string.IsNullOrWhiteSpace(powerRestoredHint))
            questUI.ShowHint(powerRestoredHint);

        SetSingleQuest(leaveLabTitle, leaveLabDescription);
    }

    private void ResolveUiRefs()
    {
        if (questUI == null)
            questUI = FindObjectOfType<QuestUI>(true);
        if (questSystem == null)
            questSystem = FindObjectOfType<QuestSystem>(true);
    }

    private void ApplyInitialLevelQuest()
    {
        if (!ShouldRunLevel1QuestFlow()) return;
        SetSingleQuest(findElevatorTitle, findElevatorDescription);
    }

    private bool ShouldRunLevel1QuestFlow()
    {
        if (!useLevel1QuestFlow) return false;
        string activeSceneName = SceneManager.GetActiveScene().name;
        return string.Equals(activeSceneName, level1SceneName, System.StringComparison.Ordinal);
    }

    private void SetSingleQuest(string title, string description)
    {
        if (!ShouldRunLevel1QuestFlow()) return;
        if (questSystem == null) return;
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description)) return;
        questSystem.SetSingleActiveQuest(title, description);
    }

    private static void SetGroupsActive(GameObject[] groups, bool active)
    {
        if (groups == null) return;
        for (int i = 0; i < groups.Length; i++)
        {
            if (groups[i] != null) groups[i].SetActive(active);
        }
    }

    private static void SetEmissionGroups(LightEmissionGroupController[] groups, bool enabled)
    {
        if (groups == null) return;
        for (int i = 0; i < groups.Length; i++)
        {
            if (groups[i] != null) groups[i].SetEmissionEnabled(enabled);
        }
    }

    private void EnsureAlarmLoopSource()
    {
        if (alarmLoopSource != null) return;
        alarmLoopSource = GetComponent<AudioSource>();
        if (alarmLoopSource == null)
            alarmLoopSource = gameObject.AddComponent<AudioSource>();

        alarmLoopSource.playOnAwake = false;
        alarmLoopSource.loop = true;
    }

    private void StartAlarmLoop()
    {
        if (emergencyAlarmLoopCue == null || !emergencyAlarmLoopCue.IsValid) return;
        EnsureAlarmLoopSource();
        if (alarmLoopSource == null) return;

        alarmLoopSource.clip = emergencyAlarmLoopCue.GetRandomClip();
        alarmLoopSource.pitch = emergencyAlarmLoopCue.GetRandomPitch();
        alarmLoopSource.spatialBlend = emergencyAlarmLoopCue.SpatialBlend;
        alarmLoopSource.minDistance = emergencyAlarmLoopCue.MinDistance;
        alarmLoopSource.maxDistance = emergencyAlarmLoopCue.MaxDistance;
        alarmLoopSource.volume = emergencyAlarmLoopCue.Volume * Mathf.Max(0f, alarmLoopVolume) *
                                 AudioService.GetMasterVolume(AudioCue.AudioCategory.MusicAmbient);
        if (!alarmLoopSource.isPlaying)
            alarmLoopSource.Play();
    }

    private void StopAlarmLoop()
    {
        if (alarmLoopSource == null) return;
        if (alarmLoopSource.isPlaying)
            alarmLoopSource.Stop();
    }

    private IEnumerator ShowFlashlightHintRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, flashlightHintDelaySeconds));
        if (questUI == null) yield break;
        if (!HasOutageStarted || IsPowerRestored) yield break;
        questUI.ShowHint(flashlightHint);
    }
}
