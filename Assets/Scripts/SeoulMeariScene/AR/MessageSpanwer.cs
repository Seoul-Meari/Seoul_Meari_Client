using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class MessageSpawner : MonoBehaviour
{
    public GameObject messagePrefabServer;
    public GameObject messagePrefabPerson;
    public TMP_InputField messageBox;
    private Dictionary<MessageData, GameObject> activeMessages = new Dictionary<MessageData, GameObject>();
    private string tempWriter = "Seoul Meari";

    public void SpawnMessage(MessageData data)
    {
        if (activeMessages.ContainsKey(data)) return;
        Vector3 position = ConvertGpsToWorldPosition(data);
        GameObject messageObject = Instantiate(messagePrefabServer, position, Quaternion.identity, this.transform);
        messageObject.GetComponent<MessageDisplay>().Setup(data);
        activeMessages.Add(data, messageObject);
    }

    public void HideMessage(MessageData data)
    {
        if (activeMessages.TryGetValue(data, out GameObject messageObject))
        {
            Destroy(messageObject);
            activeMessages.Remove(data);
        }
    }

    public void SendMessage()
    {
        if (messageBox.text.Length > 0)
        {
            TransferMessage(messageBox.text);
            messageBox.text = "";
        }
    }

    public void TransferMessage(string content, float spawnDistance = 0.3f)
    {
        Transform cameraTransform = Camera.main.transform;
        Vector3 spawnPosition = cameraTransform.position + cameraTransform.forward * spawnDistance;
        GameObject messageObject = Instantiate(messagePrefabPerson, spawnPosition, Quaternion.identity);

        Vector2 pos = GpsService.Instance.CurrentPosition;
        MessageData showMessageData = new(tempWriter, content, new LocationData(pos.x, pos.y, 0));
        messageObject.GetComponent<MessageDisplay>().Setup(showMessageData);
        
        NetworkManager.Instance.SendMessage(showMessageData);
    }

    private Vector3 ConvertGpsToWorldPosition(MessageData data)
    {
        var baseLat = Input.location.lastData.latitude;
        var baseLon = Input.location.lastData.longitude;
        float worldX = (float)((data.location.longitude - baseLon) * 89000f);
        float worldZ = (float)((data.location.latitude - baseLat) * 111000f);
        
        // data.z 값이 null이면 기본값으로 1.0f를 사용하도록 수정
        float worldY = data.location.z ?? 1.0f; 

        return new Vector3(worldX, worldY, worldZ);
    }
}