using UnityEngine;

[DisallowMultipleComponent]
public class ElevatorDoorController : MonoBehaviour
{
    [Header("Door")]
    [SerializeField] private Transform door;
    [SerializeField] private Vector3 openOffsetLocal = new Vector3(1.25f, 0f, 0f);
    [SerializeField] private float moveSpeed = 2.8f;

    [Header("Zones (Trigger Colliders)")]
    [SerializeField] private Collider approachZone;
    [SerializeField] private Collider cabinZone;
    [SerializeField] private Collider doorwayZone;

    [Header("Safety")]
    [SerializeField] private float closeDelayAfterCabinEnter = 0.25f;
    [SerializeField] private int overlapBufferSize = 24;

    private Vector3 closedLocalPos;
    private Vector3 openLocalPos;
    private float closeAllowedAfterTime;
    private bool doorShouldBeOpen;
    private Collider[] overlapBuffer;
    private int prevCabinCount;
    private int approachPlayers;
    private int cabinPlayers;
    private int doorwayPlayers;

    public bool IsDoorFullyClosed => door != null && Vector3.Distance(door.localPosition, closedLocalPos) <= 0.01f;
    public bool IsDoorOpeningOrOpen => doorShouldBeOpen;
    public bool HasPlayerInApproach => approachPlayers > 0;
    public bool HasPlayerInCabin => cabinPlayers > 0;
    public bool HasPlayerInDoorway => doorwayPlayers > 0;

    private void Awake()
    {
        if (door == null)
        {
            door = transform;
        }

        closedLocalPos = door.localPosition;
        openLocalPos = closedLocalPos + openOffsetLocal;
        overlapBuffer = new Collider[Mathf.Max(8, overlapBufferSize)];
    }

    private void Update()
    {
        approachPlayers = CountPlayersInside(approachZone);
        cabinPlayers = CountPlayersInside(cabinZone);
        doorwayPlayers = CountPlayersInside(doorwayZone);

        if (cabinPlayers > 0 && prevCabinCount == 0)
        {
            closeAllowedAfterTime = Time.time + Mathf.Max(0f, closeDelayAfterCabinEnter);
        }
        prevCabinCount = cabinPlayers;

        if (doorwayPlayers > 0)
        {
            
            doorShouldBeOpen = true;
        }
        else if (approachPlayers > 0)
        {
            
            doorShouldBeOpen = true;
        }
        else if (cabinPlayers > 0 && Time.time >= closeAllowedAfterTime)
        {
            
            doorShouldBeOpen = false;
        }
        else
        {
            doorShouldBeOpen = false;
        }

        Vector3 target = doorShouldBeOpen ? openLocalPos : closedLocalPos;
        door.localPosition = Vector3.MoveTowards(door.localPosition, target, moveSpeed * Time.deltaTime);
    }

    private int CountPlayersInside(Collider zone)
    {
        if (zone == null) return 0;

        Bounds bounds = zone.bounds;
        int hits = Physics.OverlapBoxNonAlloc(
            bounds.center,
            bounds.extents,
            overlapBuffer,
            Quaternion.identity,
            ~0,
            QueryTriggerInteraction.Collide
        );

        int players = 0;
        for (int i = 0; i < hits; i++)
        {
            Collider c = overlapBuffer[i];
            if (c == null) continue;
            if (!zone.bounds.Contains(c.ClosestPoint(bounds.center))) continue;
            if (ComponentSearch.IsPlayer(c))
            {
                players++;
            }
        }

        return players;
    }
}
