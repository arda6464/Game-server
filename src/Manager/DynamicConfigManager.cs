using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading;

public enum EventType
{
    XPMultiplier = 1,
    GoldRain = 2,
    NoTrophyLoss = 3,
    DoubleTrophy = 4
}

public class ActiveEvent
{
    public EventType Type { get; set; }
    public int Value { get; set; }
    public int Value2 { get; set; }
    public DateTime EndTime { get; set; } = DateTime.MaxValue;
    public bool IsStarted {get;set;} 
    public DateTime StartTime {get;set;}
}
public class CustomError
{
    public string Title { get; set; }
    public string Message { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Now;
}

public class DynamicConfig
{
    public bool IsMatchmakingEnabled { get; set; } = true;
    public bool IsShopEnabled { get; set; } = true;
    public bool IsRankSystemEnabled { get; set; } = true;
    
    public List<ActiveEvent> ActiveEvents { get; set; } = new List<ActiveEvent>();
    public List<CustomError> CustomErrors { get; set; } = new List<CustomError>();
}

public static class DynamicConfigManager
{
    public static DynamicConfig Config { get; private set; } = new DynamicConfig();
    private static string _filePath = "dynamic_config.json";
    private static readonly object _lock = new object();

    static DynamicConfigManager()
    {
        Load();
    }

    public static void Load()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    Config = JsonConvert.DeserializeObject<DynamicConfig>(json) ?? new DynamicConfig();
                    Logger.genellog("[DynamicConfig] Yapılandırma yüklendi.");
                }
                else
                {
                    Save();
                    Logger.genellog("[DynamicConfig] Varsayılan yapılandırma oluşturuldu.");
                }
            }
            catch (Exception ex)
            {
                Logger.errorslog($"[DynamicConfig] Yükleme hatası: {ex.Message}");
                Config = new DynamicConfig();
            }
        }
    }

    public static void Save()
    {
        lock (_lock)
        {
            try
            {
                var json = JsonConvert.SerializeObject(Config, Formatting.Indented);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                Logger.errorslog($"[DynamicConfig] Kaydetme hatası: {ex.Message}");
            }
        }
    }

    public static void Update(DynamicConfig newConfig)
    {
        lock (_lock)
        {
            Config = newConfig;
            Save();
            Logger.genellog("[DynamicConfig] Yapılandırma güncellendi.");
        }
    }

    public static bool IsEventActive(EventType type)
    {
        ProcessEvents();
        return Config.ActiveEvents.Exists(e => e.Type == type && e.IsStarted);
    }

    public static int GetEventValue(EventType type, int defaultValue = 1)
    {
        ProcessEvents();
        var evt = Config.ActiveEvents.Find(e => e.Type == type && e.IsStarted);
        return evt != null ? evt.Value : defaultValue;
    }

    public static void ProcessEvents()
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            bool changed = false;

            // Süresi dolanları kaldır
            int removedCount = Config.ActiveEvents.RemoveAll(e => e.EndTime <= now);
            if (removedCount > 0)
            {
                changed = true;
                Logger.genellog($"[DynamicConfig] {removedCount} adet süresi dolan etkinlik kaldırıldı.");
            }

            // Başlama zamanı gelenleri aktif et veya vakti geçmemiş ama "IsStarted" hatalı olanları düzelt
            foreach (var evt in Config.ActiveEvents)
            {
                bool shouldBeStarted = now >= evt.StartTime && now < evt.EndTime;
                if (evt.IsStarted != shouldBeStarted)
                {
                    evt.IsStarted = shouldBeStarted;
                    changed = true;
                    Logger.genellog($"[DynamicConfig] Etkinlik durumu güncellendi: {evt.Type} -> {(evt.IsStarted ? "Başlatıldı" : "Beklemeye alındı")}");
                }
            }

            if (changed)
            {
                Save();
            }
        }
    }
}
