using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Text;

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
    // private static string baseUrl = "http://54.153.21.98";
    private static string baseUrl = "http://192.168.0.14:3000";
    private string healthCheckEndpoint = $"{baseUrl}/health"; // NestJS Health Check 주소
    private string bucketEndpoint = $"{baseUrl}/s3";
    private string messagesEndpoint => $"{baseUrl}/echo"; // 메시지 전송 API 주소

    // --- DTOs ---
    [Serializable] private class PresignReq { public string filename; public string contentType; }
    [Serializable] private class PresignRes { public string url; public string key; }

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
        Debug.Log("Base Url: " + baseUrl /*ConfigProvider.BaseUrl*/);
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
        string imageKey;
        byte[] pngBytes = data.image.EncodeToPNG();
        string imageFilename = Guid.NewGuid().ToString() + ".png";
        string imageContentType = "image/png";



        // 1-1) presigned URL 발급
        var presignBody = JsonConvert.SerializeObject(new
        {
            filename = imageFilename,
            contentType = imageContentType
        });

        using (UnityWebRequest webRequest = new UnityWebRequest(bucketEndpoint + "/presigned-url/echo", "POST"))
        {
            webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(presignBody));
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[Echo] presign failed: " + webRequest.error + " / " + webRequest.downloadHandler.text);
                yield break;
            }

            var presignRes = JsonConvert.DeserializeObject<PresignRes>(webRequest.downloadHandler.text);
            imageKey = presignRes.key;

            // 1-2) S3 PUT 업로드 (Content-Type 반드시 일치)
            using (UnityWebRequest putRequest = new UnityWebRequest(presignRes.url, UnityWebRequest.kHttpVerbPUT))
            {
                putRequest.uploadHandler = new UploadHandlerRaw(pngBytes);
                putRequest.downloadHandler = new DownloadHandlerBuffer();
                putRequest.SetRequestHeader("Content-Type", imageContentType);
                yield return putRequest.SendWebRequest();

                if (putRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("[Echo] S3 PUT failed: " + putRequest.error + " / " + putRequest.downloadHandler.text);
                    yield break;
                }
            }
        }
        // 1. C# 객체 → JSON 문자열로 변환
        data.imageKey = imageKey;
        string json = JsonUtility.ToJson(data);

        // 2. 문자열을 바이트 배열로 변환
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);


        // 3. UnityWebRequest 설정
        using (UnityWebRequest webRequest = new UnityWebRequest($"{messagesEndpoint}", "POST"))
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

    /// <summary>
    /// 특정 위치 주변의 메시지를 서버에 요청합니다.
    /// </summary>
    /// <param name="position">중심 GPS 좌표 (x=latitude, y=longitude)</param>
    /// <param name="degree">검색 반경 (미터)</param>
    /// <param name="onSuccess">성공 시 호출될 콜백 (메시지 리스트 전달)</param>
    /// <param name="onError">실패 시 호출될 콜백 (에러 메시지 전달)</param>
    public void RequestMessagesNear(Vector3 position, float degree, Action<List<MessageData>> onSuccess, Action<string> onError)
    {
        StartCoroutine(GetMessagesCoroutine(position, degree, onSuccess, onError));
    }

    private IEnumerator GetMessagesCoroutine(Vector3 position, float degree, Action<List<MessageData>> onSuccess, Action<string> onError)
    {
        // 1. 쿼리 파라미터를 포함한 최종 URL 생성
        string uri = $"{messagesEndpoint}/nearby?lat={position.x}&lon={position.y}&z={position.z}&degree={degree}";

        // 2. UnityWebRequest를 사용한 GET 요청
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            // 3. 요청 전송 및 대기
            yield return webRequest.SendWebRequest();
            // 4. 결과 확인
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = webRequest.downloadHandler.text;

                // Unity의 JsonUtility는 JSON 배열 [ ... ]을 직접 파싱하지 못하므로,
                // { "messages": [ ... ] } 형태의 JSON을 파싱하기 위해 래퍼 클래스(MessageList)를 사용합니다.
                List<MessageData> messages = JsonConvert.DeserializeObject<List<MessageData>>(jsonResponse);

                if (messages != null)
                {
                    Debug.Log($"{messages.Count}개의 메시지를 서버로부터 수신했습니다.");
                    onSuccess?.Invoke(messages); // 성공 콜백 호출
                }
                else
                {
                    Debug.LogError("JSON 파싱에 실패했습니다. 응답 형식 확인 필요: " + jsonResponse);
                    onError?.Invoke("Failed to parse JSON response."); // 실패 콜백 호출
                }
            }
            else
            {
                // 6. 실패 시 에러 콜백 호출
                Debug.LogError("메시지 수신 실패: " + webRequest.error);
                onError?.Invoke(webRequest.error);
            }
        }
    }


    public void LoadEchoImage(string imageKey, Action<Texture2D> onSuccess, Action<string> onError)
    {
        StartCoroutine(LoadImageRoutine(imageKey, onSuccess, onError));
    }

    private IEnumerator LoadImageRoutine(string imageKey, Action<Texture2D> onSuccess, Action<string> onError)
    {
        string url = $"{bucketEndpoint}/presigned-url/echo/image?image-key={imageKey}";
        Debug.Log("api url : " + url);
        // 1) 먼저 presigned URL 받기
        UnityWebRequest apiRequest = UnityWebRequest.Get(url);
        yield return apiRequest.SendWebRequest();

        if (apiRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("API 요청 실패: " + apiRequest.error);
            yield break;
        }

        string presignedUrl = apiRequest.downloadHandler.text.Replace("\"", ""); // 문자열로 받은 URL

        // 2) presigned URL로 이미지 다운로드
        UnityWebRequest imageRequest = UnityWebRequestTexture.GetTexture(presignedUrl);
        yield return imageRequest.SendWebRequest();

        if (imageRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("이미지 다운로드 실패: " + imageRequest.error);
            yield break;
        }

        Texture2D texture = DownloadHandlerTexture.GetContent(imageRequest);

        if (texture != null)
        {
            onSuccess?.Invoke(texture);
        }
        else
        {
            onError?.Invoke("Failed");
        }
    }
}

