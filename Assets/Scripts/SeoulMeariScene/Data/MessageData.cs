// MessageData.cs
using System;

[System.Serializable]
public class MessageData : System.IEquatable<MessageData>
{
    // 외부에서 읽을 수 있도록 public get; 으로 변경하는 것이 좋습니다.
    public string id { get; private set; }
    public string content;
    public double latitude;
    public double longitude;
    public float? z;

    public MessageData(string content, double latitude, double longitude, float? z = null)
    {
        this.id = Guid.NewGuid().ToString(); // ID 자동 생성
        this.content = content;
        this.latitude = latitude;
        this.longitude = longitude;
        this.z = z;
    }

    public MessageData(string id, string content, double latitude, double longitude, float? z = null)
    {
        this.id = id; // 전달받은 ID를 그대로 사용
        this.content = content;
        this.latitude = latitude;
        this.longitude = longitude;
        this.z = z;
    }

    public bool Equals(MessageData other)
    {
        if (other is null)
            return false;
        
        return !string.IsNullOrEmpty(this.id) && this.id == other.id;
    }

    public override bool Equals(object obj) => Equals(obj as MessageData);

    public override int GetHashCode() => id?.GetHashCode() ?? base.GetHashCode();
}