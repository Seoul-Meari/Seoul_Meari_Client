using UnityEngine;
using System.Collections.Generic;
using TMPro;
using CesiumForUnity;
using System.Collections;
using System.Linq;

public class MessageSpawner : MonoBehaviour
{
    private static MessageSpawner _instance;
    public static MessageSpawner Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<MessageSpawner>();
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject(nameof(MessageSpawner));
                    _instance = singletonObject.AddComponent<MessageSpawner>();
                }
            }
            return _instance;
        }
    }

    public GameObject messagePrefabServer;
    public GameObject messagePrefabPerson;
    [SerializeField]
    private CesiumGeoreference georeference;
    public TMP_InputField messageBox;
    // 동시에 생성 진행 중일 수 있는 최대 개수(프레임 드랍 방지)
    [SerializeField] private int maxSpawnInflight = 5;
    // 한 프레임에 처리할 셀(그리드) 수 제한
    [SerializeField] private int maxCellsPerFrame = 2;

    private Dictionary<MessageData, GameObject> activeMessages = new Dictionary<MessageData, GameObject>();
    private string tempWriter = "Seoul Meari";

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    public IEnumerator RenderByDictionary(Queue<Vector3> renderMessageQue)
    {
        // 코루틴으로 큐를 나눠 처리
        yield return StartCoroutine(RenderByDictionaryRoutine(renderMessageQue));
    }

    private IEnumerator RenderByDictionaryRoutine(Queue<Vector3> renderMessageQue)
    {
        if (renderMessageQue == null) yield break;

        int processedCellsThisFrame = 0;

        while (renderMessageQue.Count > 0)
        {
            Vector3 nowPos = renderMessageQue.Dequeue();
            Vector3Int nowIntPos = Calculator.CalculateBenchMarkInt(nowPos);

            if (MessageCache.cachedMessageBoard != null &&
                MessageCache.cachedMessageBoard.TryGetValue(nowIntPos, out var messages) &&
                messages != null && messages.Count > 0)
            {
                // 메시지 스폰을 배치 코루틴으로 처리
                yield return StartCoroutine(SpawnMessages(messages));
            }

            // 프레임 분할
            processedCellsThisFrame++;
            if (processedCellsThisFrame >= maxCellsPerFrame)
            {
                processedCellsThisFrame = 0;
                yield return null;
            }
        }
    }


    public IEnumerator SpawnMessages(List<MessageData> messages)
    {
        int inflight = 0;

        for (int i = 0; i < messages.Count; i++)
        {
            var msg = messages[i];
            if (msg == null) continue;

            // 이미 스폰된 메시지는 스킵
            if (activeMessages.ContainsKey(msg)) continue;

            // 동시 진행 개수 제한
            while (inflight >= maxSpawnInflight)
                yield return null;

            inflight++;
            // 개별 스폰은 별도 코루틴으로 실행
            StartCoroutine(SpawnMessage(msg, () => { inflight--; }));
        }

        // 모두 끝날 때까지 대기
        while (inflight > 0)
            yield return null;
    }
    private IEnumerator SpawnMessage(MessageData message, System.Action onDone)
    {

        // 중복 체크(레이스 방지: 코루틴 사이 경쟁 가능)
        if (activeMessages.ContainsKey(message))
        {
            onDone?.Invoke();
            yield break;
        }

        // 위치 계산 (지오리퍼런스 사용)
        Vector3 position = Calculator.ToWorldPosition(georeference, message);
        position.z = Calculator.RandomAround(-1.0f, 1.5f);
        Debug.Log("z position value: " + position.z);

        // Instantiate
        GameObject messageObject = Instantiate(
            messagePrefabServer,
            position,
            Quaternion.identity,
            this.transform
        );

        // 초기화
        var display = messageObject.GetComponent<MessageDisplay>();
        if (display != null)
        {
            display.Setup(message);
        }
        else
        {
            Debug.LogWarning("MessageDisplay component missing on messagePrefabServer.");
        }

        // 등록
        activeMessages[message] = messageObject;

        onDone?.Invoke();
        yield break;
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

        Vector3 pos = GpsService.Instance.CurrentPosition;
        MessageData showMessageData = new(tempWriter, content, new LocationData(pos.x, pos.y, 0));
        messageObject.GetComponent<MessageDisplay>().Setup(showMessageData);

        NetworkManager.Instance.SendMessage(showMessageData);
    }

    // private Vector3 ConvertGpsToWorldPosition(MessageData data)
    // {
    //     var baseLat = Input.location.lastData.latitude;
    //     var baseLon = Input.location.lastData.longitude;
    //     float worldX = (float)((data.location.longitude - baseLon) * 89000f);
    //     float worldZ = (float)((data.location.latitude - baseLat) * 111000f);

    //     // data.z 값이 null이면 기본값으로 1.0f를 사용하도록 수정
    //     float worldY = data.location.z;

    //     return new Vector3(worldX, worldY, worldZ);
    // }
}