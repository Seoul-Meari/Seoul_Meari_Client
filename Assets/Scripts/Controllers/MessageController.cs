// AppController.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class AppController : MonoBehaviour
{
    [Tooltip("메시지가 보이기 시작하는 거리 (미터 단위)")]
    public float viewingDistance = 20.0f;

    // Unity 인스펙터 창에서 각 스크립트 컴포넌트를 드래그하여 연결해줘야 합니다.
    public GpsService gpsService;
    public MessageRepository messageRepository;
    public MessageSpawner messageSpawner;
    private List<MessageData> messagesCurrentlyVisible = new List<MessageData>();

    void Start()
    {
        // GpsService의 위치 업데이트 이벤트가 발생하면 UpdateVisibleMessages 함수를 호출하도록 등록
        gpsService.OnLocationUpdated.AddListener(UpdateVisibleMessages);
    }

    private void OnDestroy()
    {
        // 오브젝트 파괴 시 이벤트 리스너를 안전하게 제거
        if (gpsService != null)
        {
            gpsService.OnLocationUpdated.RemoveListener(UpdateVisibleMessages);
        }
    }

    // GPS 위치가 바뀔 때마다 GpsService에 의해 자동으로 호출될 함수
    private void UpdateVisibleMessages(Vector2 currentGpsPosition)
    {
        if (!gpsService.IsInitialized) return;

        List<MessageData> messagesToShow = messageRepository.GetMessagesNear(currentGpsPosition, viewingDistance);
        List<MessageData> messagesToHide = messagesCurrentlyVisible.Except(messagesToShow).ToList();

        // 새로 보여줘야 할 메시지들을 스폰
        foreach (var msg in messagesToShow)
        {
            if (!messagesCurrentlyVisible.Contains(msg))
            {
                messageSpawner.SpawnMessage(msg);
            }
        }
        
        // 사라져야 할 메시지들을 숨김
        foreach (var msg in messagesToHide)
        {
            messageSpawner.HideMessage(msg);
        }

        // 현재 보이는 메시지 목록을 최신화
        messagesCurrentlyVisible = messagesToShow;
    }
}