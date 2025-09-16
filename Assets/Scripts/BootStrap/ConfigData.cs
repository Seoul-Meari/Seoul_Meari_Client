using System.IO;
using UnityEngine;
using UnityEngine.Networking;

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
        // 1) OS 환경변수
        var env = System.Environment.GetEnvironmentVariable("API_BASE_URL");
        if (!string.IsNullOrWhiteSpace(env))
            return new ConfigData { baseUrl = env };

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
                var dataA = JsonUtility.FromJson<ConfigData>(req.downloadHandler.text);
                if (dataA != null && !string.IsNullOrWhiteSpace(dataA.baseUrl)) return dataA;
            }
        }
#else
        if (File.Exists(localPath))
        {
            var json = File.ReadAllText(localPath);
            var dataB = JsonUtility.FromJson<ConfigData>(json);
            if (dataB != null && !string.IsNullOrWhiteSpace(dataB.baseUrl)) return dataB;
        }
#endif

        // 3) Resources 기본값
        var text = Resources.Load<TextAsset>("config");
        if (text != null)
        {
            var dataC = JsonUtility.FromJson<ConfigData>(text.text);
            if (dataC != null) return dataC;
        }

        // 4) 최종 fallback
        return new ConfigData();
    }
}