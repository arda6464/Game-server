using Newtonsoft.Json;

[HttpController]
public class ConfigController : BaseController
{
    [HttpRoute("GET", "/api/dynamicconfig")]
    public object Get()
    {
        return DynamicConfigManager.Config;
    }

    [HttpRoute("POST", "/api/dynamicconfig/update")]
    public object Update([FromBody] DynamicConfig newConfig)
    {
        if (newConfig == null) return Fail("Geçersiz veri.");
        DynamicConfigManager.Update(newConfig);
        return Ok("Yapılandırma güncellendi.");
    }

    [HttpRoute("POST", "/api/maintenance/toggle")]
    public object ToggleMaintenance([FromBody] Dictionary<string, string> data)
    {
        bool enable = data != null && data.ContainsKey("enabled") && bool.Parse(data["enabled"]);
        bool panic = data != null && data.ContainsKey("panic") && bool.Parse(data["panic"]);

        if (enable)
        {
            new Thread(() => Maintenance.StartMaintenance(TimeSpan.FromHours(2), panic)).Start();
            return Ok("Bakım modu başlatılıyor...");
        }
        else
        {
            Maintenance.finishMaintenance();
            return Ok("Bakım modu sona erdirildi.");
        }
    }

    [HttpRoute("POST", "/api/command")]
    public object Command([FromBody] Dictionary<string, string> data)
    {
        if (data == null || !data.ContainsKey("command")) return Fail("Komut gerekli.");
        Logger.genellog($"[AdminServer] Web üzerinden komut alındı: {data["command"]}");
        return new { success = true, message = "Komut alındı (Sunucu konsolunda işleniyor olabilir)" };
    }
}
