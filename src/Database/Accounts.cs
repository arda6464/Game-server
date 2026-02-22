using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Microsoft.Data.Sqlite;
using System.Linq;
using System.Collections.Concurrent;

public static class AccountManager
{
    private static ConcurrentDictionary<string, AccountData> accounts = new ConcurrentDictionary<string, AccountData>();
    private static int maxAccountId = 1;

    public class AccountData
    {
        // security data
        public int IDCounter { get; set; }
        [JsonIgnore]
        public string? AccountId { get; set; }
        public string? Token { get; set; }
        public string? FBNToken { get; set; }
        // account data
        [JsonIgnore]
        public string? Username { get; set; }
        public int Trophy { get; set; }
        public string? Dil { get; set; }
        public int Premium { get; set; }
        public DateTime PremiumEndTime { get; set; }
        public bool Banned { get; set; }
        public string? Banreason { get; set; }
        public int Avatarid { get; set; }
        public int Namecolorid { get; set; }
        public int Level { get; set; }
        public int Gems { get; set; }
        public int Coins { get; set; }
        public string? ClubName { get; set; }
        public int Clubid { get; set; }
        public ClubRole clubRole { get; set; }
        public bool TicketBan { get; set; } = false;
        public bool ChatBan { get; set; } = false;
        public bool SendOnlineBestFriendNotification { get; set; } = true;
        public bool SendNewEventNotification { get; set; } = true;
        public bool SendInviteNotification { get; set; } = true;
        public bool SendClaimRewardNotification { get; set; } = true;

        public List<FriendInfo> Friends { get; set; } = new List<FriendInfo>();
        public List<FriendInfo> Requests { get; set; } = new List<FriendInfo>();
        public List<Notfication> Notfications { get; set; } = new List<Notfication>();
        public List<Notfication> inboxesNotfications { get; set; } = new List<Notfication>();
        public List<Role.Roles> Roles { get; set; } = new List<Role.Roles>();
        public List<SupportTicketData> Tickets { get; set; } = new List<SupportTicketData>();
        public List<Quest> Quests { get; set; } = new List<Quest>();
        public List<BanData> BanHistory { get; set; } = new List<BanData>();
        public Dictionary<int, DateTime> NotificationCooldowns { get; set; } = new Dictionary<int, DateTime>();
        

        // login data
        public DateTime LastLogin { get; set; }
        public string? LastIp { get; set; }
        public string? Device { get; set; }
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public DateTime LastQuestRefreshDate { get; set; } = DateTime.MinValue;

        [JsonIgnore]
        public object SyncLock = new object();

    }

    // Tüm hesapları yükle
    public static void LoadAccounts()
    {
        Console.WriteLine("Hesaplar veritabanından yükleniyor...");

        using (var connection = DatabaseManager.GetConnection())
        {
            connection.Open();


            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = "SELECT * FROM Accounts";
            using (var reader = selectCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    string jsonData = reader.GetString(reader.GetOrdinal("Data"));
                    var account = JsonConvert.DeserializeObject<AccountData>(jsonData);
                    
                    if (account != null)
                    {
                        // Sütunlardan her ihtimale karşı güncelleyelim (sorgulama için dışarıdalar)
                        account.AccountId = reader.GetString(reader.GetOrdinal("AccountId"));
                        account.Username = reader.IsDBNull(reader.GetOrdinal("Username")) ? null : reader.GetString(reader.GetOrdinal("Username"));

                        accounts[account.AccountId] = account;
                        if (account.IDCounter >= maxAccountId)
                            maxAccountId = account.IDCounter + 1;

                        AccountCache.Cache(account);
                    }
                }
            }
        }

