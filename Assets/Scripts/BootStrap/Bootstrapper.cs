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

    private void Awake()
    {
        // Persistent 씬을 Single로 갈아끼우는 경우 이 오브젝트는 유지
        DontDestroyOnLoad(gameObject);
    }

    private IEnumerator Start()
    {
        // 0) 로딩 UI 안전하게 표시
        if (loadingCanvas != null)
        {
            loadingCanvas.gameObject.SetActive(true); // ✅ GameObject 활성화
            loadingCanvas.enabled = true;              // 렌더 활성화
        }
        if (progressSpinner != null)
        {
            progressSpinner.SetActive(true);
        }

        // 1) GpsService 준비 대기(타임아웃)
        // yield return new WaitUntil(() => GpsService.Instance != null);
        float timeout = 5f, t = 0f;
        while ( /* !GpsService.Instance.IsInitialized && */ t < timeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        // if (!GpsService.Instance.IsInitialized)
        //     Debug.LogWarning("[Bootstrap] GPS 초기화 타임아웃. 계속 진행합니다.");

        // 2) 씬 비동기 로드 시작
        var mode = loadAdditively ? LoadSceneMode.Additive : LoadSceneMode.Single;
        AsyncOperation op = SceneManager.LoadSceneAsync(firstSceneName, mode);
        op.allowSceneActivation = false; // 0.9에서 대기

        // 5) 씬 활성화
        op.allowSceneActivation = true;

        // ✨ 중요: 씬이 완전히 활성화되고 첫 프레임을 그릴 때까지 한 프레임 기다립니다.
        yield return null; 

        // 6) 씬 넘어간 뒤 로딩 UI 끄기
        if (loadingCanvas != null)
        {
            loadingCanvas.gameObject.SetActive(false);
        }
    }
}