using System.IO;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class GlobalConfig { public string baseUrl = "http://localhost:3000"; }

public static class ConfigProvider
{
    private static GlobalConfig _cached;

    public static string BaseUrl
    {
        get
        {
            if (_cached == null) _cached = Load();
            return _cached.baseUrl;
        }
    }

    // ======================
    // OS 프로퍼티
    // ======================
    private static string _os;
    public static string Os
    {
        get
        {
            if (string.IsNullOrEmpty(_os))
                _os = LoadOs();
            return _os;
        }
    }

    private static string LoadOs()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.Android:
                return "android";
            case RuntimePlatform.IPhonePlayer:
                return "ios";
            default:
                return "unknown"; // 필요하다면 서버에서 무시하거나 처리
        }
    }

    // ======================
    // Config 로딩
    // ======================
    private static GlobalConfig Load()
    {
        // 1) OS 환경변수
        var env = System.Environment.GetEnvironmentVariable("API_BASE_URL");
        if (!string.IsNullOrWhiteSpace(env))
            return new GlobalConfig { baseUrl = env };

        // 2) StreamingAssets (개인 오버라이드)
        var localPath = Path.Combine(Application.streamingAssetsPath, "config.local.json");
#if UNITY_ANDROID && !UNITY_EDITOR
        // 안드로이드는 jar 내부라서 UnityWebRequest로 읽어야 함
        using (UnityWebRequest req = UnityWebRequest.Get(localPath))
        {
            var op = req.SendWebRequest();
            while (!op.isDone) { } // 간단 동기 대기 (초간단용; 필요시 코루틴으로 변경)
            if (req.result == UnityWebRequest.Result.Success && !string.IsNullOrEmpty(req.downloadHandler.text))
            {
                var dataA = JsonUtility.FromJson<GlobalConfig>(req.downloadHandler.text);
                if (dataA != null && !string.IsNullOrWhiteSpace(dataA.baseUrl)) return dataA;
            }
        }
#else
        if (File.Exists(localPath))
        {
            var json = File.ReadAllText(localPath);
            var dataB = JsonUtility.FromJson<GlobalConfig>(json);
            if (dataB != null && !string.IsNullOrWhiteSpace(dataB.baseUrl)) return dataB;
        }
#endif

        // 3) Resources 기본값
        var text = Resources.Load<TextAsset>("config");
        if (text != null)
        {
            var dataC = JsonUtility.FromJson<GlobalConfig>(text.text);
            if (dataC != null) return dataC;
        }

        // 4) 최종 fallback
        return new GlobalConfig();
    }
}