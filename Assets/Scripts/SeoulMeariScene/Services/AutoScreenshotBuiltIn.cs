using UnityEngine;
using System.IO;
using System.Threading.Tasks;

/// <summary>
/// Built-in RP / BG 카메라 전용: AR 배경만 주기적으로 캡처.
/// - 이 스크립트는 "ARBackgroundCamera"에 부착 (ARCameraBackground가 붙은 카메라)
/// - Game 카메라는 오브젝트/UI만 렌더, BG 카메라는 cullingMask=Nothing
/// </summary>
[RequireComponent(typeof(Camera))]
public class ARBackgroundOnlyCapture : MonoBehaviour
{
    [Header("Capture")]
    [Tooltip("캡처 간격(초)")]
    [SerializeField] private float captureInterval = 10f;

    [Tooltip("원본 대비 해상도 스케일(0.25~1.0)")]
    [Range(0.25f, 1f)]
    [SerializeField] private float resolutionScale = 0.5f;

    [Header("Encoding")]
    [SerializeField] private bool useJpeg = true;
    [Range(1,100)] [SerializeField] private int jpegQuality = 80;

    [Header("IO")]
    [SerializeField] private int maxConcurrentSaves = 1;

    private float _nextTime;
    private int _inflightSaves = 0;
    private Texture2D _reuseTex;

    void Start()
    {
        if (captureInterval < 0.1f) captureInterval = 0.1f;
        _nextTime = Time.time + captureInterval;
    }

    // BG 카메라에 부착해야 '배경만' 캡처됩니다.
    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        // BG 카메라는 화면에도 나가야 하므로 Blit 유지
        Graphics.Blit(src, dest);

        if (Time.time < _nextTime) return;
        if (_inflightSaves >= maxConcurrentSaves) return;
        _nextTime = Time.time + captureInterval;

        // 다운스케일용 RT
        int w = Mathf.Max(1, Mathf.RoundToInt(src.width * resolutionScale));
        int h = Mathf.Max(1, Mathf.RoundToInt(src.height * resolutionScale));
        var tmp = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Graphics.Blit(src, tmp);

        // RT -> Texture2D
        if (_reuseTex == null || _reuseTex.width != w || _reuseTex.height != h)
        {
            if (_reuseTex != null) Destroy(_reuseTex);
            _reuseTex = new Texture2D(w, h, TextureFormat.RGB24, false, false);
        }
        var prev = RenderTexture.active;
        RenderTexture.active = tmp;
        _reuseTex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        _reuseTex.Apply(false, false);
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(tmp);

        // 인코딩
        byte[] bytes = useJpeg
            ? _reuseTex.EncodeToJPG(Mathf.Clamp(jpegQuality, 1, 100))
            : _reuseTex.EncodeToPNG();

        if (useJpeg)
        {
            var nowIso = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            string device = SystemInfo.deviceModel;
            // 가능하면 위치 정보
            string latStr = "", lonStr = "";
            if (Input.location.status == LocationServiceStatus.Running)
            {
                var last = Input.location.lastData;
                latStr = last.latitude.ToString("F7", System.Globalization.CultureInfo.InvariantCulture);
                lonStr = last.longitude.ToString("F7", System.Globalization.CultureInfo.InvariantCulture);
            }

            string xmp = XmpBuilder.BuildSimpleXmp(
                description: "AR Background capture",
                createDateIso8601: nowIso,
                deviceModel: device,
                latitude: latStr,
                longitude: lonStr
            );

            bytes = JpegXmpInjector.InjectXmp(bytes, xmp);
        }

        // 저장 (백그라운드)
        _inflightSaves++;
        string dir = ScreenShotConfig.screenshotFolderPath;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        string ext = useJpeg ? "jpg" : "png";

        string fileName;
        if (Input.location.status == LocationServiceStatus.Running)
        {
            var last = Input.location.lastData;
            string latStr = last.latitude.ToString("F5", System.Globalization.CultureInfo.InvariantCulture);
            string lonStr = last.longitude.ToString("F5", System.Globalization.CultureInfo.InvariantCulture);
            fileName = $"AR_BG_{latStr}_{lonStr}.{ext}";
        }
        else
        {
            // 위치를 못 가져온 경우 fallback: 날짜 기반
            fileName = $"AR_BG_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.{ext}";
        }

        string file = Path.Combine(dir, fileName);

        Task.Run(() => File.WriteAllBytes(file, bytes))
            .ContinueWith(_ =>
            {
                _inflightSaves--;
            }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    void OnDestroy()
    {
        if (_reuseTex != null) Destroy(_reuseTex);
    }
}
