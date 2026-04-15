using Newtonsoft.Json;
using System.Collections.Concurrent;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;

public class BanData
{
    public int AccountId { get; set; }
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
    private static ConcurrentDictionary<int, BanData> activeBans = new ConcurrentDictionary<int, BanData>();
    private static System.Threading.Timer? saveTimer;
    private static readonly object saveLock = new object();

    public static void Init()
    {
        LoadBans();

        saveTimer = new System.Threading.Timer(
            callback: _ => SaveAll(),
            state: null,
            dueTime: TimeSpan.FromMinutes(5),
            period: TimeSpan.FromMinutes(5)
        );
    }

    public static void Stop()
    {
        saveTimer?.Dispose();
        SaveAll();
        Console.WriteLine("[BanManager] Tüm ban verileri kaydedildi.");
    }

    public static void SaveAll()
    {
        lock (saveLock)
        {
            try
            {
                using (var connection = DatabaseManager.GetConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        foreach (var ban in activeBans.Values)
                        {
                            SaveBanToDb(ban, connection);
                        }
                        transaction.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.errorslog($"[BanManager] SaveAll hatası: {ex.Message}");
            }
        }
    }

    private static void LoadBans()
    {
        using (var connection = DatabaseManager.GetConnection())
        {
            connection.Open();
            var selectBans = connection.CreateCommand();
            selectBans.CommandText = "SELECT * FROM Bans WHERE Active = 1";

            using (var reader = selectBans.ExecuteReader())
            {
                int count = 0;
                while (reader.Read())
                {
                    var ban = new BanData
                    {
                        AccountId = reader.GetInt32(reader.GetOrdinal("AccountId")),
                        AccountName = reader.IsDBNull(reader.GetOrdinal("AccountName")) ? null : reader.GetString(reader.GetOrdinal("AccountName")),
                        Reason = reader.IsDBNull(reader.GetOrdinal("Reason")) ? null : reader.GetString(reader.GetOrdinal("Reason")),
                        BannedBy = reader.IsDBNull(reader.GetOrdinal("BannedBy")) ? null : reader.GetString(reader.GetOrdinal("BannedBy")),
                        BanDate = DateTime.Parse(reader.GetString(reader.GetOrdinal("BanDate"))),
                        BanFinishDate = reader.IsDBNull(reader.GetOrdinal("BanFinishDate")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("BanFinishDate"))),
                        Perma = reader.GetInt32(reader.GetOrdinal("Perma")) == 1,
                        IP = reader.IsDBNull(reader.GetOrdinal("IP")) ? null : reader.GetString(reader.GetOrdinal("IP")),
                        DeviceId = reader.IsDBNull(reader.GetOrdinal("DeviceId")) ? null : reader.GetString(reader.GetOrdinal("DeviceId")),
                        Active = reader.GetInt32(reader.GetOrdinal("Active")) == 1,
                        Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes"))
                    };

                    if (ban.Perma || (ban.BanFinishDate.HasValue && ban.BanFinishDate > DateTime.Now))
                    {
                        activeBans[ban.AccountId] = ban;
                        count++;
                    }
                }
                Console.WriteLine($"[BanManager] {count} aktif ban yüklendi (SQLite)");
            }
        }
    }

