using System.Linq;
using Newtonsoft.Json;

[HttpController]
public class PlayersController : BaseController
{
    [HttpRoute("GET", "/api/players", RequiresAuth = true)]
    public object GetPlayers()
    {
        try
        {
            var accounts = AccountCache.GetAllAccounts();
            if (accounts == null || !accounts.Any())
                return NotFound("Oyuncu bulunamadı.");

            return Ok(accounts.Select(a => new
            {
                id = a.ID,
                username = a.Username,
                level = a.Level,
                isOnline = SessionManager.IsOnline(a.ID),
                isBanned = a.Banned,
                trophies = a.Trophy,
                gems = a.Gems,
                coins = a.Coins
            }).ToList());
        }
        catch (Exception ex)
        {
            return Fail("Oyuncular getirilirken bir hata oluştu: " + ex.Message);
        }
    }

    [HttpRoute("GET", "/api/players/{id}", RequiresAuth = true)]
    public object GetPlayer(string id)
    {
        try
        {
            if (!int.TryParse(id, out int playerId))
                return Fail("Geçersiz oyuncu ID.");

            var account = AccountCache.Load(playerId);
            if (account == null)
                return NotFound("Oyuncu bulunamadı.");

            return Ok(new
            {
                id = account.ID,
                username = account.Username,
                level = account.Level,
                trophies = account.Trophy,
                gems = account.Gems,
                coins = account.Coins,
                isOnline = SessionManager.IsOnline(account.ID),
                isBanned = account.Banned,
                banReason = account.Banned ? account.Banreason : null,
                banEndTime = account.Banned ? account.MutedEndTime.ToString("dd.MM.yyyy HH:mm") : null,
                lastLogin = account.LastLogin > DateTime.MinValue ? account.LastLogin.ToString("dd.MM.yyyy HH:mm") : "Hiç giriş yapmadı",
                ip = account.LastIp ?? "N/A",
                device = account.Device ?? "N/A",
                email = account.Email ?? "N/A",
                clubName = account.ClubName ?? "Yok",
                clubRole = account.clubRole.ToString(),
                premium = account.Premium > 0,
                premiumEndTime = account.PremiumEndTime > DateTime.Now ? account.PremiumEndTime.ToString("dd.MM.yyyy HH:mm") : null,
                muted = account.Muted,
                mutedEndTime = account.Muted ? account.MutedEndTime.ToString("dd.MM.yyyy HH:mm") : null,
                chatBan = account.ChatBan,
                ticketBan = account.TicketBan,
                roles = account.Roles.Select(r => r.ToString()).ToList(),
                banHistory = account.BanHistory.Select(b => new
                {
                    time = b.BanDate.ToString("dd.MM.yyyy HH:mm"),
                    banner = b.BannedBy,
                    reason = b.Reason,
                    finishDate = b.BanFinishDate?.ToString("dd.MM.yyyy HH:mm") ?? "Kalıcı",
                    isPerma = b.Perma
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            return Fail("Oyuncu getirilirken bir hata oluştu: " + ex.Message);
        }
    }

    [HttpRoute("POST", "/api/player/update", RequiresAuth = true)]
    public object UpdatePlayer()
    {
        try
        {
            var data = ReadFormData();
            if (data == null || !data.ContainsKey("id"))
                return Fail("Oyuncu ID gerekli.");

            if (!int.TryParse(data["id"], out int playerId))
                return Fail("Geçersiz oyuncu ID.");

            var account = AccountCache.Load(playerId);
            if (account == null)
                return NotFound("Oyuncu bulunamadı.");

            int? level = data.ContainsKey("level") ? int.Parse(data["level"]) : null;
            int? trophies = data.ContainsKey("trophies") ? int.Parse(data["trophies"]) : null;
            int? gems = data.ContainsKey("gems") ? int.Parse(data["gems"]) : null;
            int? coins = data.ContainsKey("coins") ? int.Parse(data["coins"]) : null;

            if (level != null)
                account.Level = level.Value;
            if (trophies != null)
                account.Trophy = trophies.Value;
            if (gems != null)
                account.Gems = gems.Value;
            if (coins != null)
                account.Coins = coins.Value;

            
            Audit("Oyuncu Güncellendi", account.ID.ToString(), $"Level: {level}, Trophies: {trophies}, Gems: {gems}, Coins: {coins}");
            return Ok(new { success = true, message = "Oyuncu güncellendi." });
        }
        catch (Exception ex)
        {
            return Fail("Oyuncu güncellenirken bir hata oluştu: " + ex.Message);
        }
    }

    [HttpRoute("POST", "/api/player/ban", RequiresAuth = true)]
    public object BanPlayer()
    {
        try
        {
            var data = ReadFormData();
            if (data == null || !data.ContainsKey("id"))
                return Fail("Oyuncu ID gerekli.");

            if (!int.TryParse(data["id"], out int playerId))
                return Fail("Geçersiz oyuncu ID.");

            var account = AccountCache.Load(playerId);
            if (account == null)
                return NotFound("Oyuncu bulunamadı.");

            string reason = data.ContainsKey("reason") ? data["reason"] : "Belirtilmedi";
            account.Banned = true;
            account.Banreason = reason;
            account.MutedEndTime = DateTime.MaxValue; // Kalıcı ban

           
            Audit("Oyuncu Banlandı", account.ID.ToString(), $"Sebep: {reason}");
            return Ok(new { success = true, message = "Oyuncu banlandı." });
        }
        catch (Exception ex)
        {
            return Fail("Oyuncu banlanırken bir hata oluştu: " + ex.Message);
        }
    }

    [HttpRoute("POST", "/api/player/unban", RequiresAuth = true)]
    public object UnbanPlayer()
    {
        try
        {
            var data = ReadFormData();
            if (data == null || !data.ContainsKey("id"))
                return Fail("Oyuncu ID gerekli.");

            if (!int.TryParse(data["id"], out int playerId))
                return Fail("Geçersiz oyuncu ID.");

            var account = AccountCache.Load(playerId);
            if (account == null)
                return NotFound("Oyuncu bulunamadı.");

            account.Banned = false;
            account.Banreason = null;
            account.MutedEndTime = DateTime.MinValue;

          
            Audit("Oyuncu Ban Kaldırıldı", account.ID.ToString());
            return Ok(new { success = true, message = "Oyuncunun banı kaldırıldı." });
        }
        catch (Exception ex)
        {
            return Fail("Oyuncunun banı kaldırılırken bir hata oluştu: " + ex.Message);
        }
    }

    [HttpRoute("POST", "/api/player/kick", RequiresAuth = true)]
    public object KickPlayer()
    {
        try
        {
            var data = ReadFormData();
            if (data == null || !data.ContainsKey("id"))
                return Fail("Oyuncu ID gerekli.");

            if (!int.TryParse(data["id"], out int playerId))
                return Fail("Geçersiz oyuncu ID.");

            var session = SessionManager.GetSession(playerId);
            if (session == null)
                return Fail("Oyuncu çevrimiçi değil.");

            session.Close();
            Audit("Oyuncu Sunucudan Atıldı", playerId.ToString());
            return Ok(new { success = true, message = "Oyuncu sunucudan atıldı." });
        }
        catch (Exception ex)
        {
            return Fail("Oyuncu sunucudan atılırken bir hata oluştu: " + ex.Message);
        }
    }
}