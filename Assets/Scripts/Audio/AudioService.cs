using System.Collections.Generic;
using UnityEngine;

public class AudioService : MonoBehaviour
{
    private static AudioService instance;
    public static AudioService Instance
    {
        get
        {
            if (instance != null) return instance;
            instance = FindObjectOfType<AudioService>();
            if (instance != null) return instance;

            GameObject go = new GameObject("AudioService");
            instance = go.AddComponent<AudioService>();
            return instance;
        }
    }

    [SerializeField] private int poolSize = 24;
    [SerializeField] private int maxPoolSize = 64;
    [Header("Master Volumes")]
    [SerializeField, Range(0f, 1f)] private float masterMusicAmbientVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float masterWeaponsVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float masterOtherVolume = 1f;

    private readonly List<AudioSource> sourcePool = new();
    private readonly Dictionary<AudioSource, SourceState> sourceStates = new();
    private int poolIndex;

    private struct SourceState
    {
        public AudioCue.AudioCategory Category;
        public float CueVolume;
        public float VolumeScale;
        public bool Initialized;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsurePool();
    }

    private void EnsurePool()
    {
        int target = Mathf.Max(8, poolSize);
        while (sourcePool.Count < target)
        {
            GameObject child = new GameObject($"AudioSource_{sourcePool.Count}");
            child.transform.SetParent(transform, false);
            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            sourcePool.Add(source);
            sourceStates[source] = new SourceState { Initialized = false };
        }
    }

    private AudioSource GetNextSource()
    {
        EnsurePool();
        for (int i = 0; i < sourcePool.Count; i++)
        {
            poolIndex = (poolIndex + 1) % sourcePool.Count;
            AudioSource candidate = sourcePool[poolIndex];
            if (!candidate.isPlaying) return candidate;
        }

        int safeMaxPool = Mathf.Max(poolSize, maxPoolSize);
        if (sourcePool.Count < safeMaxPool)
        {
            GameObject child = new GameObject($"AudioSource_{sourcePool.Count}");
            child.transform.SetParent(transform, false);
            AudioSource newSource = child.AddComponent<AudioSource>();
            newSource.playOnAwake = false;
            newSource.loop = false;
            sourcePool.Add(newSource);
            sourceStates[newSource] = new SourceState { Initialized = false };
            poolIndex = sourcePool.Count - 1;
            return newSource;
        }

        poolIndex = (poolIndex + 1) % sourcePool.Count;
        AudioSource fallback = sourcePool[poolIndex];
        fallback.Stop();
        return fallback;
    }

    public static AudioSource Play2D(AudioCue cue, float volumeScale = 1f, bool ui = false)
    {
        if (cue == null || !cue.IsValid) return null;
        AudioCue.AudioCategory category = ui ? AudioCue.AudioCategory.Other : cue.Category;
        return Instance.PlayInternal(cue, Vector3.zero, false, volumeScale, category);
    }

    public static AudioSource PlayAt(AudioCue cue, Vector3 position, float volumeScale = 1f, bool ambience = false)
    {
        if (cue == null || !cue.IsValid) return null;
        AudioCue.AudioCategory category = ambience ? AudioCue.AudioCategory.MusicAmbient : cue.Category;
        return Instance.PlayInternal(cue, position, true, volumeScale, category);
    }

    private AudioSource PlayInternal(AudioCue cue, Vector3 position, bool useWorldPosition, float volumeScale, AudioCue.AudioCategory category)
    {
        AudioSource source = GetNextSource();
        source.transform.position = useWorldPosition ? position : transform.position;
        source.clip = cue.GetRandomClip();
        source.pitch = cue.GetRandomPitch();
        source.loop = cue.Loop;
        source.spatialBlend = cue.SpatialBlend;
        source.minDistance = cue.MinDistance;
        source.maxDistance = cue.MaxDistance;
        source.volume = cue.Volume * Mathf.Max(0f, volumeScale) * Mathf.Clamp01(GetCategoryMasterVolume(category));
        sourceStates[source] = new SourceState
        {
            Category = category,
            CueVolume = cue.Volume,
            VolumeScale = Mathf.Max(0f, volumeScale),
            Initialized = true
        };
        source.Play();
        return source;
    }

    private float GetCategoryMasterVolume(AudioCue.AudioCategory category)
    {
        switch (category)
        {
            case AudioCue.AudioCategory.MusicAmbient:
                return masterMusicAmbientVolume;
            case AudioCue.AudioCategory.Weapons:
                return masterWeaponsVolume;
            default:
                return masterOtherVolume;
        }
    }

    public static float GetMasterVolume(AudioCue.AudioCategory category)
    {
        return Mathf.Clamp01(Instance.GetCategoryMasterVolume(category));
    }

    public static void SetMasterVolume(AudioCue.AudioCategory category, float value)
    {
        Instance.SetMasterVolumeInternal(category, value);
    }

    public static void StopCategory(AudioCue.AudioCategory category)
    {
        Instance.StopCategoryInternal(category);
    }

    private void SetMasterVolumeInternal(AudioCue.AudioCategory category, float value)
    {
        float clamped = Mathf.Clamp01(value);
        switch (category)
        {
            case AudioCue.AudioCategory.MusicAmbient:
                masterMusicAmbientVolume = clamped;
                break;
            case AudioCue.AudioCategory.Weapons:
                masterWeaponsVolume = clamped;
                break;
            default:
                masterOtherVolume = clamped;
                break;
        }

        RefreshActiveSourceVolumes();
    }

    private void RefreshActiveSourceVolumes()
    {
        for (int i = 0; i < sourcePool.Count; i++)
        {
            AudioSource source = sourcePool[i];
            if (source == null || !source.isPlaying) continue;
            if (!sourceStates.TryGetValue(source, out SourceState state) || !state.Initialized) continue;
            float master = Mathf.Clamp01(GetCategoryMasterVolume(state.Category));
            source.volume = state.CueVolume * state.VolumeScale * master;
        }
    }

    private void StopCategoryInternal(AudioCue.AudioCategory category)
    {
        for (int i = 0; i < sourcePool.Count; i++)
        {
            AudioSource source = sourcePool[i];
            if (source == null || !source.isPlaying) continue;
            if (!sourceStates.TryGetValue(source, out SourceState state) || !state.Initialized) continue;
            if (state.Category != category) continue;
            source.Stop();
        }
    }
}
