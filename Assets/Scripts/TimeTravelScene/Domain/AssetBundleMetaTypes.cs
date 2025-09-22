using System;
using System.Collections.Generic;

[Serializable] public class AssetLocation { public double lon; public double lat; public double altitude; }
[Serializable] public class PrefabMeta { public string id; public string name; public List<string> tags; public double sizeMB; }

[Serializable]
public class AssetBundleMeta
{
    public string id;
    public string name;
    public string version;
    public string os;
    public string usage;
    public string status;
    public List<string> tags;
    public string description;
    public List<PrefabMeta> prefabs;
    public AssetLocation location;
}