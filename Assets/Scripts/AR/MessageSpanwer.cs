// MessageSpawner.cs
using UnityEngine;
using System.Collections.Generic;

public class MessageSpawner : MonoBehaviour
{
    public GameObject messagePrefab;

    private Dictionary<MessageData, GameObject> activeMessages = new Dictionary<MessageData, GameObject>();

    public void SpawnMessage(MessageData data)
    {
        if (activeMessages.ContainsKey(data)) return;

        Debug.Log($"Spawning message: {data.content}");

        LocationInfo currentUserLocation = Input.location.lastData;
        Vector3 position = ConvertGpsToWorldPosition(data.latitude, data.longitude, currentUserLocation.latitude, currentUserLocation.longitude, data.z);

        GameObject messageObject = Instantiate(messagePrefab, position, Quaternion.identity, this.transform);
        // messagePrefab에 MessageDisplay.cs와 같은 스크립트가 있고, Setup 함수가 있다고 가정합니다.
        // messageObject.GetComponent<MessageDisplay>().Setup(data); 
        
        activeMessages.Add(data, messageObject);
    }

    public void HideMessage(MessageData data)
    {
        if (activeMessages.TryGetValue(data, out GameObject messageObject))
        {
            Debug.Log($"Hiding message: {data.content}");
            Destroy(messageObject);
            activeMessages.Remove(data);
        }
    }

    private Vector3 ConvertGpsToWorldPosition(double targetLat, double targetLon, double baseLat, double baseLon, float? z)
    {
        float worldX = (float)((targetLon - baseLon) * 89000f);
        float worldZ = (float)((targetLat - baseLat) * 111000f);
        float worldY = z ?? 1.0f;
        return new Vector3(worldX, worldY, worldZ);
    }
}