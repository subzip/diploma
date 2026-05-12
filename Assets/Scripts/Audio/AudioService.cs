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
    private int poolIndex;

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
}
