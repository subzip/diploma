using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-300)]
public class GameSettingsService : MonoBehaviour
{
    private static GameSettingsService instance;
    public static GameSettingsService Instance
    {
        get
        {
            if (instance != null) return instance;
            instance = FindObjectOfType<GameSettingsService>();
            if (instance != null) return instance;

            GameObject go = new GameObject("GameSettingsService");
            instance = go.AddComponent<GameSettingsService>();
            return instance;
        }
    }

    private const string FileName = "game_settings.json";
    private GameSettingsData current = new GameSettingsData();

    public static bool IsReady => instance != null && instance.isInitialized;
    public GameSettingsData Current => current;

    private bool isInitialized;

    private string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeIfNeeded();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene _, LoadSceneMode __)
    {
        ApplyAll();
    }

    public void InitializeIfNeeded()
    {
        if (isInitialized) return;
        Load();
        ApplyAll();
        isInitialized = true;
    }

    public void SetMouseSensitivity(float value, bool save = true)
    {
        current.mouseSensitivity = Mathf.Clamp(value, 0.03f, 10f);
        ApplyMouseSensitivity();
        if (save) Save();
    }

    public void SetGraphicsPreset(int preset, bool save = true)
    {
        current.graphicsPreset = Mathf.Clamp(preset, 0, 2);
        ApplyGraphics();
        if (save) Save();
    }

    public void SetAudioVolume(AudioCue.AudioCategory category, float value, bool save = true)
    {
        float clamped = Mathf.Clamp01(value);
        switch (category)
        {
            case AudioCue.AudioCategory.MusicAmbient:
                current.musicVolume = clamped;
                break;
            case AudioCue.AudioCategory.Weapons:
                current.weaponsVolume = clamped;
                break;
            default:
                current.otherVolume = clamped;
                break;
        }

        ApplyAudio();
        if (save) Save();
    }

    public void ApplyAll()
    {
        ApplyMouseSensitivity();
        ApplyGraphics();
        ApplyAudio();
    }

    public void Save()
    {
        try
        {
            string json = JsonUtility.ToJson(current, true);
            File.WriteAllText(FilePath, json);
        }
        catch (System.Exception)
        {
        }
    }

    public void ReloadFromDisk()
    {
        Load();
        ApplyAll();
    }

    private void Load()
    {
        if (!File.Exists(FilePath))
        {
            current = CreateDefault();
            Save();
            return;
        }

        try
        {
            string json = File.ReadAllText(FilePath);
            GameSettingsData loaded = JsonUtility.FromJson<GameSettingsData>(json);
            current = loaded ?? CreateDefault();
        }
        catch (System.Exception)
        {
            current = CreateDefault();
        }

        SanitizeCurrent();
    }

    private void SanitizeCurrent()
    {
        current.mouseSensitivity = Mathf.Clamp(current.mouseSensitivity, 0.03f, 10f);
        current.graphicsPreset = Mathf.Clamp(current.graphicsPreset, 0, 2);
        current.musicVolume = Mathf.Clamp01(current.musicVolume);
        current.weaponsVolume = Mathf.Clamp01(current.weaponsVolume);
        current.otherVolume = Mathf.Clamp01(current.otherVolume);
    }

    private GameSettingsData CreateDefault()
    {
        return new GameSettingsData
        {
            mouseSensitivity = 2f,
            graphicsPreset = 1,
            musicVolume = 1f,
            weaponsVolume = 1f,
            otherVolume = 1f
        };
    }

    private void ApplyMouseSensitivity()
    {
        PlayerLook[] looks = FindObjectsOfType<PlayerLook>(true);
        for (int i = 0; i < looks.Length; i++)
            looks[i].SetLookSensitivity(current.mouseSensitivity);
    }

    private void ApplyGraphics()
    {
        int qualityCount = QualitySettings.names != null ? QualitySettings.names.Length : 0;
        if (qualityCount <= 0) return;

        int low = 0;
        int medium = Mathf.Clamp((qualityCount - 1) / 2, 0, qualityCount - 1);
        int high = qualityCount - 1;

        int target = current.graphicsPreset switch
        {
            0 => low,
            2 => high,
            _ => medium
        };

        if (QualitySettings.GetQualityLevel() != target)
            QualitySettings.SetQualityLevel(target, true);
    }

    private void ApplyAudio()
    {
        AudioService.SetMasterVolume(AudioCue.AudioCategory.MusicAmbient, current.musicVolume);
        AudioService.SetMasterVolume(AudioCue.AudioCategory.Weapons, current.weaponsVolume);
        AudioService.SetMasterVolume(AudioCue.AudioCategory.Other, current.otherVolume);
    }
}
