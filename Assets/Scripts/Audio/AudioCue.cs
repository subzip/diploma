using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Audio Cue", fileName = "AudioCue")]
public class AudioCue : ScriptableObject
{
    public enum AudioCategory
    {
        Other = 0,
        Weapons = 1,
        MusicAmbient = 2
    }

    [SerializeField] private AudioClip[] clips;
    [SerializeField] private AudioCategory category = AudioCategory.Other;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [SerializeField, Range(0.5f, 1.5f)] private float pitchMin = 1f;
    [SerializeField, Range(0.5f, 1.5f)] private float pitchMax = 1f;
    [SerializeField, Range(0f, 1f)] private float spatialBlend = 1f;
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 20f;
    [SerializeField] private bool loop;

    public bool IsValid => clips != null && clips.Length > 0;
    public AudioCategory Category => category;
    public float Volume => volume;
    public float SpatialBlend => spatialBlend;
    public float MinDistance => minDistance;
    public float MaxDistance => maxDistance;
    public bool Loop => loop;

    public AudioClip GetRandomClip()
    {
        if (!IsValid) return null;
        int index = clips.Length == 1 ? 0 : Random.Range(0, clips.Length);
        return clips[index];
    }

    public float GetRandomPitch()
    {
        if (pitchMin > pitchMax) return pitchMin;
        return Random.Range(pitchMin, pitchMax);
    }
}
