using System;

[System.Serializable]
public class LocationData
{
  public double latitude { get; set; }
  public double longitude { get; set; }
  public float? z { get; set; }

  public LocationData(double latitude, double longitude, float z)
  {
    this.latitude = latitude;
    this.longitude = longitude;
    this.z = z;
  }
}