
using System;
using Newtonsoft.Json;
using UnityEngine;

[System.Serializable]
public class DocentData
{
    public string gps;
    public string img_url;
    public string question;

    [JsonConstructor]
    public DocentData(string gps, string img_url, string question)
    {
        this.gps = gps;
        this.img_url = img_url;
        this.question = question;
    }
}

[Serializable]
public class DocentRes
{
    public bool success;
    public string answer; 
}