using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using VRContent.Utils;

namespace VRContent.Loading
{
    public class BundleLoader
    {
        // ===== 속성/필드 =====
        public bool ForceRedownload { get; set; } = false;
        // true면 이미 파일이 캐시에 있어도 무조건 다시 다운로드한다.

        public bool EnableLogs { get; set; } = true;
        // true면 로드/다운로드 상황을 Debug.Log로 출력한다.

        public string CacheRoot { get; set; } =
            Path.Combine(Application.persistentDataPath, "Bundles");
        // 캐시 루트

        private AssetBundleManifest _manifest;
        // 매니페스트 번들 안에 들어있는 "AssetBundleManifest" 자산.
        // 의존성 관계(Dependencies) 확인할 때 필요하다.

        private readonly Dictionary<string, AssetBundle> _loadedBundles = new();
        // 현재 메모리에 로드된 AssetBundle 모음 (bundleName → AssetBundle 객체)

        public AssetBundleManifest Manifest => _manifest;
        // 읽기 전용 프로퍼티. 현재 로드된 매니페스트를 외부에서 확인할 때 쓴다.

        public IReadOnlyDictionary<string, AssetBundle> LoadedBundles => _loadedBundles;
        // 현재 로드된 번들들을 읽기 전용 형태로 외부에 노출.


        // 1) 지정한 manifest 번들 URL을 persistentDataPath/main_manifest 에 다운로드한다.
        //    (이미 있으면 재사용, ForceRedownload=true면 무조건 다시 받음)
        // 2) 다운로드된 파일을 AssetBundle.LoadFromFileAsync 로 메모리에 올린다.
        // 3) 그 안에서 "AssetBundleManifest" 자산을 꺼내서 _manifest에 저장한다.
        //    (모든 번들의 의존 관계를 알기 위해 반드시 필요)
        // 4) manifestBundle 자체는 Unload(false) 해서 메모리 낭비 줄인다.
        // 반환: 성공(true) / 실패(false)
        public async Task<bool> LoadManifestAsync(string manifestBundleUrl)
        {
            string source = UrlUtil.ResolveSource(manifestBundleUrl);
            string cachePath = Path.Combine(Application.persistentDataPath, "main_manifest");

            bool ok = await DownloadToFileAsync(source, cachePath, ForceRedownload);
            if (!ok) return false;

            // 비동기 로드
            var abReq = AssetBundle.LoadFromFileAsync(cachePath);
            while (!abReq.isDone) await Task.Yield();
            var manifestBundle = abReq.assetBundle;

            if (manifestBundle == null)
            {
                Debug.LogError("매니페스트 번들 파일 로드에 실패했습니다.");
                return false;
            }

            _manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            manifestBundle.Unload(false);
            return _manifest != null;
        }

        // 1) 이미 _loadedBundles에 있으면 재사용(true 반환)
        // 2) baseUrl + bundleName으로 원격 URL 생성 후 persistentDataPath/bundleName 에 저장
        // 3) AssetBundle.LoadFromFileAsync 로 번들 파일을 메모리에 올린다.
        // 4) 성공하면 _loadedBundles에 등록 (bundleName → AssetBundle)
        // 반환: 성공(true) / 실패(false)
        public async Task<bool> LoadBundleAsync(string bundleBaseUrl, string bundleName)
        {
            if (_loadedBundles.ContainsKey(bundleName)) return true;

            string baseResolved = UrlUtil.ResolveSource(bundleBaseUrl);
            string source = UrlUtil.CombineUrl(baseResolved, bundleName);
            string cachePath = Path.Combine(Application.persistentDataPath, bundleName);

            bool ok = await DownloadToFileAsync(source, cachePath, ForceRedownload);
            if (!ok)
            {
                Debug.LogError($"{bundleName} 번들 다운로드 실패");
                return false;
            }

            var abReq = AssetBundle.LoadFromFileAsync(cachePath);
            while (!abReq.isDone) await Task.Yield();
            var bundle = abReq.assetBundle;

            if (bundle == null)
            {
                Debug.LogError($"{bundleName} 번들 로드 실패");
                return false;
            }

            _loadedBundles[bundleName] = bundle;
            if (EnableLogs) Debug.Log($"번들 로드 성공: {bundleName}");
            return true;
        }

