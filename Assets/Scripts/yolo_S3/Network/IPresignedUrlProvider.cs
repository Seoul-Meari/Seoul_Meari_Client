// IPresignedUrlProvider.cs
using System;
using System.Collections;
using System.Collections.Generic;

public interface IPresignedUrlProvider
{
    IEnumerator GetAnalysisPresignedUrlsCoroutine(
        List<FileItem> files,
        Action<List<PresignedInfo>> onSuccess,
        Action<string> onError);
}

// 요청/응답 모델 (서버 명세에 정확히 맞춤)
[System.Serializable] public class FileItem { public string originalFilename; public string objectName; public string contentType; }
[System.Serializable] public class PresignBulkRequest { public List<FileItem> files; }

[System.Serializable] public class PresignedInfo { public string url; public string key; }
// JsonUtility가 루트 배열 파싱 못하므로 래퍼 사용
[System.Serializable] class PresignedArrayWrapper { public PresignedInfo[] items; }