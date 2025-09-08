using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class MessageManager : MonoBehaviour
{
    [Header("AR & GPS Settings")]
    public GameObject messagePrefab;
    [Tooltip("메시지가 보이기 시작하는 거리 (미터 단위)")]
    public float viewingDistance = 20.0f; // 20미터 반경

    // --- 데이터베이스 역할을 하는 전체 메시지 목록 ---
    // 실제 앱에서는 이 목록을 서버에서 받아오게 됩니다.
    private List<MessageData> allMessages = new List<MessageData>();

    // 현재 화면에 보여지고 있는 메시지들을 관리하는 딕셔너리
    // Key: MessageData, Value: 생성된 GameObject
    private Dictionary<MessageData, GameObject> activeMessages = new Dictionary<MessageData, GameObject>();

    private bool isGpsInitialized = false;

    public TMP_Text gpsShow;
    // --- GPS 보정을 위한 변수 추가 ---
    [Tooltip("GPS 보정 강도입니다. 0에 가까울수록 부드러워지지만 반응이 느려집니다. (추천값: 0.1)")]
    public float smoothingFactor = 0.1f;
    private Vector2 smoothedPosition; // 부드럽게 보정된 2D 위치 (위도, 경도)
    private bool isFirstUpdate = true; // 첫 GPS 업데이트인지 확인

    void Awake()
    {
        // 사용자님의 현재 위치를 기준으로 가까운 곳에 메시지를 배치하도록 좌표를 수정했습니다.
        // 이제 앱을 켜면 바로 주변에 메시지들이 보일 것입니다.
        double currentUserLat = 35.18713;
        double currentUserLon = 129.07420;

        // 1. 북쪽 약 11미터 지점
        allMessages.Add(new MessageData("북쪽 메시지", currentUserLat + 0.00001, currentUserLon, 1.5f));

        // 2. 동쪽 약 9미터 지점
        allMessages.Add(new MessageData("동쪽 메시지", currentUserLat, currentUserLon + 0.00001, 1.0f));

        // 3. 남서쪽 약 7미터 지점
        allMessages.Add(new MessageData("남서쪽 메시지", currentUserLat - 0.00002, currentUserLon - 0.00002, 0.5f));

        // 4. 조금 먼 곳 (약 30미터 북쪽) - 테스트용
        // 이 메시지는 viewingDistance를 30 이상으로 늘려야 보입니다.
        allMessages.Add(new MessageData("조금 먼 북쪽 메시지", currentUserLat + 0.00027, currentUserLon, 2.0f));

        int messageCount = 50;
        double step = 0.000001; // 위도/경도 약 0.5m 간격
        int gridSize = (int)Mathf.Ceil(Mathf.Sqrt(messageCount)); // √50 ≈ 7 → 7x7 격자

        int index = 1;
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                if (index > messageCount) break;

                double lat = currentUserLat + (i - gridSize / 2) * step;
                double lon = currentUserLon + (j - gridSize / 2) * step;

                string name = $"주변 메시지 {index}";
                float height = UnityEngine.Random.Range(0.5f, 2.0f); // 0.5~2m 랜덤 높이

                allMessages.Add(new MessageData(name, lat, lon, height));
                index++;
            }
        }
    }

    IEnumerator Start()
    {
        // 1. GPS 초기화 시작
        yield return StartCoroutine(InitializeGps());

        // 2. GPS가 준비되면, 주변 메시지를 주기적으로 업데이트하는 코루틴 시작
        if (isGpsInitialized)
        {
            StartCoroutine(UpdateVisibleMessagesCoroutine());
        }
    }

    // GPS를 켜고 현재 위치를 가져오는 코루틴
    private IEnumerator InitializeGps()
    {
        // 이 부분은 이전 코드와 동일합니다.
        if (!Input.location.isEnabledByUser)
        {
            Debug.LogError("User has not enabled GPS");
            yield break;
        }

        Input.location.Start();
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (maxWait < 1 || Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.LogError("GPS initialization failed.");
        }
        else
        {
            isGpsInitialized = true;
            Debug.Log($"GPS Initialized. Current Location: {Input.location.lastData.latitude}, {Input.location.lastData.longitude}");
        }
    }

    // 주기적으로 주변 메시지의 가시성을 업데이트하는 메인 코루틴
    private IEnumerator UpdateVisibleMessagesCoroutine()
    {
        while (true)
        {
            // 1. 현재 GPS의 실제(Raw) 위치를 가져옴
            LocationInfo currentUserLocation = Input.location.lastData;
            Vector2 currentRawPosition = new Vector2(currentUserLocation.latitude, currentUserLocation.longitude);
            gpsShow.text = $"X: {currentUserLocation.latitude} Y: {currentUserLocation.longitude}";

            // 2. GPS 위치 보정 (Smoothing)
            if (isFirstUpdate)
            {
                // 첫 업데이트일 경우, 보정 없이 현재 위치를 바로 사용
                smoothedPosition = currentRawPosition;
                isFirstUpdate = false;
            }
            else
            {
                // 이전 보정 위치와 현재 실제 위치 사이를 부드럽게 이동
                // Vector2.Lerp를 사용하여 간단한 이동 평균 필터를 구현
                smoothedPosition = Vector2.Lerp(smoothedPosition, currentRawPosition, smoothingFactor);
            }

            // --- 3. 보여줘야 할 메시지 스캔 (보정된 위치 기준) ---
            // 이제 모든 계산은 currentUserLocation 대신 보정된 smoothedPosition을 사용합니다.
            foreach (var messageData in allMessages)
            {
                float distance = CalculateDistance(smoothedPosition.x, smoothedPosition.y, messageData.latitude, messageData.longitude);

                if (distance <= viewingDistance && !activeMessages.ContainsKey(messageData))
                {
                    // SpawnMessage 함수에 실제 위치 정보(currentUserLocation)를 넘겨주는 것은 그대로 둡니다.
                    // 메시지 생성 위치 계산은 여전히 '순간적인' 실제 위치를 기준으로 하는 것이 더 정확할 수 있습니다.
                    SpawnMessage(messageData, currentUserLocation);
                }
            }

            // --- 4. 숨겨야 할 메시지 스캔 (보정된 위치 기준) ---
            List<MessageData> messagesToHide = new List<MessageData>();
            foreach (var activePair in activeMessages)
            {
                MessageData messageData = activePair.Key;
                float distance = CalculateDistance(smoothedPosition.x, smoothedPosition.y, messageData.latitude, messageData.longitude);

                if (distance > viewingDistance)
                {
                    messagesToHide.Add(messageData);
                }
            }
            
            foreach (var messageData in messagesToHide)
            {
                HideMessage(messageData);
            }

            // 1초마다 한번씩 체크
            yield return new WaitForSeconds(1.0f);
        }
    }

    // 메시지를 AR 공간에 생성하는 함수
    void SpawnMessage(MessageData data, LocationInfo currentUserLocation)
    {
        Debug.Log($"Spawning message: {data.content}");

        // 메시지의 GPS 좌표를 '현재 내 위치 기준'의 AR 월드 좌표로 변환
        Vector3 position = ConvertGpsToWorldPosition(data.latitude, data.longitude, currentUserLocation.latitude, currentUserLocation.longitude, data.z);

        // 생성
        GameObject messageObject = Instantiate(messagePrefab, position, Quaternion.identity);
        messageObject.GetComponent<MessageDisplay>().Setup(data);
        
        // 활성화 목록에 추가
        activeMessages.Add(data, messageObject);
    }

    // 메시지를 AR 공간에서 제거하는 함수
    void HideMessage(MessageData data)
    {
        Debug.Log($"Hiding message: {data.content}");
        if (activeMessages.TryGetValue(data, out GameObject messageObject))
        {
            Destroy(messageObject);
            activeMessages.Remove(data);
        }
    }

    // GPS 좌표를 '현재 사용자 위치' 기준의 로컬 AR 좌표로 변환
    Vector3 ConvertGpsToWorldPosition(double targetLat, double targetLon, double baseLat, double baseLon, float? z)
    {
        float worldX = (float)((targetLon - baseLon) * 89000f); // 경도 -> X축
        float worldZ = (float)((targetLat - baseLat) * 111000f); // 위도 -> Z축
        float worldY = z ?? 1.0f;

        // AR Session Origin을 기준으로 한 상대 위치 반환
        return new Vector3(worldX, worldY, worldZ);
    }

    // 두 GPS 좌표 사이의 거리를 미터 단위로 계산하는 간단한 함수
    // 두 GPS 좌표 사이의 거리를 미터 단위로 계산하는 함수 (오류 수정된 버전)
    float CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var R = 6371e3f; // 지구 반지름 (미터), f를 붙여 float 타입으로 지정
        var phi1 = (float)lat1 * Mathf.Deg2Rad;
        var phi2 = (float)lat2 * Mathf.Deg2Rad;
        var deltaPhi = (float)(lat2 - lat1) * Mathf.Deg2Rad;
        var deltaLambda = (float)(lon2 - lon1) * Mathf.Deg2Rad;

        var a = Mathf.Sin(deltaPhi / 2) * Mathf.Sin(deltaPhi / 2) +
                Mathf.Cos(phi1) * Mathf.Cos(phi2) *
                Mathf.Sin(deltaLambda / 2) * Mathf.Sin(deltaLambda / 2);
        var c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));

        return R * c; // R과 c 모두 float이므로 결과도 float
    }
}