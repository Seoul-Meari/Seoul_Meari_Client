using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraController : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("장면을 비추는 메인 AR 카메라")]
    [SerializeField] private Camera mainCamera;

    [Tooltip("숨겨야 할 모든 Screen Space - Overlay Canvas들")]
    [SerializeField] private List<Canvas> overlayCanvases = new List<Canvas>();


    [SerializeField] private GameObject cameraImageObject; // 캡처 미리보기용
    [SerializeField] private GameObject cameraIcon; // 아이콘 숨기기용

    private Texture2D _lastCapture;

    public void Capture()
    {
        StartCoroutine(CaptureRoutine());
    }

    private IEnumerator CaptureRoutine()
    {
        // 1) UI 숨기기
        int originalCullingMask = mainCamera.cullingMask;

        // UI 레이어 마스킹(카메라가 그리는 UI라면)
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer != -1)
            mainCamera.cullingMask &= ~(1 << uiLayer);

        // Screen Space - Overlay 캔버스는 카메라 마스크와 무관 -> 직접 끄기
        if (overlayCanvases != null)
        {
            foreach (var canvas in overlayCanvases)
                if (canvas) canvas.enabled = false;
        }

        // **중요**: 렌더가 끝난 뒤에 픽셀 읽기
        yield return new WaitForEndOfFrame();

        // 2) 화면 픽셀 읽기
        if (_lastCapture) Destroy(_lastCapture);

        // 화면 위쪽 절반 크기로 텍스처 생성
        int w = Screen.width;
        int h = Screen.height / 2;

        _lastCapture = new Texture2D(w, h, TextureFormat.RGB24, false);

        // 화면의 "위쪽 절반" 영역을 (0,0)부터 꽉 채워서 읽기
        _lastCapture.ReadPixels(new Rect(0, Screen.height - h, w, h), 0, 0);
        _lastCapture.Apply();

        // 3) UI 복구
        mainCamera.cullingMask = originalCullingMask;
        if (overlayCanvases != null)
        {
            foreach (var canvas in overlayCanvases)
                if (canvas) canvas.enabled = true;
        }

        RawImage cameraImage = cameraImageObject.GetComponent<RawImage>();
        // 4) 미리보기 표시
        if (cameraImage)
        {
            cameraImage.enabled = true;
            cameraImage.texture = _lastCapture;
            cameraImageObject.SetActive(true);
        }
        if (cameraIcon) cameraIcon.SetActive(false);
    }
}