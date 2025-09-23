using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using Unity.Sentis;


[System.Serializable]

public class ObjectDetector : MonoBehaviour
{
    [Header("Model & Labels")]
    public ModelAsset modelAsset;           // best.onnx
    public TextAsset dataYaml;              // 학습 yaml (권장)

    [Header("Runtime")]
    public bool useGPU = true;
    [Range(320, 1280)]
    public int inputSize = 640;

    private Worker worker;
    private string[] classNames = Array.Empty<string>();
    private int yamlNc = -1;                // yaml의 nc (없으면 -1)

    void Start()
    {
        // 1) YAML 로드 (줄 단위 파싱, [] / key:value / - list 모두 지원)
        (classNames, yamlNc) = LoadClassNamesFromYaml(dataYaml);
        if (classNames.Length == 0) classNames = new[] { "cls_0", "cls_1" };

        // 2) Sentis 런타임 준비
        var runtimeModel = ModelLoader.Load(modelAsset);
        var backend = (useGPU && SystemInfo.supportsComputeShaders) ? BackendType.GPUCompute : BackendType.CPU;
        worker = new Worker(runtimeModel, backend);

        Debug.Log($"[Sentis] Worker ready: {backend}");
        Debug.Log($"[Sentis] YAML nc={yamlNc}, names=({string.Join(",", classNames)})");

        // 보조: nc와 names 길이가 어긋나면 경고
        if (yamlNc > 0 && yamlNc != classNames.Length)
            Debug.LogWarning($"[Sentis][Validate] yaml nc({yamlNc}) != names.Length({classNames.Length}). yaml을 점검하세요.");
    }

    public List<DetectionResult> RunOnce(Texture2D tex)
    {
        var results = new List<DetectionResult>();
        if (worker == null || tex == null) return results;

        // 입력 변환
        Tensor<float> input = TextureConverter.ToTensor(
            tex,
            width:  inputSize,
            height: inputSize,
            channels: 3
        );

        // 실행
        worker.Schedule(input);

        // 출력 (GPU → CPU readable 복제)
        Tensor outputGpu = worker.PeekOutput();
        var output = outputGpu.ReadbackAndClone() as Tensor<float>;

        if (output != null)
        {
            // ====== (2) 데이터 검증: 모델 출력으로 추정한 nc와 yaml 비교 ======
            // YOLO류: D=4+1+nc 또는 D=4+nc / 또는 변형 D=6([cx,cy,w,h,score,clsId])
            var s = output.shape;              // (1, D, N) or (1, N, D)
            int inferredNc = InferNumClassesFromOutputShape(s);

            if (inferredNc > 0 && classNames.Length > 0 && inferredNc != classNames.Length)
                Debug.LogWarning($"[Sentis][Validate] model-out nc≈{inferredNc} != names.Length({classNames.Length}). " +
                                 $"yaml이 학습 당시 것과 다를 가능성이 큽니다.");

            Debug.Log($"[Sentis] output shape = ({string.Join(",", s)})");

            // 추론 파싱
            results = ParseDetections(
                output, tex.width, tex.height,
                modelInputSize: inputSize,
                confThresh: 0.25f, iouThresh: 0.45f,
                classNames: classNames
            );

            // 샘플 로깅
            foreach (var r in results)
                Debug.Log($"[Det] {r.ClassName} {r.Confidence:P1} @ (x:{r.Box.x:F2}, y:{r.Box.y:F2}, width:{r.Box.width:F2}, height:{r.Box.height:F2})");
        }
        else
        {
            Debug.LogWarning($"[Sentis] Output tensor is not float. dataType={outputGpu.dataType}");
        }

        // 정리
        input.Dispose();
        outputGpu.Dispose();
        output?.Dispose();

        return results;
    }

