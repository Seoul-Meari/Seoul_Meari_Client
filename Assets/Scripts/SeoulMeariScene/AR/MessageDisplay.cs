using UnityEngine;
using TMPro;

public class MessageDisplay : MonoBehaviour
{
    [Header("Message")]
    [SerializeField] private TextMeshProUGUI _contentText;
    [SerializeField] private TextMeshProUGUI _writer;
    [SerializeField] private TextMeshProUGUI _createdAt;

    public TextMeshProUGUI ContentText => _contentText;
    public TextMeshProUGUI Writer => _writer;
    public TextMeshProUGUI CreatedAt => _createdAt;

    public void Setup(MessageData data)
    {
        _contentText.text = MessageColoring.MessageWithColor(data.content, "#000000");
        _writer.text = MessageColoring.MessageWithColor(data.writer, "#000000");
        _createdAt.text = MessageColoring.MessageWithColor(TimeFormatter.ToRelativeTime(data.createdAt), "#000000");
    }

    void Start()
    {
        transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
    }
}