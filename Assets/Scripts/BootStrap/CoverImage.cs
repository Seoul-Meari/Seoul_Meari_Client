using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class FitHeightRawImage : MonoBehaviour
{
    private RawImage rawImage;
    private RectTransform rectTr;
    private RectTransform parentRect;

    // 0.0 = 왼쪽 보존, 0.5 = 중앙 보존(기본), 1.0 = 오른쪽 보존
    [Range(0f, 1f)] public float focalX = 0.5f;

    void Awake()
    {
        rawImage = GetComponent<RawImage>();
        rectTr   = GetComponent<RectTransform>();
        parentRect = transform.parent as RectTransform;

        // 부모를 꽉 채우도록 보장
        rectTr.anchorMin = Vector2.zero;
        rectTr.anchorMax = Vector2.one;
        rectTr.offsetMin = Vector2.zero;
        rectTr.offsetMax = Vector2.zero;
    }

    void OnEnable() => UpdateLayout();

    void OnRectTransformDimensionsChange()
    {
        if (isActiveAndEnabled) UpdateLayout();
    }

    public void UpdateLayout()
    {
        if (rawImage.texture == null || parentRect == null) return;

        float texW = rawImage.texture.width;
        float texH = rawImage.texture.height;
        if (texW <= 0 || texH <= 0) return;

        float texAspect    = texW / texH;                   // 원본 가로/세로 비
        Vector2 parentSize = parentRect.rect.size;
        float parentAspect = parentSize.x / parentSize.y;   // 화면(컨테이너) 가로/세로 비

        // "세로에 맞춤": 텍스처의 세로(uv 높이)는 1.0 그대로, 가로만 잘라냄
        // 보이는 가로 비율(0~1): parentAspect / texAspect  (화면이 더 가로로 넓을수록 더 많이 잘라야 함)
        float visibleWidth = parentAspect / texAspect;

        if (visibleWidth >= 1f)
        {
            // 원본이 더 세로로 길거나 화면과 비슷 -> 잘라낼 필요 없음 (좌우 여백 없이 전체 표시)
            rawImage.uvRect = new Rect(0f, 0f, 1f, 1f);
        }
        else
        {
            // 원본이 더 가로로 넓음 -> 좌우를 잘라내 중앙(또는 focalX) 기준으로 노출
            float x = Mathf.Clamp01(focalX * (1f - visibleWidth));
            rawImage.uvRect = new Rect(x, 0f, visibleWidth, 1f);
        }
    }
}