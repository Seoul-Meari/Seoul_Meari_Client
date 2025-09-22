// MessageData.cs
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

[System.Serializable]
public class MessageData : System.IEquatable<MessageData>
{
    public string id;
    public string writer;
    public string content;
    public string createdAt;
    public LocationData location;

    #nullable enable
    public Texture2D? image;
    public string? imageKey;

    //새로 생성되는 메시지 데이터
    public MessageData(string writer, string content, LocationData location, Texture2D image)
    {
        this.id = Guid.NewGuid().ToString(); // ID 자동 생성
        this.writer = writer;
        this.content = content;
        this.createdAt = DateTime.UtcNow.ToString("o");
        this.location = location;
        this.image = image;
    }

    //서버에서 가져오는 메시지 데이터
    [JsonConstructor]
    public MessageData(string id, string writer, string content, string createdAt, LocationData location, string imageKey)
    {
        this.id = id;
        this.writer = writer;
        this.content = content;
        this.createdAt = createdAt;
        this.location = location;
        this.imageKey = imageKey;
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

// MessageData를 감싸기 위한 Wrapper 클래스
// GET API 호출 시 사용
[System.Serializable]
public class MessageList
{
    public List<MessageData> messages;
}