    private static void SaveBanToDb(BanData ban, SqliteConnection connection)
    {
        var upsertQuery = @"
            INSERT INTO Bans (AccountId, AccountName, Reason, BannedBy, BanDate, BanFinishDate, Perma, IP, DeviceId, Active, Notes) 
            VALUES (@AccountId, @AccountName, @Reason, @BannedBy, @BanDate, @BanFinishDate, @Perma, @IP, @DeviceId, @Active, @Notes) 
            ON CONFLICT(AccountId) DO UPDATE SET
                AccountName=excluded.AccountName,
                Reason=excluded.Reason,
                BannedBy=excluded.BannedBy,
                BanDate=excluded.BanDate,
                BanFinishDate=excluded.BanFinishDate,
                Perma=excluded.Perma,
                IP=excluded.IP,
                DeviceId=excluded.DeviceId,
                Active=excluded.Active,
                Notes=excluded.Notes;";

        using (var command = connection.CreateCommand())
        {
            command.CommandText = upsertQuery;
            command.Parameters.AddWithValue("@AccountId", ban.AccountId);
            command.Parameters.AddWithValue("@AccountName", ban.AccountName ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Reason", ban.Reason ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@BannedBy", ban.BannedBy ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@BanDate", ban.BanDate.ToString("o"));
            command.Parameters.AddWithValue("@BanFinishDate", ban.BanFinishDate?.ToString("o") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Perma", ban.Perma ? 1 : 0);
            command.Parameters.AddWithValue("@IP", ban.IP ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@DeviceId", ban.DeviceId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Active", ban.Active ? 1 : 0);
            command.Parameters.AddWithValue("@Notes", ban.Notes ?? (object)DBNull.Value);
            command.ExecuteNonQuery();
        }
    }

    public static void RegisterBan(BanData banRecord)
    {
        activeBans[banRecord.AccountId] = banRecord;
    }

    public static void BanPlayer(int targetAccountId, string adminName, string reasonText, bool perma, TimeSpan? duration = null)
    {
        var targetAccount = AccountCache.Load(targetAccountId);
        if (targetAccount == null) return;

        var banRecord = new BanData
        {
            AccountId = targetAccountId,
            AccountName = targetAccount.Username,
            Reason = reasonText,
            BannedBy = adminName,
            BanDate = DateTime.Now,
            BanFinishDate = perma ? null : DateTime.Now.Add(duration ?? TimeSpan.Zero),
            Perma = perma,
            IP = targetAccount.LastIp,
            DeviceId = targetAccount.Device,
            Active = true,
        };

        lock (targetAccount.SyncLock)
        {
            targetAccount.Banned = true;
            targetAccount.Banreason = reasonText;
            targetAccount.BanHistory.Add(banRecord);
        }

        activeBans[targetAccountId] = banRecord;
        Logger.genellog($"Oyuncu banlandı: {targetAccount.Username} ({targetAccountId}) - Sebep: {reasonText}");
        
        AccountManager.SaveAccounts();

        if (SessionManager.IsOnline(targetAccountId))
        {
            var sess = SessionManager.GetSession(targetAccountId);
            sess?.Close();
        }
    }

    public static void UnbanPlayer(int targetAccountId, string adminName, string note = "")
    {
        if (!activeBans.ContainsKey(targetAccountId))
        {
            Logger.genellog($"{targetAccountId} adlı oyuncu zaten banlı değil");
            return;
        }

        var banRecord = activeBans[targetAccountId];
        banRecord.Active = false;
        banRecord.Notes += $"\nBan kaldıran: {adminName} | Tarih: {DateTime.Now} | Not: {note}";

        activeBans.TryRemove(targetAccountId, out _);

        Logger.genellog($"Oyuncunun banı kaldırıldı: {banRecord.AccountName} ({targetAccountId})");
    }

    public static bool IsBanned(int accountId)
    {
        if (activeBans.TryGetValue(accountId, out var ban))
        {
            if (!ban.Perma && ban.BanFinishDate.HasValue && ban.BanFinishDate < DateTime.Now)
            {
                UnbanExpired(accountId);
                return false;
            }
            return ban.Active;
        }
        return false;
    }

    public static BanData? GetBanInfo(int accountId)
    {
        activeBans.TryGetValue(accountId, out var ban);
        return ban;
    }

    public static List<BanData> GetActiveBans()
    {
        return activeBans.Values.ToList();
    }

    public static List<BanData> GetBanHistory(int accountId)
    {
        var account = AccountCache.Load(accountId);
        if (account == null) return new List<BanData>();

        lock (account.SyncLock)
        {
            return account.BanHistory.OrderByDescending(b => b.BanDate).ToList();
        }
    }

    private static void UnbanExpired(int accountId)
    {
        if (activeBans.TryGetValue(accountId, out var ban))
        {
            ban.Active = false;
            activeBans.TryRemove(accountId, out _);
        }
    }

    public static string? GetBanMessage(int accountId)
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
}
