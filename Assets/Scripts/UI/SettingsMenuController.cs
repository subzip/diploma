using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject returnPanel;

    [Header("Mouse")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private TMP_Text sensitivityValueText;
    [SerializeField] private float sensitivityTextMultiplier = 1f;

    [Header("Graphics")]
    [SerializeField] private TMP_Dropdown graphicsDropdown;

    [Header("Audio")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private TMP_Text musicValueText;
    [SerializeField] private Slider weaponsSlider;
    [SerializeField] private TMP_Text weaponsValueText;
    [SerializeField] private Slider otherSlider;
    [SerializeField] private TMP_Text otherValueText;

    private bool isRefreshing;

    private void Awake()
    {
        GameSettingsService.Instance.InitializeIfNeeded();
        BindListeners();
    }

    private void OnEnable()
    {
        RefreshUIFromSettings();
    }

    public void OpenSettings()
    {
        GameSettingsService.Instance.InitializeIfNeeded();
        if (returnPanel != null) returnPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        RefreshUIFromSettings();
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (returnPanel != null) returnPanel.SetActive(true);
    }

    public void ApplyAndClose()
    {
        GameSettingsService.Instance.Save();
        CloseSettings();
    }

    public void RestoreFromDisk()
    {
        GameSettingsService.Instance.ReloadFromDisk();
        RefreshUIFromSettings();
    }

    private void BindListeners()
    {
        if (sensitivitySlider != null)
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);

        if (graphicsDropdown != null)
            graphicsDropdown.onValueChanged.AddListener(OnGraphicsChanged);

        if (musicSlider != null)
            musicSlider.onValueChanged.AddListener(v => OnAudioChanged(AudioCue.AudioCategory.MusicAmbient, v));

        if (weaponsSlider != null)
            weaponsSlider.onValueChanged.AddListener(v => OnAudioChanged(AudioCue.AudioCategory.Weapons, v));

        if (otherSlider != null)
            otherSlider.onValueChanged.AddListener(v => OnAudioChanged(AudioCue.AudioCategory.Other, v));
    }

    private void RefreshUIFromSettings()
    {
        GameSettingsData data = GameSettingsService.Instance.Current;
        isRefreshing = true;

        if (sensitivitySlider != null)
            sensitivitySlider.value = data.mouseSensitivity;
        UpdateSensitivityText(data.mouseSensitivity);

        if (graphicsDropdown != null)
        {
            EnsureGraphicsOptions();
            graphicsDropdown.value = Mathf.Clamp(data.graphicsPreset, 0, 2);
            graphicsDropdown.RefreshShownValue();
        }

        if (musicSlider != null) musicSlider.value = data.musicVolume;
        if (weaponsSlider != null) weaponsSlider.value = data.weaponsVolume;
        if (otherSlider != null) otherSlider.value = data.otherVolume;

        UpdatePercentText(musicValueText, data.musicVolume);
        UpdatePercentText(weaponsValueText, data.weaponsVolume);
        UpdatePercentText(otherValueText, data.otherVolume);

        isRefreshing = false;
    }

    private void OnSensitivityChanged(float value)
    {
        if (!isRefreshing)
            GameSettingsService.Instance.SetMouseSensitivity(value);
        UpdateSensitivityText(value);
    }

    private void OnGraphicsChanged(int preset)
    {
        if (isRefreshing) return;
        GameSettingsService.Instance.SetGraphicsPreset(preset);
    }

    private void OnAudioChanged(AudioCue.AudioCategory category, float value)
    {
        if (!isRefreshing)
            GameSettingsService.Instance.SetAudioVolume(category, value);

        if (category == AudioCue.AudioCategory.MusicAmbient) UpdatePercentText(musicValueText, value);
        else if (category == AudioCue.AudioCategory.Weapons) UpdatePercentText(weaponsValueText, value);
        else UpdatePercentText(otherValueText, value);
    }

    private void UpdateSensitivityText(float value)
    {
        if (sensitivityValueText == null) return;
        float shown = value * sensitivityTextMultiplier;
        sensitivityValueText.text = shown.ToString("0.00");
    }

    private void UpdatePercentText(TMP_Text target, float value)
    {
        if (target == null) return;
        target.text = $"{Mathf.RoundToInt(Mathf.Clamp01(value) * 100f)}%";
    }

    private void EnsureGraphicsOptions()
    {
        if (graphicsDropdown == null) return;
        if (graphicsDropdown.options != null && graphicsDropdown.options.Count >= 3) return;

        graphicsDropdown.options.Clear();
        graphicsDropdown.options.Add(new TMP_Dropdown.OptionData("Low"));
        graphicsDropdown.options.Add(new TMP_Dropdown.OptionData("Medium"));
        graphicsDropdown.options.Add(new TMP_Dropdown.OptionData("High"));
    }
}
