using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AssetListUI : MonoBehaviour
{
    public Transform contentParent;
    public AssetItem itemPrefab;

public void Render(List<AssetBundleMeta> items)
    {
        // 0) 기본 안전검사
        if (contentParent == null || itemPrefab == null) {
            Debug.LogError("[AssetListUI] contentParent 또는 itemPrefab 미지정");
            return;
        }

        // 1) 기존 자식 제거
        for (int i = contentParent.childCount - 1; i >= 0; i--)
            Destroy(contentParent.GetChild(i).gameObject);

        if (items == null || items.Count == 0) return;

        // 2) 아이템 생성
        foreach (var m in items)
        {
            var go = Instantiate(itemPrefab.gameObject);
            go.SetActive(true);
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();

            // 부모 설정 (worldPositionStays=false 권장)
            rt.SetParent(contentParent, false);
            
            // 데이터 바인딩
            var view = go.GetComponent<AssetItem>();
            if (view == null) view = go.AddComponent<AssetItem>();
            view.Bind(m);
        }

        Debug.Log($"[AssetListUI] 렌더 완료. count={items.Count}");
    }
}