using System.Collections;
using TMPro;
using UnityEngine;

public class GpsTextGetter : MonoBehaviour
{
    [SerializeField] private TMP_Text gpsText;

    private void Start()
    {
        // 인스턴스가 이미 떠 있으면 바로 구독
        if (GpsService.Instance != null)
        {
            Vector3 pos = GpsService.Instance.CurrentPosition;
            OnLocationUpdated(pos);
            Subscribe();
        }
        else
        {
            // 아직 생성 전이면 기다렸다가 구독
            StartCoroutine(WaitAndSubscribe());
        }
    }

    private IEnumerator WaitAndSubscribe()
    {
        yield return new WaitUntil(() => GpsService.Instance != null);
        Subscribe();
    }

    private void Subscribe()
    {
        // 초기 상태 표시
        if (!GpsService.Instance.IsInitialized)
            gpsText.text = "GPS 초기화 중…";

        // 이벤트 구독
        GpsService.Instance.OnLocationUpdated.AddListener(OnLocationUpdated);

        // 이미 초기화되어 있고 첫 값이 있었다면 현재값으로 즉시 갱신(선택)
        if (GpsService.Instance.IsInitialized)
        {
            Vector3 pos = GpsService.Instance.CurrentPosition;
            OnLocationUpdated(pos);
        }
    }

    private void OnDestroy()
    {
        if (GpsService.Instance != null)
            GpsService.Instance.OnLocationUpdated.RemoveListener(OnLocationUpdated);
    }

    private void OnLocationUpdated(Vector3 pos)
    {
        // 문자열 할당 최소화: 포맷 지정
        gpsText.text = $"Lat: {pos.x:F6}\nLon: {pos.y:F6}";
    }
}