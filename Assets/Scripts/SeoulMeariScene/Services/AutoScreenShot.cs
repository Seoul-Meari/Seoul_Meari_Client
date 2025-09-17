using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks; // Task를 사용하기 위해 추가
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

    [Tooltip("숨겨야 할 모든 Screen Space - Overlay Canvas들")]
    [SerializeField]
    private List<Canvas> overlayCanvases = new List<Canvas>();

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        
        if (LayerMask.NameToLayer("UI") == -1)
        {
            Debug.LogWarning("'UI' Layer가 프로젝트에 정의되지 않았습니다. World Space UI 숨기기 기능이 작동하지 않을 수 있습니다.");
        }
        
        StartCoroutine(CaptureRoutine());
    }

    private IEnumerator CaptureRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(captureInterval);
            StartCoroutine(TakeScreenshotWithoutUI()); // 코루틴으로 호출
        }
    }

    private IEnumerator TakeScreenshotWithoutUI()
    {
        // --- 1. UI 숨기기 ---
        int originalCullingMask = mainCamera.cullingMask;
        if (LayerMask.NameToLayer("UI") != -1)
        {
            mainCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("UI"));
        }
        
        // if (overlayCanvases != null)
        // {
        //     foreach (var canvas in overlayCanvases)
        //     {
        //         if (canvas != null) canvas.enabled = false;
        //     }
        // }

        yield return new WaitForEndOfFrame();

        // --- 2. 화면 픽셀 읽기 (여기까지는 메인 스레드에서) ---
        Texture2D screenTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        screenTexture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenTexture.Apply();
        
        // --- 3. UI 즉시 복구 ---
        // mainCamera.cullingMask = originalCullingMask;
        // if (overlayCanvases != null)
        // {
        //     foreach (var canvas in overlayCanvases)
        //     {
        //         if (canvas != null) canvas.enabled = true;
        //     }
        // }

        // --- 4. 무거운 인코딩 및 파일 저장 작업은 비동기로 처리 ---
        // UI를 복구한 후, 비동기 메소드를 호출하여 백그라운드에서 저장 시작
        SaveImageAsync(screenTexture);
    }

    // async Task 메소드로 변경
    private async void SaveImageAsync(Texture2D texture)
    {
        // PNG로 인코딩하는 작업. 이것도 약간 무거울 수 있습니다.
        byte[] imageData = texture.EncodeToPNG();
        Destroy(texture); // 텍스처는 이제 필요 없으므로 파괴

        string directoryPath = ScreenShotConfig.screenshotFolderPath; // 별도 정의된 경로로 가정
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string fileName = "AR_Capture_" + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".png";
        string filePath = Path.Combine(directoryPath, fileName);
        
        // File.WriteAllBytes를 Task.Run으로 감싸 백그라운드 스레드에서 실행
        await Task.Run(() => File.WriteAllBytes(filePath, imageData));
        
        Debug.Log($"✅ 사진 저장 완료 (비동기): {filePath}");
    }
}