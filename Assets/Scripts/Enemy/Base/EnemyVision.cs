// EnemyVision.cs
using UnityEngine;

public class EnemyVision : MonoBehaviour
{
    [SerializeField] private float sightRange = 20f;
    [SerializeField] private float fieldOfView = 90f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstacleMask = ~0; // можно настроить в инспекторе

    private Transform player;
    private float lastSeenTime = -999f;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    public bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 direction = player.position - transform.position;
        float distance = direction.magnitude;

        if (distance > sightRange) return false;

        float angle = Vector3.Angle(transform.forward, direction);
        if (angle > fieldOfView * 0.5f) return false;

        if (Physics.Raycast(transform.position + Vector3.up * 1.3f, direction.normalized, out RaycastHit hit, distance, obstacleMask))
        {
            bool seen = hit.collider.CompareTag("Player");
            if (seen) lastSeenTime = Time.time;
            return seen;
        }
        return false;
    }

    public bool SeenRecently(float graceSeconds) => Time.time - lastSeenTime <= graceSeconds;
}
