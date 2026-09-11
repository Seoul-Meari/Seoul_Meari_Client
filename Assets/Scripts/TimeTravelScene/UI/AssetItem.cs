using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AssetItem : MonoBehaviour
{
    private string id;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI metaText;   // 예: v1.0.0 • android • published
    public TextMeshProUGUI descText;
    public TextMeshProUGUI tagsText;
    public Button button;
    public void Bind(AssetBundleMeta m)
    {
        Debug.Log("m: " + m.name);
        id = m.id;
        if (nameText) nameText.text = m.name ?? "(no name)";
        if (metaText) metaText.text = $"v{m.version}";
        if (descText) descText.text = m.description ?? "";
        if (tagsText) tagsText.text = (m.tags != null && m.tags.Count > 0) ? $"#{string.Join(" #", m.tags)}" : "";
    }

    public static event Action<string> OnAnyClick;

    void Awake()
    {
        button.onClick.AddListener(() => DownloadAsset());
    }
    public void DownloadAsset()
    {
        if (id != null)
        {
            Debug.Log("on click event added");
            OnAnyClick?.Invoke(id);
        }
    }
}