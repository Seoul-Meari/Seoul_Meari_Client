using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 위치, 사진 등 디바이스의 권한과 관련된 모든 로직을 처리하는 클래스입니다.
/// </summary>
public class PermissionManager : MonoBehaviour
{
    public static PermissionManager Instance { get; private set; }

    [Header("Location Settings")]
    [SerializeField] private float locationInitTimeout = 20f;
    [SerializeField] private float desiredAccuracyMeters = 10f;
    [SerializeField] private float updateDistanceMeters = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 앱에 필요한 모든 권한을 순차적으로 요청합니다.
    /// </summary>
    public IEnumerator RequestAllPermissions(System.Action<bool> callback, bool requirePhoto)
    {
        bool locationReady = false;
        yield return StartCoroutine(EnsureLocationReady(result => locationReady = result));
        
        if (!locationReady)
        {
            callback(false);
            yield break;
        }

        if (requirePhoto)
        {
            bool photoPermissionGranted = false;
            yield return StartCoroutine(EnsureCameraPermission(result => photoPermissionGranted = result));
            if (!photoPermissionGranted)
            {
                callback(false);
                yield break;
            }
        }
        
        callback(true);
    }
    
    private IEnumerator EnsureLocationReady(System.Action<bool> callback)
    {
#if UNITY_EDITOR
        Debug.Log("[Location] Editor: Skipping permission check.");
        callback(true);
        yield break;
#endif

        bool hasPermission = false;
#if UNITY_ANDROID
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.FineLocation))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.FineLocation);
            float timer = 5f;
            while(timer > 0)
            {
                if(UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.FineLocation)) break;
                timer -= Time.unscaledDeltaTime;
                yield return null;
            }
        }
        hasPermission = UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.FineLocation);
#elif UNITY_IOS
        hasPermission = Input.location.isEnabledByUser;
#endif

        if (!hasPermission)
        {
             Debug.LogWarning("[Location] Permission denied by user.");
             callback(false);
             yield break;
        }

        if (!Input.location.isEnabledByUser)
        {
            Debug.LogWarning("[Location] Location service is disabled in OS settings.");
            callback(false);
            yield break;
        }

        Input.location.Start(desiredAccuracyMeters, updateDistanceMeters);

        float timeout = locationInitTimeout;
        while (Input.location.status == LocationServiceStatus.Initializing && timeout > 0f)
        {
            yield return new WaitForSeconds(0.5f);
            timeout -= 0.5f;
        }

        if (timeout <= 0f)
        {
            Debug.LogError("[Location] Timed out while initializing.");
            Input.location.Stop();
            callback(false);
            yield break;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.LogError($"[Location] Failed to start. Status={Input.location.status}");
            callback(false);
            yield break;
        }
        
        Debug.Log($"[Location] Ready. lat={Input.location.lastData.latitude}, lon={Input.location.lastData.longitude}");
        callback(true);
    }

    private IEnumerator EnsureCameraPermission(Action<bool> callback)
    {
#if UNITY_EDITOR
        Debug.Log("[Camera] Editor: Skipping permission check.");
        callback?.Invoke(true);
        yield break;
#endif

        bool isGranted = false;

#if UNITY_ANDROID
        // 1) 현재 권한 보유 여부 확인
        isGranted = UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera);

        if (!isGranted)
        {
            // 2) 권한 요청 (단일 권한)
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);

            // 3) 간단 타임아웃 대기(권장: PermissionCallbacks 사용)
            float timer = 8f;
            while (timer > 0f)
            {
                if (UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
                    break;

                timer -= Time.unscaledDeltaTime;
                yield return null;
            }

            isGranted = UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera);
        }

#elif UNITY_IOS
        // iOS는 WebCamTexture/AR 세션을 시작하려고 할 때 시스템 팝업이 뜸.
        // 여기에서는 true로 가정하고, 실제 카메라 시작 시 사용자가 거부하면 스트림이 오지 않습니다.
        // 반드시 Info.plist에 NSCameraUsageDescription 키를 넣어주세요.
        Debug.LogWarning("[Camera] iOS: 권한 팝업은 카메라 사용 시 자동으로 표시됩니다. Info.plist에 NSCameraUsageDescription이 필요합니다.");
        isGranted = true;
#else
        isGranted = true; // 기타 플랫폼
#endif

        if (isGranted) Debug.Log("[Camera] Permission granted.");
        else Debug.LogError("[Camera] Permission denied.");

        callback?.Invoke(isGranted);
        yield break;
    }

    /// <summary>
    /// 앱의 설정 화면으로 이동 (Android/iOS)
    /// </summary>
    public void OpenAppSettings()
    {
#if UNITY_ANDROID
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        {
            var intent = new AndroidJavaObject("android.content.Intent", "android.settings.APPLICATION_DETAILS_SETTINGS");
            var uri = new AndroidJavaClass("android.net.Uri").CallStatic<AndroidJavaObject>("fromParts", "package", currentActivity.Call<string>("getPackageName"), null);
            intent.Call<AndroidJavaObject>("setData", uri);
            currentActivity.Call("startActivity", intent);
        }
#elif UNITY_IOS
        Application.OpenURL("app-settings:");
#endif
    }
}