        // 전제: _manifest 가 로드되어 있어야 한다.
        // 1) manifest에서 mainBundleName의 모든 의존성 리스트를 가져온다.
        // 2) 각 의존성(d)에 대해 LoadBundleAsync 실행
        // 3) 마지막으로 mainBundleName 번들도 LoadBundleAsync 실행
        // 즉, mainBundle을 실행하기 위해 필요한 모든 번들을 캐시에 다운로드+메모리에 로드한다.
        public async Task LoadRequiredAsync(string bundleBaseUrl, string mainBundleName)
        {
            if (_manifest == null || string.IsNullOrEmpty(mainBundleName)) return;

            var deps = _manifest.GetAllDependencies(mainBundleName) ?? System.Array.Empty<string>();
            foreach (var d in deps) await LoadBundleAsync(bundleBaseUrl, d);
            await LoadBundleAsync(bundleBaseUrl, mainBundleName);
        }

        public async Task<bool> LoadManifestIntoFolderAsync(string manifestBundleUrl, string folder, string fileNameFromUrlFallback = null)
        {
            Directory.CreateDirectory(Path.Combine(CacheRoot, folder));

            string fileName = GetFileNameFromUrl(manifestBundleUrl);
            if (string.IsNullOrEmpty(fileName)) fileName = fileNameFromUrlFallback ?? "manifestbundle";

            string savePath = Path.Combine(CacheRoot, folder, fileName);

            string source = UrlUtil.ResolveSource(manifestBundleUrl);
            bool ok = await DownloadToFileAsync(source, savePath, ForceRedownload);
            if (!ok) return false;

            var abReq = AssetBundle.LoadFromFileAsync(savePath);
            while (!abReq.isDone) await Task.Yield();
            var manifestBundle = abReq.assetBundle;

            if (manifestBundle == null)
            {
                Debug.LogError("매니페스트 번들 파일 로드 실패 (폴더 버전)");
                return false;
            }

            _manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            manifestBundle.Unload(false);
            return _manifest != null;
        }

        // 현재 메모리에 올라와 있는 모든 AssetBundle들을 언로드한다.
        // unloadAllLoadedObjects=true 면 번들 안의 에셋까지 전부 언로드
        // unloadAllLoadedObjects=false 면 AssetBundle만 해제, 에셋은 유지
        // _loadedBundles 딕셔너리를 비우고, manifest도 null로 만든다.
        public void UnloadAll(bool unloadAllLoadedObjects = false)
        {
            foreach (var b in _loadedBundles.Values) b?.Unload(unloadAllLoadedObjects);
            _loadedBundles.Clear();
            _manifest = null;
        }

        private static string GetFileNameFromUrl(string url)
        {
            try
            {
                var u = new Uri(url);
                var name = Path.GetFileName(u.LocalPath);
                return string.IsNullOrEmpty(name) ? null : name;
            }
            catch
            {
                var withoutQuery = url.Split('?')[0];
                var parts = withoutQuery.Split('/');
                return parts.Length == 0 ? null : parts[^1];
            }
        }

        // 1) 캐시 파일이 이미 있고 forceRedownload=false면 다운로드 생략(true 반환)
        // 2) UnityWebRequest.Get 으로 sourceUri 다운로드 진행
        //    - 진행 중에 Debug.Log로 다운로드율/용량 출력
        // 3) 완료되면 dstPath 위치에 파일로 저장
        //    - 경로가 없으면 Directory.CreateDirectory 로 생성
        //    - 저장 실패하면 false 반환
        // 반환: 성공(true) / 실패(false)
        private static async Task<bool> DownloadToFileAsync(string sourceUri, string dstPath, bool forceRedownload)
        {
            if (!forceRedownload && File.Exists(dstPath)) return true;

            using var req = UnityWebRequest.Get(sourceUri);
            var op = req.SendWebRequest();
            while (!op.isDone)
            {
                Debug.Log($"다운로드 진행률: {req.downloadProgress * 100f:0.0}% ({req.downloadedBytes / (1024f * 1024f):0.0}MB)");
                await Task.Yield();
            }


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
}