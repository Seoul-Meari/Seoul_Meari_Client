
using Newtonsoft.Json;
using UnityEngine;

[System.Serializable]
public class DocentData
{
    public Vector3 gps;
    public string img_url;
    public string question;

    [JsonConstructor]
    public DocentData(Vector3 gps, string img_url, string question)
    {
        this.gps = gps;
        this.img_url = img_url;
        this.question = question;
    }
}
