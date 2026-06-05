using UnityEngine;

public static class CombatStimulusHub
{
    private struct Stimulus
    {
        public Vector3 position;
        public float radius;
        public float time;
    }

    private static Stimulus lastGunshot;

    public static void RegisterGunshot(Vector3 position, float radius)
    {
        lastGunshot.position = position;
        lastGunshot.radius = Mathf.Max(0f, radius);
        lastGunshot.time = Time.time;
    }

    public static bool TryGetRecentGunshot(float maxAgeSeconds, out Vector3 position, out float radius)
    {
        if (Time.time - lastGunshot.time <= Mathf.Max(0f, maxAgeSeconds))
        {
            position = lastGunshot.position;
            radius = lastGunshot.radius;
            return true;
        }

        position = Vector3.zero;
        radius = 0f;
        return false;
    }
}
