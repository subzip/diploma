
using UnityEngine;
using UnityEngine.Serialization;

public class EnemyVision : MonoBehaviour
{
    [SerializeField] private float sightRange = 20f;
    [SerializeField] private float fieldOfView = 90f;
    [SerializeField] private float hearingRange = 7f;
    [SerializeField] private float gunshotMemorySeconds = 1.25f;
    [SerializeField] private float gunshotHearingMultiplier = 1.5f;
    [FormerlySerializedAs("playerLayer")]
    [SerializeField] private LayerMask playerLayer = ~0; 
    [SerializeField] private LayerMask obstacleMask = ~0;

    private Transform player;
    private float lastSeenTime = -999f;
    private Vector3 lastKnownPlayerPosition;

    private void Start()
    {
        ResolvePlayerRef(force: true);
        if (player != null) lastKnownPlayerPosition = player.position;
    }

    public bool CanSeePlayer()
    {
        ResolvePlayerRef();
        if (player == null) return false;

        Vector3 eyePos = transform.position + Vector3.up * 1.3f;
        Vector3 direction = player.position - eyePos;
        float distance = direction.magnitude;

        if (distance > sightRange) return false;

        float angle = Vector3.Angle(transform.forward, direction);
        if (angle > fieldOfView * 0.5f) return false;

        if (Physics.Raycast(eyePos, direction.normalized, out RaycastHit hit, distance, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            bool seen = hit.collider.CompareTag("Player") || hit.collider.transform.root.CompareTag("Player");
            if (seen)
            {
                lastSeenTime = Time.time;
                lastKnownPlayerPosition = player.position;
            }
            return seen;
        }

        return false;
    }

    
    public bool CanSensePlayer(bool canSeePlayerAlready = false)
    {
        if (canSeePlayerAlready) return true;

        ResolvePlayerRef();
        if (player == null) return false;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > hearingRange) return false;

        if (!player.TryGetComponent<PlayerMovement>(out var movement)) return false;
        if (movement.IsMoving)
        {
            lastSeenTime = Time.time;
            lastKnownPlayerPosition = player.position;
            return true;
        }

        if (CombatStimulusHub.TryGetRecentGunshot(gunshotMemorySeconds, out Vector3 gunshotPos, out float gunshotRadius))
        {
            float effectiveRange = Mathf.Max(hearingRange * gunshotHearingMultiplier, gunshotRadius);
            if (Vector3.Distance(transform.position, gunshotPos) <= effectiveRange)
            {
                lastSeenTime = Time.time;
                lastKnownPlayerPosition = player.position;
                return true;
            }
        }

        return false;
    }

    public bool SeenRecently(float graceSeconds) => Time.time - lastSeenTime <= graceSeconds;

    public Vector3 GetLastKnownPlayerPosition()
    {
        ResolvePlayerRef();
        if (lastKnownPlayerPosition == Vector3.zero && player != null)
            lastKnownPlayerPosition = player.position;
        return lastKnownPlayerPosition;
    }

    private void ResolvePlayerRef(bool force = false)
    {
        if (!force && player != null) return;
        player = PlayerLocator.GetPlayerTransform(force);
    }
}
