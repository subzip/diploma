using UnityEngine;
using UnityEngine.SceneManagement;

public class StartManager : MonoBehaviour
{
    [SerializeField] private string firstGameplayScene = "level0_var0";
    [Header("Menu Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Intro Transition")]
    [SerializeField] private bool useIntroBlackScreen = true;
    [SerializeField, TextArea(3, 12)] private string introNarrativeText =
        "АРХИВ АВАРИЙНОГО ПРОТОКОЛА // КОМПЛЕКС ARK // ФРАГМЕНТ 01\n\n" +
        "Время инцидента: 04:17. Внутренний контур исследовательского комплекса ARK перешел в режим каскадного отказа. " +
        "Нейроинтерфейс, связывающий операторов с системами безопасности, вышел из-под контроля. Автоматические охранные модули " +
        "получили приоритет «боевой изоляции», а архитектура сектора начала меняться после каждой критической ошибки.\n\n" +
        "После смерти оператора пространство перестраивается: коридоры смещаются, укрытия исчезают, маршруты противника меняются. " +
        "Это не визуальный сбой HUD, а реакция комплекса на рост энтропии.\n\n" +
        "Ты — последний оперативник, у которого сохранился доступ к модулю нейрорезиста. Связь с командованием потеряна, " +
        "эвакуационный протокол поврежден, ключевые узлы питания работают нестабильно.\n\n" +
        "ЗАДАЧА №1: найти ключ-карту и открыть переход в следующий сектор.\n" +
        "ЗАДАЧА №2: удержаться в бою при растущей энтропии цикла.\n" +
        "ЗАДАЧА №3: восстановить питание критических систем, добраться до лифта и спуститься ниже.\n\n" +
        "Помни: каждая смерть меняет правила. Каждое возрождение — новый вариант той же ловушки.";

    [SerializeField] private float introTypewriterCharsPerSecond = 45f;
    [SerializeField] private float introMinBlackSeconds = 2.2f;

    private bool isStarting;
    private StartMenuAudio startMenuAudio;

    private void Awake()
    {
        GameSettingsService.Instance.InitializeIfNeeded();
        startMenuAudio = FindObjectOfType<StartMenuAudio>(true);
    }

    public void OpenSettings()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    public void Continue()
    {
        if (isStarting)
        {
            return;
        }

        isStarting = true;
        if (startMenuAudio != null)
            startMenuAudio.StopMusic();

        if (useIntroBlackScreen)
        {
            CycleTransitionScreen transition = CycleTransitionScreen.Instance;
            if (transition != null)
            {
                transition.BeginTransitionAndLoad(
                    firstGameplayScene,
                    introNarrativeText,
                    introTypewriterCharsPerSecond,
                    introMinBlackSeconds
                );
            }
            else
            {
                SceneManager.LoadScene(firstGameplayScene);
            }
            return;
        }

        SceneManager.LoadScene(firstGameplayScene);
    }

    public void Exit()
    {
        Application.Quit();
    }

}
