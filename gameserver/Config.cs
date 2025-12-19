using System.IO;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using System.Threading;

public class Config
{
    public static Config Instance;
    private static FileSystemWatcher? _watcher;
    private static string? _configFilePath;
    private static readonly object _reloadLock = new object();

    [JsonProperty("Port")] public readonly int Port;
    // [JsonProperty("mysql_host")] public readonly string MysqlHost;

    //[JsonProperty("mysql_port")] public readonly int MysqlPort;
    //[JsonProperty("mysql_username")] public readonly string MysqlUsername;
    //[JsonProperty("mysql_password")] public readonly string MysqlPassword;
    //[JsonProperty("mysql_database")] public readonly string MysqlDatabase;
    [JsonProperty("anti-ddos")] public readonly bool antiddos;
    [JsonProperty("BotToken")] public readonly string BotToken;
    [JsonProperty("ChannelId")] public readonly ulong ChannelId;
    [JsonProperty("CreatorCodes")] public readonly string CreatorCodes;
    [JsonProperty("AllowEmulators")] public readonly bool AllowEmulators;
    [JsonProperty("Maintance")] public readonly bool Maintance;
    [JsonProperty("EmailPassword")] public readonly string EmailPassword;
    [JsonProperty("Email")] public readonly string Email;
    [JsonProperty("UpdateLink")] public readonly string UpdateLink;
    [JsonProperty("ServerVersion")] public readonly string ServerVersion;

    public static void  LoadFromFile(string filename)
    {
        try
        {
            if (!File.Exists(filename))
            {
                Logger.errorslog($"[Config] Dosya bulunamadı: {filename}");
               return;
            }

            var json = File.ReadAllText(filename);
            var config = JsonConvert.DeserializeObject<Config>(json);
            Instance = config;
            
            if (config == null)
            {
                Logger.errorslog("[Config] JSON deserialize hatası!");
                return ;
            }

            Logger.genellog($"[Config] Config yüklendi");
               
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[Config] Yükleme hatası: {ex.Message}");
            return;
        }
    }

    // Config dosyası değişikliklerini izle
    public static void StartWatcher(string configFilePath)
    {
       
        
        if (!File.Exists(configFilePath))
        {
            Logger.errorslog($"[Config] Watcher başlatılamadı, dosya bulunamadı: {configFilePath}");
            return;
        }

        try
        {
            _watcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(Path.GetFullPath(configFilePath)),
                Filter = Path.GetFileName(configFilePath),
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            _watcher.Changed += OnConfigChanged;
            _watcher.Error += OnWatcherError;

            Logger.genellog($"[Config] FileSystemWatcher başlatıldı: {configFilePath}");
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[Config] Watcher başlatma hatası: {ex.Message}");
        }
    }

    private static void OnConfigChanged(object sender, FileSystemEventArgs e)
    {
        // Dosya yazma işlemi tamamlanana kadar bekle
        Thread.Sleep(100);
        
        lock (_reloadLock)
        {
            try
            {
                Logger.genellog("[Config] Config dosyası değişti, yeniden yükleniyor...");
                
                LoadFromFile(_configFilePath);
               
                    
                    Logger.genellog($"[Config] Config başarıyla yenilendi!");
                    
                    // Tüm değişiklikleri kontrol et ve güncelle
                   
                
            
            }
            catch (Exception ex)
            {
                Logger.errorslog($"[Config] Yenileme hatası: {ex.Message}");
            }
        }
    }

    private static void OnWatcherError(object sender, ErrorEventArgs e)
    {
        Exception ex = e.GetException();
        if (ex != null)
        {
            Logger.errorslog($"[Config] FileSystemWatcher hatası: {ex.Message}");
        }
    }

    // Config değişikliklerine göre tüm sistemleri güncelle
   

    // Watcher'ı durdur
    public static void StopWatcher()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _watcher = null;
            Logger.genellog("[Config] FileSystemWatcher durduruldu.");
        }
    }
}
