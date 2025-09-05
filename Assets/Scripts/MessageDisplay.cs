using UnityEngine;
using TMPro;

public class MessageDisplay : MonoBehaviour
{
    // Inspector 창에서 연결할 TextMeshPro UI 요소
    public TextMeshProUGUI contentText;

    // MessageManager가 데이터를 넣어줄 함수
    public void Setup(MessageData data)
    {
        // 전달받은 데이터로 텍스트 내용 업데이트
        contentText.text = data.content;

        // 추가적으로 텍스트 스타일에 따라 패널 크기 조절 등의 로직도 가능
    }
}