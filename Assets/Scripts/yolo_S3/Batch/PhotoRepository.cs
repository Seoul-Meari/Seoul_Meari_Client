using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PhotoRepository
{
    private static readonly string[] exts = { ".jpg", ".jpeg", ".png" };

    public List<string> ListImages(string folder)
    {
        var list = new List<string>();
        if (!Directory.Exists(folder)) return list;

        foreach (var ext in exts)
            list.AddRange(Directory.GetFiles(folder, "*" + ext, SearchOption.TopDirectoryOnly));

        // 시간순 정렬(선택)
        list.Sort(StringComparer.OrdinalIgnoreCase);
        return list;
    }

    public byte[] ReadAllBytes(string path)
    {
        try { return File.ReadAllBytes(path); }
        catch { return null; }
    }

    public Texture2D LoadTexture(byte[] bytes)
    {
        try
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(bytes)) { UnityEngine.Object.Destroy(tex); return null; }
            return tex;
        }
        catch { return null; }
    }

    public void SafeDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (Exception e) { Debug.LogWarning("[PhotoRepo] delete fail: " + e.Message); }
    }

    public string GuessContentTypeByExtension(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return (ext == ".png") ? "image/png" :
               (ext == ".jpg" || ext == ".jpeg") ? "image/jpeg" : null;
    }
}