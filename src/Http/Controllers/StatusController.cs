using System.Diagnostics;

[HttpController]
public class StatusController : BaseController
{
    [HttpRoute("GET", "/api/status", RequiresAuth = false)]
    public object Status()
    {
        var process = Process.GetCurrentProcess();
        return new
        {
            version = Config.Instance.ServerVersion,
            onlinePlayers = SessionManager.GetSessions().Count,
            lobbyCount = LobbyManager.Lobbies.Count,
            uptime = (DateTime.Now - process.StartTime).ToString(@"dd\.hh\:mm\:ss"),
            memoryUsage = GC.GetTotalMemory(false) / 1024 / 1024 + " MB",
            cpuUsage = GetCpuUsage(process),
            threadCount = process.Threads.Count,
            tps = TickManager.instance?.TickRate ?? 0,
            maintenanceMode = Maintenance.MaintenanceMode
        };
    }

    [HttpRoute("GET", "/api/traffic/details")]
    public object TrafficDetails()
    {
        var report = TrafficMonitor.GetDetailedReport();
        return report ?? new { error = "Veri alınamadı" };
    }

    [HttpRoute("GET", "/api/traffic/history")]
    public object TrafficHistory()
    {
        return new { history = TrafficMonitor.GetHistory() };
    }

    [HttpRoute("GET", "/api/logs")]
    public object Logs()
    {
        return new { logs = GetLastLogs("Data/genellog.txt", 100) };
    }

    [HttpRoute("GET", "/api/client/errors")]
    public object ClientErrors()
    {
        return ClientErrorManager.GetErrors();
    }

    [HttpRoute("POST", "/api/client/errors/clear")]
    public object ClearClientErrors()
    {
        ClientErrorManager.ClearLogs();
        Audit("Hata Günlüğü Temizlendi", "ClientErrors", "Tüm istemci hata kayıtları silindi.");
        return Ok("Tüm hatalar temizlendi.");
    }

    private double _currentCpuUsage = 0;
    private DateTime _lastCpuCheck = DateTime.MinValue;
    private TimeSpan _lastProcessorTime;

    private double GetCpuUsage(Process process)
    {
        var now = DateTime.Now;
        if ((now - _lastCpuCheck).TotalSeconds < 1) return _currentCpuUsage;
        var currentProcessorTime = process.TotalProcessorTime;
        if (_lastCpuCheck != DateTime.MinValue)
        {
            var cpuUsedMs = (currentProcessorTime - _lastProcessorTime).TotalMilliseconds;
            var elapsedMs = (now - _lastCpuCheck).TotalMilliseconds;
            _currentCpuUsage = Math.Round((cpuUsedMs / (Environment.ProcessorCount * elapsedMs)) * 100, 1);
        }
        _lastCpuCheck = now;
        _lastProcessorTime = currentProcessorTime;
        return _currentCpuUsage;
    }

    private string GetLastLogs(string fileName, int lineCount)
    {
        try
        {
            if (!File.Exists(fileName)) return "Log dosyası bulunamadı.";
            using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fs))
            {
                var lines = new List<string>();
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                    if (lines.Count > lineCount * 2) lines.RemoveRange(0, lineCount);
                }
                int start = Math.Max(0, lines.Count - lineCount);
                return string.Join("\n", lines.Skip(start));
            }
        }
        catch (Exception ex)
        {
            return $"Log okuma hatası: {ex.Message}";
        }
    }
}
