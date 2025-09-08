// GpsService.cs
using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using TMPro;

public class GpsService : MonoBehaviour
{
    [Tooltip("GPS 보정 강도입니다. (추천값: 0.1)")]
    public float smoothingFactor = 0.1f;

    public Vector2 CurrentPosition { get; private set; }
    public bool IsInitialized { get; private set; } = false;

    // 위치가 업데이트될 때마다 호출될 이벤트
    public UnityEvent<Vector2> OnLocationUpdated;
    public TMP_Text gpsText;

    private bool isFirstUpdate = true;
    
    void Start()
    {
        StartCoroutine(InitializeAndRunGps());
    }

    private IEnumerator InitializeAndRunGps()
    {
        // 1. GPS 초기화
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
            yield break;
        }

        IsInitialized = true;
        Debug.Log("GPS Initialized.");

        // 2. 초기화 성공 시, 주기적으로 위치 업데이트
        while (true)
        {
            UpdateLocation();
            yield return new WaitForSeconds(1.0f);
        }
    }

    private void UpdateLocation()
    {
        LocationInfo locationData = Input.location.lastData;
        Vector2 rawPosition = new Vector2(locationData.latitude, locationData.longitude);
        gpsText.text = $"X: {locationData.latitude} Y: {locationData.longitude}";

        if (isFirstUpdate)
        {
            CurrentPosition = rawPosition;
            isFirstUpdate = false;
        }
        else
        {
            CurrentPosition = Vector2.Lerp(CurrentPosition, rawPosition, smoothingFactor);
        }

        // 위치가 업데이트되었음을 이벤트 구독자들에게 알림
        OnLocationUpdated?.Invoke(CurrentPosition);
    }
    
    // 두 GPS 좌표 사이의 거리를 계산하는 유틸리티 함수
    public static float CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var R = 6371e3f; // 지구 반지름 (미터)
        var phi1 = (float)lat1 * Mathf.Deg2Rad;
        var phi2 = (float)lat2 * Mathf.Deg2Rad;
        var deltaPhi = (float)(lat2 - lat1) * Mathf.Deg2Rad;
        var deltaLambda = (float)(lon2 - lon1) * Mathf.Deg2Rad;

        var a = Mathf.Sin(deltaPhi / 2) * Mathf.Sin(deltaPhi / 2) +
                Mathf.Cos(phi1) * Mathf.Cos(phi2) *
                Mathf.Sin(deltaLambda / 2) * Mathf.Sin(deltaLambda / 2);
        var c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));

        return R * c;
    }
}