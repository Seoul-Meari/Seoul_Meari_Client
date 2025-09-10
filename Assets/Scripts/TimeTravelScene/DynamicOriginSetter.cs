using System.Collections;
using UnityEngine;
using CesiumForUnity;
using Unity.Mathematics; // double3

public class DynamicOriginSetter : MonoBehaviour
{
    [Header("Assign via Inspector")]
    public CesiumGeoreference georeference; // 씬에 있는 Georeference
    public CesiumGlobeAnchor globeAnchor;   // 선택: 따라 움직일 오브젝트(없으면 생략 가능)

    [Tooltip("GpsService에 고도(altitude)가 없으면 이 높이를 사용")]
    public double defaultHeight = 0.0;

    private bool _originSet = false;

    private void Start()
    {
        StartCoroutine(InitAndHook());
    }

    private IEnumerator InitAndHook()
    {
        // 1) GpsService 인스턴스 대기
        yield return new WaitUntil(() => GpsService.Instance != null);

        // 2) 초기화 완료 대기
        if (!GpsService.Instance.IsInitialized)
        {
            // GpsService에 OnInitialized 이벤트가 있다면 구독
            // 없다면 IsInitialized polling
            // 아래는 polling 예시:
            while (!GpsService.Instance.IsInitialized)
                yield return null;
        }

        // 3) 현재 값으로 즉시 한 번 Origin/Anchor 세팅
        ApplyGeorefOnce(GpsService.Instance.CurrentPosition);

        // 4) 이후 위치 갱신은 Anchor만(선택)
        GpsService.Instance.OnLocationUpdated.AddListener(OnGpsUpdated);
    }

    private void OnDestroy()
    {
        if (GpsService.Instance != null)
            GpsService.Instance.OnLocationUpdated.RemoveListener(OnGpsUpdated);
    }

    private void ApplyGeorefOnce(Vector2 latLon)
    {
        if (georeference != null && !_originSet)
        {
            georeference.latitude  = latLon.x;
            georeference.longitude = latLon.y;
            georeference.height    = defaultHeight; // GpsService가 고도를 안 주니 기본값 사용
            _originSet = true;
            Debug.Log($"[Cesium] Origin set to lat:{latLon.x}, lon:{latLon.y}, h:{defaultHeight}");
        }

        // 첫 프레임에 globeAnchor 위치도 맞춰두기(있을 때만)
        if (globeAnchor != null)
        {
            globeAnchor.longitudeLatitudeHeight = new double3(
                georeference.longitude,
                georeference.latitude,
                georeference.height
            );
        }
    }

    private void OnGpsUpdated(Vector2 latLon)
    {
        // Origin은 빈번히 바꾸지 마세요(타일 재배치 비용 큼)
        // 대신 Anchor(오브젝트)만 이동시키는 걸 권장
        if (globeAnchor != null)
        {
            // 고도는 알 수 없으니 defaultHeight 사용(필요시 GpsService 확장해서 altitude 전달)
            globeAnchor.longitudeLatitudeHeight = new double3(latLon.y, latLon.x, defaultHeight);
        }
    }
}