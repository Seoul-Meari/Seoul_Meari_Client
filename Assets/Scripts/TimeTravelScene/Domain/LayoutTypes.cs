using System;
using System.Collections.Generic;
using UnityEngine;

// ===== JSON/DTO =====
[Serializable] public class PrefabInfo { public string id; public string name; }
[Serializable] public class PlacementGroup { public string groupId; public string prefabId; public TransformData[] transforms; public bool? active; }
[Serializable] public class TransformData { public LocationData location; public Rotation rotation; public Scale scale; }

// 필요 타입들 (프로젝트에 이미 있다면 이 블록은 생략)
[Serializable] public class LayoutRoot { public string bundleId; public PrefabInfo[] prefabs; public PlacementGroup[] placementGroups; }
[Serializable] public class Rotation { public float x; public float y; public float z; }
[Serializable] public class Scale { public float x; public float y; public float z; }

[System.Serializable]
public class Transform3D
{
  public LocationData location;
  public Rotation rotation;
  public Scale scale;
}

// === Prefab/Placement ===
[System.Serializable]
public class PrefabDefinition
{
  public string id;
  public string name;
  public float sizeMB;
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