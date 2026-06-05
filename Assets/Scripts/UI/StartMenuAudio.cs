using UnityEngine;

[DisallowMultipleComponent]
public class StartMenuAudio : MonoBehaviour
{
    [Header("Background Music")]
    [SerializeField] private AudioCue musicCue;
    [SerializeField, Range(0f, 2f)] private float musicVolumeScale = 1f;
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool stopOnDisable = true;
    [SerializeField] private bool use2D = true;

    private AudioSource musicSource;
    private float lastAppliedVolume = -1f;

    private void Awake()
    {
        musicSource = GetComponent<AudioSource>();
        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();

        musicSource.playOnAwake = false;
        musicSource.loop = true;
    }

    private void OnEnable()
    {
        if (playOnEnable)
            PlayMusic();
    }

    private void OnDisable()
    {
        if (stopOnDisable)
            StopMusic();
    }

    public void PlayMusic()
    {
        if (musicCue == null || !musicCue.IsValid || musicSource == null) return;

        if (musicSource.isPlaying) return;

        musicSource.clip = musicCue.GetRandomClip();
        musicSource.pitch = musicCue.GetRandomPitch();
        musicSource.loop = true;
        musicSource.spatialBlend = use2D ? 0f : musicCue.SpatialBlend;
        musicSource.minDistance = musicCue.MinDistance;
        musicSource.maxDistance = musicCue.MaxDistance;
        ApplyVolume();
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource == null) return;
        if (!musicSource.isPlaying) return;
        musicSource.Stop();
    }

    private void Update()
    {
        if (musicSource == null || !musicSource.isPlaying || musicCue == null) return;
        ApplyVolume();
    }

    private void ApplyVolume()
    {
        float master = AudioService.GetMasterVolume(AudioCue.AudioCategory.MusicAmbient);
        float value = musicCue.Volume * Mathf.Max(0f, musicVolumeScale) * master;
        if (Mathf.Abs(value - lastAppliedVolume) < 0.0001f) return;
        musicSource.volume = value;
        lastAppliedVolume = value;
    }
}
