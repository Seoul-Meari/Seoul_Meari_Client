using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings; // 로컬라이제이션 설정 사용
using TMPro; // TextMeshPro 드롭다운 사용

public class LanguageDropdown : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;

    IEnumerator Start()
    {
        // 드롭다운 컴포넌트 가져오기
        dropdown = GetComponent<TMP_Dropdown>();
        
        // 1. 로컬라이제이션 시스템이 준비될 때까지 대기
        yield return LocalizationSettings.InitializationOperation;

        // 2. 드롭다운의 기존 옵션들을 모두 삭제
        dropdown.ClearOptions();

        // 3. 사용 가능한 언어 목록으로 드롭다운 채우기
        var options = new List<string>();
        int selectedIndex = 0;
        for (int i = 0; i < LocalizationSettings.AvailableLocales.Locales.Count; i++)
        {
            // 각 언어의 이름(예: "English", "한국어")을 목록에 추가
            var locale = LocalizationSettings.AvailableLocales.Locales[i];
            options.Add(locale.LocaleName);

            // 현재 선택된 언어와 일치하는 항목의 인덱스를 저장
            if (LocalizationSettings.SelectedLocale == locale)
            {
                selectedIndex = i;
            }
        }
        dropdown.AddOptions(options);

        // 4. 드롭다운의 현재 값을 실제 선택된 언어로 설정
        dropdown.value = selectedIndex;
        
        // (중요) 5. 드롭다운의 값이 변경될 때마다 LanguageManager의 함수를 호출하도록 연결
        dropdown.onValueChanged.AddListener(OnLanguageChanged);
    }

    // 드롭다운에서 새로운 언어를 선택했을 때 호출될 함수
    private void OnLanguageChanged(int index)
    {
        // LanguageManager를 통해 언어 변경 요청
        LanguageManager.Instance.ChangeLanguage(index);
    }
}