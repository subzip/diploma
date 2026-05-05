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
    [SerializeField, Range(0f, 1f)] private float masterSfxVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float masterUiVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float masterAmbienceVolume = 1f;

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
            AudioSource source = gameObject.AddComponent<AudioSource>();
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
            AudioSource newSource = gameObject.AddComponent<AudioSource>();
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
        return Instance.PlayInternal(cue, Vector3.zero, false, volumeScale, ui ? Instance.masterUiVolume : Instance.masterSfxVolume);
    }

    public static AudioSource PlayAt(AudioCue cue, Vector3 position, float volumeScale = 1f, bool ambience = false)
    {
        if (cue == null || !cue.IsValid) return null;
        float master = ambience ? Instance.masterAmbienceVolume : Instance.masterSfxVolume;
        return Instance.PlayInternal(cue, position, true, volumeScale, master);
    }

    private AudioSource PlayInternal(AudioCue cue, Vector3 position, bool useWorldPosition, float volumeScale, float master)
    {
        AudioSource source = GetNextSource();
        source.transform.position = useWorldPosition ? position : transform.position;
        source.clip = cue.GetRandomClip();
        source.pitch = cue.GetRandomPitch();
        source.loop = cue.Loop;
        source.spatialBlend = cue.SpatialBlend;
        source.minDistance = cue.MinDistance;
        source.maxDistance = cue.MaxDistance;
        source.volume = cue.Volume * Mathf.Max(0f, volumeScale) * Mathf.Clamp01(master);
        source.Play();
        return source;
    }
}
