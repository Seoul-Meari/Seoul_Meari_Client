using UnityEngine;
using UnityEngine.XR.ARFoundation;

[DefaultExecutionOrder(1000)] // AR 업데이트 후에 실행되도록
public class SyncToArCamera : MonoBehaviour
{
    [SerializeField] private Camera arCamera;         // ARCameraManager가 붙은 카메라
    [SerializeField] private Camera gameCamera;       // 동기화 대상(= 게임 카메라)
    [SerializeField] private bool copyProjection = true;
    [SerializeField] private bool copyClipsFov = true;

    private ARCameraManager arCamMgr;

    void Awake()
    {
        if (gameCamera == null) gameCamera = GetComponent<Camera>();
        if (arCamera != null) arCamMgr = arCamera.GetComponent<ARCameraManager>();
    }

    void LateUpdate()
    {
        if (arCamera == null || gameCamera == null) return;

        // 1) 포즈(Transform) 동기화
        transform.position = arCamera.transform.position;
        transform.rotation = arCamera.transform.rotation;

        // 2) 프로젝션/클리핑 동기화
        if (copyClipsFov)
        {
            gameCamera.nearClipPlane = arCamera.nearClipPlane;
            gameCamera.farClipPlane  = arCamera.farClipPlane;
            gameCamera.fieldOfView   = arCamera.fieldOfView; // 참고: 실제론 projectionMatrix가 더 정확
        }

        if (copyProjection)
        {
            // ARFoundation이 세팅한 실제 투영행렬을 복사 (가장 정확)
            gameCamera.projectionMatrix = arCamera.projectionMatrix;
        }
    }

    // AR 프레임 수신 시점에 맞춰도 됨 (선택)
    void OnEnable()
    {
        if (arCamMgr != null) arCamMgr.frameReceived += OnArFrame;
    }
    void OnDisable()
    {
        if (arCamMgr != null) arCamMgr.frameReceived -= OnArFrame;
    }
    void OnArFrame(ARCameraFrameEventArgs _)
    {
        if (arCamera == null || gameCamera == null) return;
        gameCamera.projectionMatrix = arCamera.projectionMatrix;
    }
}