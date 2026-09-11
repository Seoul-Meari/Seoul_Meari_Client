// HttpPresignedUrlProvider.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class HttpPresignedUrlProvider : IPresignedUrlProvider
{
    private readonly string baseUrl;              // 예: https://your-nest-server.com
    private readonly string analysisEndpoint;     // 예: /s3/presigned-urls/analysis
    private readonly Dictionary<string,string> extraHeaders;

    public HttpPresignedUrlProvider(string baseUrl, string analysisEndpoint = "/s3/presigned-urls/analysis",
                                    Dictionary<string,string> headers = null)
    {
        this.baseUrl = baseUrl.TrimEnd('/');
        this.analysisEndpoint = analysisEndpoint;
        this.extraHeaders = headers ?? new Dictionary<string,string>();
    }

    public IEnumerator GetAnalysisPresignedUrlsCoroutine(
        List<FileItem> files,
        Action<List<PresignedInfo>> onSuccess,
        Action<string> onError)
    {
        var reqObj = new PresignBulkRequest { files = files };
        var json = JsonUtility.ToJson(reqObj); // {"files":[{...},{...}]}

        var url = baseUrl + analysisEndpoint;
        var req = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        foreach (var kv in extraHeaders) req.SetRequestHeader(kv.Key, kv.Value);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(req.error + " / " + req.downloadHandler.text);
            yield break;
        }

        var txt = req.downloadHandler.text; // 서버 응답: [ { url,key }, ... ]
        // 래퍼 씌워서 파싱
        var wrapped = "{\"items\":" + txt + "}";
        var wrap = JsonUtility.FromJson<PresignedArrayWrapper>(wrapped);
        var list = new List<PresignedInfo>(wrap.items);
        onSuccess?.Invoke(list);
    }
}