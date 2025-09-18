using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using CesiumForUnity;
using Unity.Mathematics;

#region Data_Structures
[System.Serializable]
public class PrefabInfo
{
    public string id;
    public string name;
}

[System.Serializable]
public class PlacementGroup
{
    public string groupId;
    public string prefabId;
    public TransformData[] transforms;
    public bool? active;
}

[System.Serializable]
public class TransformData
{
    public LocationData location;
    public Rotation rotation;
    public Scale scale;
}

// 예: LayoutRoot, LocationData, Rotation, Scale 등은 기존 정의를 그대로 사용한다고 가정
#endregion

public class BundleAndLayoutSpawner : MonoBehaviour
{
    [Header("Source URLs or StreamingAssets-relative paths")]
    [Tooltip("메인 매니페스트 번들의 전체 URL 또는 StreamingAssets 상대 경로 (예: http://.../Android 또는 StreamingAssets에 복사한 경우 파일명 'Android')")]
    public string manifestBundleUrl; // 예: https://cdn.example.com/AssetBundles/Android/Android  또는  "Android" (StreamingAssets 내)

    [Tooltip("개별 에셋 번들 폴더의 URL 또는 StreamingAssets 상대 폴더명 (마지막 슬래시 포함/미포함 허용)")]
    public string bundleBaseUrl = "";     // 예: https://cdn.example.com/AssetBundles/Android/  또는  "Android" (StreamingAssets 내 폴더)

    [Tooltip("레이아웃 JSON의 URL 또는 StreamingAssets 상대 경로 (예: test_bundle.json)")]
    public string layoutJsonUrl;     // 예: https://cdn.../test_bundle.json  또는  "test_bundle.json"

    [Header("Cesium Georeference")]
    public CesiumGeoreference georeference;

    [Header("Options")]
    [Tooltip("캐시 무시하고 항상 다시 받기")]
    public bool forceRedownload = false;

    private readonly Dictionary<string, AssetBundle> _loadedBundles = new();
    private readonly Dictionary<string, string> _prefabIdToName = new();
    private readonly Dictionary<string, string> _prefabIdToBundleId = new();
    private AssetBundleManifest _manifest;

    // --- 플랫폼/경로 유틸 ---
    private static bool LooksLikeHttp(string s) =>
        !string.IsNullOrEmpty(s) && (s.StartsWith("http://") || s.StartsWith("https://"));

    // URL 결합 전용 (Path.Combine 쓰지 마세요)
    private static string CombineUrl(string baseUrl, string segment)
    {
        if (string.IsNullOrEmpty(baseUrl)) return segment ?? "";
        if (string.IsNullOrEmpty(segment)) return baseUrl;
        if (baseUrl.EndsWith("/")) baseUrl = baseUrl.TrimEnd('/');
        segment = segment.TrimStart('/');
        return $"{baseUrl}/{segment}";
    }

    private void OnDestroy()
    {
        foreach (var bundle in _loadedBundles.Values)
        {
            if (bundle != null) bundle.Unload(false);
        }
    }

    private async void Start()
    {
        // 0) 필수 체크
        if (georeference == null)
        {
            Debug.LogError("CesiumGeoreference가 할당되지 않았습니다. 인스펙터에서 지정해주세요.");
            return;
        }
        if (string.IsNullOrEmpty(manifestBundleUrl))
        {
            Debug.LogError("manifestBundleUrl이 비어 있습니다.");
            return;
        }
        // if (string.IsNullOrEmpty(bundleBaseUrl))
        // {
        //     Debug.LogError("bundleBaseUrl이 비어 있습니다.");
        //     return;
        // }
        if (string.IsNullOrEmpty(layoutJsonUrl))
        {
            Debug.LogError("layoutJsonUrl이 비어 있습니다.");
            return;
        }

        // 1) 메인 매니페스트 로드
        await LoadManifest();
        if (_manifest == null)
        {
            Debug.LogError("에셋 번들 매니페스트 로드에 실패했습니다.");
            return;
        }

        // 2) 레이아웃 JSON 로드/파싱
        var layout = await DownloadAndParseJson<LayoutRoot>(ResolveSource(layoutJsonUrl), "layout.json");

        if (layout == null)
        {
            Debug.LogError("layout.json 로드 또는 파싱에 실패했습니다.");
            return;
        }

        // 3) 필요한 번들 의존성 + 메인 번들 로드
        await LoadRequiredBundlesFromLayout(layout);
        Debug.Log("레이아웃에서 번들 로드 완료");

        // 4) 프리팹 조회 맵
        BuildPrefabLookups(layout);
        Debug.Log("프리팹 조회 완료");

        // 5) 스폰
        SpawnObjectsFromLayout(layout);
        Debug.Log("배치 완료!");

    }

