using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;

public class GenerateBundleMetadata
{
    // JSON 구조에 맞춘 C# 클래스들 (직렬화를 위해 public이어야 함)
    [System.Serializable]
    public class PrefabDefinition
    {
        public string id;
        public string name;
        public float sizeMB; // 참고용이며, 실제 기여도를 정확히 계산하기는 어려움
        public string[] tags;
    }

    [System.Serializable]
    public class PrefabPlacementGroup
    {
        // 이 데이터는 프리팹 자체에 없어 별도로 생성해야 하므로 여기서는 비워둡니다.
        // 예시: public string groupId;
    }

    [System.Serializable]
    public class AssetBundleData
    {
        public string bundleId;
        public string bundleUrl;
        public List<PrefabDefinition> prefabs;
        public List<PrefabPlacementGroup> placementGroups; // 이 스크립트에서는 빈 리스트로 생성
        public string name;
        public string version;
        public string status;
        public string usage;
        public string os;
        public string updatedAt;
        public float totalSizeMB;
        public string[] tags;
        public string description;
    }

    public static void Generate(AssetBundleManifest manifest, string bundleDirectory)
    {
        Debug.Log("메타데이터 JSON 파일 생성을 시작합니다...");

        string[] allBundles = manifest.GetAllAssetBundles();

        foreach (string bundleName in allBundles)
        {
            // --- 1. 기본 정보 채우기 (필요에 따라 수정하세요) ---
            var data = new AssetBundleData
            {
                bundleId = Path.GetFileNameWithoutExtension(bundleName),
                name = "My Bundle: " + Path.GetFileNameWithoutExtension(bundleName), // 예시 이름
                version = "1.0.0",
                status = "draft",
                usage = "both",
                os = EditorUserBuildSettings.activeBuildTarget.ToString().ToLower(),
                updatedAt = DateTime.UtcNow.ToString("o"), // ISO 8601 형식
                tags = new string[] { "generated", "sample" },
                description = "자동으로 생성된 에셋 번들 메타데이터입니다.",
                prefabs = new List<PrefabDefinition>(),
                placementGroups = new List<PrefabPlacementGroup>() // 배치 정보는 별도 시스템에서 관리해야 함
            };

            // --- 2. 번들 파일 정보 가져오기 ---
            string bundlePath = Path.Combine(bundleDirectory, bundleName);
            FileInfo fileInfo = new FileInfo(bundlePath);
            if (fileInfo.Exists)
            {
                data.totalSizeMB = (float)Math.Round(fileInfo.Length / (1024.0 * 1024.0), 2);
            }
            // URL은 실제 서버 주소에 맞게 수정해야 합니다.
            data.bundleUrl = $"https://cdn.example.com/bundles/{data.os}/{bundleName}";

            // --- 3. 번들 안의 프리팹 정보 가져오기 ---
            string[] assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName);
            int prefabCounter = 0;
            foreach (string path in assetPaths)
            {
                if (path.EndsWith(".prefab"))
                {
                    prefabCounter++;
                    GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    
                    var prefabDef = new PrefabDefinition
                    {
                        id = $"pfb-{prefabCounter:D3}", // pfb-001 형식
                        name = Path.GetFileNameWithoutExtension(path),
                        sizeMB = 0.0f, // 개별 프리팹 사이즈는 계산이 복잡하므로 0으로 둠
                        tags = AssetDatabase.GetLabels(prefabAsset)
                    };
                    data.prefabs.Add(prefabDef);
                }
            }
            
            // --- 4. JSON 파일로 저장 ---
            string json = JsonUtility.ToJson(data, true); // 'true'는 예쁘게 들여쓰기
            string jsonPath = Path.ChangeExtension(bundlePath, ".json");
            File.WriteAllText(jsonPath, json);
            Debug.Log($"JSON 생성 완료: {jsonPath}");
        }
    }
}
