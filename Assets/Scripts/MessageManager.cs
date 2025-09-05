using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class MessageManager : MonoBehaviour
{
    public GameObject messagePrefab;
    public Button spawnButton;
    
    // 카메라로부터 얼마나 앞에 생성할지 거리를 설정하는 변수 추가
    public float distanceFromCamera = 1.5f;

    private List<MessageData> messageDatabase = new List<MessageData>
    {
        // 위치(position) 데이터는 이제 사용되지 않지만, 내용은 그대로 사용합니다.
        new MessageData { content = "Hello World! Seoul Meari" },
        new MessageData { content = "Hello Js" },
        new MessageData { content = "Hello Ms" }
    };

    private int currentIndex = 0;

    public void SpawnNextMessage()
    {
        if (currentIndex < messageDatabase.Count)
            {
                MessageData dataToSpawn = messageDatabase[currentIndex];

                Transform cameraTransform = Camera.main.transform;
                Vector3 spawnPosition = cameraTransform.position + cameraTransform.forward * distanceFromCamera;

                // --- 이 부분이 바뀌었습니다 ---
                // 3. 계산된 위치에 프리팹을 생성합니다. (회전값은 기본값인 Quaternion.identity 사용)
                GameObject messageObject = Instantiate(messagePrefab, spawnPosition, Quaternion.identity);
                // --- 여기까지 ---

                MessageDisplay display = messageObject.GetComponent<MessageDisplay>();
                if (display != null)
                {
                    display.Setup(dataToSpawn);
                }

                currentIndex++;
            }

        if (currentIndex >= messageDatabase.Count)
        {
            if (spawnButton != null)
            {
                spawnButton.interactable = false;
            }
        }
    }
}