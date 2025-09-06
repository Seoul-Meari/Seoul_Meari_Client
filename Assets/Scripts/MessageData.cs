using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable] // Inspector 창에서 보이게 함
public class MessageData
{
    public string content;    // 메시지 내용
    public Vector3 position;  // 메시지가 나타날 위치 (AR 공간 좌표)
    public double latitude;
    public double longitude;
    public MessageData(string content)
    {
        this.content = content;
    }
}