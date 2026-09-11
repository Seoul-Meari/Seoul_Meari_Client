using System.Collections;
using System.Collections.Generic;
using CesiumForUnity;
using UnityEngine;

public class CeisumInitialSetting : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private CesiumGeoreference georeference;

    void Start()
    {
        StartCoroutine(setGeoreference());
    }

    private IEnumerator setGeoreference()
    {
        yield return new WaitUntil(() => GpsService.Instance != null && GpsService.Instance.IsInitialized);

        double lon = GpsService.Instance.CurrentPosition.y;
        double lat = GpsService.Instance.CurrentPosition.x;
        double h = GpsService.Instance.CurrentPosition.z;

        georeference.SetOriginLongitudeLatitudeHeight(lon, lat, h);
    }

}
