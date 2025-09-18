using System.Collections.Generic;
using UnityEngine;
using CesiumForUnity;
using Unity.Mathematics;
using VRContent.Loading;
using VRContent.Spawning;

public class LayoutSpawner : MonoBehaviour
{
    [Header("Sources (URL or StreamingAssets-relative)")]
    public string manifestBundleUrl;     // "Android" or "https://.../Android"
    public string bundleBaseUrl = "";    // "Android" or "https://.../Android"
    public string layoutJsonUrl;         // "layout.json" or "https://.../layout.json"

    [Header("Cesium")]
    public CesiumGeoreference georeference;

    [Header("Options")]
    public bool forceRedownload = false;
    public SpawnConfig config;

    private BundleLoader _bundles;
    private readonly Dictionary<string, string> _prefabIdToName = new();
    private readonly Dictionary<string, string> _prefabIdToBundleId = new();

    private async void Start()
    {
        if (georeference == null) { Debug.LogError("CesiumGeoreference 미지정"); return; }
        if (string.IsNullOrEmpty(manifestBundleUrl)) { Debug.LogError("manifestBundleUrl 비어있음"); return; }
        if (string.IsNullOrEmpty(layoutJsonUrl)) { Debug.LogError("layoutJsonUrl 비어있음"); return; }

        _bundles = new BundleLoader { ForceRedownload = forceRedownload, EnableLogs = config ? config.enableLogs : false };

        // 1) 매니페스트
        bool manifestOk = await _bundles.LoadManifestAsync(manifestBundleUrl);
        if (!manifestOk) { Debug.LogError("매니페스트 로드 실패"); return; }

        // 2) 레이아웃 JSON
        var layout = await JsonLoader.DownloadAndParseJson<LayoutRoot>(layoutJsonUrl, "layout.json", forceRedownload);
        if (layout == null) { Debug.LogError("layout 로드/파싱 실패"); return; }

        // 3) 필요한 번들 로드
        await _bundles.LoadRequiredAsync(bundleBaseUrl, layout.bundleId);

        // 4) 프리팹 맵 구성
        BuildPrefabLookups(layout);

        // 5) 스폰 코루틴 (프레임 분할)
        StartCoroutine(SpawnFromLayoutCo(layout));
    }

    private void OnDestroy()
    {
        _bundles?.UnloadAll(false);
    }

    private void BuildPrefabLookups(LayoutRoot layout)
    {
        _prefabIdToName.Clear();
        _prefabIdToBundleId.Clear();
        if (layout.prefabs == null) return;

        foreach (var p in layout.prefabs)
        {
            if (p == null || string.IsNullOrEmpty(p.id)) continue;
            if (!_prefabIdToName.ContainsKey(p.id))
            {
                _prefabIdToName[p.id] = p.name;
                _prefabIdToBundleId[p.id] = layout.bundleId;
            }
        }
    }

    private System.Collections.IEnumerator SpawnFromLayoutCo(LayoutRoot layout)
    {
        if (layout?.placementGroups == null) yield break;

        int perFrame = (config ? Mathf.Max(1, config.spawnPerFrame) : 8);
        int spawned = 0;

        foreach (var group in layout.placementGroups)
        {
            if (group == null) continue;
            if (group.active.HasValue && !group.active.Value) continue;
            if (group.transforms == null || group.transforms.Length == 0) continue;

            if (!_prefabIdToName.TryGetValue(group.prefabId, out var prefabName) ||
                !_prefabIdToBundleId.TryGetValue(group.prefabId, out var bundleId) ||
                !_bundles.LoadedBundles.TryGetValue(bundleId, out var bundle) || bundle == null)
            {
                continue;
            }

            // 프리팹 비동기 로드 (한 번만)
            var assetReq = bundle.LoadAssetAsync<GameObject>(prefabName);
            while (!assetReq.isDone) yield return null;
            var prefab = (GameObject)assetReq.asset;
            if (prefab == null) continue;

            foreach (var t in group.transforms)
            {
                if (t == null || t.location == null || t.rotation == null || t.scale == null) continue;

                // 비활성 생성 → 세팅 → 활성화
                GameObject go = Instantiate(prefab, Vector3.zero, Quaternion.Euler(t.rotation.x, t.rotation.y, t.rotation.z));
                go.SetActive(false);
                go.transform.SetParent(georeference.transform, false);
                go.transform.localScale = new Vector3(t.scale.x, t.scale.y, t.scale.z);

                // Anchor 세팅
                var anchor = go.GetComponent<CesiumGlobeAnchor>() ?? go.AddComponent<CesiumGlobeAnchor>();
                anchor.longitudeLatitudeHeight = new double3(t.location.longitude, t.location.latitude, t.location.z);

                Debug.Log("secret" + t.location.z);

                go.SetActive(true);

                // 프레임 분할
                spawned++;
                if (spawned % perFrame == 0)
                    yield return null;
            }
        }
    }
}