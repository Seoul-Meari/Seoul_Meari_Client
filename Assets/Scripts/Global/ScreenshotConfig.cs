using System.IO;
using UnityEngine;

public static class ScreenShotConfig
{
    private static string subFolderName = "MyScreenshots";
    public static string screenshotFolderPath = Path.Combine(Application.persistentDataPath, subFolderName);
}