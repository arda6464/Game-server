using System.Text.Json;
using System.Numerics;

namespace Logic
{
    public static class MapManager
    {
        private static Dictionary<int, MapData> maps = new();
        private static readonly string MapsFilePath = "maps.json";

        public static void LoadMaps()
        {
            try
            {
                if (!File.Exists(MapsFilePath))
                {
                    CreateDefaultMaps();
                }

                string json = File.ReadAllText(MapsFilePath);
                var mapList = JsonSerializer.Deserialize<List<MapData>>(json);
                
                if (mapList != null)
                {
                    maps = mapList.ToDictionary(m => m.Id);
                    Logger.genellog($"[MapManager] {maps.Count} harita yüklendi.");
                }
            }
            catch (Exception ex)
            {
                Logger.errorslog($"[MapManager] Harita yükleme hatası: {ex.Message}");
            }
        }

        private static void CreateDefaultMaps()
        {
            var defaultMaps = new List<MapData>
            {
                new MapData
                {
                    Id = 1,
                    Name = "Desert Arena",
                    SpawnPoints = new List<Vector3>
                    {
                        new Vector3(0, 0, 0),
                        new Vector3(15, 0, 15),
                        new Vector3(-15, 0, -15),
                        new Vector3(0, 0, 15),
                        new Vector3(15, 0, 0)
                    }
                },
                new MapData
                {
                    Id = 2,
                    Name = "Ice Tundra",
                    SpawnPoints = new List<Vector3>
                    {
                        new Vector3(5, 0, 5),
                        new Vector3(-5, 0, -5),
                        new Vector3(20, 0, 20),
                        new Vector3(-20, 0, -20)
                    }
                }
            };

            string json = JsonSerializer.Serialize(defaultMaps, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(MapsFilePath, json);
            Logger.genellog("[MapManager] Varsayılan maps.json oluşturuldu.");
        }

        public static MapData? GetMap(int id)
        {
            maps.TryGetValue(id, out var map);
            return map;
        }

        public static MapData GetRandomMap()
        {
            if (maps.Count == 0) LoadMaps();
            var list = maps.Values.ToList();
            return list[new Random().Next(list.Count)];
        }
    }
}
