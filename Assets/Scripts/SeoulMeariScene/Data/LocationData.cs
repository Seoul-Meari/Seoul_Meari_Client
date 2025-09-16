using System;

[System.Serializable]
public class LocationData
{
  public double latitude;
  public double longitude;
  public float z;

  public LocationData(double latitude, double longitude, float z = 0)
  {
    this.latitude = latitude;
    this.longitude = longitude;
    this.z = z;
  }
}