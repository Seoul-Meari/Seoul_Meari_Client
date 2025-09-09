using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using TMPro;

public class GpsService : MonoBehaviour
{
    public float smoothingFactor = 0.1f;
    public Vector2 CurrentPosition { get; private set; }
    public bool IsInitialized { get; private set; } = false;
    public UnityEvent<Vector2> OnLocationUpdated;
    private bool isFirstUpdate = true;
    public static GpsService Instance { get; private set; } // 싱글톤으로 어디서든 접근
    public TMP_Text gpsText;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }
    
    void Start()
    {
        StartCoroutine(InitializeAndRunGps());
    }

    private IEnumerator InitializeAndRunGps()
    {
        if (!Input.location.isEnabledByUser) { yield break; }
        Input.location.Start();
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (maxWait < 1 || Input.location.status == LocationServiceStatus.Failed) { yield break; }
        
        IsInitialized = true;
        while (true)
        {
            UpdateLocation();
            // 이 부분의 대기 시간을 1초에서 5초로 변경했습니다. 
            // 혹시 움직이다가 갑자기 렉걸리는 현상이 잦다면 이 부분 바꾸기
            // (한 번에 렌더링 vs 조금씩 렌더링)
            yield return new WaitForSeconds(5.0f);
        }
    }

    private void UpdateLocation()
    {
        Vector2 rawPosition = new Vector2(Input.location.lastData.latitude, Input.location.lastData.longitude);
        CurrentPosition = isFirstUpdate ? rawPosition : Vector2.Lerp(CurrentPosition, rawPosition, smoothingFactor);
        gpsText.text = $"X: {CurrentPosition.x} Y: {CurrentPosition.y}";
        isFirstUpdate = false;
        OnLocationUpdated?.Invoke(CurrentPosition);
    }
    
    public static float CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var R = 6371e3f;
        var phi1 = (float)lat1 * Mathf.Deg2Rad;
        var phi2 = (float)lat2 * Mathf.Deg2Rad;
        var deltaPhi = (float)(lat2 - lat1) * Mathf.Deg2Rad;
        var deltaLambda = (float)(lon2 - lon1) * Mathf.Deg2Rad;
        var a = Mathf.Sin(deltaPhi / 2) * Mathf.Sin(deltaPhi / 2) + Mathf.Cos(phi1) * Mathf.Cos(phi2) * Mathf.Sin(deltaLambda / 2) * Mathf.Sin(deltaLambda / 2);
        var c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));
        return R * c;
    }
}