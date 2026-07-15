[HttpController]
public class SeasonController : BaseController
{
    [HttpRoute("GET", "/api/seasons")]
    public object GetAll() => SeasonManager.GetSnapshot();

    [HttpRoute("GET", "/api/seasons/player")]
    public object GetPlayer([FromQuery] int id)
    {
        if (id <= 0) return Fail("Geçersiz oyuncu ID.");
        var view = SeasonManager.GetPlayerView(id);
        return view ?? Fail("Oyuncu sezon verisi bulunamadı.");
    }

    [HttpRoute("POST", "/api/seasons/save")]
    public object Save([FromBody] SeasonConfig config)
    {
        if (config == null) return Fail("Geçersiz sezon verisi.");
        SeasonManager.UpdateSettings(config);
        return new { success = true, message = "Sezon ayarları güncellendi." };
    }

    [HttpRoute("POST", "/api/seasons/open")]
    public object Open([FromBody] SeasonConfig config)
    {
        if (config == null) return Fail("Geçersiz sezon verisi.");
        var summary = SeasonManager.OpenConfiguredSeason(config);
        return new { success = true, message = "Yeni sezon açıldı.", season = summary };
    }

    [HttpRoute("POST", "/api/seasons/close")]
    public object Close([FromBody] Dictionary<string, object> data)
    {
        bool grantRewards = data != null && data.ContainsKey("grantRewards") && Convert.ToBoolean(data["grantRewards"]);
        bool resetPlayers = data != null && data.ContainsKey("resetPlayers") && Convert.ToBoolean(data["resetPlayers"]);
        var summary = SeasonManager.CloseCurrentSeason(grantRewards, resetPlayers);
        return new { success = true, message = "Sezon kapatıldı.", season = summary };
    }

    [HttpRoute("POST", "/api/seasons/reset")]
    public object Reset([FromBody] Dictionary<string, object> data)
    {
        int resetTo = 0;
        if (data != null && data.ContainsKey("resetTrophyTo")) resetTo = Convert.ToInt32(data["resetTrophyTo"]);
        SeasonManager.HardResetAllPlayersTo(resetTo);
        return new { success = true, message = $"Tüm oyuncular kupa değeri {resetTo} ile resetlendi." };
    }
}