    // ================== YOLO 파서 ===================
    private List<DetectionResult> ParseDetections(
        Tensor<float> t,
        int srcW, int srcH,
        int modelInputSize = 640,
        float confThresh = 0.25f,
        float iouThresh = 0.45f,
        string[] classNames = null)
    {
        classNames ??= new[] { "cls_0", "cls_1" };

        var s = t.shape; // 기대: rank 3
        if (s.rank != 3 || s[0] != 1) return new List<DetectionResult>();

        bool CHW = s[1] <= s[2];   // (1, D, N) 가정
        int N = CHW ? s[2] : s[1];
        int D = CHW ? s[1] : s[2];

        var raw = new List<(Rect r, int cls, float p)>(Mathf.Min(N, 2048));

        if (D >= 10)
        {
            // 표준: [cx,cy,w,h,obj,cls...], 일부: [cx,cy,w,h,cls...] (obj 없음)
            bool hasObj = true;
            int clsStart = 5;
            if (D == 4 + classNames.Length) { hasObj = false; clsStart = 4; }

            int nc = D - clsStart;

            for (int i = 0; i < N; i++)
            {
                float cx = CHW ? t[0, 0, i] : t[0, i, 0];
                float cy = CHW ? t[0, 1, i] : t[0, i, 1];
                float w  = CHW ? t[0, 2, i] : t[0, i, 2];
                float h  = CHW ? t[0, 3, i] : t[0, i, 3];
                float obj = hasObj ? (CHW ? t[0, 4, i] : t[0, i, 4]) : 1f;

                int best = -1; float bestP = -1f;
                for (int c = 0; c < nc; c++)
                {
                    float p = CHW ? t[0, clsStart + c, i] : t[0, i, clsStart + c];
                    if (p > bestP) { bestP = p; best = c; }
                }
                float conf = obj * bestP;
                if (conf < confThresh) continue;

                AddRaw(raw, cx, cy, w, h, best, conf, srcW, srcH, modelInputSize);
            }
        }
        else if (D == 6)  // 변형: [cx,cy,w,h,score,classId]
        {
            for (int i = 0; i < N; i++)
            {
                float cx = CHW ? t[0, 0, i] : t[0, i, 0];
                float cy = CHW ? t[0, 1, i] : t[0, i, 1];
                float w  = CHW ? t[0, 2, i] : t[0, i, 2];
                float h  = CHW ? t[0, 3, i] : t[0, i, 3];
                float sc = CHW ? t[0, 4, i] : t[0, i, 4];
                float cid = CHW ? t[0, 5, i] : t[0, i, 5];

                if (sc < confThresh) continue;
                int cls = Mathf.RoundToInt(cid);
                AddRaw(raw, cx, cy, w, h, cls, sc, srcW, srcH, modelInputSize);
            }
        }
        else
        {
            Debug.LogWarning($"[Sentis] Unsupported detection layout. shape=({string.Join(",", s)})");
        }

        var kept = ClasswiseNMS(raw, iouThresh);

        var results = new List<DetectionResult>(kept.Count);
        foreach (var k in kept)
        {
            string name = (k.cls >= 0 && k.cls < classNames.Length) ? classNames[k.cls] : $"cls_{k.cls}";
            results.Add(new DetectionResult { ClassName = name, Confidence = k.p, Box = k.r });
        }
        return results;
    }

    private static void AddRaw(List<(Rect r, int cls, float p)> raw,
        float cx, float cy, float w, float h, int cls, float p,
        int srcW, int srcH, int modelInput)
    {
        float x = cx - w * 0.5f;
        float y = cy - h * 0.5f;
        float sx = (float)srcW / modelInput;
        float sy = (float)srcH / modelInput;
        var rect = new Rect(x * sx, y * sy, w * sx, h * sy);
        rect = ClampRect(rect, srcW, srcH);
        raw.Add((rect, cls, p));
    }

    private static List<(Rect r, int cls, float p)> ClasswiseNMS(List<(Rect r, int cls, float p)> boxes, float iouT)
    {
        var per = new Dictionary<int, List<(Rect r, int cls, float p)>>();
        foreach (var b in boxes)
        {
            if (!per.ContainsKey(b.cls)) per[b.cls] = new List<(Rect, int, float)>();
            per[b.cls].Add(b);
        }

        var kept = new List<(Rect r, int cls, float p)>();
        foreach (var kv in per)
        {
            var list = kv.Value;
            list.Sort((a, b) => b.p.CompareTo(a.p));
            while (list.Count > 0)
            {
                var cur = list[0]; kept.Add(cur); list.RemoveAt(0);
                list.RemoveAll(b => IoU(cur.r, b.r) > iouT);
            }
        }
        return kept;
    }

    private static float IoU(Rect a, Rect b)
    {
        float x1 = Mathf.Max(a.xMin, b.xMin), y1 = Mathf.Max(a.yMin, b.yMin);
        float x2 = Mathf.Min(a.xMax, b.xMax), y2 = Mathf.Min(a.yMax, b.yMax);
        float inter = Mathf.Max(0, x2 - x1) * Mathf.Max(0, y2 - y1);
        float uni = a.width * a.height + b.width * b.height - inter;
        return uni <= 0 ? 0 : inter / uni;
    }

