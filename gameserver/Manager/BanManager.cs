using Newtonsoft.Json;

public class BanData
{
    public string? AccountId { get; set; }
    public string? AccountName { get; set; }
    public string? Reason { get; set; }
    public string? BannedBy { get; set; }
    public DateTime BanDate { get; set; }
    public DateTime? BanFinishDate { get; set; }
    public bool Perma { get; set; }
    public string? IP { get; set; }
    public string? DeviceId { get; set; }
    public bool Active { get; set; }
    public string? Notes { get; set; }


}
public static class BanManager
{
       private static Dictionary<string, BanData> activeBans = new Dictionary<string, BanData>();
    private static List<BanData> banHistory = new List<BanData>();
    private static string banFilePath = "bans.json";
    private static string banHistoryPath = "ban_history.json";



    public static void Init()
    {
        LoadBans();
        

    }

    private static void LoadBans()
{
    if (File.Exists(banFilePath))
    {
        try
        {
            var json = File.ReadAllText(banFilePath);
            var bans = JsonConvert.DeserializeObject<List<BanData>>(json);
            
            Console.WriteLine($"[BanManager] JSON'dan {bans?.Count ?? 0} ban yüklendi");

            activeBans = bans?
                .Where(ban => ban.Active && (ban.Perma || 
                      (ban.BanFinishDate.HasValue && ban.BanFinishDate > DateTime.Now)))
                .ToDictionary(ban => ban.AccountId) ?? new Dictionary<string, BanData>();

            Console.WriteLine($"[BanManager] {activeBans.Count} aktif ban filtrelendi");
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[BanManager] Banlar yüklenirken hata: {ex.Message}");
        }
    }
    else
    {
        Logger.errorslog($"[BanManager] {banFilePath} bulunamadı, yeni dosya oluşturulacak.");
        // Sadece dosya oluştur, içi boş kalsın
        File.WriteAllText(banFilePath, "[]");
    }
}
    private static void SaveBans()
    {
        try
        {
            var activeBanList = activeBans.Values.ToList();
            var json = JsonConvert.SerializeObject(activeBanList, Formatting.Indented);
            File.WriteAllText(banFilePath, json);
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[BanManager] Banlar kaydedilirken hata: {ex.Message}");
        }
    }

     private static void SaveBanHistory()
    {
        try
        {
            var json = JsonConvert.SerializeObject(banHistory, Formatting.Indented);
            File.WriteAllText(banHistoryPath, json);
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[BanManager] Ban geçmişi kaydedilirken hata: {ex.Message}");
        }
    }
      



    public static void BanPlayer(string targetAccountId, string adminName, string reasonText, bool perma, TimeSpan? duration = null)
    {
        var targetAccount = AccountCache.Load(targetAccountId);
        if (targetAccount == null)
        {
            Logger.errorslog($"[Ban Manager] {targetAccountId} idli hesap bulunamadı, banlanma başarısız");
        }

        
          if (IsBanned(targetAccountId))
        {
             Logger.errorslog($"[Ban Manager] {targetAccountId} idli hesap zaten banlı, banlanma başarısız");
            return;
        }
       


        // Admin yetkisi kontrolü (bu kısmı kendi sistemine göre özelleştir)
      

       

        var banRecord = new BanData
        {
            AccountId = targetAccountId,
            AccountName = targetAccount.Username,
            Reason = reasonText,

            BannedBy = adminName,
            BanDate = DateTime.Now,
            BanFinishDate = perma ? null : DateTime.Now.Add(duration.Value),
            IP = targetAccount.LastIp, // IP'yi session'dan al
            DeviceId = targetAccount.Device, // Hardware ID'yi kaydet
            Active = true,

        };

        // Aktif banlara ekle
        activeBans[targetAccountId] = banRecord;
        banHistory.Add(banRecord);

        // Eğer oyuncu online ise disconnect et
        if (SessionManager.IsOnline(targetAccountId))
        {
            var session = SessionManager.GetSession(targetAccountId);
            // DisconnectBannedPlayer(session, banRecord);
        }



        SaveBans();
        SaveBanHistory();


        Logger.genellog($"Oyuncu banlandı: {targetAccount.Username} ({targetAccountId}) - Sebep: {banRecord.Reason} süre {banRecord.BanFinishDate}({banRecord.BanFinishDate - DateTime.Now})");


    }
     
     #region  Unbanned
    public static void UnbanPlayer(string targetAccountId, string adminName, string note ="")
    {
        if (!activeBans.ContainsKey(targetAccountId))

        {
            Logger.genellog($"{targetAccountId} adlı oyuncu zaten banlı değil");
            return;     
        }

       

        var banRecord = activeBans[targetAccountId];
        banRecord.Active = false;
        
        // Ban geçmişini güncelle
        var historyRecord = banHistory.FirstOrDefault(b => b.AccountId == targetAccountId && b.Active);
        if (historyRecord != null)
        {
            historyRecord.Active = false;
            historyRecord.Notes += $"\nBan kaldıran: {adminName} | Tarih: {DateTime.Now} | Not: {note}";
        }

        activeBans.Remove(targetAccountId);
        
        SaveBans();
        SaveBanHistory();
      

        Logger.genellog($"Oyuncunun banı kaldırıldı: {banRecord.AccountName} ({targetAccountId})");

      
    }
    #endregion

    #region Kontrol Metodları
    public static bool IsBanned(string accountId)
    {
        if (activeBans.TryGetValue(accountId, out var ban))
        {
            // Geçici ban süresi doldu mu kontrol et
            if (!ban.Perma && ban.BanFinishDate.HasValue && ban.BanFinishDate < DateTime.Now)
            {
                UnbanExpired(accountId);
                return false;
            }
            return ban.Active;
        }
        return false;
    }


    public static BanData GetBanInfo(string accountId)
    {
        activeBans.TryGetValue(accountId, out var ban);
        return ban;
    }

    public static List<BanData> GetActiveBans()
    {
        return activeBans.Values.ToList();
    }

    public static List<BanData> GetBanHistory(string accountId = null)
    {
        if (accountId == null)
            return banHistory;
        
        return banHistory.Where(b => b.AccountId == accountId).ToList();
    }
    #endregion

    #region Yardımcı Metodlar
    private static void UnbanExpired(string accountId)
    {
        if (activeBans.ContainsKey(accountId))
        {
            activeBans[accountId].Active = false;
            activeBans.Remove(accountId);
            SaveBans();
        }
    }

      public static string GetBanMessage(string accountId)
    {
        if (!IsBanned(accountId)) return null;

        var banInfo = GetBanInfo(accountId);
        if (banInfo == null) return null;

        string message = $"🔨 HESABINIZ BANLANDI 🔨\n" +
                       $"Sebep: {banInfo.Reason}\n" +
                       $"Ban Tarihi: {banInfo.BanDate:dd.MM.yyyy HH:mm}\n" +
                       $"Banlayan: {banInfo.BannedBy}\n";

        if (banInfo.Perma)
        {
            message += "Süre: Kalıcı\n";
        }
        else if (banInfo.BanFinishDate.HasValue)
        {
            var timeLeft = banInfo.BanFinishDate.Value - DateTime.Now;
            message += $"Kalan Süre: {timeLeft:dd\\:hh\\:mm\\:ss}\n" +
                      $"Açılma Tarihi: {banInfo.BanFinishDate.Value:dd.MM.yyyy HH:mm}";
        }

        return message;
    }

    private static bool IsAdmin(AccountManager.AccountData account)
    {
        return true;
    }

  

   
    #endregion
}
