using System.Collections;
using System.IO; // Directory 클래스를 사용하기 위해 필수
using UnityEngine;

public class AutoScreenshot : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("사진을 찍는 시간 간격 (초)")]
    [SerializeField]
    private float captureInterval = 5.0f;
    
    [Tooltip("장면을 비추는 메인 AR 카메라")]
    [SerializeField]
    private Camera mainCamera;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        StartCoroutine(CaptureRoutine());
    }

    private IEnumerator CaptureRoutine()
    {
        if (LayerMask.NameToLayer("UI") == -1)
        {
            Debug.LogError("'UI' Layer가 생성되지 않았습니다.");
            yield break;
        }

        while (true)
        {
            yield return new WaitForSeconds(captureInterval);
            yield return TakeScreenshotWithoutUI();
        }
    }

    private IEnumerator TakeScreenshotWithoutUI()
    {
        int originalCullingMask = mainCamera.cullingMask;
        mainCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("UI"));
        yield return new WaitForEndOfFrame();

        Texture2D screenTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        screenTexture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenTexture.Apply();
        
        mainCamera.cullingMask = originalCullingMask;

        byte[] imageData = screenTexture.EncodeToPNG();
        Destroy(screenTexture);

        // --- 파일 경로 설정 부분 수정 ---

        // 1. 최종 저장될 폴더 경로를 생성합니다. (기본 경로 + 하위 폴더 이름)
        string directoryPath = ScreenShotConfig.screenshotFolderPath;
        // 2. 폴더가 존재하지 않으면, 새로 생성합니다.
        //    이 과정을 거치면 파일 저장 시 폴더가 없어서 발생하는 에러를 막을 수 있습니다.
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        // 3. 파일 이름과 최종 경로를 결합합니다.
        string fileName = "AR_Capture_" + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".png";
        string filePath = Path.Combine(directoryPath, fileName);
        
        // --- 파일 저장 ---
        File.WriteAllBytes(filePath, imageData);
        
        Debug.Log($"✅ 사진 저장 완료: {filePath}");
    }
}