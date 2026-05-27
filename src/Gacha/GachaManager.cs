using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace GachaSystem
{
    [Serializable]
    public class GachaDrop
    {
        public RewardItem Reward { get; set; }
        public int Weight { get; set; } // Probability weight (e.g. 10, 50, 100)
    }

    [Serializable]
    public class GachaBox
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string IconKey { get; set; }
        public List<GachaDrop> Drops { get; set; } = new List<GachaDrop>();
    }

    public static class GachaManager
    {
        private static List<GachaBox> _boxes = new List<GachaBox>();
        private static readonly string SavePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database", "gacha_boxes.json");
        private static readonly Random _random = new Random();

        public static void Init()
        {
            Load();
            if (_boxes.Count == 0)
            {
                // Add a default box if none exists
                CreateDefaultBox();
            }
        }

        public static List<GachaBox> GetAllBoxes() => _boxes;

        public static void AddOrUpdateBox(GachaBox box)
        {
            var existing = _boxes.FirstOrDefault(b => b.Id == box.Id);
            if (existing != null)
            {
                _boxes.Remove(existing);
            }
            else if (box.Id == 0)
            {
                box.Id = _boxes.Count > 0 ? _boxes.Max(b => b.Id) + 1 : 1;
            }
            _boxes.Add(box);
            Save();
        }

        public static void RemoveBox(int id)
        {
            _boxes.RemoveAll(b => b.Id == id);
            Save();
        }

        public static RewardItem Roll(int boxId)
        {
            var box = _boxes.FirstOrDefault(b => b.Id == boxId);
            if (box == null || box.Drops.Count == 0) return null;

            int totalWeight = box.Drops.Sum(d => d.Weight);
            int roll = _random.Next(0, totalWeight);
            int current = 0;

            foreach (var drop in box.Drops)
            {
                current += drop.Weight;
                if (roll < current)
                {
                    return drop.Reward;
                }
            }

            return box.Drops.Last().Reward;
        }

        private static void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(SavePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(SavePath, JsonConvert.SerializeObject(_boxes, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GachaManager] Save Error: {ex.Message}");
            }
        }

        private static void Load()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    _boxes = JsonConvert.DeserializeObject<List<GachaBox>>(File.ReadAllText(SavePath)) ?? new List<GachaBox>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GachaManager] Load Error: {ex.Message}");
            }
        }

        private static void CreateDefaultBox()
        {
            var box = new GachaBox
            {
                Id = 1,
                Name = "Gümüş Sandık",
                Description = "İçinden şansına bağlı olarak altın veya elmas çıkabilir.",
                IconKey = "box_silver",
                Drops = new List<GachaDrop>
                {
                    new GachaDrop { Weight = 70, Reward = new RewardItem { Type = ItemType.Coins, Count = 500 } },
                    new GachaDrop { Weight = 25, Reward = new RewardItem { Type = ItemType.Coins, Count = 1500 } },
                    new GachaDrop { Weight = 5,  Reward = new RewardItem { Type = ItemType.Gems,  Count = 50 } }
                }
            };
            _boxes.Add(box);
            Save();
        }
    }
}
