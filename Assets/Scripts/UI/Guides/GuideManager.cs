using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class GuideManager : MonoBehaviour
{
    [System.Serializable]
    public class GuideEntry
    {
        public string id;
        [TextArea(1, 3)] public string actionText;
        public Sprite[] keyIcons;
        public bool showOnce = true;
        public bool autoHide = false;
        public float autoHideSeconds = 4f;
    }

    public static GuideManager Instance { get; private set; }

    [Header("Catalog")]
    [SerializeField] private List<GuideEntry> guides = new();

    [Header("UI")]
    [SerializeField] private Transform guidesRoot;
    [SerializeField] private GuideHintWidget guideItemPrefab;

    [Header("Persistence")]
    [SerializeField] private bool persistCompletionInPlayerPrefs = false;
    [SerializeField] private string prefsPrefix = "guide_completed_";

    private readonly Dictionary<string, GuideEntry> guideById = new();
    private readonly Dictionary<string, GuideHintWidget> activeWidgets = new();
    private readonly HashSet<string> completedIds = new();
    private readonly Dictionary<string, Coroutine> hideCoroutines = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        BuildCatalog();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void ShowGuide(string id)
    {
        if (!TryGetEntry(id, out GuideEntry entry)) return;
        if (entry.showOnce && IsCompleted(id)) return;
        if (guidesRoot == null || guideItemPrefab == null) return;

        GuideHintWidget widget = GetOrCreateWidget(id);
        widget.SetContent(entry.actionText, entry.keyIcons);

        if (entry.autoHide)
        {
            RestartAutoHide(id, Mathf.Max(0.1f, entry.autoHideSeconds));
        }
    }

    public void ShowCustomHint(string id, string text, Sprite[] icons = null, float autoHideSeconds = 3f)
    {
        if (string.IsNullOrWhiteSpace(id)) return;
        if (guidesRoot == null || guideItemPrefab == null) return;

        GuideHintWidget widget = GetOrCreateWidget(id);
        widget.SetContent(text, icons);
        RestartAutoHide(id, Mathf.Max(0.1f, autoHideSeconds));
    }

    public void HideGuide(string id)
    {
        if (hideCoroutines.TryGetValue(id, out Coroutine running) && running != null)
            StopCoroutine(running);
        hideCoroutines.Remove(id);

        if (!activeWidgets.TryGetValue(id, out GuideHintWidget widget)) return;
        activeWidgets.Remove(id);
        if (widget != null) Destroy(widget.gameObject);
    }

    public void CompleteGuide(string id, bool hide = true)
    {
        if (string.IsNullOrWhiteSpace(id)) return;
        completedIds.Add(id);
        if (persistCompletionInPlayerPrefs)
            PlayerPrefs.SetInt(prefsPrefix + id, 1);
        if (hide) HideGuide(id);
    }

    public bool IsCompleted(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        if (completedIds.Contains(id)) return true;
        if (!persistCompletionInPlayerPrefs) return false;
        return PlayerPrefs.GetInt(prefsPrefix + id, 0) == 1;
    }

    private IEnumerator AutoHideRoutine(string id, float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        HideGuide(id);
    }

    private bool TryGetEntry(string id, out GuideEntry entry)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            entry = null;
            return false;
        }
        return guideById.TryGetValue(id, out entry) && entry != null;
    }

    private void BuildCatalog()
    {
        guideById.Clear();
        for (int i = 0; i < guides.Count; i++)
        {
            GuideEntry entry = guides[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.id)) continue;
            guideById[entry.id] = entry;
        }
    }

    private GuideHintWidget GetOrCreateWidget(string id)
    {
        if (activeWidgets.TryGetValue(id, out GuideHintWidget existing) && existing != null)
            return existing;

        GuideHintWidget widget = Instantiate(guideItemPrefab, guidesRoot);
        activeWidgets[id] = widget;
        return widget;
    }

    private void RestartAutoHide(string id, float seconds)
    {
        if (hideCoroutines.TryGetValue(id, out Coroutine existing) && existing != null)
            StopCoroutine(existing);
        hideCoroutines[id] = StartCoroutine(AutoHideRoutine(id, seconds));
    }
}
