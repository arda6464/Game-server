using System.Numerics;

namespace Logic
{
    public class MapData
    {
        public int Id { get; set; }
        public string Name { get; set; } = "Default Map";
        public List<Vector3> SpawnPoints { get; set; } = new List<Vector3>();
    }
}
