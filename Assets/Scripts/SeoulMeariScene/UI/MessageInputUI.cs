using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MessageInputUI : MonoBehaviour
{
    public TMP_InputField messageInputField;
    public Button submitButton;
    public MessageSpawner messageSpawner;

    void Start()
    {
        submitButton.onClick.AddListener(SubmitMessage);
    }

    private void SubmitMessage()
    {
        string messageText = messageInputField.text;
        if (string.IsNullOrWhiteSpace(messageText)) return;
        
        SendMessageToServer(messageText, GpsService.Instance.CurrentPosition);
        messageSpawner.TransferMessage(messageText);
        messageInputField.text = "";
    }

    private void SendMessageToServer(string message, Vector3 position)
    {
        Debug.Log($"서버로 전송 시도: \"{message}\" at ({position.x}, {position.y})");
        // 실제 서버 통신 코드가 들어갈 자리입니다.
    }
}