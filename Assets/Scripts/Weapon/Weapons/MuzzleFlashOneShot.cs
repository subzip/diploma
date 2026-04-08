using UnityEngine;

public class MuzzleFlashOneShot : MonoBehaviour
{
    [SerializeField] private float fallbackLifetime = 0.08f;
    [SerializeField] private bool randomizeZRotation = true;
    [SerializeField] private float randomScaleMin = 0.9f;
    [SerializeField] private float randomScaleMax = 1.1f;

    private bool initialized;

    public void PlayAndAutoDestroy(float overrideLifetime = -1f)
    {
        if (initialized) return;
        initialized = true;

        if (randomizeZRotation)
        {
            Vector3 euler = transform.localEulerAngles;
            euler.z += Random.Range(0f, 360f);
            transform.localEulerAngles = euler;
        }

        float randomScale = Random.Range(randomScaleMin, randomScaleMax);
        transform.localScale *= randomScale;

        ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].Play(true);
        }

        Light[] lights = GetComponentsInChildren<Light>(true);
        for (int i = 0; i < lights.Length; i++)
        {
            lights[i].enabled = true;
        }

        float life = overrideLifetime > 0f ? overrideLifetime : fallbackLifetime;
        if (particles.Length > 0)
        {
            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem.MainModule main = particles[i].main;
                life = Mathf.Max(life, main.duration + main.startLifetime.constantMax);
            }
        }

        Destroy(gameObject, life);
    }

    private void OnEnable()
    {
        if (!initialized)
            PlayAndAutoDestroy();
    }
}
