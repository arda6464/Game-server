using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Linq;

public class ClientErrorInfo
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public int Count { get; set; }
    public string LogMessage { get; set; }
    public string StackTrace { get; set; }
    public int LogType { get; set; }
    public string SceneName { get; set; }
    public List<int> AffectedPlayerIds { get; set; } = new List<int>();
}

public static class ClientErrorManager
{
    private static string _filePath = "client_errors.json";
    private static List<ClientErrorInfo> _errors = new List<ClientErrorInfo>();
    private static readonly object _lock = new object();
    private static int _maxErrors = 500;

    static ClientErrorManager()
    {
        Load();
    }

    public static void StoreLog(ClientErrorPacket packet)
    {
        lock (_lock)
        {
            // Benzersiz hata anahtarı (Mesaj + StackTrace)
            string fingerprint = $"{packet.LogMessage}|{packet.StackTrace}";
            
            var existing = _errors.FirstOrDefault(e => $"{e.LogMessage}|{e.StackTrace}" == fingerprint);

            if (existing != null)
            {
                existing.Count++;
                existing.LastSeen = DateTime.Now;
                existing.SceneName = packet.SceneName; // Son görüldüğü sahneyi güncelle
                
                if (packet.AccountId > 0 && !existing.AffectedPlayerIds.Contains(packet.AccountId))
                {
                    existing.AffectedPlayerIds.Insert(0, packet.AccountId);
                    if (existing.AffectedPlayerIds.Count > 10)
                        existing.AffectedPlayerIds.RemoveAt(10);
                }
            }
            else
            {
                var newError = new ClientErrorInfo
                {
                    FirstSeen = DateTime.Now,
                    LastSeen = DateTime.Now,
                    Count = 1,
                    LogMessage = packet.LogMessage ?? "Unknown Error",
                    StackTrace = packet.StackTrace ?? "No Stack Trace",
                    LogType = packet.LogType,
                    SceneName = packet.SceneName ?? "Unknown"
                };

                if (packet.AccountId > 0)
                {
                    newError.AffectedPlayerIds.Add(packet.AccountId);
                }

                _errors.Insert(0, newError);

                if (_errors.Count > _maxErrors)
                {
                    _errors = _errors.Take(_maxErrors).ToList();
                }
            }

            Save();
        }
    }

    public static List<ClientErrorInfo> GetErrors()
    {
        lock (_lock)
        {
            return _errors.OrderByDescending(e => e.LastSeen).ToList();
        }
    }

    public static void ClearLogs()
    {
        lock (_lock)
        {
            _errors.Clear();
            Save();
        }
    }

    private static void Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                _errors = JsonConvert.DeserializeObject<List<ClientErrorInfo>>(json) ?? new List<ClientErrorInfo>();
            }
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[ClientErrorManager] Yükleme hatası: {ex.Message}");
        }
    }

    private static void Save()
    {
        try
        {
            var json = JsonConvert.SerializeObject(_errors, Formatting.Indented);
            File.WriteAllText(_filePath, json);
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[ClientErrorManager] Kayıt hatası: {ex.Message}");
        }
    }
}
