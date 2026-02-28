using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NeuroresistUI : MonoBehaviour
{
    [SerializeField] private PlayerNeuroresist neuroresist;
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text valueText;

    private void Awake()
    {
        if (neuroresist == null) neuroresist = FindObjectOfType<PlayerNeuroresist>();
    }

    private void Update()
    {
        if (neuroresist == null || fillImage == null) return;

        float max = Mathf.Max(0.001f, neuroresist.MaxValue);
        float t = Mathf.Clamp01(neuroresist.CurrentValue / max);
        fillImage.fillAmount = t;

        if (valueText != null)
            valueText.text = $"{Mathf.RoundToInt(t * 100f)}%";
    }
}
