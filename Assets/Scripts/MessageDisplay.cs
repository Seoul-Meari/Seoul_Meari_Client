using UnityEngine;
using TMPro;

public class MessageDisplay : MonoBehaviour
{
    public TextMeshProUGUI contentText;

    public void Setup(MessageData data)
    {
        contentText.text = data.content;
    }
}