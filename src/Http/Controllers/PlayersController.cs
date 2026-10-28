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

            return Ok(
                accounts
                    .Select(a => new
                    {
                        id = a.ID,
                        username = a.Username,
                        level = a.Level,
                        isOnline = SessionManager.IsOnline(a.ID),
                        isBanned = a.Banned,
                        trophies = a.Trophy,
                        gems = a.Gems,
                        coins = a.Coins,
                    })
                    .ToList()
            );
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

            return Ok(
                new
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
                    banEndTime = account.Banned
                        ? account.MutedEndTime.ToString("dd.MM.yyyy HH:mm")
                        : null,
                    lastLogin = account.LastLogin > DateTime.MinValue
                        ? account.LastLogin.ToString("dd.MM.yyyy HH:mm")
                        : "Hiç giriş yapmadı",
                    ip = account.LastIp ?? "N/A",
                    device = account.Device ?? "N/A",
                    email = account.Email ?? "N/A",
                    clubName = account.ClubName ?? "Yok",
                    clubRole = account.clubRole.ToString(),
                    premium = account.Premium > 0,
                    premiumEndTime = account.PremiumEndTime > DateTime.Now
                        ? account.PremiumEndTime.ToString("dd.MM.yyyy HH:mm")
                        : null,
                    muted = account.Muted,
                    mutedEndTime = account.Muted
                        ? account.MutedEndTime.ToString("dd.MM.yyyy HH:mm")
                        : null,
                    chatBan = account.ChatBan,
                    ticketBan = account.TicketBan,
                    roles = account.Roles.Select(r => r.ToString()).ToList(),
                    banHistory = account
                        .BanHistory.Select(b => new
                        {
                            time = b.BanDate.ToString("dd.MM.yyyy HH:mm"),
                            banner = b.BannedBy,
                            reason = b.Reason,
                            finishDate = b.BanFinishDate?.ToString("dd.MM.yyyy HH:mm") ?? "Kalıcı",
                            isPerma = b.Perma,
                        })
                        .ToList(),
                }
            );
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

            Audit(
                "Oyuncu Güncellendi",
                account.ID.ToString(),
                $"Level: {level}, Trophies: {trophies}, Gems: {gems}, Coins: {coins}"
            );
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

    [HttpRoute("GET", "/api/player/profile", RequiresAuth = true)]
    public object GetPlayerProfile([FromQuery] string id)
    {
        try
        {
            if (!int.TryParse(id, out int playerId))
                return Fail("Geçersiz oyuncu ID.");

            var account = AccountCache.Load(playerId);
            if (account == null)
                return NotFound("Oyuncu bulunamadı.");

            return Ok(new { success = true, profile = new
                    {
                        id = account.ID,
                        username = account.Username,
                        email = string.IsNullOrEmpty(account.Email) ? "Email Yok" : account.Email,
                        dil = account.CountryCode ?? "tr",
                        ip = account.LastIp ?? "N/A",
                        device = account.Device ?? "Bilinmiyor",
                        lastLogin = account.LastLogin > DateTime.MinValue
                            ? account.LastLogin.ToString("dd.MM.yyyy HH:mm")
                            : "Hiç giriş yapmadı",
                        level = account.Level,
                        trophies = account.Trophy,
                        gems = account.Gems,
                        coins = account.Coins,
                        isOnline = SessionManager.IsOnline(account.ID),
                        hasPushToken = !string.IsNullOrEmpty(account.FBNToken),
                        chatBan = account.ChatBan,
                        ticketBan = account.TicketBan,
                        muted = account.Muted,
                        mutedEndTime = account.Muted && account.MutedEndTime > DateTime.MinValue
                            ? account.MutedEndTime.ToString("dd.MM.yyyy HH:mm")
                            : null,
                        banHistoryCount = account.BanHistory?.Count ?? 0,
                        friendRequestCount = account.Requests?.Count ?? 0,
                        inboxCount = account.inboxesNotifications?.Count ?? 0,
                        clubName = account.ClubName ?? "Kulüp Yok",
                        clubRole = account.clubRole.ToString(),
                        clubId = account.Clubid,
                        premium = account.Premium > 0,
                        premiumEndTime = account.Premium > 0
                        && account.PremiumEndTime > DateTime.Now
                            ? account.PremiumEndTime.ToString("dd.MM.yyyy HH:mm")
                            : null,
                        notificationSettings = new
                        {
                            bestFriends = account.SendOnlineBestFriendNotification,
                            events = account.SendNewEventNotification,
                            invites = account.SendInviteNotification,
                            rewards = account.SendClaimRewardNotification,
                        },
                        roles = account.Roles.Select(r => r.ToString()).ToList(),
                        friendCount = account.Friends?.Count ?? 0,
                        questCount = account.Quests?.Count ?? 0,
                        banHistory = account
                            .BanHistory?.Select(static b => new
                            {
                                time = b.BanDate.ToString("dd.MM.yyyy HH:mm"),
                                banner = b.BannedBy,
                                reason = b.Reason,
                                finishDate = b.BanFinishDate?.ToString("dd.MM.yyyy HH:mm")
                                    ?? "Kalıcı",
                                isPerma = b.Perma,
                            })
                            .ToList(),
                    },
                }
            );
        }
        catch (Exception ex)
        {
            return Fail("Profil getirilirken bir hata oluştu: " + ex.Message);
        }
    }

    [HttpRoute("POST", "/api/player/mute", RequiresAuth = true)]
    public object MutePlayer()
    {
        try
        {
            var data = ReadJsonBody<Dictionary<string, object>>();
            if (data == null || !data.ContainsKey("id"))
                return Fail("Oyuncu ID gerekli.");

            if (!int.TryParse(data["id"]?.ToString(), out int playerId))
                return Fail("Geçersiz oyuncu ID.");

            var account = AccountCache.Load(playerId);
            if (account == null)
                return NotFound("Oyuncu bulunamadı.");

            int minutes = 60;
            if (data.ContainsKey("minutes") && int.TryParse(data["minutes"]?.ToString(), out int m))
                minutes = Math.Max(1, m);

            account.Muted = true;
            account.MutedEndTime = DateTime.Now.AddMinutes(minutes);

            Audit("Oyuncu Susturuldu", account.ID.ToString(), $"{minutes} dakika");
            return Ok(new { success = true, message = $"Oyuncu {minutes} dakika susturuldu." });
        }
        catch (Exception ex)
        {
            return Fail("Oyuncu susturulurken bir hata oluştu: " + ex.Message);
        }
    }

    [HttpRoute("POST", "/api/player/unmute", RequiresAuth = true)]
    public object UnmutePlayer()
    {
        try
        {
            var data = ReadJsonBody<Dictionary<string, object>>();
            if (data == null || !data.ContainsKey("id"))
                return Fail("Oyuncu ID gerekli.");

            if (!int.TryParse(data["id"]?.ToString(), out int playerId))
                return Fail("Geçersiz oyuncu ID.");

            var account = AccountCache.Load(playerId);
            if (account == null)
                return NotFound("Oyuncu bulunamadı.");

            account.Muted = false;
            account.MutedEndTime = DateTime.MinValue;

            Audit("Oyuncu Susturması Kaldırıldı", account.ID.ToString());
            return Ok(new { success = true, message = "Oyuncunun susturması kaldırıldı." });
        }
        catch (Exception ex)
        {
            return Fail("Oyuncunun susturması kaldırılırken bir hata oluştu: " + ex.Message);
        }
    }

    [HttpRoute("POST", "/api/player/send-message", RequiresAuth = true)]
    public object SendMessage()
    {
        try
        {
            var data = ReadJsonBody<Dictionary<string, object>>();
            if (data == null || !data.ContainsKey("id"))
                return Fail("Oyuncu ID gerekli.");

            if (!int.TryParse(data["id"]?.ToString(), out int playerId))
                return Fail("Geçersiz oyuncu ID.");

            var account = AccountCache.Load(playerId);
            if (account == null)
                return NotFound("Oyuncu bulunamadı.");

            string title = data.ContainsKey("title")
                ? data["title"]?.ToString() ?? "SİSTEM MESAJI"
                : "SİSTEM MESAJI";
            string message = data.ContainsKey("message") ? data["message"]?.ToString() ?? "" : "";

            if (string.IsNullOrEmpty(message))
                return Fail("Mesaj içeriği boş olamaz.");

            account.inboxesNotifications.Add(
                new Notification
                {
                    Title = title,
                    Message = message,
                    type = NotificationTypes.NotificationType.Inbox,
                    Timespam = DateTime.Now,
                    Sender = AdminUser,
                }
            );

            Audit("Özel Mesaj Gönderildi", account.ID.ToString(), message);
            return Ok(new { success = true, message = "Mesaj oyuncuya iletildi." });
        }
        catch (Exception ex)
        {
            return Fail("Mesaj gönderilirken bir hata oluştu: " + ex.Message);
        }
    }
}
