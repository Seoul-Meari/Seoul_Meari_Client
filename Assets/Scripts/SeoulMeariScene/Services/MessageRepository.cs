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

    public void AddMessage(MessageData newMessage)
    {
        allMessages.Add(newMessage);
    }

    /// <summary>
    /// 요청사항에 맞춰 테스트 데이터를 생성하는 수정된 메서드입니다.
    /// 특정 위경도를 중심으로 10,000개의 메시지를 격자 형태로 생성합니다.
    /// </summary>
    private void GenerateTestData()
    {
        // 기존 데이터를 초기화합니다.
        allMessages.Clear();

        double centerLat = 35.1860;
        double centerLon = 129.0741;

        // 2. 생성할 총 메시지 개수
        int totalMessages = 100000;

        // 3. 메시지 간 간격 (위경도 단위)
        double step = 0.00001;

        // 5. 10,000개 메시지를 생성하기 위한 격자(Grid) 크기 계산 (100 x 100 = 10,000)
        int gridSize = (int)Mathf.Ceil(Mathf.Sqrt(totalMessages));

        int messageIndex = 1;
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                if (messageIndex > totalMessages) break;

                // 중심점을 기준으로 격자 위치를 계산합니다.
                // (i, j) 인덱스를 (-gridSize/2 ~ +gridSize/2) 범위로 변환하여 중심에 배치합니다.
                double latOffset = (i - gridSize / 2.0) * step;
                double lonOffset = (j - gridSize / 2.0) * step;

                // 최종 위도와 경도를 계산합니다.
                double lat = centerLat + latOffset;
                double lon = centerLon + lonOffset;

                // 메시지 데이터 생성 및 리스트에 추가
                string name = $"주변 메시지 {messageIndex} 연승 메롱";
                float height = UnityEngine.Random.Range(-0.5f, 0.5f);
                allMessages.Add(new MessageData(messageIndex.ToString(), name, lat, lon, height));

                messageIndex++;
            }
            if (messageIndex > totalMessages) break;
        }

        Debug.Log($"{allMessages.Count}개의 테스트 메시지가 생성되었습니다.");
    }
}