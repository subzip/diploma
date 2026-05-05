using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PowerOutageScenario : MonoBehaviour
{
    [Header("Flow")]
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private float blackoutSeconds = 1.5f;

    [Header("Lights")]
    [SerializeField] private GameObject[] normalLightGroups;
    [SerializeField] private GameObject[] emergencyLightGroups;

    [Header("Enemies")]
    [SerializeField] private GameObject[] enemyGroupsBeforeOutage;
    [SerializeField] private GameObject[] enemyGroupsAfterOutage;

    [Header("Generator")]
    [SerializeField] private GeneratorSwitchInteract generatorSwitch;
    [SerializeField] private GameObject generatorRoomHintMarker;
    [SerializeField] private QuestUI questUI;
    [SerializeField] private string outageStartHint = "Задание обновлено: Найдите генератор и восстановите питание.";
    [SerializeField] private string powerRestoredHint = "Питание восстановлено. Вернитесь к лифту.";

    [Header("Optional Quest Hook")]
    [SerializeField] private QuestSystem questSystem;
    [SerializeField] private bool completeCurrentQuestOnOutageStart;
    [SerializeField] private bool completeCurrentQuestOnPowerRestore;

    public bool HasOutageStarted { get; private set; }
    public bool IsPowerRestored { get; private set; }

    private bool transitionRunning;

    private void Start()
    {
        ResolveUiRefs();
        SetGroupsActive(normalLightGroups, true);
        SetGroupsActive(emergencyLightGroups, false);
        SetGroupsActive(enemyGroupsAfterOutage, false);

        if (generatorSwitch != null)
            generatorSwitch.SetInteractEnabled(false);

        if (generatorRoomHintMarker != null)
            generatorRoomHintMarker.SetActive(false);
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

        SetGroupsActive(normalLightGroups, false);
        SetGroupsActive(enemyGroupsBeforeOutage, false);
        SetGroupsActive(emergencyLightGroups, false);

        yield return new WaitForSeconds(Mathf.Max(0.1f, blackoutSeconds));

        SetGroupsActive(emergencyLightGroups, true);
        SetGroupsActive(enemyGroupsAfterOutage, true);

        if (generatorSwitch != null)
            generatorSwitch.SetInteractEnabled(true);

        if (generatorRoomHintMarker != null)
            generatorRoomHintMarker.SetActive(true);

        if (questUI != null && !string.IsNullOrWhiteSpace(outageStartHint))
            questUI.ShowHint(outageStartHint);

        transitionRunning = false;
    }

    public void RestorePower()
    {
        if (!HasOutageStarted) return;
        if (IsPowerRestored) return;

        IsPowerRestored = true;
        SetGroupsActive(normalLightGroups, true);
        SetGroupsActive(emergencyLightGroups, false);

        if (generatorRoomHintMarker != null)
            generatorRoomHintMarker.SetActive(false);

        if (generatorSwitch != null)
            generatorSwitch.SetInteractEnabled(false);

        if (completeCurrentQuestOnPowerRestore && questSystem != null)
            questSystem.CompleteCurrentQuest();

        if (questUI != null && !string.IsNullOrWhiteSpace(powerRestoredHint))
            questUI.ShowHint(powerRestoredHint);
    }

    private void ResolveUiRefs()
    {
        if (questUI == null)
            questUI = FindObjectOfType<QuestUI>(true);
        if (questSystem == null)
            questSystem = FindObjectOfType<QuestSystem>(true);
    }

    private static void SetGroupsActive(GameObject[] groups, bool active)
    {
        if (groups == null) return;
        for (int i = 0; i < groups.Length; i++)
        {
            if (groups[i] != null) groups[i].SetActive(active);
        }
    }
}
