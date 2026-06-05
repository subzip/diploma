using UnityEngine;

[DisallowMultipleComponent]
public class GuideOnNeuroresistActivated : MonoBehaviour
{
    [SerializeField] private string guideId;
    [SerializeField] private bool completeAfterShow = true;

    private void OnEnable()
    {
        PlayerNeuroresist.OnActivated += OnActivated;
    }

    private void OnDisable()
    {
        PlayerNeuroresist.OnActivated -= OnActivated;
    }

    private void OnActivated()
    {
        GuideManager manager = GuideManager.Instance;
        if (manager == null) return;
        manager.ShowGuide(guideId);
        if (completeAfterShow) manager.CompleteGuide(guideId, hide: false);
    }
}

