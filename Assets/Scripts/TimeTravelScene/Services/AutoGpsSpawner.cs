// AutoGpsSpawner.cs
using UnityEngine;
using System.Collections.Generic;
using CesiumForUnity;
using TMPro;

/// <summary>
/// GPS 좌표 데이터를 보관하기 위한 간단한 클래스입니다.
/// </summary>
[System.Serializable]
public class GpsPointData
{
    public double latitude;
    public double longitude;
    public double height;

    public GpsPointData(double lat, double lon, double h)
    {
        latitude = lat;
        longitude = lon;
        height = h;
    }
}

/// <summary>
/// 씬이 시작되면 자동으로 GPS 데이터 생성부터
/// Cesium 월드에 실제 오브젝트를 배치하는 것까지 모두 수행합니다.
/// </summary>
public class AutoGpsSpawner : MonoBehaviour
{
    [Header("생성 설정")]
    public GameObject cubePrefab;
    public int totalCubes = 1000;

    [Header("GPS 기준 설정")]
    public double centerLatitude = 35.1796;  // 부산시청 위도
    public double centerLongitude = 129.0756; // 부산시청 경도
    public double baseHeight = 50.0;
    public double step = 0.0001; // 약 10m 간격

    void Start()
    {
        // 씬이 시작되면 데이터 생성과 오브젝트 배치를 순차적으로 바로 실행합니다.
        GenerateAndSpawnObjects();
    }

    /// <summary>
    /// GPS 데이터 리스트 생성과 실제 오브젝트 배치를 모두 처리하는 메인 메서드입니다.
    /// </summary>
    private void GenerateAndSpawnObjects()
    {
        if (cubePrefab == null)
        {
            Debug.LogError("Cube Prefab이 할당되지 않았습니다! Inspector 창을 확인해주세요.");
            return;
        }

        // --- 1단계: GPS 데이터 리스트 생성 ---
        Debug.Log("[1단계 시작] GPS 좌표 데이터 생성을 시작합니다...");
        List<GpsPointData> gpsPointsList = new List<GpsPointData>();
        
        int gridSize = (int)Mathf.Ceil(Mathf.Sqrt(totalCubes));
        int pointCount = 0;

        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                if (pointCount >= totalCubes) break;

                double latOffset = (i - gridSize / 2.0) * step;
                double lonOffset = (j - gridSize / 2.0) * step;
                double targetLat = centerLatitude + latOffset;
                double targetLon = centerLongitude + lonOffset;

                gpsPointsList.Add(new GpsPointData(targetLat, targetLon, baseHeight));
                pointCount++;
            }
            if (pointCount >= totalCubes) break;
        }
        Debug.Log($"[1단계 완료] {gpsPointsList.Count}개의 GPS 데이터가 메모리에 준비되었습니다.");

        // --- 2단계: 생성된 데이터를 기반으로 Cesium에 오브젝트 배치 ---
        Debug.Log("[2단계 시작] 실제 오브젝트 배치를 시작합니다...");
        GameObject cubeContainer = new GameObject("Auto_Spawned_Cubes");

        foreach (var pointData in gpsPointsList)
        {
            GameObject newCube = Instantiate(cubePrefab, Vector3.zero, Quaternion.identity);

            var globeAnchor = newCube.AddComponent<CesiumGlobeAnchor>();
            globeAnchor.longitudeLatitudeHeight = new Unity.Mathematics.double3(
                pointData.longitude,
                pointData.latitude,
                pointData.height
            );

            TextMeshProUGUI coordText = newCube.GetComponentInChildren<TextMeshProUGUI>();
            if (coordText != null)
            {
                coordText.text = $"Lat: {pointData.latitude:F5}\nLon: {pointData.longitude:F5}";
            }

            newCube.transform.SetParent(cubeContainer.transform, false);
        }
        
        Debug.Log($"[2단계 완료] {pointCount}개의 모든 오브젝트가 Cesium 월드에 배치되었습니다.");
    }
}