using System.IO;
using UnityEngine;

[System.Serializable]
public class ConfigData { public string baseUrl = "http://localhost:3000"; }

public static class ConfigProvider
{
    private static ConfigData _cached;

    public static string BaseUrl
    {
        get
        {
            if (_cached == null) _cached = Load();
            return _cached.baseUrl;
        }
    }

    private static ConfigData Load()
    {
        // 1) OS 환경변수 (PC/서버 실행 시)
        var env = System.Environment.GetEnvironmentVariable("API_BASE_URL");
        if (!string.IsNullOrWhiteSpace(env))
            return new ConfigData { baseUrl = env };

        // 2) StreamingAssets 로컬 오버라이드 (개발자별)
        var localPath = Path.Combine(Application.streamingAssetsPath, "config.local.json");
#if UNITY_ANDROID && !UNITY_EDITOR
        // 안드로이드에선 StreamingAssets가 패키지 내부라 WWW/UnityWebRequest 필요.
        // 개발 중엔 에디터/PC가 대상일 테니 간단화: 필요하면 이후 보완.
#else
        if (File.Exists(localPath))
        {
            var json = File.ReadAllText(localPath);
            var data = JsonUtility.FromJson<ConfigData>(json);
            if (data != null && !string.IsNullOrWhiteSpace(data.baseUrl)) return data;
        }
#endif

        // 3) Resources 기본값 (공유)
        var text = Resources.Load<TextAsset>("config");
        if (text != null)
        {
            var data = JsonUtility.FromJson<ConfigData>(text.text);
            if (data != null) return data;
        }

        return new ConfigData(); // 최종 fallback
    }
}