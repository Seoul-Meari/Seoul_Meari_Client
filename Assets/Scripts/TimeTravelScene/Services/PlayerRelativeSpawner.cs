// PlayerRelativeSpawner.cs (핵심 수정 포함)
using UnityEngine;
using System.Collections;
using CesiumForUnity;
using TMPro;
using Unity.Mathematics;

public class PlayerRelativeSpawner : MonoBehaviour
{
    [Header("Georeference")]
    [SerializeField] private CesiumGeoreference cesiumGeoreference;

    [Header("플레이어/카메라 설정")]
    public Transform playerTransform;

    [Header("생성 설정")]
    public GameObject cubePrefab;
    public int totalCubes = 500;
    public int cubesPerFrame = 50;

    [Header("GPS 기준 설정")]
    public double heightOffset = 20.0;
    public double step = 0.0001;

    [Header("디버그 설정")]
    public float logInterval = 5.0f;
    private float logTimer = 0.0f;

    private CesiumGlobeAnchor playerAnchor;
    private GameObject cubeContainer;

    private Vector2 currentGpsPosition;
    private float currentAltitude;
    private bool isGpsReady = false;
    private bool hasSetInitialOrigin = false;

    IEnumerator Start()
    {
        if (playerTransform == null)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("[Spawner] Camera.main 이 없습니다. playerTransform을 인스펙터에 지정하세요.");
                yield break;
            }
            playerTransform = cam.transform;
        }

        // 플레이어 앵커 보장
        playerAnchor = playerTransform.GetComponent<CesiumGlobeAnchor>();
        if (playerAnchor == null) playerAnchor = playerTransform.gameObject.AddComponent<CesiumGlobeAnchor>();

        // GpsService 준비 대기
        yield return new WaitUntil(() => GpsService.Instance != null);

        // 중복 구독 방지용 안전장치
        GpsService.Instance.OnLocationUpdated.RemoveListener(HandleLocationUpdate);
        GpsService.Instance.OnLocationUpdated.AddListener(HandleLocationUpdate);
    }

    private void OnDisable()
    {
        if (GpsService.Instance != null)
            GpsService.Instance.OnLocationUpdated.RemoveListener(HandleLocationUpdate);
    }

    private void HandleLocationUpdate(Vector2 newPosition)
    {
        isGpsReady = true;
        currentGpsPosition = newPosition;

        // 고도 값 방어 (권한/서비스에 따라 0/NaN일 수 있음)
        if (Input.location.status == LocationServiceStatus.Running)
            currentAltitude = Input.location.lastData.altitude;
        else
            currentAltitude = 0f;

        // 첫 위치 수신 시 큐브 생성 시작
        if (!hasSetInitialOrigin)
        {
            hasSetInitialOrigin = true;

            if (cubeContainer != null) Destroy(cubeContainer);
            StartCoroutine(SpawnCubesAroundPlayer());
        }

        // 플레이어 앵커 갱신 (lon,lat,height 순서 주의)
        if (playerAnchor != null)
            playerAnchor.longitudeLatitudeHeight = new double3(currentGpsPosition.y, currentGpsPosition.x, currentAltitude);
    }

    void Update()
    {
        if (!isGpsReady) return;

        logTimer += Time.deltaTime;
        if (logTimer >= logInterval)
        {
            Vector3 worldPosition = playerTransform.position;
            Debug.Log(
                $"GPS -> 위도: {currentGpsPosition.x:F5}, 경도: {currentGpsPosition.y:F5}, 고도: {currentAltitude:F1} m" +
                $" | Unity 월드 -> X: {worldPosition.x:F1}, Y: {worldPosition.y:F1}, Z: {worldPosition.z:F1}"
            );
            logTimer = 0.0f;
        }
    }

    private IEnumerator SpawnCubesAroundPlayer()
    {
        Debug.Log($"'{currentGpsPosition.x:F5}, {currentGpsPosition.y:F5}' 위치를 기준으로 큐브 생성을 시작합니다.");
        cubeContainer = new GameObject($"Spawned_Near_Player_{Time.time:F0}");

        if (cesiumGeoreference != null)
            cubeContainer.transform.SetParent(cesiumGeoreference.transform, false);
        else
            Debug.LogWarning("[Spawner] cesiumGeoreference가 null입니다. cubeContainer를 루트에 배치합니다.");

        if (cubePrefab == null)
        {
            Debug.LogError("[Spawner] cubePrefab이 비어있습니다.");
            yield break;
        }

        int gridSize = (int)Mathf.Ceil(Mathf.Sqrt(totalCubes));
        int spawnedCount = 0;

        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                if (spawnedCount >= totalCubes) break;

                double latOffset = (i - gridSize / 2.0) * step;
                double lonOffset = (j - gridSize / 2.0) * step;
                double targetLat = currentGpsPosition.x + latOffset;
                double targetLon = currentGpsPosition.y + lonOffset;
                double targetHeight = currentAltitude + heightOffset;

                // 인스턴스 생성
                GameObject newCube = Instantiate(cubePrefab, Vector3.zero, Quaternion.identity);
                newCube.transform.SetParent(cubeContainer.transform, false);

                // Cesium 앵커 지정 (lon,lat,height)
                var globeAnchor = newCube.GetComponent<CesiumGlobeAnchor>();
                if (globeAnchor == null) globeAnchor = newCube.AddComponent<CesiumGlobeAnchor>();
                globeAnchor.longitudeLatitudeHeight = new double3(targetLon, targetLat, targetHeight);

                // 좌표 텍스트 표시 (UI용 TextMeshProUGUI 대신 3D TextMeshPro를 썼다면 타입 맞추기)
                var coordTextUI = newCube.GetComponentInChildren<TextMeshProUGUI>();
                if (coordTextUI != null)
                {
                    coordTextUI.text = $"Lat: {targetLat:F5}\nLon: {targetLon:F5}";
                }
                else
                {
                    var coordText = newCube.GetComponentInChildren<TextMeshPro>();
                    if (coordText != null)
                        coordText.text = $"Lat: {targetLat:F5}\nLon: {targetLon:F5}";
                }

                spawnedCount++;

                if (spawnedCount % cubesPerFrame == 0)
                    yield return null; // 프레임 분산
            }
            if (spawnedCount >= totalCubes) break;
        }

        Debug.Log($"[완료] 플레이어 주변에 {spawnedCount}개의 큐브를 배치했습니다.");
    }
}