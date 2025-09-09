using System.Collections;
using UnityEngine;
using CesiumForUnity; // Cesium을 사용하기 위해 필요

public class DynamicOriginSetter : MonoBehaviour
{
    // 인스펙터 창에서 Cesium Georeference 객체를 연결해줄 변수
    public CesiumGeoreference georeference;
    public CesiumGlobeAnchor globeAnchor;

    void Start()
    {
        // 비동기로 GPS 위치를 가져오는 코루틴 실행
        StartCoroutine(SetOriginToGPS());
    }

    IEnumerator SetOriginToGPS()
    {
        // --- 1. 사용자 위치 정보 접근 권한 확인 ---
        if (!Input.location.isEnabledByUser)
        {
            Debug.LogError("사용자가 위치 정보 접근을 허용하지 않았습니다.");
            yield break; // 권한이 없으면 여기서 중단
        }

        // --- 2. 위치 서비스 시작 ---
        Input.location.Start();
        Debug.Log("위치 서비스 시작...");

        // --- 3. 위치 정보 초기화 대기 ---
        int maxWait = 20; // 최대 20초 대기
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1); // 1초 대기
            maxWait--;
        }

        // --- 4. 결과 확인 및 Origin 설정 ---
        if (maxWait < 1)
        {
            Debug.LogError("시간 초과: GPS 위치를 가져올 수 없습니다.");
            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.LogError("GPS 위치를 가져오는 데 실패했습니다.");
            yield break;
        }
        else
        {
            // 성공! 현재 위치 정보 가져오기
            LocationInfo locationData = Input.location.lastData;
            Debug.Log($"GPS 위치 획득 성공: {locationData.latitude}, {locationData.longitude}, {locationData.altitude}");

            // CesiumGeoreference의 Origin 값을 현재 GPS 좌표로 설정
            georeference.latitude = locationData.latitude;
            georeference.longitude = locationData.longitude;
            georeference.height = locationData.altitude;

            Debug.Log("Cesium Georeference Origin을 현재 위치로 설정 완료!");
        }

        // --- 5. 위치 서비스 중지 (배터리 절약) ---
        Input.location.Stop();
        Debug.Log("위치 서비스 중지.");
    }
}