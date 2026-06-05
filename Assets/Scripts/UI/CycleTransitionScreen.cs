using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
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
    [SerializeField] private RectTransform skipHintRoot;
    [SerializeField] private Image skipSpaceIcon;
    [SerializeField] private TMP_Text skipPrefixText;
    [SerializeField] private TMP_Text skipSuffixText;
    [SerializeField] private Sprite skipSpaceSprite;

    [Header("Timing")]
    [SerializeField] private float fadeInSeconds = 0.2f;
    [SerializeField] private float fadeOutSeconds = 0.25f;
    [SerializeField] private float postLoadHoldSeconds = 0.15f;
    [SerializeField] private float skipHintDelaySeconds = 2f;

    [Header("Skip Hint Layout")]
    [SerializeField] private Vector2 skipHintAnchoredOffset = new Vector2(-120f, 42f);

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

        if (skipHintRoot == null)
        {
            GameObject hintGo = new GameObject("SkipHint", typeof(RectTransform), typeof(CanvasGroup), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            hintGo.transform.SetParent(canvas.transform, false);
            skipHintRoot = hintGo.GetComponent<RectTransform>();
            skipHintRoot.anchorMin = new Vector2(1f, 0f);
            skipHintRoot.anchorMax = new Vector2(1f, 0f);
            skipHintRoot.pivot = new Vector2(1f, 0f);
            skipHintRoot.anchoredPosition = skipHintAnchoredOffset;
            skipHintRoot.sizeDelta = new Vector2(520f, 52f);

            CanvasGroup hintGroup = hintGo.GetComponent<CanvasGroup>();
            hintGroup.alpha = 0f;
            hintGroup.blocksRaycasts = false;
            hintGroup.interactable = false;

            HorizontalLayoutGroup layout = hintGo.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = hintGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        if (skipPrefixText == null)
        {
            GameObject prefixGo = new GameObject("SkipPrefix", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            prefixGo.transform.SetParent(skipHintRoot, false);
            skipPrefixText = prefixGo.GetComponent<TextMeshProUGUI>();
            skipPrefixText.text = "Нажмите";
            skipPrefixText.fontSize = 30f;
            skipPrefixText.color = new Color(0.85f, 0.93f, 1f, 0.95f);
            skipPrefixText.alignment = TextAlignmentOptions.MidlineRight;
            skipPrefixText.enableWordWrapping = false;
            skipPrefixText.overflowMode = TextOverflowModes.Overflow;
            skipPrefixText.raycastTarget = false;
            LayoutElement prefixLayout = prefixGo.GetComponent<LayoutElement>();
            prefixLayout.preferredWidth = -1f;
            prefixLayout.minWidth = 0f;
            prefixLayout.flexibleWidth = 0f;
        }

        if (skipSpaceIcon == null)
        {
            GameObject iconGo = new GameObject("SkipSpaceIcon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            iconGo.transform.SetParent(skipHintRoot, false);
            skipSpaceIcon = iconGo.GetComponent<Image>();
            skipSpaceIcon.preserveAspect = true;
            skipSpaceIcon.raycastTarget = false;
            skipSpaceIcon.color = Color.white;
            iconGo.GetComponent<LayoutElement>().preferredWidth = 120f;
            iconGo.GetComponent<LayoutElement>().preferredHeight = 38f;
        }

        if (skipSuffixText == null)
        {
            GameObject suffixGo = new GameObject("SkipSuffix", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            suffixGo.transform.SetParent(skipHintRoot, false);
            skipSuffixText = suffixGo.GetComponent<TextMeshProUGUI>();
            skipSuffixText.text = "чтобы пропустить";
            skipSuffixText.fontSize = 30f;
            skipSuffixText.color = new Color(0.85f, 0.93f, 1f, 0.95f);
            skipSuffixText.alignment = TextAlignmentOptions.MidlineLeft;
            skipSuffixText.enableWordWrapping = false;
            skipSuffixText.overflowMode = TextOverflowModes.Overflow;
            skipSuffixText.raycastTarget = false;
            LayoutElement suffixLayout = suffixGo.GetComponent<LayoutElement>();
            suffixLayout.preferredWidth = -1f;
            suffixLayout.minWidth = 0f;
            suffixLayout.flexibleWidth = 0f;
        }

        if (skipSpaceIcon != null)
        {
            skipSpaceIcon.sprite = skipSpaceSprite;
            skipSpaceIcon.enabled = true;
            skipSpaceIcon.color = skipSpaceSprite != null
                ? Color.white
                : new Color(0.08f, 0.15f, 0.24f, 0.95f);

            TMP_Text fallbackLabel = skipSpaceIcon.GetComponentInChildren<TMP_Text>(true);
            if (skipSpaceSprite == null)
            {
                if (fallbackLabel == null)
                {
                    GameObject labelGo = new GameObject("SpaceLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
                    labelGo.transform.SetParent(skipSpaceIcon.transform, false);
                    RectTransform lrt = labelGo.GetComponent<RectTransform>();
                    lrt.anchorMin = Vector2.zero;
                    lrt.anchorMax = Vector2.one;
                    lrt.offsetMin = Vector2.zero;
                    lrt.offsetMax = Vector2.zero;
                    fallbackLabel = labelGo.GetComponent<TextMeshProUGUI>();
                    fallbackLabel.alignment = TextAlignmentOptions.Center;
                    fallbackLabel.raycastTarget = false;
                    fallbackLabel.fontSize = 24f;
                    fallbackLabel.text = "SPACE";
                    fallbackLabel.color = new Color(0.85f, 0.93f, 1f, 0.95f);
                }
                else
                {
                    fallbackLabel.gameObject.SetActive(true);
                }
            }
            else if (fallbackLabel != null)
            {
                fallbackLabel.gameObject.SetActive(false);
            }
        }

        if (skipHintRoot != null)
            skipHintRoot.anchoredPosition = skipHintAnchoredOffset;

        if (skipPrefixText != null)
        {
            skipPrefixText.enableWordWrapping = false;
            skipPrefixText.overflowMode = TextOverflowModes.Overflow;
        }

        if (skipSuffixText != null)
        {
            skipSuffixText.enableWordWrapping = false;
            skipSuffixText.overflowMode = TextOverflowModes.Overflow;
        }

        SetSkipHintVisible(false);
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
        SetSkipHintVisible(false);
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
            SetSkipHintVisible(false);
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
        SetSkipHintVisible(false);

        if (charsPerSecond <= 0f)
        {
            messageText.text = fullText;
            yield break;
        }

        messageText.text = string.Empty;
        float elapsed = 0f;
        float typedChars = 0f;
        int shownChars = 0;
        bool hintShown = false;

        while (shownChars < fullText.Length)
        {
            elapsed += Time.unscaledDeltaTime;
            if (!hintShown && elapsed >= Mathf.Max(0f, skipHintDelaySeconds))
            {
                hintShown = true;
                SetSkipHintVisible(true);
            }

            if (WasSkipPressed())
            {
                shownChars = fullText.Length;
                messageText.text = fullText;
                break;
            }

            typedChars += charsPerSecond * Time.unscaledDeltaTime;
            int newShown = Mathf.Clamp(Mathf.FloorToInt(typedChars), 0, fullText.Length);
            if (newShown != shownChars)
            {
                shownChars = newShown;
                messageText.text = fullText.Substring(0, shownChars);
            }

            yield return null;
        }

        if (messageText.text != fullText)
            messageText.text = fullText;
        SetSkipHintVisible(false);
    }

    private bool WasSkipPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Space);
#endif
        return false;
    }

    private void SetSkipHintVisible(bool visible)
    {
        if (skipHintRoot == null) return;
        CanvasGroup cg = skipHintRoot.GetComponent<CanvasGroup>();
        if (cg == null) return;
        cg.alpha = visible ? 1f : 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;
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
        SetSkipHintVisible(false);
        IsTransitionActive = visible || isTransitioning;
    }
}
