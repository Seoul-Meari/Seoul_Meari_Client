using UnityEngine;
using System.Collections;
using TMPro;

[RequireComponent(typeof(MessageDisplay))]
public class TemporaryMessageAnimator : MonoBehaviour
{
    public float displayDuration = 1.5f;
    public float animationDuration = 3.0f;
    public float flyUpDistance = 2.0f;
    
    [Tooltip("애니메이션의 속도 곡선을 정의합니다.")]
    public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // 기본값 설정

    private TextMeshProUGUI contentText;

    void Start()
    {
        contentText = GetComponent<MessageDisplay>().contentText;
        StartCoroutine(AnimateAndDestroy());
    }

    private IEnumerator AnimateAndDestroy()
    {
        yield return new WaitForSeconds(displayDuration);
        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition + (Vector3.up * flyUpDistance);
        Color startColor = contentText.color;
        float startTime = Time.time;

        while (Time.time < startTime + animationDuration)
        {
            float linearProgress = (Time.time - startTime) / animationDuration;
            
            // AnimationCurve를 이용해 변환된 진행률을 가져옵니다.
            float curvedProgress = movementCurve.Evaluate(linearProgress);

            transform.position = Vector3.Lerp(startPosition, endPosition, curvedProgress);
            float newAlpha = Mathf.Lerp(startColor.a, 0f, curvedProgress);
            contentText.color = new Color(startColor.r, startColor.g, startColor.b, newAlpha);
            
            yield return null;
        }
        Destroy(gameObject);
    }
}