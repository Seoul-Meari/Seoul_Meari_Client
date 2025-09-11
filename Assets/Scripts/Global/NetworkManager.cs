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
    private string healthCheckEndpoint = $"{ConfigProvider.BaseUrl}/health"; // NestJS Health Check 주소
    private string MessagesEndpoint => $"{ConfigProvider.BaseUrl}/echo"; // 메시지 전송 API 주소


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

            Debug.Log("check end point  " + healthCheckEndpoint);
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

    /// <summary>
    /// MessageData를 서버로 전송합니다.
    /// </summary>
    /// <param name="data">전송할 메시지 데이터</param>
    public void SendMessage(MessageData data)
    {
        StartCoroutine(PostMessageCoroutine(data));
    }

    private IEnumerator PostMessageCoroutine(MessageData data)
{
    // 1. C# 객체 → JSON 문자열로 변환
    //TODO Debug data.writer, data.content ...

    string json = JsonUtility.ToJson(data);

    // 2. 문자열을 바이트 배열로 변환
    byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

    Debug.Log(MessagesEndpoint);

    // 3. UnityWebRequest 설정
        using (UnityWebRequest webRequest = new UnityWebRequest(MessagesEndpoint, "POST"))
        {
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            // 4. 요청 전송
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Message sent successfully! Response: " + webRequest.downloadHandler.text);
            }
            else
            {
                Debug.LogError("Failed to send message: " + webRequest.error);
            }
        }
}
}

