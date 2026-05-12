using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GuideHintWidget : MonoBehaviour
{
    [SerializeField] private TMP_Text actionText;
    [SerializeField] private RectTransform iconsRoot;
    [SerializeField] private Image iconTemplate;
    [SerializeField] private LayoutElement rootLayoutElement;
    [SerializeField] private RectTransform rootRect;
    [SerializeField] private float fixedHeight = 80f;
    [SerializeField] private float minWidth = 280f;
    [SerializeField] private float maxWidth = 560f;
    [SerializeField] private float horizontalPadding = 16f;
    [SerializeField] private float iconSpacing = 6f;
    [SerializeField] private float iconTextGap = 10f;
    [Header("Icon Size")]
    [SerializeField] private Vector2 defaultIconSize = new Vector2(28f, 28f);
    [SerializeField] private Sprite tabIconSprite;
    [SerializeField] private Vector2 tabIconSize = new Vector2(42f, 26f);

    private readonly System.Collections.Generic.List<Image> spawnedIcons = new();
    private readonly System.Collections.Generic.List<float> spawnedIconWidths = new();
    private LayoutElement actionLayoutElement;
    private RectTransform actionRect;
    private ContentSizeFitter actionFitter;

    private void Awake()
    {
        if (rootRect == null) rootRect = transform as RectTransform;
        if (rootLayoutElement == null) rootLayoutElement = GetComponent<LayoutElement>();
        if (actionText != null)
        {
            actionRect = actionText.rectTransform;
            actionLayoutElement = actionText.GetComponent<LayoutElement>();
            if (actionLayoutElement == null) actionLayoutElement = actionText.gameObject.AddComponent<LayoutElement>();
            actionFitter = actionText.GetComponent<ContentSizeFitter>();
            if (actionFitter == null) actionFitter = actionText.gameObject.AddComponent<ContentSizeFitter>();
            actionFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            actionFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        HorizontalLayoutGroup h = GetComponent<HorizontalLayoutGroup>();
        if (h != null) h.enabled = false;
        VerticalLayoutGroup v = GetComponent<VerticalLayoutGroup>();
        if (v != null) v.enabled = false;
        ContentSizeFitter rootFitter = GetComponent<ContentSizeFitter>();
        if (rootFitter != null) rootFitter.enabled = false;
    }

    public void SetContent(string text, Sprite[] icons)
    {
        bool hasAnyIcon = false;
        if (icons != null)
        {
            for (int i = 0; i < icons.Length; i++)
            {
                if (icons[i] != null)
                {
                    hasAnyIcon = true;
                    break;
                }
            }
        }

        if (actionText != null)
        {
            actionText.text = text;
            actionText.alignment = hasAnyIcon ? TextAlignmentOptions.Left : TextAlignmentOptions.Center;
            actionText.enableWordWrapping = false;
            actionText.overflowMode = TextOverflowModes.Ellipsis;
        }

        if (iconsRoot == null || iconTemplate == null) return;

        for (int i = 0; i < spawnedIcons.Count; i++)
            if (spawnedIcons[i] != null) Destroy(spawnedIcons[i].gameObject);
        spawnedIcons.Clear();
        spawnedIconWidths.Clear();

        iconTemplate.gameObject.SetActive(false);
        bool hasIcons = hasAnyIcon;
        iconsRoot.gameObject.SetActive(hasIcons);

        if (hasIcons)
        {
            for (int i = 0; i < icons.Length; i++)
            {
                Sprite icon = icons[i];
                if (icon == null) continue;
                Image img = Instantiate(iconTemplate, iconsRoot);
                img.sprite = icon;
                img.gameObject.SetActive(true);
                RectTransform imgRt = img.rectTransform;
                Vector2 targetSize = GetIconSize(icon);
                imgRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetSize.x);
                imgRt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetSize.y);
                spawnedIcons.Add(img);
                spawnedIconWidths.Add(targetSize.x);
            }
        }
        ApplyManualLayout();
    }

    private void ApplyManualLayout()
    {
        if (rootRect == null || actionRect == null || actionText == null) return;

        float textWidth = Mathf.Max(0f, actionText.preferredWidth);
        float textHeight = Mathf.Max(24f, actionText.preferredHeight);
        float iconsWidth = 0f;
        int iconCount = spawnedIcons.Count;
        if (iconCount > 0)
        {
            for (int i = 0; i < spawnedIconWidths.Count; i++)
                iconsWidth += spawnedIconWidths[i];
            iconsWidth += Mathf.Max(0, iconCount - 1) * iconSpacing;
        }

        float contentWidth = textWidth + (iconCount > 0 ? iconsWidth + iconTextGap : 0f);
        float cardWidth = Mathf.Clamp(contentWidth + horizontalPadding * 2f, minWidth, maxWidth);

        if (rootLayoutElement != null)
        {
            rootLayoutElement.minHeight = fixedHeight;
            rootLayoutElement.preferredHeight = fixedHeight;
            rootLayoutElement.flexibleHeight = 0f;
            rootLayoutElement.minWidth = minWidth;
            rootLayoutElement.preferredWidth = cardWidth;
            rootLayoutElement.flexibleWidth = 0f;
        }

        rootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cardWidth);
        rootRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, fixedHeight);

        float startX = -contentWidth * 0.5f;
        float centerY = 0f;

        if (iconsRoot != null)
        {
            iconsRoot.anchorMin = new Vector2(0.5f, 0.5f);
            iconsRoot.anchorMax = new Vector2(0.5f, 0.5f);
            iconsRoot.pivot = new Vector2(0f, 0.5f);
            iconsRoot.anchoredPosition = new Vector2(startX, centerY);
            iconsRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(0f, iconsWidth));
            iconsRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, fixedHeight);

            float x = 0f;
            for (int i = 0; i < spawnedIcons.Count; i++)
            {
                Image img = spawnedIcons[i];
                if (img == null) continue;
                float w = (i < spawnedIconWidths.Count) ? spawnedIconWidths[i] : defaultIconSize.x;
                RectTransform rt = img.rectTransform;
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.anchoredPosition = new Vector2(x, 0f);
                x += w + iconSpacing;
            }
        }

        actionRect.anchorMin = new Vector2(0.5f, 0.5f);
        actionRect.anchorMax = new Vector2(0.5f, 0.5f);
        if (iconCount > 0)
        {
            actionRect.pivot = new Vector2(0f, 0.5f);
            float textX = startX + iconsWidth + iconTextGap;
            actionRect.anchoredPosition = new Vector2(textX, centerY);
            actionRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, textWidth + 4f);
        }
        else
        {
            actionRect.anchorMin = new Vector2(0.5f, 0.5f);
            actionRect.anchorMax = new Vector2(0.5f, 0.5f);
            actionRect.pivot = new Vector2(0.5f, 0.5f);
            actionRect.anchoredPosition = Vector2.zero;
            float textOnlyWidth = Mathf.Min(textWidth + 8f, Mathf.Max(0f, cardWidth - horizontalPadding * 2f));
            actionRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, textOnlyWidth);
        }
        actionRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Min(textHeight + 6f, fixedHeight - 10f));
    }

    private Vector2 GetIconSize(Sprite icon)
    {
        if (icon == null) return defaultIconSize;
        if (tabIconSprite != null && icon == tabIconSprite) return tabIconSize;
        return defaultIconSize;
    }
}
