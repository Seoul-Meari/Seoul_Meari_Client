using UnityEditor;
using System.IO;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class BuildAssetBundles
{
    // --- 메뉴 항목 1: 프로젝트의 모든 번들 빌드 ---
    [MenuItem("Assets/Build AssetBundles/Build All")]
    static void BuildAllAssetBundles()
    {
        // ... 기존 코드 ...
        RemoveUnusedAssetBundleNames();

        string platformName = EditorUserBuildSettings.activeBuildTarget.ToString();
        string assetBundleDirectory = Path.Combine("Assets/AssetBundles", platformName);

        if (!Directory.Exists(assetBundleDirectory))
        {
            Directory.CreateDirectory(assetBundleDirectory);
        }

        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(assetBundleDirectory,
                                                                        BuildAssetBundleOptions.None,
                                                                        EditorUserBuildSettings.activeBuildTarget);
        if (manifest == null) {
            Debug.LogError("에셋 번들 빌드에 실패했습니다.");
            return;
        }

        Debug.Log($"전체 에셋 번들 빌드가 완료되었습니다! ({platformName}) 경로: {assetBundleDirectory}");
        
        // 빌드 완료 후 메타데이터 JSON 파일 생성
        GenerateBundleMetadata.Generate(manifest, assetBundleDirectory);

        // [추가된 기능] 빌드 후 생성된 .meta 파일들 정리
        CleanUpMetaFiles(assetBundleDirectory);
    }

    // --- 메뉴 항목 2: 선택한 에셋만 번들로 빌드 ---
    [MenuItem("Assets/Build AssetBundles/Build Selected")]
    static void BuildSelectedAssetBundles()
    {
        // ... 기존 코드 ...
        Object[] selectedAssets = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);

        if (selectedAssets.Length == 0)
        {
            Debug.LogWarning("아무 에셋도 선택되지 않았습니다. 프로젝트 창에서 번들로 만들 프리팹을 선택해주세요.");
            return;
        }
        
        var assetsByBundle = new Dictionary<string, List<string>>();
        foreach (var asset in selectedAssets)
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);
            AssetImporter importer = AssetImporter.GetAtPath(assetPath);

            if (importer == null || string.IsNullOrEmpty(importer.assetBundleName))
            {
                Debug.LogWarning($"선택된 에셋 '{asset.name}'에 에셋 번들 이름이 지정되지 않았습니다. 빌드에서 제외됩니다.", asset);
                continue;
            }

            string bundleName = importer.assetBundleName;
            if (!assetsByBundle.ContainsKey(bundleName))
            {
                assetsByBundle[bundleName] = new List<string>();
            }
            assetsByBundle[bundleName].Add(assetPath);
        }
        
        if (assetsByBundle.Count == 0)
        {
            Debug.LogError("선택된 에셋 중에 에셋 번들 이름이 지정된 것이 하나도 없습니다.");
            return;
        }

        AssetBundleBuild[] buildMap = new AssetBundleBuild[assetsByBundle.Count];
        int i = 0;
        foreach (var pair in assetsByBundle)
        {
            buildMap[i].assetBundleName = pair.Key;
            buildMap[i].assetNames = pair.Value.ToArray();
            i++;
        }

        string platformName = EditorUserBuildSettings.activeBuildTarget.ToString();
        string assetBundleDirectory = Path.Combine("Assets/AssetBundles", platformName);
        if (!Directory.Exists(assetBundleDirectory))
        {
            Directory.CreateDirectory(assetBundleDirectory);
        }

        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(assetBundleDirectory,
                                                                        buildMap,
                                                                        BuildAssetBundleOptions.None,
                                                                        EditorUserBuildSettings.activeBuildTarget);
        if (manifest == null)
        {
            Debug.LogError("선택된 에셋 번들 빌드에 실패했습니다.");
            return;
        }

        Debug.Log($"선택된 에셋 번들 빌드가 완료되었습니다! ({platformName}) 경로: {assetBundleDirectory}");

        // 빌드된 번들에 대해서만 메타데이터 JSON 파일 생성
        GenerateBundleMetadata.Generate(manifest, assetBundleDirectory);
        
        // [추가된 기능] 빌드 후 생성된 .meta 파일들 정리
        CleanUpMetaFiles(assetBundleDirectory);
    }
    
    // ... 기존 코드 ...
    [MenuItem("Assets/Build AssetBundles/Build Selected", true)]
    static bool ValidateBuildSelectedAssetBundles()
    {
        return Selection.GetFiltered(typeof(Object), SelectionMode.Assets).Length > 0;
    }

    // --- 메뉴 항목 3: 수동으로 정리하는 기능 ---
    [MenuItem("Assets/Build AssetBundles/Remove Unused Names")]
    static void RemoveUnusedAssetBundleNames()
    {
        AssetDatabase.RemoveUnusedAssetBundleNames();
        Debug.Log("사용하지 않는 에셋 번들 이름을 모두 제거했습니다.");
    }
    
    // --- [새로 추가된 메뉴 항목] ---
    [MenuItem("Assets/Build AssetBundles/Clean Meta Files")]
    static void CleanMetaFilesMenu()
    {
        string platformName = EditorUserBuildSettings.activeBuildTarget.ToString();
        string assetBundleDirectory = Path.Combine("Assets/AssetBundles", platformName);
        if (Directory.Exists(assetBundleDirectory))
        {
            CleanUpMetaFiles(assetBundleDirectory);
        }
        else
        {
            Debug.LogWarning("메타 파일을 정리할 빌드 폴더를 찾을 수 없습니다. 먼저 빌드를 실행해주세요.");
        }
    }

    // --- [새로 추가된 기능 함수] ---
    /// <summary>
    /// 지정된 디렉토리와 모든 하위 디렉토리에서 .meta 파일들을 삭제합니다.
    /// </summary>
    /// <param name="directory">정리할 최상위 디렉토리 경로</param>
    static void CleanUpMetaFiles(string directory)
    {
        if (!Directory.Exists(directory)) return;

        string[] metaFiles = Directory.GetFiles(directory, "*.meta", SearchOption.AllDirectories);
        foreach (string file in metaFiles)
        {
            try
            {
                File.Delete(file);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"{file} 파일 삭제 중 오류 발생: {e.Message}");
            }
        }
        
        if (metaFiles.Length > 0)
        {
            Debug.Log($"빌드 폴더에서 {metaFiles.Length}개의 .meta 파일을 삭제했습니다.");
        }
    }
}