    /// <summary>
    /// 입력이 HTTP/HTTPS가 아니면 StreamingAssets 상대경로로 간주해 실제 UWR에 사용 가능한 URI로 변환
    /// </summary>
    private string ResolveSource(string maybeUrlOrStreamingAssetsRelative)
    {
        // HTTP(S)면 그대로
        if (LooksLikeHttp(maybeUrlOrStreamingAssetsRelative)) return maybeUrlOrStreamingAssetsRelative;

        // 그렇지 않으면 StreamingAssetsPath와 합성 (여기서는 파일시스템 경로가 아니라 UWR에서 쓸 수 있는 경로 문자열)
        return CombineUrl(Application.streamingAssetsPath, maybeUrlOrStreamingAssetsRelative);
    }

    /// <summary>
    /// 메인 매니페스트('Android' 등)를 받아와 캐시에 저장하고 AssetBundleManifest 로드
    /// </summary>
    private async Task LoadManifest()
    {
        string source = ResolveSource(manifestBundleUrl);
        string cachePath = Path.Combine(Application.persistentDataPath, "main_manifest"); // 캐시 파일명

        bool ok = await DownloadToFileAsync(source, cachePath, forceRedownload: forceRedownload);
        if (!ok) return;

        var manifestBundle = AssetBundle.LoadFromFile(cachePath);
        if (manifestBundle == null)
        {
            Debug.LogError("매니페스트 번들 파일 로드에 실패했습니다.");
            return;
        }

        _manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        manifestBundle.Unload(false);
    }

    /// <summary>
    /// layout.json이 지목한 bundleId 및 의존성 번들을 모두 로드
    /// </summary>
    private async Task LoadRequiredBundlesFromLayout(LayoutRoot layout)
    {
        if (layout == null || string.IsNullOrEmpty(layout.bundleId))
        {
            Debug.LogWarning("layout.bundleId가 비어 있습니다.");
            return;
        }

        var deps = _manifest.GetAllDependencies(layout.bundleId) ?? System.Array.Empty<string>();

        // 1) 의존성 먼저
        foreach (var d in deps)
        {
            if (!_loadedBundles.ContainsKey(d))
                await LoadBundle(d);
        }
        // 2) 메인 번들
        if (!_loadedBundles.ContainsKey(layout.bundleId))
            await LoadBundle(layout.bundleId);
    }

    /// <summary>
    /// 개별 번들 로드 (원격 or StreamingAssets → 캐시 → LoadFromFile)
    /// </summary>
    private async Task LoadBundle(string bundleName)
    {
        // bundleBaseUrl이 URL이면 URL 결합, 아니면 StreamingAssets 상대 경로 결합
        string baseResolved = ResolveSource(bundleBaseUrl);
        string source = LooksLikeHttp(baseResolved)
            ? CombineUrl(baseResolved, bundleName)
            : CombineUrl(baseResolved, bundleName);

        string cachePath = Path.Combine(Application.persistentDataPath, bundleName);

        bool ok = await DownloadToFileAsync(source, cachePath, forceRedownload);
        if (!ok)
        {
            Debug.LogError($"{bundleName} 번들 다운로드 실패");
            return;
        }

        var bundle = AssetBundle.LoadFromFile(cachePath);
        if (bundle == null)
        {
            Debug.LogError($"{bundleName} 번들 로드 실패");
            return;
        }

        _loadedBundles[bundleName] = bundle;
        Debug.Log($"번들 로드 성공: {bundleName}");
    }

    /// <summary>
    /// JSON을 UWR로 받아 캐시에 저장 후 역직렬화
    /// </summary>
    private async Task<T> DownloadAndParseJson<T>(string sourceUri, string cacheFileName) where T : class
    {
        string cachePath = Path.Combine(Application.persistentDataPath, cacheFileName);
        if (File.Exists(cachePath)) File.Delete(cachePath);

        // 캐시 재사용 옵션
        if (!forceRedownload && File.Exists(cachePath))
        {
            try
            {
                var cached = await File.ReadAllTextAsync(cachePath);
                return JsonConvert.DeserializeObject<T>(cached);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"캐시 JSON 파싱 실패(무시 후 재다운로드 시도): {ex.Message}");
            }
        }

