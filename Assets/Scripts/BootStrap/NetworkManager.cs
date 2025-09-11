using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    private static NetworkManager _instance;
    public static NetworkManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<NetworkManager>();
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject(nameof(NetworkManager));
                    _instance = singletonObject.AddComponent<NetworkManager>();
                }
            }
            return _instance;
        }
    }

    // --- REST API ---
    [SerializeField] private string healthCheckEndpoint = "http://192.168.0.14:3000/health"; // NestJS Health Check 주소

    // --- State ---
    /// <summary>
    /// 서버가 준비되었는지 여부
    /// </summary>
    public bool IsServerReady { get; private set; } = false;

    private void Awake()
    {
        // --- Singleton Pattern ---
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 서버 상태를 한 번 확인하는 코루틴입니다.
    /// Bootstrapper에서 이 함수를 반복적으로 호출하게 됩니다.
    /// </summary>
    public IEnumerator CheckServerStatus()
    {
        // UnityWebRequest를 사용하여 GET 요청을 보냅니다.
        using (UnityWebRequest webRequest = UnityWebRequest.Get(healthCheckEndpoint))
        {
            yield return webRequest.SendWebRequest();

            // 요청 결과 확인
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                // 성공적으로 200 OK 응답을 받으면 서버가 준비된 것입니다.
                Debug.Log("Server health check successful. Server is ready.");
                IsServerReady = true;
            }
            else
            {
                // 통신에 실패하면 아직 준비되지 않은 것입니다.
                Debug.LogWarning("Server health check failed: " + webRequest.error);
                IsServerReady = false;
            }
        }
    }
}

