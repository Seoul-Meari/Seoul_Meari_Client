using System.Collections;
using UnityEngine;
using CesiumForUnity;
using Unity.Mathematics;

public class InitialGpsSetter : MonoBehaviour
{
    [Header("세슘 지오레퍼런스")]
    public CesiumGeoreference georeference;

    [Header("플레이어 앵커")]
    public Camera playerCamera;
    private CesiumGlobeAnchor playerAnchor;

    IEnumerator Start()
    {
        // --- 1. 권한 확인 ---
        if (!Input.location.isEnabledByUser)
        {
            Debug.LogError("사용자가 위치 서비스 권한을 허용하지 않았습니다.");
            yield break;
        }

        // --- 2. 위치 서비스 시작 ---
        Input.location.Start();

        // --- 3. 초기화 대기 (최대 20초까지) ---
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (maxWait < 1)
        {
            Debug.LogError("GPS 초기화 시간 초과");
            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.LogError("GPS 위치를 가져올 수 없습니다.");
            yield break;
        }

        // --- 4. GPS 데이터 가져오기 ---
        LocationInfo loc = Input.location.lastData;
        double latitude = loc.latitude;
        double longitude = loc.longitude;
        double height = 1;

        Debug.Log($"초기 GPS 위치: Lat {latitude}, Lon {longitude}, H {height}");

        playerAnchor = playerCamera.GetComponent<CesiumGlobeAnchor>();
        playerAnchor.longitudeLatitudeHeight = new double3(longitude, latitude, height);

        // --- 6. 위치 서비스 정지 (한 번만 쓸 거니까) ---
        Input.location.Stop();
    }
}