using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using VRContent.Utils;

namespace VRContent.Loading
{
    public static class JsonLoader
    {
        // 필요하면 true로 두고 쿼리 버전 추가 (CDN/프록시 캐시 회피)
        public static bool EnableCacheBust = false;

        public static async Task<T> DownloadAndParseJson<T>(string sourceUri, string cacheFileName, bool forceRedownload) where T : class
        {
            string cachePath = Path.Combine(Application.persistentDataPath, cacheFileName);

            if (!forceRedownload && File.Exists(cachePath))
            {
                try
                {
                    string cached = await File.ReadAllTextAsync(cachePath);
                    return JsonConvert.DeserializeObject<T>(cached);
                }
                catch { /* 캐시 파싱 실패 시 재다운 */ }
            }

            string url = UrlUtil.ResolveSource(sourceUri);
            if (EnableCacheBust && UrlUtil.LooksLikeHttp(url))
            {
                url += (url.Contains("?") ? "&" : "?") + "ts=" + System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }

            using var req = UnityWebRequest.Get(url);
            req.SetRequestHeader("Cache-Control", "no-cache, no-store, must-revalidate");
            req.SetRequestHeader("Pragma", "no-cache");
            req.SetRequestHeader("Expires", "0");

            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();

#if UNITY_2020_2_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogError($"JSON 다운로드 실패: {url} | {req.error}");
                return null;
            }

            string jsonText = req.downloadHandler.text;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(cachePath));
                await File.WriteAllTextAsync(cachePath, jsonText);
            }
            catch { /* 캐시 저장 실패는 치명적 아님 */ }

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
    }
}