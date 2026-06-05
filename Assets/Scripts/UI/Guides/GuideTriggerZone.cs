using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class GuideTriggerZone : MonoBehaviour
{
    public enum TriggerMode
    {
        ShowOnEnter,
        CompleteOnEnter,
        ShowAndCompleteOnEnter
    }

    [SerializeField] private string guideId;
    [SerializeField] private TriggerMode triggerMode = TriggerMode.ShowOnEnter;
    [SerializeField] private bool hideOnExit;

    private void Reset()
    {
        Collider c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!ComponentSearch.IsPlayer(other)) return;
        GuideManager manager = GuideManager.Instance;
        if (manager == null) return;

        switch (triggerMode)
        {
            case TriggerMode.ShowOnEnter:
                manager.ShowGuide(guideId);
                break;
            case TriggerMode.CompleteOnEnter:
                manager.CompleteGuide(guideId, hide: true);
                break;
            case TriggerMode.ShowAndCompleteOnEnter:
                manager.ShowGuide(guideId);
                manager.CompleteGuide(guideId, hide: false);
                break;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!hideOnExit) return;
        if (!ComponentSearch.IsPlayer(other)) return;
        GuideManager manager = GuideManager.Instance;
        if (manager == null) return;
        manager.HideGuide(guideId);
    }
}

