// MessageData.cs
using System;

[System.Serializable]
public class MessageData : System.IEquatable<MessageData>
{
    // 외부에서 읽을 수 있도록 public get; 으로 변경하는 것이 좋습니다.
    public string id { get; private set; }
    public string writer { get; private set; }
    public string content { get; private set; }
    public DateTime createdAt { get; private set; }
    public LocationData location;

    //새로 생성되는 메시지 데이터
    public MessageData(string writer, string content, LocationData location)
    {
        this.id = Guid.NewGuid().ToString(); // ID 자동 생성
        this.writer = writer;
        this.content = content;
        this.createdAt = DateTime.UtcNow;
        this.location = location;
    }

    //서버에서 가져오는 메시지 데이터
    public MessageData(string id, string writer, string content, DateTime createdAt, LocationData location)
    {
        this.id = id;
        this.writer = writer;
        this.content = content;
        this.createdAt = createdAt;
        this.location = location;
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