        Console.WriteLine($"[AccountManager] {accounts.Count} hesap yüklendi.");
    }

    private static void SaveAccountToDb(AccountData account, SqliteConnection connection)
    {
        var upsertQuery = @"
            INSERT INTO Accounts (AccountId, Username, Data) 
            VALUES (@AccountId, @Username, @Data) 
            ON CONFLICT(AccountId) DO UPDATE SET
                Username=excluded.Username, 
                Data=excluded.Data;";

        using (var command = connection.CreateCommand())
        {
            command.CommandText = upsertQuery;
            command.Parameters.AddWithValue("@AccountId", account.AccountId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Username", account.Username ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Data", JsonConvert.SerializeObject(account));
            command.ExecuteNonQuery();
        }
    }

    // Hesapları kaydet
    public static void SaveAccounts()
    {
        using (var connection = DatabaseManager.GetConnection())
        {
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                foreach (var account in accounts.Values)
                {
                    SaveAccountToDb(account, connection);
                }
                
                // Cache'den güncel olanları da al (eğer dictionary'de yoksa)
                foreach (var cachedAccount in AccountCache.GetCachedAccounts())
                {
                    if (!accounts.ContainsKey(cachedAccount.Key))
                    {
                        SaveAccountToDb(cachedAccount.Value, connection);
                    }
                }
                transaction.Commit();
            }
        }
    }

    // Hesap oluştur
    public static AccountData CreateAccount(string dil, string username = "arda64best")
    {
        string accountId = TokenManager.GeneratePlayerId();
        var newAccount = new AccountData
        {
            IDCounter = maxAccountId,
            AccountId = accountId,
            Username = username,
            Dil = dil,
            Premium = 0,
            Avatarid = 1,
            Namecolorid = 1,
            Token = TokenManager.GenerateNumericToken(),
            LastLogin = DateTime.Now,
            Clubid = -1,
            Trophy = 0,
            Email = null,
            Password = null
        };

        accounts[accountId] = newAccount;
        maxAccountId++;
        AccountCache.Cache(newAccount);
        
        Notfication notification = new Notfication
        {
             type =  NotficationTypes.NotficationType.banner,
            Title = "SQLite Geçişi!",
            Message = "Veritabanına geçiş yapıldı. Tüm verileriniz korundu.",
            ButtonText = "Tamam",
            IsViewed = false
        };
        newAccount.Notfications.Add(notification);

        using (var connection = DatabaseManager.GetConnection())
        {
            connection.Open();
            SaveAccountToDb(newAccount, connection);
        }

        Console.WriteLine($"[AccountManager] Yeni hesap oluşturuldu: {username} (ID: {newAccount.IDCounter}, AccountId: {newAccount.AccountId})");

        return newAccount;
    }

    public static void Getaccountinfo(string id)
    {
        var account = LoadAccount(id);
        if (account != null)
            Console.WriteLine($"isim: {account.Username}\n avatarid : {account.Avatarid} \n pushtoken : {account.FBNToken} \n colorid: {account.Namecolorid}\n  son giriş: {account.LastLogin} \n Dil: {account.Dil} \n clubid: {account.Clubid}\n club name: {account.ClubName}");
    }

    // Hesap yükle (AccountId ile)
    public static AccountData LoadAccount(string accountId)
    {
        if (accounts.TryGetValue(accountId, out var account))
        {
            account.LastLogin = DateTime.Now;
            return account;
        }

        Console.WriteLine("[AccountManager] Kullanıcı bulunamadı: " + accountId);
        return null;
    }

    public static void DeleteNotfications()
    {
        foreach (AccountData account in accounts.Values)
        {
            lock (account.SyncLock)
            {
                account.Notfications.Clear();
            }
        }
        SaveAccounts();
        Console.WriteLine("Accountsların notficationları silindi");
    }

    public static void AddRole(AccountData account, Role.Roles role)
    {
        lock (account.SyncLock)
        {
            if (account.Roles.Contains(role))
            {
                Logger.genellog($"{account.Username} ({account.AccountId}) kişisine {role}'ü eklenmeye çalıştı fakat zaten var olduğu için eklenmedi");
                return;
            }
            account.Roles.Add(role);
        }
        Logger.genellog($"{account.Username} ({account.AccountId}) kişisine {role}'ü eklendi!");
    }

    public static void RemoveRole(AccountData account, Role.Roles role)
    {
        lock (account.SyncLock)
        {
            if (!account.Roles.Contains(role))
            {
                Logger.genellog($"{account.Username} ({account.AccountId}) kişisinden {role}'ü kaldırılmaya çalıştı fakat zaten o role sahip olmadığı için kaldırılmadı");
                return;
            }
            account.Roles.Remove(role);
        }
        Logger.genellog($"{account.Username} ({account.AccountId}) kişisinden {role}'ü kaldırıldı!");
    }

    public static List<AccountData> GetTop100Players()
    {
        return accounts.Values
            .Where(a => !a.Banned)
            .OrderByDescending(a => a.Trophy)
            .Take(100)
            .ToList();
    }

    public static int GetPlayerRank(string accountId)
    {
        var sortedPlayers = accounts.Values
            .Where(a => !a.Banned)
            .OrderByDescending(a => a.Trophy)
            .ToList();

        int rank = sortedPlayers.FindIndex(a => a.AccountId == accountId) + 1;
        return rank;
    }

    public static bool CheckMail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        string normalizedEmail = email.Trim().ToLower();

        return accounts.Values.Any(a => !string.IsNullOrEmpty(a.Email) && a.Email.Trim().ToLower() == normalizedEmail);
    }

    public static AccountData FindAccountByEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        string normalizedEmail = email.Trim().ToLower();
        
        return accounts.Values.FirstOrDefault(a => !string.IsNullOrEmpty(a.Email) && a.Email.Trim().ToLower() == normalizedEmail);
    }
}
    
    
   
    
   
          

