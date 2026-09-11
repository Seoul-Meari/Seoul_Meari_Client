using UnityEngine;
using System.Collections;
using TMPro;

[RequireComponent(typeof(MessageInfo))]
public class TemporaryMessageAnimator : MonoBehaviour
{
    public float displayDuration = 1.5f;
    public float animationDuration = 3.0f;
    public float flyUpDistance = 2.0f;
    
    [Tooltip("애니메이션의 속도 곡선을 정의합니다.")]
    public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // 기본값 설정

    private TextMeshProUGUI contentText;
    private TextMeshProUGUI writer;
    private TextMeshProUGUI createdAt;

    void Start()
    {
        contentText = GetComponent<MessageInfo>().ContentText;
        writer = GetComponent<MessageInfo>().Writer;
        createdAt = GetComponent<MessageInfo>().CreatedAt;
        StartCoroutine(AnimateAndDestroy());
    }

    private IEnumerator AnimateAndDestroy()
    {
        yield return new WaitForSeconds(displayDuration);
        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition + (Vector3.up * flyUpDistance);
        Color contentStartColor = contentText.color;
        Color writerStartColor = writer.color;
        Color createdAtStartColor = createdAt.color;
        float startTime = Time.time;

        while (true)
        {
            float elapsed = Time.time - startTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);     // 0~1
            float p = movementCurve.Evaluate(t);                      // 커브 적용

            // 위로 이동
            transform.position = Vector3.Lerp(startPosition, endPosition, p);

            // 알파 보간 (각 텍스트 동일하게 0으로)
            float a1 = Mathf.Lerp(contentStartColor.a,   0f, p);
            float a2 = Mathf.Lerp(writerStartColor.a,    0f, p);
            float a3 = Mathf.Lerp(createdAtStartColor.a, 0f, p);

            contentText.color  = new Color(contentStartColor.r,   contentStartColor.g,   contentStartColor.b,   a1);
            writer.color       = new Color(writerStartColor.r,    writerStartColor.g,    writerStartColor.b,    a2);
            createdAt.color    = new Color(createdAtStartColor.r, createdAtStartColor.g, createdAtStartColor.b, a3);

            if (t >= 1f) break;
            yield return null;
        }
        Destroy(gameObject);
    }
}