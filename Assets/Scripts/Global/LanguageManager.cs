using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class LanguageManager : MonoBehaviour
{
    public static LanguageManager Instance { get; private set; }
    private bool isChangingLanguage = false;
    private const string LOCALE_KEY = "SelectedLocaleID"; // 오타 방지를 위해 키를 상수로 관리

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    // Start에서 초기화를 진행합니다.
    IEnumerator Start()
    {
        // 로컬라이제이션 시스템이 준비될 때까지 기다립니다.
        yield return LocalizationSettings.InitializationOperation;

        // ★ 불러오기: 저장된 언어 설정이 있는지 확인
        int savedLocaleID = PlayerPrefs.GetInt(LOCALE_KEY, -1); // 키가 없으면 -1 반환

        if (savedLocaleID != -1)
        {
            // 저장된 값이 있으면 해당 언어로 즉시 설정
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[savedLocaleID];
        }
        // 저장된 값이 없으면 기본 언어(보통 목록의 첫 번째)나 시스템 언어로 유지됩니다.
    }

    public void ChangeLanguage(int localeIndex)
    {
        if (isChangingLanguage) return;
        StartCoroutine(SetLocale(localeIndex));
    }

    private IEnumerator SetLocale(int localeIndex)
    {
        isChangingLanguage = true;
        yield return LocalizationSettings.InitializationOperation;

        if (localeIndex >= 0 && localeIndex < LocalizationSettings.AvailableLocales.Locales.Count)
        {
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[localeIndex];

            // ★ 저장: 언어를 변경할 때마다 PlayerPrefs에 해당 인덱스를 저장
            PlayerPrefs.SetInt(LOCALE_KEY, localeIndex);
            PlayerPrefs.Save(); // (선택) 확실한 저장을 위해 호출
        }
        else
        {
            Debug.LogWarning("Invalid locale index: " + localeIndex);
        }

        isChangingLanguage = false;
    }
}