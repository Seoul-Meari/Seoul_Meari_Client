using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class Bootstrapper : MonoBehaviour
{
    [Header("Loading UI")]
    [SerializeField] private Canvas loadingCanvas; // Canvas 컴포넌트
    [SerializeField] private GameObject progressSpinner;   // 0~1

    [Header("Next Scene")]
    [SerializeField] private string firstSceneName = "MainScene";
    [SerializeField] private bool loadAdditively = false;

    [Header("Connection Settings")]
    [SerializeField] private float connectionTimeout = 10f; // 서버 연결 시도 시간 (초)
    [SerializeField] private float retryInterval = 2f;    // 재시도 간격 (초)

    private void Awake()
    {
        // Persistent 씬을 Single로 갈아끼우는 경우 이 오브젝트는 유지
        DontDestroyOnLoad(gameObject);
    }

    private IEnumerator Start()
    {
        NetworkManager networkManager = NetworkManager.Instance;
        if (networkManager == null)
        {
            Debug.LogError("NetworkManager instance not found in the scene.");
            yield break;
        }

        // 로딩 UI 표시
        if (loadingCanvas != null)
        {
            loadingCanvas.gameObject.SetActive(true); // GameObject 활성화
            loadingCanvas.enabled = true;              // 렌더 활성화
        }
        if (progressSpinner != null)
        {
            progressSpinner.SetActive(true);
        }

        // 서버가 준비될 때까지 일정 간격으로 상태 확인 (타임아웃 포함)
        float elapsedTime = 0f;
        while (elapsedTime < connectionTimeout)
        {
            // NetworkManager의 상태 확인 코루틴을 호출하고 끝날 때까지 대기
            yield return StartCoroutine(networkManager.CheckServerStatus());

            // 서버가 준비되었다면 반복문을 탈출
            if (networkManager.IsServerReady)
            {
                break;
            }

            // 준비되지 않았다면 재시도 간격만큼 대기
            Debug.Log($"Server not ready. Retrying in {retryInterval} seconds...");
            yield return new WaitForSeconds(retryInterval);
            elapsedTime += retryInterval;
        }

        // 씬 비동기 로드 시작
        var mode = loadAdditively ? LoadSceneMode.Additive : LoadSceneMode.Single;
        AsyncOperation op = SceneManager.LoadSceneAsync(firstSceneName, mode);
        op.allowSceneActivation = false; // 0.9에서 대기

        // 씬 활성화
        op.allowSceneActivation = true;

        // ✨ 중요: 씬이 완전히 활성화되고 첫 프레임을 그릴 때까지 한 프레임 기다립니다.
        yield return null; 

        // 씬 넘어간 뒤 로딩 UI 끄기
        if (loadingCanvas != null)
        {
            loadingCanvas.gameObject.SetActive(false);
        }
    }
}