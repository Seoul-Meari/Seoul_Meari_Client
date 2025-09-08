using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class MessageData
{
    public string content;    // 메시지 내용
    public double latitude;   // 위도
    public double longitude;  // 경도
    public float? z;          // 고도 (Nullable, 값이 없을 수 있음)

    // 생성자 수정
    public MessageData(string content, double latitude, double longitude, float? z = null)
    {
        this.content = content;
        this.latitude = latitude;
        this.longitude = longitude;
        this.z = z;
    }
}