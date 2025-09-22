using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class MessageCache : MonoBehaviour
{
    private static MessageCache _instance;
    public static MessageCache Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<MessageCache>();
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject(nameof(MessageCache));
                    _instance = singletonObject.AddComponent<MessageCache>();
                }
            }
            return _instance;
        }
    }
    public static Dictionary<Vector3Int, List<MessageData>> cachedMessageBoard = new Dictionary<Vector3Int, List<MessageData>>();
    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private static Queue<Vector3> insertMessageQue = new Queue<Vector3>();
    private static Queue<Vector3> removeMessageQue;

    public IEnumerator InitiateMessage(Vector3 nowPos)
    {
        int half = GridConfig.gridSize / 2;
        for (int i = -half; i <= half; i++)
        {
            for (int j = -half; j <= half; j++)
            {
                Vector3 cellPos = new Vector3(
                    nowPos.x + i * GridConfig.interval,
                    nowPos.y + j * GridConfig.interval,
                    0
                );
                insertMessageQue.Enqueue(cellPos);
            }
        }

        yield return StartCoroutine(RequestMessages(insertMessageQue));
    }

    public IEnumerator RequestMessages(Queue<Vector3> cellPoses)
    {
        //네트워크에 요청할 리스트 수 (쓰로틀링)
        int maxFetch = 5;
        int inflight = 0;
        while (cellPoses.Count > 0)
        {
            while (inflight >= maxFetch)
            {
                yield return null;
            }
            inflight++;
            Vector3 newCellPos = cellPoses.Dequeue();
            StartCoroutine(RequestMessage(newCellPos, onDone: () =>
            {
                inflight--;
            }));
        }

        while (inflight > 0)
        {
            yield return null;
        }
    }

    private IEnumerator RequestMessage(Vector3 cellPos, System.Action onDone)
    {
        //이미 데이터가 있는 경우
        if (cachedMessageBoard.ContainsKey(Calculator.CalculateBenchMarkInt(cellPos)))
        {
            onDone?.Invoke();
            yield break;
        }
        
        bool done = false;
        NetworkManager.Instance.RequestMessagesNear(
            cellPos,
            GridConfig.interval,
            onSuccess: messages =>
            {
                AddMessagesToDictionary(cellPos, messages);
                done = true;
            },
            onError: err =>
            {
                done = true;
            }
        );

        yield return new WaitUntil(() => done);

        if (done)
        {
            onDone?.Invoke();
        }
    }

    private void AddMessagesToDictionary(Vector3 pos, List<MessageData> messages)
    {
        Vector3Int newPos = Calculator.CalculateBenchMarkInt(pos);
        cachedMessageBoard[newPos] = messages;
    }

    // private IEnumerator RemoveMessages(List<Vector3> messagesPos)
    // {
    // }

    private void DeleteMessagesFromDictionary(Vector3 pos)
    {
        Vector3Int newPos = Calculator.CalculateBenchMarkInt(pos);
        cachedMessageBoard.Remove(newPos);
    }
}