        // UWR로 다운로드 (HTTP든 StreamingAssets든 동일 경로로 처리)
        using var req = UnityWebRequest.Get(sourceUri);
        var op = req.SendWebRequest();
        while (!op.isDone) await Task.Yield();

#if UNITY_2020_2_OR_NEWER
        if (req.result != UnityWebRequest.Result.Success)
#else
        if (req.isNetworkError || req.isHttpError)
#endif
        {
            Debug.LogError($"JSON 다운로드 실패: {sourceUri} | {req.error}");
            return null;
        }

        string jsonText = req.downloadHandler.text;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(cachePath));
            await File.WriteAllTextAsync(cachePath, jsonText);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"JSON 캐시 저장 실패(무시): {ex.Message}");
        }

        try
        {
            return JsonConvert.DeserializeObject<T>(jsonText);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"JSON 파싱 실패: {ex.Message}");
            return null;
        }
    }

    private void BuildPrefabLookups(LayoutRoot layout)
    {
        _prefabIdToName.Clear();
        _prefabIdToBundleId.Clear();

        if (layout?.prefabs == null) return;

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

    private void SpawnObjectsFromLayout(LayoutRoot layout)
    {
        if (layout?.placementGroups == null) return;

        foreach (var group in layout.placementGroups)
        {
            if (group == null) continue;
            if (group.active.HasValue && !group.active.Value) continue;

            if (!_prefabIdToName.TryGetValue(group.prefabId, out var prefabName) ||
                !_prefabIdToBundleId.TryGetValue(group.prefabId, out var bundleId))
            {
                Debug.LogWarning($"프리팹 ID 매핑 실패: {group?.prefabId}");
                continue;
            }

            if (!_loadedBundles.TryGetValue(bundleId, out var bundle) || bundle == null)
            {
                Debug.LogError($"'{bundleId}' 번들이 로드되지 않았습니다.");
                continue;
            }

            var prefab = bundle.LoadAsset<GameObject>(prefabName);
            if (prefab == null)
            {
                Debug.LogError($"'{bundleId}' 번들에서 '{prefabName}' 프리팹을 찾을 수 없습니다.");
                continue;
            }

            if (group.transforms == null) continue;
            Debug.Log("prefab Name: " + prefabName);

            foreach (var t in group.transforms)
            {
                if (t == null || t.location == null || t.rotation == null || t.scale == null) continue;
                Debug.Log("prefab gps: " + t.location.latitude + ", " + t.location.longitude + ", " + t.location.z);
                Quaternion rotation = Quaternion.Euler(t.rotation.x, t.rotation.y, t.rotation.z);

                GameObject go = Instantiate(prefab, Vector3.zero, rotation);
                go.transform.SetParent(georeference.transform, false);

                var globeAnchor = go.GetComponent<CesiumGlobeAnchor>();
                if (globeAnchor == null) globeAnchor = go.AddComponent<CesiumGlobeAnchor>();
                globeAnchor.longitudeLatitudeHeight = new double3(t.location.longitude, t.location.latitude, t.location.z);
            }
        }
    }

    /// <summary>
    /// sourceUri(HTTP/HTTPS/StreamingAssets-jar) → 파일로 저장
    /// </summary>
    private async Task<bool> DownloadToFileAsync(string sourceUri, string dstPath, bool forceRedownload)
    {
        // 캐시 사용
        if (!forceRedownload && File.Exists(dstPath))
        {
            // Debug.Log($"캐시 사용: {Path.GetFileName(dstPath)}");
            return true;
        }

        using var req = UnityWebRequest.Get(sourceUri);
        var op = req.SendWebRequest();
        while (!op.isDone) await Task.Yield();

#if UNITY_2020_2_OR_NEWER
        if (req.result != UnityWebRequest.Result.Success)
#else
        if (req.isNetworkError || req.isHttpError)
#endif
        {
            Debug.LogError($"다운로드 실패: {sourceUri} | 오류: {req.error}");
            return false;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(dstPath));
            await File.WriteAllBytesAsync(dstPath, req.downloadHandler.data);
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"파일 저장 실패 {dstPath}: {ex.Message}");
            return false;
        }
    }
}