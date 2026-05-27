using DietPhysics;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

public class ServerMapData
{
    public List<WallData> walls;
    public List<Vec3> spawnPoints;
}

public class WallData
{
    public Vec3 pos;
    public Vec3 size;
    public Vec3 center;
    public Vec3 rot;
}

public static class MapManager
{
    public static ServerMapData LoadedMap;

    public static void Load(string path)
    {
        string json = File.ReadAllText(path);
        LoadedMap = JsonConvert.DeserializeObject<ServerMapData>(json);
        System.Console.WriteLine($"[JSON] {LoadedMap.walls.Count} duvar ve {LoadedMap.spawnPoints.Count} spawn noktası yüklendi.");
    }

    // OYUNCUYU DOĞURMA MANTIĞI
    public static Vec3 GetRandomSpawnPoint()
    {
        if (LoadedMap.spawnPoints.Count == 0) return new Vec3(0, 0, 0);
        
        Random rnd = new Random();
        int index = rnd.Next(LoadedMap.spawnPoints.Count);
        return LoadedMap.spawnPoints[index];
    }
}