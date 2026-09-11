using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 로딩 UI, 권한 요청 UI 등 모든 UI 요소를 관리하는 클래스입니다.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Loading UI")]
    [SerializeField] private GameObject loadingCanvas;
    [SerializeField] private GameObject progressSpinner;

    [Header("Permission Blocker UI")]
    [SerializeField] private GameObject permissionBlockerPanel;
    [SerializeField] private TextMeshProUGUI permissionMessageText;
    [SerializeField] private Button openSettingsButton;
    [SerializeField] private Button quitButton;

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

        // 버튼 리스너 연결
        if (openSettingsButton != null)
            openSettingsButton.onClick.AddListener(() => PermissionManager.Instance.OpenAppSettings());
        if (quitButton != null)
            quitButton.onClick.AddListener(() => Application.Quit());
            
        // 초기 UI 상태 설정
        if (permissionBlockerPanel != null) permissionBlockerPanel.SetActive(false);
    }
    
    /// <summary>
    /// 로딩 UI를 켜거나 끕니다.
    /// </summary>
    public void ShowLoading(bool show)
    {
        if (loadingCanvas != null) loadingCanvas.gameObject.SetActive(show);
        if (progressSpinner != null) progressSpinner.SetActive(show);
        
        // 로딩 UI가 켜지면 다른 UI는 가립니다.
        if(show && permissionBlockerPanel != null)
        {
            permissionBlockerPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// 권한이 거부되었을 때 안내 UI를 표시합니다.
    /// </summary>
    public void ShowPermissionBlocker(string message)
    {
        ShowLoading(false);
        
        if (permissionBlockerPanel != null)
        {
            permissionBlockerPanel.SetActive(true);
            if(permissionMessageText != null)
            {
                permissionMessageText.text = message;
            }
        }
        
#if UNITY_IOS
        if (quitButton != null) quitButton.gameObject.SetActive(false);
#endif
    }
}
