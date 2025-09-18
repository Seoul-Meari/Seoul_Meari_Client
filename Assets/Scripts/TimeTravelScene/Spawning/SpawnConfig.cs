using UnityEngine;

namespace VRContent.Spawning
{
    [CreateAssetMenu(fileName = "SpawnConfig", menuName = "VRContent/SpawnConfig")]
    public class SpawnConfig : ScriptableObject
    {
        [Min(1)] public int spawnPerFrame = 8;
        public bool enableLogs = false;
    }
}