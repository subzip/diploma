using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class ElevatorEndSequence : MonoBehaviour
{
    [Header("Door Controller")]
    [SerializeField] private ElevatorDoorController doorController;

    [Header("Zones (Fallback if no Door Controller)")]
    [SerializeField] private Collider cabinZone;
    [SerializeField] private Collider approachZone;
    [SerializeField] private Collider doorwayZone;

    [Header("Condition")]
    [SerializeField] private float stayInCabinSeconds = 2f;

    [Header("Final Narrative")]
    [SerializeField, TextArea(3, 10)] private string finalNarrativeText =
        "СЕКТОР ЛАБОРАТОРИИ ОСТАВЛЕН ПОЗАДИ.\n\n" +
        "По аварийному журналу ясно одно: цепная реакция началась не с пожара и не с внешнего вторжения, " +
        "а с перегрузки нейроинтерфейса ARK. Система начала перестраивать среду, сохраняя цикл после каждой критической ошибки.\n\n" +
        "Ты восстановил доступ к лифту, но спуск вниз — это не эвакуация. " +
        "Это вход в зону, где источник аварии все еще активен.\n\n" +
        "ПРОТОТИП ЗАВЕРШЕН: Первая локация пройдена.";

    [SerializeField] private float typewriterCharsPerSecond = 42f;
    [SerializeField] private float blackScreenHoldSeconds = 2.5f;
    [SerializeField] private bool keepBlackScreenAtEnd = true;

    private float cabinStayTimer;
    private bool sequenceStarted;
    private Collider[] overlapBuffer;

    private void Awake()
    {
        overlapBuffer = new Collider[24];
        if (doorController == null)
        {
            doorController = GetComponent<ElevatorDoorController>();
        }
    }

    private void Update()
    {
        if (sequenceStarted) return;

        int inCabin = doorController != null ? (doorController.HasPlayerInCabin ? 1 : 0) : CountPlayersInside(cabinZone);
        int inApproach = doorController != null ? (doorController.HasPlayerInApproach ? 1 : 0) : CountPlayersInside(approachZone);
        int inDoorway = doorController != null ? (doorController.HasPlayerInDoorway ? 1 : 0) : CountPlayersInside(doorwayZone);
        bool doorClosed = doorController == null || doorController.IsDoorFullyClosed;

        bool tryingToExitOrOpen = inApproach > 0 || inDoorway > 0;
        bool validStay = inCabin > 0 && !tryingToExitOrOpen && doorClosed;

        if (validStay)
        {
            cabinStayTimer += Time.deltaTime;
            if (cabinStayTimer >= stayInCabinSeconds)
            {
                StartCoroutine(BeginFinalSequence());
            }
        }
        else
        {
            cabinStayTimer = 0f;
        }
    }

    private IEnumerator BeginFinalSequence()
    {
        if (sequenceStarted) yield break;
        sequenceStarted = true;

        Transform player = PlayerLocator.GetPlayerTransform(forceRefresh: true);
        if (player != null)
        {
            DisablePlayerControl(player);
        }

        AudioListener.pause = true;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        yield return null;

        CycleTransitionScreen transition = CycleTransitionScreen.Instance;
        if (transition != null)
        {
            yield return transition.PlayNarrativeOnly(
                finalNarrativeText,
                typewriterCharsPerSecond,
                blackScreenHoldSeconds,
                keepBlackScreenAtEnd
            );
        }
    }

    private void DisablePlayerControl(Transform player)
    {
        DisableOnPlayer<PlayerMovement>(player);
        DisableOnPlayer<PlayerLook>(player);
        DisableOnPlayer<PlayerCrouch>(player);
        DisableOnPlayer<PlayerNeuroresist>(player);
        DisableOnPlayer<AimController>(player);
        DisableOnPlayer<SwayNBobScript>(player);
        DisableOnPlayer<PlayerAudio>(player);

        WeaponManager wm = player.GetComponentInChildren<WeaponManager>(true);
        if (wm != null) wm.enabled = false;
    }

    private static void DisableOnPlayer<T>(Transform player) where T : Behaviour
    {
        T behaviour = player.GetComponentInChildren<T>(true);
        if (behaviour != null)
        {
            behaviour.enabled = false;
        }
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
            if (ComponentSearch.IsPlayer(c)) players++;
        }

        return players;
    }
}
