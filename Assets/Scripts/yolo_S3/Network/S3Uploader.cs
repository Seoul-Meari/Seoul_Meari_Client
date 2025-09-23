using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class S3Uploader
{
    public IEnumerator UploadCoroutine(string presignedUrl, byte[] data, string contentType, Action<bool> onDone)
    {
        var req = new UnityWebRequest(presignedUrl, UnityWebRequest.kHttpVerbPUT);
        req.uploadHandler = new UploadHandlerRaw(data);
        req.downloadHandler = new DownloadHandlerBuffer();
        if (!string.IsNullOrEmpty(contentType))
            req.SetRequestHeader("Content-Type", contentType);

        yield return req.SendWebRequest();
        bool ok = (req.result == UnityWebRequest.Result.Success);
        if (!ok) Debug.LogWarning("[S3] upload fail: " + req.error + " / " + req.downloadHandler.text);
        onDone?.Invoke(ok);
    }
}