using UnityEngine;

public class FlickeringLight : MonoBehaviour
{
    private Light lightComp;
    [SerializeField] private float minIntensity = 0.5f;
    [SerializeField] private float maxIntensity = 2.5f;
    [SerializeField] private float flickerSpeed = 5f;

    void Start()
    {
        lightComp = GetComponent<Light>();
    }

    void Update()
    {
        float flicker = Mathf.PerlinNoise(Time.time * flickerSpeed, 0) * (maxIntensity - minIntensity) + minIntensity;
        lightComp.intensity = flicker;
    }
}
