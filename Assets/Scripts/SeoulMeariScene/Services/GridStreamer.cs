using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridStreamer : MonoBehaviour
{
    private Queue<Vector3> newGrid = new Queue<Vector3>();
    private Queue<Vector3> newRenderGrid = new Queue<Vector3>();
    private Queue<Vector3> oldGrid = new Queue<Vector3>();
    private Queue<Vector3> oldRenderGrid = new Queue<Vector3>();

    public bool IsInitialized = false;


    void Awake()
    {
        StartCoroutine(AddOnUpdateListener());
    }
    void Start()
    {

    }

    IEnumerator AddOnUpdateListener()
    {
        yield return new WaitUntil(() => GpsService.Instance != null && GpsService.Instance.IsInitialized);
        GpsService.Instance.OnIntLocationUpdated.AddListener(onUpdateGridPositionCoroutine);
        IsInitialized = true;
    }

    private void onUpdateGridPositionCoroutine(Vector3Int prev, Vector3Int current)
    {
        StartCoroutine(onUpdateGridPosition(prev, current));
    }

    private IEnumerator onUpdateGridPosition(Vector3Int prev, Vector3Int current)
    {
        DiffBands(prev, current, GridConfig.gridSize / 2, newGrid, oldGrid);
        DiffBands(prev, current, 1, newRenderGrid, oldRenderGrid);

        //데이터 추가 우선, 캐시 - 렌더링 순 (바라보고 있음)
        yield return StartCoroutine(MessageCache.Instance.RequestMessages(newGrid));
        yield return StartCoroutine(MessageSpawner.Instance.RenderByDictionary(newRenderGrid));

        //데이터 삭제, 언로드 - 캐시 순

    }

    void EnqueueRect(Vector3Int center, int x0, int x1, int y0, int y1, Queue<Vector3> q)
    {
        int m = GridConfig.meter;
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                Vector3 tmpPos = Calculator.CalculateIntToGps(new Vector3Int(center.x + x * m, center.y + y * m, center.z));
                q.Enqueue(tmpPos);
            }
    }

    void DiffBands(
    Vector3Int prev, Vector3Int curr, int half,
    Queue<Vector3> addQ, Queue<Vector3> removeQ)
    {
        int m = GridConfig.meter;
        int side = 2 * half + 1;

        int dxCells = (curr.x - prev.x) / m;
        int dyCells = (curr.y - prev.y) / m;

        int sx = Math.Sign(dxCells);
        int sy = Math.Sign(dyCells);

        int ax = Math.Min(Math.Abs(dxCells), side);
        int ay = Math.Min(Math.Abs(dyCells), side);

        // 1) Y-방향 띠 (위/아래 새로 들어온 줄 + 반대편 제거할 줄)
        if (ay > 0)
        {
            // add (curr 쪽)
            int yAdd0 = (sy > 0) ? (half - ay + 1) : (-half);
            int yAdd1 = (sy > 0) ? (half) : (-half + ay - 1);
            EnqueueRect(curr, -half, half, yAdd0, yAdd1, addQ);

            // remove (prev 쪽)
            int yRem0 = (sy > 0) ? (-half) : (half - ay + 1);
            int yRem1 = (sy > 0) ? (-half + ay - 1) : (half);
            EnqueueRect(prev, -half, half, yRem0, yRem1, removeQ);
        }

        // 2) X-방향 띠 (좌/우 새로 들어온 열 + 반대편 제거할 열)
        //    단, 위에서 이미 다룬 Y-띠(새로 들어온 줄/삭제 줄)과 겹치는 구간은 제외
        if (ax > 0)
        {
            // add (curr 쪽)
            int xAdd0 = (sx > 0) ? (half - ax + 1) : (-half);
            int xAdd1 = (sx > 0) ? (half) : (-half + ax - 1);

            // Y-겹침 제외: sy>0이면 윗부분 ay줄이 이미 처리됨, sy<0이면 아랫부분 ay줄이 이미 처리됨
            int yAdd0 = (sy < 0) ? (-half + ay) : (-half);
            int yAdd1 = (sy > 0) ? (half - ay) : (half);
            if (ay == 0) { yAdd0 = -half; yAdd1 = half; } // Y이동 없으면 전체 허용

            if (yAdd0 <= yAdd1)
                EnqueueRect(curr, xAdd0, xAdd1, yAdd0, yAdd1, addQ);

            // remove (prev 쪽)
            int xRem0 = (sx > 0) ? (-half) : (half - ax + 1);
            int xRem1 = (sx > 0) ? (-half + ax - 1) : (half);

            int yRem0 = (sy > 0) ? (-half + ay) : (-half);
            int yRem1 = (sy < 0) ? (half - ay) : (half);
            if (ay == 0) { yRem0 = -half; yRem1 = half; }

            if (yRem0 <= yRem1)
                EnqueueRect(prev, xRem0, xRem1, yRem0, yRem1, removeQ);
        }
    }
}