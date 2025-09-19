using UnityEngine;

public static class GridConfig
{
    private const double _degree = 1e-5; // 0.00001
    public const int meter = 30;
    public static float interval
    {
        get
        {
            return (float)(_degree * (double)meter);
        }
    }
    public const double eps = 1e-12;  // 충분히 작은 epsilon
    public static int gridSize { get; } = 5;
}