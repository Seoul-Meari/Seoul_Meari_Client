using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GpsFormatter
{
    public static string ToGpsString(this Vector3 gpsVector)
    {
        return $"latitude={gpsVector.y:F6}, longitude={gpsVector.x:F6}, altitude={gpsVector.z:F2}";
    }
}
