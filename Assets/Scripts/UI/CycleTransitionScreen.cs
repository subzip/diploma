using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CycleTransitionScreen : MonoBehaviour
{
    private static CycleTransitionScreen instance;
    public static bool IsTransitionActive { get; private set; }
    public static CycleTransitionScreen Instance
    {
        get
        {
            if (instance != null) return instance;

            GameObject go = new GameObject("CycleTransitionScreen");
            instance = go.AddComponent<CycleTransitionScreen>();
            return instance;
        }
    }

    [Header("UI")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup group;
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text messageText;

    [Header("Timing")]
    [SerializeField] private float fadeInSeconds = 0.2f;
    [SerializeField] private float fadeOutSeconds = 0.25f;
    [SerializeField] private float postLoadHoldSeconds = 0.15f;

    private bool isTransitioning;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureUi();
        SetVisibleImmediate(false);
    }

    private void EnsureUi()
    {
        if (canvas == null)
        {
            canvas = GetComponentInChildren<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasGo.transform.SetParent(transform, false);
                canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 32760;

                CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        if (group == null)
        {
            group = canvas.GetComponent<CanvasGroup>();
            if (group == null) group = canvas.gameObject.AddComponent<CanvasGroup>();
        }

        if (background == null)
        {
            GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(canvas.transform, false);
            RectTransform rt = bg.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            background = bg.GetComponent<Image>();
            background.color = Color.black;
            background.raycastTarget = true;
        }

        if (messageText == null)
        {
            GameObject textGo = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(canvas.transform, false);
            RectTransform rt = textGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(1100f, 280f);
            rt.anchoredPosition = new Vector2(0f, -40f);

            messageText = textGo.GetComponent<TextMeshProUGUI>();
            messageText.fontSize = 36f;
            messageText.alignment = TextAlignmentOptions.Center;
            messageText.color = new Color(0.88f, 0.95f, 1f, 1f);
            messageText.enableWordWrapping = true;
            messageText.text = string.Empty;
            messageText.raycastTarget = false;
        }
    }

    public IEnumerator PlayTransitionAndLoad(
        string targetSceneName,
        string message,
        float typewriterCharsPerSecond = 45f,
        float minBlackScreenSeconds = 1.6f)
    {
        EnsureUi();
        while (isTransitioning) yield return null;
        isTransitioning = true;
        IsTransitionActive = true;

        Time.timeScale = 1f;
        group.blocksRaycasts = true;
        yield return FadeTo(1f, fadeInSeconds);

        yield return TypeMessage(message, typewriterCharsPerSecond);

        float minHold = Mathf.Max(0f, minBlackScreenSeconds);
        if (minHold > 0f) yield return WaitRealtime(minHold);

        AsyncOperation load = SceneManager.LoadSceneAsync(targetSceneName);
        while (!load.isDone) yield return null;

        if (postLoadHoldSeconds > 0f) yield return WaitRealtime(postLoadHoldSeconds);
        yield return FadeTo(0f, fadeOutSeconds);
        group.blocksRaycasts = false;
        messageText.text = string.Empty;
        isTransitioning = false;
        IsTransitionActive = false;
    }

    public void BeginTransitionAndLoad(
        string targetSceneName,
        string message,
        float typewriterCharsPerSecond = 45f,
        float minBlackScreenSeconds = 1.6f)
    {
        StartCoroutine(PlayTransitionAndLoad(
            targetSceneName,
            message,
            typewriterCharsPerSecond,
            minBlackScreenSeconds
        ));
    }

    public IEnumerator PlayNarrativeOnly(
        string message,
        float typewriterCharsPerSecond = 45f,
        float minBlackScreenSeconds = 2.5f,
        bool keepBlackScreen = true)
    {
        EnsureUi();
        while (isTransitioning) yield return null;
        isTransitioning = true;
        IsTransitionActive = true;

        group.blocksRaycasts = true;
        yield return FadeTo(1f, fadeInSeconds);

        yield return TypeMessage(message, typewriterCharsPerSecond);

        float minHold = Mathf.Max(0f, minBlackScreenSeconds);
        if (minHold > 0f) yield return WaitRealtime(minHold);

        if (keepBlackScreen)
        {
            group.alpha = 1f;
            group.blocksRaycasts = true;
        }
        else
        {
            if (postLoadHoldSeconds > 0f) yield return WaitRealtime(postLoadHoldSeconds);
            yield return FadeTo(0f, fadeOutSeconds);
            group.blocksRaycasts = false;
            messageText.text = string.Empty;
        }

        isTransitioning = false;
        IsTransitionActive = keepBlackScreen;
    }

    public void BeginNarrativeOnly(
        string message,
        float typewriterCharsPerSecond = 45f,
        float minBlackScreenSeconds = 2.5f,
        bool keepBlackScreen = true)
    {
        StartCoroutine(PlayNarrativeOnly(
            message,
            typewriterCharsPerSecond,
            minBlackScreenSeconds,
            keepBlackScreen
        ));
    }

    private IEnumerator TypeMessage(string fullText, float charsPerSecond)
    {
        fullText ??= string.Empty;
        if (charsPerSecond <= 0f)
        {
            messageText.text = fullText;
            yield break;
        }

        messageText.text = string.Empty;
        float interval = 1f / charsPerSecond;
        for (int i = 0; i < fullText.Length; i++)
        {
            messageText.text += fullText[i];
            yield return WaitRealtime(interval);
        }
    }

    private IEnumerator FadeTo(float target, float duration)
    {
        float start = group.alpha;
        if (duration <= 0f)
        {
            group.alpha = target;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
        group.alpha = target;
    }

    private static IEnumerator WaitRealtime(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void SetVisibleImmediate(bool visible)
    {
        EnsureUi();
        group.alpha = visible ? 1f : 0f;
        group.blocksRaycasts = visible;
        if (!visible) messageText.text = string.Empty;
        IsTransitionActive = visible || isTransitioning;
    }
}
