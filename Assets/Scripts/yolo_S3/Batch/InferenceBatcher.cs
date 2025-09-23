using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class InferenceBatcher
{
    private readonly ObjectDetector detector;
    private readonly PhotoRepository photos;
    private readonly IPresignedUrlProvider presigner;
    private readonly S3Uploader uploader;
    private readonly string contentTypeDefault;

    public InferenceBatcher(
        ObjectDetector detector,
        PhotoRepository photos,
        IPresignedUrlProvider presigner,
        S3Uploader uploader,
        string contentTypeDefault = "image/jpeg")
    {
        this.detector = detector;
        this.photos = photos;
        this.presigner = presigner;
        this.uploader = uploader;
        this.contentTypeDefault = contentTypeDefault;
    }

    // 배치 처리: 폴더 경로 하나만 받으면 끝까지(검출→프리사인→업로드→삭제)
    public IEnumerator RunBatch(string folderPath, Action<List<string>> onDone = null)
    {
        if (string.IsNullOrEmpty(folderPath))
        {
            Debug.LogWarning("[Batch] folderPath is null/empty");
            yield break;
        }

        var allFiles = photos.ListImages(folderPath);
        if (allFiles.Count == 0)
        {
            Debug.Log("[Batch] no images to process.");
            onDone?.Invoke(new List<string>());
            yield break;
        }

        Debug.Log($"[Batch] start. files={allFiles.Count}");

        // 1) 검출 결과 모으기 (미검출은 즉시 삭제)
        var candidates = new List<Candidate>(); // 업로드 대상
        foreach (var path in allFiles)
        {
            byte[] bytes = photos.ReadAllBytes(path);
            if (bytes == null || bytes.Length == 0)
            {
                Debug.LogWarning($"[Batch] failed to read: {path}");
                photos.SafeDelete(path);
                continue;
            }

            Texture2D tex = photos.LoadTexture(bytes);
            if (tex == null)
            {
                Debug.LogWarning($"[Batch] failed to decode texture: {path}");
                photos.SafeDelete(path);
                continue;
            }

            List<DetectionResult> dets = detector.RunOnce(tex);
            UnityEngine.Object.Destroy(tex);

            if (dets == null || dets.Count == 0)
            {
                // 업로드 스킵 + 로컬 삭제
                photos.SafeDelete(path);
                continue;
            }

            // 하나만 보내야 한다면 top-1(가장 높은 confidence) 채택
            var best = GetTop1(dets);
            string objectName = SanitizeName(best.ClassName);

            var contentType = photos.GuessContentTypeByExtension(path) ?? contentTypeDefault;
            candidates.Add(new Candidate
            {
                path = path,
                originalFilename = Path.GetFileName(path),
                objectName = objectName,
                bytes = bytes,
                contentType = contentType
            });

            // 프레임 잠깐 양보
            yield return null;
        }

        if (candidates.Count == 0)
        {
            Debug.Log("[Batch] no detections. done.");
            onDone?.Invoke(new List<string>());
            yield break;
        }

        Debug.Log($"[Batch] detections: {candidates.Count}/{allFiles.Count}");

        // 2) presigned URL 묶음 요청 (서버 명세: POST /s3/presigned-urls/analysis)
        var reqFiles = new List<FileItem>();
        foreach (var c in candidates)
            reqFiles.Add(new FileItem { originalFilename = c.originalFilename, objectName = c.objectName });

        List<PresignedInfo> presigned = null;
        string presignErr = null;
        yield return presigner.GetAnalysisPresignedUrlsCoroutine(
            reqFiles,
            onSuccess: list => presigned = list,
            onError:   err  => presignErr = err
        );

        if (presignErr != null)
        {
            Debug.LogError("[Batch] presign failed: " + presignErr);
            yield break;
        }
        if (presigned == null || presigned.Count != candidates.Count)
        {
            Debug.LogError($"[Batch] presign size mismatch. got={presigned?.Count}, expect={candidates.Count}");
            yield break;
        }

        // 3) 업로드 (병렬 대신 순차 코루틴; 필요시 병렬화 가능)
        var uploadedKeys = new List<string>();
        for (int i = 0; i < candidates.Count; i++)
        {
            var c = candidates[i];
            var p = presigned[i];

            bool ok = false;
            yield return uploader.UploadCoroutine(p.url, c.bytes, c.contentType, success => ok = success);

            if (ok)
            {
                uploadedKeys.Add(p.key);
                photos.SafeDelete(c.path);
            }
            else
            {
                Debug.LogWarning($"[Batch] upload failed: {c.path}");
                // 실패시 삭제 안함 (재시도 정책은 팀 합의에 따라)
            }

            // 프레임 양보
            yield return null;
        }

        Debug.Log($"[Batch] uploaded: {uploadedKeys.Count}/{candidates.Count}");
        onDone?.Invoke(uploadedKeys);
    }

    private static DetectionResult GetTop1(List<DetectionResult> dets)
    {
        int bestIdx = 0;
        float bestP = dets[0].Confidence;
        for (int i = 1; i < dets.Count; i++)
        {
            if (dets[i].Confidence > bestP) { bestP = dets[i].Confidence; bestIdx = i; }
        }
        return dets[bestIdx];
    }

    private static string SanitizeName(string s)
    {
        if (string.IsNullOrEmpty(s)) return "unknown";
        // 파일/키에 안전하도록 간단 정제
        s = s.Trim().ToLowerInvariant();
        foreach (var ch in Path.GetInvalidFileNameChars())
            s = s.Replace(ch.ToString(), "_");
        s = s.Replace(" ", "_");
        return s;
    }

    private struct Candidate
    {
        public string path;
        public string originalFilename;
        public string objectName;
        public byte[] bytes;
        public string contentType;
    }
}