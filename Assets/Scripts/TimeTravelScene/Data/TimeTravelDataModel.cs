using System.Collections.Generic;

// === Primitive 3D ===
[System.Serializable]
public class Location { public double latitude; public double longitude; public double altitude; }
[System.Serializable]
public class Rotation { public float x; public float y; public float z; }
[System.Serializable]
public class Scale    { public float x; public float y; public float z; }

[System.Serializable]
public class Transform3D
{
    public Location location;
    public Rotation rotation;
    public Scale scale;
}

// === Prefab/Placement ===
[System.Serializable]
public class PrefabDefinition
{
    public string id;
    public string name;
    public float  sizeMB;
    public List<string> tags;
}

[System.Serializable]
public class PrefabPlacementGroup
{
    public string group_id;
    public string prefab_id;
    public List<Transform3D> transforms;
    public bool? active; // optional
}

// === Root Layout ===
[System.Serializable]
public class LayoutRoot
{
    public string bundleId;
    public PrefabInfo[] prefabs;
    public PlacementGroup[] placementGroups;
}