// 예: Image가 가진 Material 인스턴스에 반영
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Graphic))]
public class UIGradientButtonBinder : MonoBehaviour
{
    RectTransform rt;
    Material mat;
    static readonly int RectSizeId = Shader.PropertyToID("_RectSize");

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        // Graphic(Material) 인스턴스 사용 보장
        var g = GetComponent<Graphic>();
        mat = g.material = Instantiate(g.material);
    }

    void LateUpdate()
    {
        if (mat == null || rt == null) return;
        Vector2 size = rt.rect.size; // (width, height) in px
        mat.SetVector(RectSizeId, new Vector4(size.x, size.y, 0, 0));
    }
}