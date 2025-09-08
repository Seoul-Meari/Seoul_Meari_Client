// MessageRepository.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MessageRepository : MonoBehaviour
{
    private List<MessageData> allMessages = new List<MessageData>();

    void Awake()
    {
        GenerateTestData();
    }

    // 특정 위치와 거리(반경) 안에 있는 모든 메시지를 반환
    public List<MessageData> GetMessagesNear(Vector2 position, float radius)
    {
        return allMessages.Where(msg => 
            GpsService.CalculateDistance(position.x, position.y, msg.latitude, msg.longitude) <= radius
        ).ToList();
    }

    private void GenerateTestData()
    {
        // 원본 코드의 테스트 데이터 생성 로직
        double currentUserLat = 35.18713;
        double currentUserLon = 129.07420;

        allMessages.Add(new MessageData("북쪽 메시지", currentUserLat + 0.00001, currentUserLon, 1.5f));
        allMessages.Add(new MessageData("동쪽 메시지", currentUserLat, currentUserLon + 0.00001, 1.0f));
        allMessages.Add(new MessageData("남서쪽 메시지", currentUserLat - 0.00002, currentUserLon - 0.00002, 0.5f));
        allMessages.Add(new MessageData("조금 먼 북쪽 메시지", currentUserLat + 0.00027, currentUserLon, 2.0f));

        int messageCount = 50;
        double step = 0.000001;
        int gridSize = (int)Mathf.Ceil(Mathf.Sqrt(messageCount));

        int index = 1;
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                if (index > messageCount) break;
                double lat = currentUserLat + (i - gridSize / 2) * step;
                double lon = currentUserLon + (j - gridSize / 2) * step;
                string name = $"주변 메시지 {index}";
                float height = UnityEngine.Random.Range(0.5f, 2.0f);
                allMessages.Add(new MessageData(name, lat, lon, height));
                index++;
            }
        }
    }
}