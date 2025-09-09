using UnityEngine;
using TMPro;

public class MessageDisplay : MonoBehaviour
{
    public TextMeshProUGUI contentText;

    public void Setup(MessageData data)
    {
        contentText.text = MessageColoring.MessageWithColor(data.content, "#000000");
    }

    void Start()
    {
        transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
    }
}