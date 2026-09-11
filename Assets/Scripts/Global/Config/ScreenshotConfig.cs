using System.IO;
using UnityEngine;

public static class ScreenShotConfig
{
    private static string subFolderName = "Screenshots";
    public static string screenshotFolderPath = Path.Combine(Application.persistentDataPath, subFolderName);
}