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
        public bool ForceRedownload { get; set; } = false;
        public bool EnableLogs { get; set; } = true;

        private AssetBundleManifest _manifest;
        private readonly Dictionary<string, AssetBundle> _loadedBundles = new();

        public AssetBundleManifest Manifest => _manifest;
        public IReadOnlyDictionary<string, AssetBundle> LoadedBundles => _loadedBundles;

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

        //url + bundle ID 형태로 다운로드
        public async Task LoadRequiredAsync(string bundleBaseUrl, string mainBundleName)
        {
            if (_manifest == null || string.IsNullOrEmpty(mainBundleName)) return;

            var deps = _manifest.GetAllDependencies(mainBundleName) ?? System.Array.Empty<string>();
            foreach (var d in deps) await LoadBundleAsync(bundleBaseUrl, d);
            await LoadBundleAsync(bundleBaseUrl, mainBundleName);
        }

        public void UnloadAll(bool unloadAllLoadedObjects = false)
        {
            foreach (var b in _loadedBundles.Values) b?.Unload(unloadAllLoadedObjects);
            _loadedBundles.Clear();
            _manifest = null;
        }

        private static async Task<bool> DownloadToFileAsync(string sourceUri, string dstPath, bool forceRedownload)
        {
            if (!forceRedownload && File.Exists(dstPath)) return true;

            using var req = UnityWebRequest.Get(sourceUri);
            var op = req.SendWebRequest();
            while (!op.isDone)
            {
                Debug.Log($"다운로드 진행률: {req.downloadProgress * 100f:0.0}% ({req.downloadedBytes / (1024f*1024f):0.0}MB)");
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