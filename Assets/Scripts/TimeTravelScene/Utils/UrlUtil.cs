using UnityEngine;

namespace VRContent.Utils
{
    public static class UrlUtil
    {
        public static bool LooksLikeHttp(string s) =>
            !string.IsNullOrEmpty(s) && (s.StartsWith("http://") || s.StartsWith("https://"));

        // URL 결합 전용 (Path.Combine 사용 금지)
        public static string CombineUrl(string baseUrl, string segment)
        {
            if (string.IsNullOrEmpty(baseUrl)) return segment ?? "";
            if (string.IsNullOrEmpty(segment)) return baseUrl;
            if (baseUrl.EndsWith("/")) baseUrl = baseUrl.TrimEnd('/');
            segment = segment.TrimStart('/');
            return $"{baseUrl}/{segment}";
        }

        // HTTP가 아니면 StreamingAssets 상대경로로 변환
        public static string ResolveSource(string maybeUrlOrStreamingAssetsRelative)
        {
            if (LooksLikeHttp(maybeUrlOrStreamingAssetsRelative)) return maybeUrlOrStreamingAssetsRelative;
            return CombineUrl(Application.streamingAssetsPath, maybeUrlOrStreamingAssetsRelative);
        }
    }
}