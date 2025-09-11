// Assets/Editor/CaptureUIWithMaterial.cs
using UnityEngine;
using UnityEditor;

public class CaptureUIWithMaterial : EditorWindow
{
    public GameObject uiTarget;
    public int width = 512, height = 512;

    [MenuItem("Tools/Capture UI (Material-aware)")]
    static void Open() => GetWindow<CaptureUIWithMaterial>("Capture UI");

    void OnGUI(){
        uiTarget = (GameObject)EditorGUILayout.ObjectField("UI Target", uiTarget, typeof(GameObject), true);
        width = EditorGUILayout.IntField("Width", width);
        height = EditorGUILayout.IntField("Height", height);
        if (GUILayout.Button("Export PNG") && uiTarget) Capture();
    }

    void Capture(){
        // 1) 임시 카메라 + 캔버스
        var camGO = new GameObject("TmpUICam");
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0,0,0,0);
        cam.orthographic = true;

        var canvasGO = new GameObject("TmpCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.pixelPerfect = true;
        var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ConstantPixelSize;

        // 2) 대상 UI 복제 → 임시 캔버스 아래로
        var clone = (GameObject)PrefabUtility.InstantiatePrefab(uiTarget);
        if (clone == null) clone = GameObject.Instantiate(uiTarget);
        clone.name = uiTarget.name + "_CloneForCapture";
        clone.transform.SetParent(canvas.transform, false);

        // 3) 렌더타깃 준비
        var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;

        // 4) 한 프레임 강제 레이아웃 후 렌더
        Canvas.ForceUpdateCanvases();
        cam.Render();

        // 5) PNG 저장
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0,0,width,height), 0, 0);
        tex.Apply();

        string path = EditorUtility.SaveFilePanel("Save PNG", Application.dataPath, uiTarget.name, "png");
        if (!string.IsNullOrEmpty(path))
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());

        // 정리
        RenderTexture.active = prev;
        cam.targetTexture = null;
        rt.Release();
        DestroyImmediate(rt);
        DestroyImmediate(tex);
        DestroyImmediate(clone);
        DestroyImmediate(canvasGO);
        DestroyImmediate(camGO);

        AssetDatabase.Refresh();
        Debug.Log("✅ UI(머티리얼 포함) PNG 익스포트 완료");
    }
}