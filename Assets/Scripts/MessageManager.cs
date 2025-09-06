using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class MessageManager : MonoBehaviour
{
    public GameObject messagePrefab;
    public TMP_InputField messageContent;
    public Button spawnButton;
    
    // 카메라로부터 얼마나 앞에 생성할지 거리를 설정하는 변수 추가
    public float distanceFromCamera = 0.2f;

    public void SpawnNextMessage()
    {
        
        if (!string.IsNullOrEmpty(messageContent.text))
        {
            MessageData dataToSpawn = new(messageContent.text);
            messageContent.text = "";

            Transform cameraTransform = Camera.main.transform;
            Vector3 spawnPosition = cameraTransform.position + cameraTransform.forward * distanceFromCamera;

            GameObject messageObject = Instantiate(messagePrefab, spawnPosition, cameraTransform.rotation);

            MessageDisplay display = messageObject.GetComponent<MessageDisplay>();
            if (display != null)
            {
                display.Setup(dataToSpawn);
            }
        }
    }
}