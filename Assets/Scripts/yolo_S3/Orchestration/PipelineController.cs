using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PipelineController : MonoBehaviour
{
    [Header("Model / Detector (same GameObject)")]
    public ObjectDetector detector;    // 같은 오브젝트에 붙인 ObjectDetector 참조
    public string arSavedFolderRelative = "ARPhotos"; // AR팀 저장 폴더 (기본)

    [Header("Presign / Upload")]
    public string apiBaseUrl;                // 예: https://your-nest-server.com
    public string analysisEndpoint = "/s3/presigned-urls/analysis";
    [Tooltip("image/jpeg 권장")]
    public string defaultContentType = "image/jpeg";

    // (선택) 인증 토큰 등 헤더가 필요하면 여기서 세팅
    public string authHeaderKey;     // 예: "Authorization"
    public string authHeaderValue;   // 예: "Bearer xxx"

    // 내부 서비스
    PhotoRepository photoRepo;
    IPresignedUrlProvider presigner;
    S3Uploader uploader;
    InferenceBatcher batcher;

    public static PipelineController Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (detector == null) detector = GetComponent<ObjectDetector>();

        photoRepo = new PhotoRepository();

        var headers = new Dictionary<string,string>();
        if (!string.IsNullOrEmpty(authHeaderKey) && !string.IsNullOrEmpty(authHeaderValue))
            headers[authHeaderKey] = authHeaderValue;

        presigner = new HttpPresignedUrlProvider(apiBaseUrl, analysisEndpoint, headers);
        uploader  = new S3Uploader();
        batcher   = new InferenceBatcher(detector, photoRepo, presigner, uploader, defaultContentType);

        Debug.Log("[Pipeline] ready");
    }

    // AR 씬에서 종료 시점에 호출:
    public void OnArSessionEnded(string absoluteFolderPath = null)
    {
        string folder = absoluteFolderPath;
        if (string.IsNullOrEmpty(folder))
            folder = System.IO.Path.Combine(Application.persistentDataPath, arSavedFolderRelative);

        StartCoroutine(batcher.RunBatch(folder, OnBatchUploaded));
    }

    // presign 응답에서 받은 key들 전달 (FastAPI 등으로 넘길 때 사용)
    private void OnBatchUploaded(List<string> uploadedKeys)
    {
        Debug.Log($"[Pipeline] batch done, keys={uploadedKeys.Count}");
        // TODO: 필요 시 여기에 FastAPI로 키 전송 로직 추가
        // StartCoroutine(PostKeysToFastApi(uploadedKeys));
    }
}