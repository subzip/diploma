using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class MenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    [Header("Targets")]
    [SerializeField] private RectTransform scaleTarget;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Graphic strokeGraphic;
    [SerializeField] private Image glowImage;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Graphic arrowGraphic;

    [Header("Timing")]
    [SerializeField] private float hoverLerpSpeed = 14f;
    [SerializeField] private float pressLerpSpeed = 22f;

    [Header("Scale")]
    [SerializeField] private float idleScale = 1f;
    [SerializeField] private float hoverScale = 1.03f;
    [SerializeField] private float pressedScale = 0.985f;

    [Header("Background Alpha")]
    [SerializeField, Range(0f, 1f)] private float idleBgAlpha = 0.67f;
    [SerializeField, Range(0f, 1f)] private float hoverBgAlpha = 0.82f;
    [SerializeField, Range(0f, 1f)] private float disabledBgAlpha = 0.35f;

    [Header("Stroke Alpha")]
    [SerializeField, Range(0f, 1f)] private float idleStrokeAlpha = 0.35f;
    [SerializeField, Range(0f, 1f)] private float hoverStrokeAlpha = 0.72f;
    [SerializeField, Range(0f, 1f)] private float disabledStrokeAlpha = 0.18f;

    [Header("Glow Alpha")]
    [SerializeField, Range(0f, 1f)] private float idleGlowAlpha = 0f;
    [SerializeField, Range(0f, 1f)] private float hoverGlowAlpha = 0.42f;
    [SerializeField, Range(0f, 1f)] private float pressGlowAlpha = 0.6f;
    [SerializeField, Range(0f, 1f)] private float disabledGlowAlpha = 0f;

    [Header("Label")]
    [SerializeField] private Color idleLabelColor = new Color(0.90f, 0.98f, 1f, 1f);
    [SerializeField] private Color hoverLabelColor = Color.white;
    [SerializeField] private Color disabledLabelColor = new Color(0.62f, 0.72f, 0.78f, 0.8f);
    [SerializeField] private float idleLabelX = 30f;
    [SerializeField] private float hoverLabelX = 38f;

    [Header("Arrow")]
    [SerializeField, Range(0f, 1f)] private float idleArrowAlpha = 0f;
    [SerializeField, Range(0f, 1f)] private float hoverArrowAlpha = 1f;
    [SerializeField, Range(0f, 1f)] private float disabledArrowAlpha = 0f;

    [Header("SFX (Optional)")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioCue hoverCue;
    [SerializeField] private AudioCue clickCue;
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip clickClip;
    [SerializeField, Range(0f, 1f)] private float hoverVolume = 0.2f;
    [SerializeField, Range(0f, 1f)] private float clickVolume = 0.35f;

    private Button button;
    private bool isHovered;
    private bool isPressed;
    private float currentScale = 1f;
    private float currentBgAlpha;
    private float currentStrokeAlpha;
    private float currentGlowAlpha;
    private float currentArrowAlpha;
    private Color currentLabelColor;
    private float currentLabelX;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (scaleTarget == null) scaleTarget = transform as RectTransform;
        if (backgroundImage == null) backgroundImage = GetComponent<Image>();
        if (uiAudioSource == null) uiAudioSource = GetComponentInParent<AudioSource>();

        if (button != null)
            button.onClick.AddListener(OnClicked);

        
        currentScale = idleScale;
        currentBgAlpha = idleBgAlpha;
        currentStrokeAlpha = idleStrokeAlpha;
        currentGlowAlpha = idleGlowAlpha;
        currentArrowAlpha = idleArrowAlpha;
        currentLabelColor = idleLabelColor;
        currentLabelX = idleLabelX;

        ApplyVisualsImmediate();
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClicked);
    }

    private void OnEnable()
    {
        isPressed = false;
        isHovered = false;
        ApplyVisualsImmediate();
    }

    private void Update()
    {
        bool interactable = button == null || button.IsInteractable();
        float speed = isPressed ? pressLerpSpeed : hoverLerpSpeed;
        float t = Mathf.Clamp01(speed * Time.unscaledDeltaTime);

        float targetScale;
        float targetBgAlpha;
        float targetStrokeAlpha;
        float targetGlowAlpha;
        float targetArrowAlpha;
        Color targetLabelColor;
        float targetLabelX;

        if (!interactable)
        {
            targetScale = idleScale;
            targetBgAlpha = disabledBgAlpha;
            targetStrokeAlpha = disabledStrokeAlpha;
            targetGlowAlpha = disabledGlowAlpha;
            targetArrowAlpha = disabledArrowAlpha;
            targetLabelColor = disabledLabelColor;
            targetLabelX = idleLabelX;
        }
        else if (isPressed)
        {
            targetScale = pressedScale;
            targetBgAlpha = hoverBgAlpha;
            targetStrokeAlpha = hoverStrokeAlpha;
            targetGlowAlpha = pressGlowAlpha;
            targetArrowAlpha = hoverArrowAlpha;
            targetLabelColor = hoverLabelColor;
            targetLabelX = hoverLabelX;
        }
        else if (isHovered)
        {
            targetScale = hoverScale;
            targetBgAlpha = hoverBgAlpha;
            targetStrokeAlpha = hoverStrokeAlpha;
            targetGlowAlpha = hoverGlowAlpha;
            targetArrowAlpha = hoverArrowAlpha;
            targetLabelColor = hoverLabelColor;
            targetLabelX = hoverLabelX;
        }
        else
        {
            targetScale = idleScale;
            targetBgAlpha = idleBgAlpha;
            targetStrokeAlpha = idleStrokeAlpha;
            targetGlowAlpha = idleGlowAlpha;
            targetArrowAlpha = idleArrowAlpha;
            targetLabelColor = idleLabelColor;
            targetLabelX = idleLabelX;
        }

        currentScale = Mathf.Lerp(currentScale, targetScale, t);
        currentBgAlpha = Mathf.Lerp(currentBgAlpha, targetBgAlpha, t);
        currentStrokeAlpha = Mathf.Lerp(currentStrokeAlpha, targetStrokeAlpha, t);
        currentGlowAlpha = Mathf.Lerp(currentGlowAlpha, targetGlowAlpha, t);
        currentArrowAlpha = Mathf.Lerp(currentArrowAlpha, targetArrowAlpha, t);
        currentLabelColor = Color.Lerp(currentLabelColor, targetLabelColor, t);
        currentLabelX = Mathf.Lerp(currentLabelX, targetLabelX, t);

        ApplyVisuals();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractable()) return;
        if (!isHovered)
            PlayHoverSound();
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsInteractable()) return;
        isPressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!IsInteractable()) return;
        if (!isHovered)
            PlayHoverSound();
        isHovered = true;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isHovered = false;
        isPressed = false;
    }

    private void OnClicked()
    {
        if (!IsInteractable()) return;
        if (clickCue != null && clickCue.IsValid)
        {
            AudioService.Play2D(clickCue, clickVolume, ui: true);
            return;
        }
        if (uiAudioSource != null && clickClip != null)
            uiAudioSource.PlayOneShot(clickClip, clickVolume);
    }

    private bool IsInteractable()
    {
        return button == null || button.IsInteractable();
    }

    private void PlayHoverSound()
    {
        if (hoverCue != null && hoverCue.IsValid)
        {
            AudioService.Play2D(hoverCue, hoverVolume, ui: true);
            return;
        }
        if (uiAudioSource != null && hoverClip != null)
            uiAudioSource.PlayOneShot(hoverClip, hoverVolume);
    }

    private void ApplyVisualsImmediate()
    {
        ApplyVisuals();
    }

    private void ApplyVisuals()
    {
        if (scaleTarget != null)
            scaleTarget.localScale = Vector3.one * currentScale;

        if (backgroundImage != null)
            SetGraphicAlpha(backgroundImage, currentBgAlpha);

        if (strokeGraphic != null)
            SetGraphicAlpha(strokeGraphic, currentStrokeAlpha);

        if (glowImage != null)
            SetGraphicAlpha(glowImage, currentGlowAlpha);

        if (arrowGraphic != null)
            SetGraphicAlpha(arrowGraphic, currentArrowAlpha);

        if (label != null)
        {
            label.color = currentLabelColor;
            RectTransform rt = label.rectTransform;
            Vector2 p = rt.anchoredPosition;
            p.x = currentLabelX;
            rt.anchoredPosition = p;
        }
    }

    private static void SetGraphicAlpha(Graphic g, float alpha)
    {
        if (g == null) return;
        Color c = g.color;
        c.a = Mathf.Clamp01(alpha);
        g.color = c;
    }
}
