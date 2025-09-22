using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public class AssetDownloadController : MonoBehaviour
{
    private void OnEnable() => AssetItem.OnAnyClick += ExecuteDownloadPipeline;
    private void OnDisable() => AssetItem.OnAnyClick -= ExecuteDownloadPipeline;

    void OnDestroy()
    {
        AssetItem.OnAnyClick -= ExecuteDownloadPipeline;
    }

    NetworkManager.BundleClientSigned _assetBundleData;
    public void ExecuteDownloadPipeline(string id)
    {
        Debug.Log("execute work");
        NetworkManager.Instance.DownLoadAsset(
            id,
            onSuccess: item =>
            {
                _assetBundleData = item;
                StartCoroutine(DownloadAllRoutine(_assetBundleData));
            },
            onError: e => { }
        );
    }

    private IEnumerator DownloadAllRoutine(NetworkManager.BundleClientSigned data)
    {
        if (data == null || data.layoutJson == null)
        {
            Debug.LogError("Invalid bundle data.");
            yield break;
        }

        // 1) 대상 폴더 준비
        string safeFolderName = SanitizeFileName(data.layoutJson.name ?? "Bundle");
        string baseDir = Path.Combine(Application.persistentDataPath, "Bundles", safeFolderName);
        try
        {
            Directory.CreateDirectory(baseDir);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to create dir: {baseDir}\n{ex}");
            yield break;
        }

        // 2) LayoutRoot.json 저장
        string layoutJsonPath = Path.Combine(baseDir, "LayoutRoot.json");
        try
        {
            var json = JsonConvert.SerializeObject(data.layoutJson, Formatting.Indented);
            File.WriteAllText(layoutJsonPath, json);
            Debug.Log($"Saved layout to: {layoutJsonPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to save LayoutRoot.json: {ex}");
            // 계속 진행할지 중단할지는 정책에 따라; 여기선 계속
        }
    }
        private static string SanitizeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }
}


