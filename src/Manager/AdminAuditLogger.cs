using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Linq;

public class AdminAuditLog
{
    public DateTime Timestamp { get; set; }
    public string AdminUsername { get; set; }
    public string Action { get; set; }
    public string Target { get; set; }
    public string Details { get; set; }
}

public static class AdminAuditLogger
{
    private static string _filePath = "Data/admin_audit_logs.json";
    private static List<AdminAuditLog> _logs = new List<AdminAuditLog>();
    private static readonly object _lock = new();
    private static int _maxLogs = 500;

    static AdminAuditLogger()
    {
        Load();
    }

    public static void Log(string admin, string action, string target, string details)
    {
        var log = new AdminAuditLog
        {
            Timestamp = DateTime.Now,
            AdminUsername = admin,
            Action = action,
            Target = target,
            Details = details
        };

        lock (_lock)
        {
            _logs.Insert(0, log);

            if (_logs.Count > _maxLogs)
            {
                _logs = _logs.Take(_maxLogs).ToList();
            }
        }

        Save();
        Logger.genellog($"[AdminAudit] {admin} -> {action} on {target}: {details}");
    }

    public static List<AdminAuditLog> GetLogs()
    {
        lock (_lock)
        {
            return _logs.ToList();
        }
    }

    private static void Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                lock (_lock)
                {
                    _logs = JsonConvert.DeserializeObject<List<AdminAuditLog>>(json) ?? new List<AdminAuditLog>();
                }
            }
        }
        catch (Exception ex) { Logger.errorslog($"[AdminAuditLogger] Load failed: {ex.Message}"); }
    }

    private static void Save()
    {
        try
        {
            string json;
            lock (_lock)
            {
                json = JsonConvert.SerializeObject(_logs, Formatting.Indented);
            }
            File.WriteAllText(_filePath, json);
        }
        catch (Exception ex) { Logger.errorslog($"[AdminAuditLogger] Save failed: {ex.Message}"); }
    }
}
