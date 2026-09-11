using System.Collections;
using UnityEngine;

public class InitialLocation : MonoBehaviour
{
    public static InitialLocation Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private Vector3 initialPos;

    public IEnumerator SetInitialPos()
    {
        if (!Input.location.isEnabledByUser)
        {
            Debug.LogError("위치 서비스 권한 없음");
            yield break;
        }

        Input.location.Start();

        int maxWait = 10;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.LogError("GPS 수신 실패");
            yield break;
        }

        int loadGPS = 5;
        while (loadGPS > 0)
        {
            yield return new WaitForSeconds(1);
            loadGPS--;
        }


        var last = Input.location.lastData;
        initialPos = new Vector3((float)last.latitude, (float)last.longitude, 0);
        Input.location.Stop();
    }

    public Vector3 GetInitialPos()
    {
        return initialPos;
    }
}
