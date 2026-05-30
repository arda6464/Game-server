using DietPhysics;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

public class ServerMapData
{
    public List<WallData> walls;
    public List<Vec3> spawnPoints;
    public List<Vec3> lootPoints;
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
        if (!File.Exists(path))
        {
            string fallback = Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, "MapData.json");
            if (File.Exists(fallback))
                path = fallback;
        }

        string json = File.ReadAllText(path);
        LoadedMap = JsonConvert.DeserializeObject<ServerMapData>(json);
        LoadedMap ??= new ServerMapData();
        LoadedMap.walls ??= new List<WallData>();
        LoadedMap.spawnPoints ??= new List<Vec3>();
        LoadedMap.lootPoints ??= new List<Vec3>();
        System.Console.WriteLine($"[JSON] {LoadedMap.walls.Count} duvar, {LoadedMap.spawnPoints.Count} spawn ve {LoadedMap.lootPoints?.Count ?? 0} loot noktasÄ± yÃ¼klendi.");
    }

    // OYUNCUYU DOĞURMA MANTIĞI
    public static Vec3 GetRandomSpawnPoint()
    {
        if (LoadedMap.spawnPoints.Count == 0) return new Vec3(0, 0, 0);
        
        Random rnd = new Random();
        int index = rnd.Next(LoadedMap.spawnPoints.Count);
        return LoadedMap.spawnPoints[index];
    }

    public static Vec3 GetRandomLootPoint()
    {
        if (LoadedMap?.lootPoints == null || LoadedMap.lootPoints.Count == 0)
            return GetRandomSpawnPoint();

        Random rnd = new Random();
        int index = rnd.Next(LoadedMap.lootPoints.Count);
        return LoadedMap.lootPoints[index];
    }
}
