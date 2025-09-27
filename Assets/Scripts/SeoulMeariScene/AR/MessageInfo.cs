using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MessageInfo : MonoBehaviour
{
    [Header("Message")]
    [SerializeField] private TextMeshProUGUI _contentText;
    [SerializeField] private TextMeshProUGUI _writer;
    [SerializeField] private TextMeshProUGUI _createdAt;
    [SerializeField] private RawImage _contentImage;
    [SerializeField] public string _imageKey;

    public TextMeshProUGUI ContentText => _contentText;
    public TextMeshProUGUI Writer => _writer;
    public TextMeshProUGUI CreatedAt => _createdAt;

    #nullable enable
    public RawImage? ContentImage => _contentImage;
    public string? ImageKey => _imageKey;

    public void Setup(MessageData data)
    {
        _contentText.text = MessageColoring.MessageWithColor(data.content, "#000000");
        _writer.text = MessageColoring.MessageWithColor(data.writer, "#000000");
        _createdAt.text = MessageColoring.MessageWithColor(TimeFormatter.ToRelativeTime(data.createdAt), "#000000");
        if (data.image != null)
        {
            _contentImage.texture = data.image;
        }
        if (data.imageKey != null)
        {
            _imageKey = data.imageKey;
        }
    }

    public void setContentImage(Texture2D contentTexture)
    {
        _contentImage.texture = contentTexture;
    }

    void Start()
    {
        transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
    }
}