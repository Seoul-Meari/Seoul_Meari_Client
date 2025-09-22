using System;
using UnityEngine;

[Serializable]
public struct DetectionResult
{
    public string ClassName;
    public float Confidence;
    public Rect Box;
}