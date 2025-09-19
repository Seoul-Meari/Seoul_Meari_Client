using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 앱의 전체 시작 시퀀스를 관리하는 메인 클래스입니다.
/// 각 매니저를 순서대로 호출하여 초기화를 진행합니다.
/// </summary>
public class Bootstrapper : MonoBehaviour
{
    [Header("Next Scene")]
    [SerializeField] private string firstSceneName = "MainScene";
    [SerializeField] private bool loadAdditively = false;

    [Header("App Requirements")]
    [SerializeField] private bool requirePhotoPermission = true;

    [Header("Connection Settings")]
    [SerializeField] private float connectionTimeout = 10f;
    [SerializeField] private float retryInterval = 2f;

    private void Awake()
    {
        Debug.Log("--------------------New Start--------------------");
        DontDestroyOnLoad(gameObject);
    }

    private IEnumerator Start()
    {
        // 필수 매니저 존재 여부 확인
        if (UIManager.Instance == null || PermissionManager.Instance == null)
        {
            Debug.LogError("UIManager or PermissionManager instance not found. Please add them to the scene.");
            yield break;
        }

        UIManager.Instance.ShowLoading(true);

        // 1) 서버 준비 상태 체크
        NetworkManager networkManager = NetworkManager.Instance;
        if (networkManager == null)
        {
            Debug.LogError("NetworkManager instance not found.");
            yield break;
        }

        float elapsedTime = 0f;
        while (elapsedTime < connectionTimeout)
        {
            yield return StartCoroutine(networkManager.CheckServerStatus());
            if (networkManager.IsServerReady) break;

            Debug.Log($"Server not ready. Retrying in {retryInterval} seconds...");
            yield return new WaitForSeconds(retryInterval);
            elapsedTime += retryInterval;
        }

        // 2) 모든 필수 권한 확인
        bool permissionsGranted = false;
        yield return StartCoroutine(PermissionManager.Instance.RequestAllPermissions(
            result => permissionsGranted = result,
            requirePhotoPermission));
        
        if (!permissionsGranted)
        {
            // PermissionManager에서 어떤 권한이 거부되었는지 구체적으로 알 수 있다면 더 좋은 메시지를 표시할 수 있습니다.
            UIManager.Instance.ShowPermissionBlocker("앱 실행에 필요한 필수 권한이 거부되었습니다.\n설정에서 권한을 허용해주세요.");
            yield break; // 부트스트랩 중단
        }

        // 3) 초기 위치 1회 측정
        Vector3 nowPos;
        InitialLocation initialLocation = InitialLocation.Instance;
        if (initialLocation == null)
        {
            Debug.LogError("InitialLocation instance not found.");
            yield break;
        }
        yield return StartCoroutine(initialLocation.SetInitialPos());
        nowPos = initialLocation.GetInitialPos();

        // 4) 메시지 캐시 준비
        MessageCache messageCache = MessageCache.Instance;
        if (messageCache == null)
        {
            Debug.LogError("MessageCache instance not found.");
            yield break;
        }
        yield return StartCoroutine(messageCache.InitiateMessage(nowPos));

        // 5) 메인 씬 로드
        Debug.Log("All checks passed. Loading main scene...");
        var mode = loadAdditively ? LoadSceneMode.Additive : LoadSceneMode.Single;
        AsyncOperation op = SceneManager.LoadSceneAsync(firstSceneName, mode);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
        {
            yield return null;
        }

        op.allowSceneActivation = true;
        yield return null;

        // 6) 로딩 UI 끄기
        UIManager.Instance.ShowLoading(false);
    }
}

