using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class UiButtonSfx : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private AudioCue hoverCue;
    [SerializeField] private AudioCue clickCue;
    [SerializeField, Range(0f, 1f)] private float hoverVolume = 0.2f;
    [SerializeField, Range(0f, 1f)] private float clickVolume = 0.35f;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(PlayClick);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(PlayClick);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractable()) return;
        PlayHover();
    }

    public void SetCues(AudioCue hover, AudioCue click, float hoverVol, float clickVol)
    {
        hoverCue = hover;
        clickCue = click;
        hoverVolume = Mathf.Clamp01(hoverVol);
        clickVolume = Mathf.Clamp01(clickVol);
    }

    private bool IsInteractable()
    {
        return button == null || button.IsInteractable();
    }

    private void PlayHover()
    {
        if (hoverCue != null && hoverCue.IsValid)
            AudioService.Play2D(hoverCue, hoverVolume, ui: true);
    }

    private void PlayClick()
    {
        if (!IsInteractable()) return;
        if (clickCue != null && clickCue.IsValid)
            AudioService.Play2D(clickCue, clickVolume, ui: true);
    }
}
