using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AmbientAudioEmitter : MonoBehaviour
{
    [SerializeField] private AudioCue loopCue;
    [SerializeField] private bool autoPlayOnStart;
    [SerializeField] private bool triggerByPlayer;
    [SerializeField, Range(0f, 2f)] private float volumeScale = 1f;

    private AudioSource loopSource;

    private void Awake()
    {
        loopSource = GetComponent<AudioSource>();
        if (loopSource == null)
            loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.playOnAwake = false;
    }

    private void Start()
    {
        if (autoPlayOnStart && !triggerByPlayer)
            StartLoop();
    }

    private void OnDisable()
    {
        StopLoop();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!triggerByPlayer) return;
        if (!ComponentSearch.IsPlayer(other)) return;
        StartLoop();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!triggerByPlayer) return;
        if (!ComponentSearch.IsPlayer(other)) return;
        StopLoop();
    }

    public void StartLoop()
    {
        if (loopCue == null || !loopCue.IsValid) return;
        if (loopSource == null) return;
        if (loopSource.isPlaying) return;

        loopSource.clip = loopCue.GetRandomClip();
        loopSource.pitch = loopCue.GetRandomPitch();
        loopSource.loop = true;
        loopSource.spatialBlend = loopCue.SpatialBlend;
        loopSource.minDistance = loopCue.MinDistance;
        loopSource.maxDistance = loopCue.MaxDistance;
        loopSource.volume = loopCue.Volume * Mathf.Max(0f, volumeScale);
        loopSource.Play();
    }

    public void StopLoop()
    {
        if (loopSource == null) return;
        if (!loopSource.isPlaying) return;
        loopSource.Stop();
    }
}
