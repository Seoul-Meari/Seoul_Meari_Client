using System.Collections;
using CesiumForUnity;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

public class InGameGpsTextGetter : MonoBehaviour
{
    [SerializeField] private TMP_Text gpsText;
    [SerializeField] private Camera player; // 혹은 Transform 으로 받아도 무방
    
    private CesiumGlobeAnchor playerTransform;
    
    private void Start()
    {
        if (player == null)
        {
            Debug.LogError("Player Camera가 할당되지 않았습니다!");
            return;
        }

        // Anchor 컴포넌트를 처음에 한 번만 찾아와서 저장합니다.
        playerTransform = player.GetComponent<CesiumGlobeAnchor>();

        if (playerTransform == null)
        {
            Debug.LogError("Player Camera에 CesiumGlobeAnchor 컴포넌트가 없습니다!");
            return;
        }

        StartCoroutine(OnInGameGpsUpdated());
    }

    private IEnumerator OnInGameGpsUpdated()
    {
        while (true)
        {
            if (playerTransform != null)
            {
                double3 playerGps = playerTransform.longitudeLatitudeHeight;
                gpsText.text = $"Lat: {playerGps.y:F6}\nLon: {playerGps.x:F6}";
            }
            yield return new WaitForSeconds(4);
        }
    }
}