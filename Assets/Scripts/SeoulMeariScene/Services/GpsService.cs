using UnityEngine;
using System.Collections;
using UnityEngine.Events;

//해당 앱이 휴대폰에서 GPS 정보를 받아오기 위해 필요한 서비스
public class GpsService : MonoBehaviour
{
    public static GpsService Instance { get; private set; } // 싱글톤으로 어디서든 접근
    public Vector3 CurrentPosition { get; private set; }
    public UnityEvent<Vector3> OnLocationUpdated = new UnityEvent<Vector3>();
    [SerializeField]
    private GridStreamer gridStreamer;
    public bool IsInitialized { get; private set; } = false;
    private bool isFirstUpdate = true;
    private float gpsRenewTime = 5.0f; //초 단위
    private float smoothingFactor = 0.1f;

    public UnityEvent<Vector3Int, Vector3Int> OnIntLocationUpdated = new UnityEvent<Vector3Int, Vector3Int>();
    private Vector3Int currentIntPosition;
    private Vector3Int prevIntPosition;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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
            //GPS 지속적으로 갱신
            UpdateLocation();
            yield return new WaitForSeconds(gpsRenewTime);
        }
    }

    private void UpdateLocation()
    {
        Vector3 rawPosition = new Vector3(
            Input.location.lastData.latitude,
            Input.location.lastData.longitude,
            0 //Input.location.lastData.altitude
        );

        CurrentPosition = isFirstUpdate 
            ? rawPosition 
            : Vector3.Lerp(CurrentPosition, rawPosition, smoothingFactor);

        // 벤치마크 정수 좌표 계산 (Vector3Int 버전)
        currentIntPosition = Calculator.CalculateBenchMarkInt(CurrentPosition);

        isFirstUpdate = false;
        OnLocationUpdated?.Invoke(CurrentPosition);

        //이전 값이 없을 때(초기값)나 위치가 변했을 때 갱신
        if (gridStreamer.IsInitialized && (prevIntPosition == null || prevIntPosition != currentIntPosition))
        {
            if (prevIntPosition == null)
            {
                prevIntPosition = new Vector3Int(-currentIntPosition.x, -currentIntPosition.y, -currentIntPosition.z);
            }
            Vector3Int prev = prevIntPosition;
            OnIntLocationUpdated?.Invoke(prev, currentIntPosition);
            prevIntPosition = new Vector3Int(
                currentIntPosition.x,
                currentIntPosition.y,
                currentIntPosition.z
            );
        }
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