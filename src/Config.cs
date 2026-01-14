using System.IO;
using Newtonsoft.Json;
using System.Threading;

public class Config
{
    public static Config Instance { get; private set; }
    private static FileSystemWatcher _watcher;
    private static string _configFilePath;
    private static readonly object _reloadLock = new object();

    [JsonProperty("Port")] public int Port { get; private set; }
    [JsonProperty("anti-ddos")] public bool AntiDdos { get; private set; }
    [JsonProperty("BotToken")] public string BotToken { get; private set; }
    [JsonProperty("ChannelId")] public ulong ChannelId { get; private set; }
    [JsonProperty("DiscordAdminIDs")] public List<ulong> DiscordAdminIDs { get; private set; } = new List<ulong>();
    [JsonProperty("CreatorCodes")] public List<string> CreatorCodes { get; private set; } = new List<string>();
    [JsonProperty("AllowEmulators")] public bool AllowEmulators { get; private set; }
    [JsonProperty("Maintance")] public bool Maintenance { get; private set; }
    [JsonProperty("EmailPassword")] public string EmailPassword { get; private set; }
    [JsonProperty("Email")] public string Email { get; private set; }
    [JsonProperty("UpdateLink")] public string UpdateLink { get; private set; }
    [JsonProperty("ServerVersion")] public string ServerVersion { get; private set; }

    // Default değerlerle constructor
    public Config()
    {
        // Default değerler (dosya bulunamazsa kullanılacak)
        Port = 5000;
        AntiDdos = false;
        BotToken = "";
        ChannelId = 0;
        CreatorCodes = new List<string>();
        AllowEmulators = false;
        Maintenance = false;
        EmailPassword = "";
        Email = "";
        UpdateLink = "https://default-update.com";
        ServerVersion = "1.0.0";
         DiscordAdminIDs = new List<ulong>();
    }

    // Ana yükleme metodu
    public static void Load(string filename = "config.json")
    {
        try
        {
            _configFilePath = Path.GetFullPath(filename);
            
            if (!File.Exists(_configFilePath))
            {
                CreateDefaultConfig(_configFilePath);
                Logger.errorslog($"[Config] Dosya bulunamadı, default config oluşturuldu: {_configFilePath}");
            }

            LoadFromFile(_configFilePath);
            
            // Watcher'ı başlat
            StartWatcher();
            
            Logger.genellog($"[Config] Config başarıyla yüklendi: {_configFilePath}");
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[Config] Yükleme hatası: {ex.Message}");
            
            // Hata durumunda default config oluştur
            Instance = new Config();
        }
    }

    private static void LoadFromFile(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            var config = JsonConvert.DeserializeObject<Config>(json);
            
            if (config == null)
            {
                Logger.errorslog("[Config] JSON deserialize hatası!");
                Instance = new Config();
                return;
            }

            Instance = config;
            Logger.genellog($"[Config] {filePath} yüklendi");
        }
        catch (JsonException ex)
        {
            Logger.errorslog($"[Config] JSON parse hatası: {ex.Message}");
            Instance = new Config();
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[Config] Dosya okuma hatası: {ex.Message}");
            Instance = new Config();
        }
    }

    // Default config oluştur
    private static void CreateDefaultConfig(string filePath)
    {
        var defaultConfig = new Config();
        
        var json = JsonConvert.SerializeObject(defaultConfig, Formatting.Indented);
        File.WriteAllText(filePath, json);
        
        Logger.genellog($"[Config] Default config oluşturuldu: {filePath}");
    }

    // Watcher başlat
    private static void StartWatcher()
    {
        if (_watcher != null)
        {
            StopWatcher();
        }

        try
        {
            string directory = Path.GetDirectoryName(_configFilePath);
            string fileName = Path.GetFileName(_configFilePath);

            _watcher = new FileSystemWatcher
            {
                Path = directory,
                Filter = fileName,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            _watcher.Changed += OnConfigChanged;
            _watcher.Error += OnWatcherError;

            Logger.genellog($"[Config] Config değişiklikleri izleniyor: {_configFilePath}");
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[Config] Watcher başlatma hatası: {ex.Message}");
        }
    }

    private static void OnConfigChanged(object sender, FileSystemEventArgs e)
    {
        // Multiple event trigger'ı önlemek için debounce
        Thread.Sleep(500);
        
        lock (_reloadLock)
        {
            try
            {
                if (e.ChangeType == WatcherChangeTypes.Changed)
                {
                    Logger.genellog("[Config] Config dosyası değişti, yeniden yükleniyor...");
                    LoadFromFile(_configFilePath);
                    Logger.genellog("[Config] Config başarıyla yenilendi!");
                }
            }
            catch (Exception ex)
            {
                Logger.errorslog($"[Config] Yenileme hatası: {ex.Message}");
            }
        }
    }

    private static void OnWatcherError(object sender, ErrorEventArgs e)
    {
        Logger.errorslog($"[Config] FileSystemWatcher hatası: {e.GetException()?.Message}");
    }

    public static void StopWatcher()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= OnConfigChanged;
            _watcher.Error -= OnWatcherError;
            _watcher.Dispose();
            _watcher = null;

            Logger.genellog("[Config] FileSystemWatcher durduruldu.");
        }
    }
    public static bool AddAdmin(ulong id)
    {
        lock (_reloadLock) // Thread safety için lock
        {
            try
            {
                if (!Instance.DiscordAdminIDs.Contains(id))
                {
                    Instance.DiscordAdminIDs.Add(id);
                    SaveConfig();
                    Logger.genellog($"[Config] Admin eklendi: {id}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Logger.errorslog($"[Config] Admin ekleme hatası: {ex.Message}");
                return false;
            }
        }
    }
public static bool RemoveAdmin(ulong id)
    {
        lock (_reloadLock)
        {
            try
            {
                if (Instance?.DiscordAdminIDs == null)
                    return false;

                bool removed = Instance.DiscordAdminIDs.Remove(id);
                if (removed)
                {
                    SaveConfig();
                    Logger.genellog($"[Config] Admin silindi: {id}");

                }
                return removed;
            }
            catch (Exception ex)
            {
                Logger.errorslog($"[Config] Admin silme hatası: {ex.Message}");
                return false;
            }
        }
    }
    private static void SaveConfig()
    {
        try
        {
            var json = JsonConvert.SerializeObject(Instance, Formatting.Indented);
            File.WriteAllText(_configFilePath, json);
            Logger.genellog($"[Config] Config kaydedildi: {_configFilePath}");
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[Config] Kaydetme hatası: {ex.Message}");
        }
    }


    
}