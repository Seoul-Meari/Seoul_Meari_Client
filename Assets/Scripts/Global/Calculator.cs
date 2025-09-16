using CesiumForUnity;
using Unity.Mathematics;
using UnityEngine;
using System.Linq;

public static class Calculator
{
  public static Vector3Int CalculateBenchMarkInt(Vector3 pos)
  {
    Vector3Int benchMark = new Vector3Int();

    // 중간 계산을 모두 double로 처리하여 정밀도를 높입니다.
    double posX = pos.x;
    double posY = pos.y;
    double interval = GridConfig.interval; // GridConfig.interval이 float이므로 double로 변환

    benchMark.x = (int)(System.Math.Floor(posX / interval) * GridConfig.meter);
    benchMark.y = (int)(System.Math.Floor(posY / interval) * GridConfig.meter);
    benchMark.z = (int)pos.z;
    return benchMark;
  }

  public static Vector3 CalculateIntToGps(Vector3Int pos)
  {
    Vector3 gps = new Vector3();
    gps.x = ((float)(pos.x / GridConfig.meter)) * GridConfig.interval;
    gps.y = ((float)(pos.y / GridConfig.meter)) * GridConfig.interval;
    gps.z = ((float)(pos.z / GridConfig.meter)) * GridConfig.interval;
    return gps;
  }

  public static Vector3 ToWorldPosition(CesiumGeoreference georeference, MessageData data)
  {
    if (georeference == null)
    {
      Debug.LogError("CesiumGeoreference가 null입니다.");
      return Vector3.zero;
    }

    double lon = data.location.longitude;
    double lat = data.location.latitude;
    double h = data.location.z;

    // 1) LLA -> ECEF
    double3 ecef = CesiumWgs84Ellipsoid.LongitudeLatitudeHeightToEarthCenteredEarthFixed(
        new double3(lon, lat, h)
    );

    double3 transformedEcef = georeference.TransformEarthCenteredEarthFixedPositionToUnity(ecef);
    Vector3 vectoredEcef = new Vector3((float)transformedEcef.x, (float)transformedEcef.y, (float)transformedEcef.z);

    // 2) ECEF -> Unity World
    return vectoredEcef;
  }


  public static float RandomAround(float range1, float range2)
  {
    return UnityEngine.Random.Range(range1, range2);
  }
}