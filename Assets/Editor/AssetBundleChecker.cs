using UnityEditor;
using UnityEngine;
using System.Linq;

public class AssetBundleChecker : EditorWindow
{
    private Vector2 scrollPosition;
    private string[] allBundleNames;
    private int selectedBundleIndex = -1;
    private string[] assetsInSelectedBundle;

    // 메뉴에 새로운 항목을 추가하여 이 창을 열 수 있게 합니다.
    [MenuItem("Assets/Build AssetBundles/Check Bundle Contents")]
    public static void ShowWindow()
    {
        // "Bundle Checker" 라는 이름의 에디터 창을 엽니다.
        GetWindow<AssetBundleChecker>("Bundle Checker");
    }

    // 창이 활성화될 때 호출됩니다.
    void OnEnable()
    {
        // 프로젝트에 정의된 모든 에셋 번들 이름을 가져옵니다.
        allBundleNames = AssetDatabase.GetAllAssetBundleNames();
        // 탐색하기 쉽도록 정렬합니다.
        if (allBundleNames != null)
        {
            System.Array.Sort(allBundleNames);
        }
    }

    // 에디터 창의 UI를 그립니다.
    void OnGUI()
    {
        GUILayout.Label("Asset Bundle Contents Checker", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("아래 목록에서 에셋 번들 이름을 선택하면 해당 번들에 포함된 에셋들을 볼 수 있습니다.", MessageType.Info);

        if (allBundleNames == null || allBundleNames.Length == 0)
        {
            EditorGUILayout.LabelField("이 프로젝트에는 정의된 에셋 번들이 없습니다.");
            if (GUILayout.Button("새로고침"))
            {
                OnEnable(); // 번들 이름 목록을 다시 불러옵니다.
            }
            return;
        }

        // 드롭다운 메뉴로 번들 이름을 선택합니다.
        int previousIndex = selectedBundleIndex;
        selectedBundleIndex = EditorGUILayout.Popup("Select Bundle Name", selectedBundleIndex, allBundleNames);

        // 선택이 변경되었다면, 해당 번들에 포함된 에셋 목록을 가져옵니다.
        if (selectedBundleIndex != previousIndex && selectedBundleIndex >= 0)
        {
            string selectedBundle = allBundleNames[selectedBundleIndex];
            assetsInSelectedBundle = AssetDatabase.GetAssetPathsFromAssetBundle(selectedBundle);
        }

        // 에셋 목록을 화면에 표시합니다.
        if (assetsInSelectedBundle != null && assetsInSelectedBundle.Length > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"'{allBundleNames[selectedBundleIndex]}' 번들에 포함된 에셋:", EditorStyles.boldLabel);

            // 스크롤 가능한 뷰를 생성합니다.
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
            foreach (string assetPath in assetsInSelectedBundle)
            {
                EditorGUILayout.BeginHorizontal();
                // 에셋 경로를 텍스트 필드에 표시합니다 (읽기 전용).
                EditorGUILayout.TextField(assetPath);
                // 'Select' 버튼을 누르면 프로젝트 창에서 해당 에셋을 하이라이트합니다.
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Object obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                    if (obj != null)
                    {
                        Selection.activeObject = obj;
                        EditorGUIUtility.PingObject(obj);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }
        else if (selectedBundleIndex >= 0)
        {
             EditorGUILayout.HelpBox("이 번들에는 포함된 에셋이 없습니다. 사용되지 않는 이름일 수 있습니다.", MessageType.Warning);
        }
    }
}