    private static Rect ClampRect(Rect r, int w, int h)
    {
        float x = Mathf.Clamp(r.x, 0, w - 1), y = Mathf.Clamp(r.y, 0, h - 1);
        float X = Mathf.Clamp(r.xMax, 0, w - 1), Y = Mathf.Clamp(r.yMax, 0, h - 1);
        return Rect.MinMaxRect(x, y, X, Y);
    }

    // ================== (1) YAML 파서: 줄 단위 범용 ===================
    // 지원:
    //   names: [a, b, c]
    //   names:
    //     - a
    //     - b
    //   names:
    //     0: a
    //     1: b
    //   nc: 6    // 있으면 같이 읽음
    private static (string[] names, int nc) LoadClassNamesFromYaml(TextAsset yaml)
    {
        if (yaml == null) return (Array.Empty<string>(), -1);

        var lines = yaml.text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        int nc = -1;
        var names = new List<string>();

        // 먼저 nc 스캔
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.StartsWith("#") || line.Length == 0) continue;
            if (line.StartsWith("nc:"))
            {
                var v = line.Substring(3).Trim();
                if (int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                    nc = parsed;
            }
        }

        // names 블록 탐색
        for (int i = 0; i < lines.Length; i++)
        {
            var orig = lines[i];
            var line = orig.Trim();
            if (line.StartsWith("#") || line.Length == 0) continue;

            if (line.StartsWith("names:"))
            {
                var after = line.Substring("names:".Length).Trim();

                // 1) 인라인 리스트: names: [a, b, c]
                if (after.StartsWith("[") && after.Contains("]"))
                {
                    var inner = after.Trim();
                    int l = inner.IndexOf('[');
                    int r = inner.LastIndexOf(']');
                    if (l >= 0 && r > l)
                    {
                        var content = inner.Substring(l + 1, r - l - 1);
                        names = SplitNames(content);
                        return (names.ToArray(), nc);
                    }
                }

                // 2) 멀티라인 블록
                // 다음 줄부터 들여쓰기 기반으로 수집
                int baseIndent = CountIndent(orig);
                for (int j = i + 1; j < lines.Length; j++)
                {
                    var rawj = lines[j];
                    if (rawj.Trim().Length == 0) continue;
                    if (rawj.TrimStart().StartsWith("#")) continue;

                    int ind = CountIndent(rawj);
                    if (ind <= baseIndent) break; // 블록 종료

                    var lj = rawj.Trim();

                    // 2-1) 하이픈 리스트: - label
                    if (lj.StartsWith("-"))
                    {
                        string label = lj.Substring(1).Trim();
                        label = TrimQuotes(label);
                        if (label.Length > 0) names.Add(label);
                        continue;
                    }

                    // 2-2) dict: 0: label
                    int colon = lj.IndexOf(':');
                    if (colon > 0)
                    {
                        string label = lj.Substring(colon + 1).Trim();
                        label = TrimQuotes(label);
                        if (label.Length > 0) names.Add(label);
                        continue;
                    }

                    // 예상치 못한 형식이면 다음 줄로
                }

                // names 키는 있었지만 값 파싱 실패 → 빈 배열
                break;
            }
        }

        return (names.ToArray(), nc);

        // 로컬 유틸
        static int CountIndent(string s)
        {
            int n = 0;
            foreach (var ch in s)
            {
                if (ch == ' ') n++;
                else if (ch == '\t') n += 4;
                else break;
            }
            return n;
        }

        static string TrimQuotes(string s)
        {
            s = s.Trim();
            if (s.Length >= 2 &&
                ((s[0] == '"' && s[^1] == '"') || (s[0] == '\'' && s[^1] == '\'')))
                return s.Substring(1, s.Length - 2);
            return s;
        }

        static List<string> SplitNames(string csv)
        {
            // 쉼표 기준 split + 공백/따옴표 제거
            var parts = csv.Split(',');
            var list = new List<string>(parts.Length);
            foreach (var p in parts)
            {
                var t = TrimQuotes(p);
                if (t.Length > 0) list.Add(t);
            }
            return list;
        }
    }

    // ====== 모델 출력 모양으로 nc 추정(실행 시) ======
    // D>=10 : (4+1+nc) or (4+nc) → 각각 nc 계산
    // D==6  : 변형([cx,cy,w,h,score,cls]) → 명확한 nc를 알 수 없어  -1
    private static int InferNumClassesFromOutputShape(TensorShape shape)
    {
        if (shape.rank != 3 || shape[0] != 1) return -1;
        bool CHW = shape[1] <= shape[2];
        int D = CHW ? shape[1] : shape[2];
        if (D >= 10) return D - 5;  // 표준 가정(5=xywh+obj)
        if (D == 6)  return -1;     // 변형: score+cls 단일
        return -1;
    }
}