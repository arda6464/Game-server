using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Microsoft.Data.Sqlite;
using System.Linq;
using System.Collections.Concurrent;

public static class AccountManager
{
    private static int maxAccountId = 1;

    public class AccountData
    {
        // security data
        public int ID { get; set; }
        public string? Token { get; set; }
        public string? FBNToken { get; set; }
        
        // account data
        public string? Username { get; set; }
        public int Trophy { get; set; }
        public DateTime CreatedAt;
        public string? Dil { get; set; }
        public int Premium { get; set; }
        public DateTime PremiumEndTime { get; set; }
        public bool Banned { get; set; }
        public bool Muted { get; set; }
        public DateTime MutedEndTime {get;set;}
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
        public int WinStreak { get; set; }

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
                        // JSON içindeki ID ile veritabanı ID'si aynı olmalı
                        account.ID = reader.GetInt32(reader.GetOrdinal("ID"));
                        account.Username = reader.IsDBNull(reader.GetOrdinal("Username")) ? null : reader.GetString(reader.GetOrdinal("Username"));

                        if (account.ID >= maxAccountId)
                            maxAccountId = account.ID + 1;

                        AccountCache.Cache(account);
                    }
                }
            }
        }

        Console.WriteLine($"[AccountManager] {AccountCache.Count()} hesap yüklendi.");
    }

    private static void SaveAccountToDb(AccountData account, SqliteConnection connection)
    {
        var upsertQuery = @"
            INSERT INTO Accounts (ID, Username, Data) 
            VALUES (@ID, @Username, @Data) 
            ON CONFLICT(ID) DO UPDATE SET
                Username=excluded.Username, 
                Data=excluded.Data;";

        using (var command = connection.CreateCommand())
        {
            command.CommandText = upsertQuery;
            command.Parameters.AddWithValue("@ID", account.ID);
            command.Parameters.AddWithValue("@Username", account.Username ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Data", JsonConvert.SerializeObject(account));
            command.ExecuteNonQuery();
        }
    }

    public static void SaveAccounts()
    {
        using (var connection = DatabaseManager.GetConnection())
        {
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                foreach (var account in AccountCache.GetAllAccounts())
                {
                    SaveAccountToDb(account, connection);
                }
                transaction.Commit();
            }
        }
    }

    public static AccountData CreateAccount(string dil, string username = "arda64best")
    {
        var newAccount = new AccountData
        {
            ID = maxAccountId,
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
            Password = null,
            CreatedAt = DateTime.Now
        };

        maxAccountId++;
        AccountCache.Cache(newAccount);

        Notfication notification = new Notfication
        {
            type = NotficationTypes.NotficationType.banner,
            Title = "ID Sistemi Güncellendi!",
            Message = "Artık tüm işlemleriniz sadece sayısal ID üzerinden yapılmaktadır.",
            ButtonText = "Tamam",
            IsViewed = false
        };
        newAccount.Notfications.Add(notification);

        using (var connection = DatabaseManager.GetConnection())
        {
            connection.Open();
            SaveAccountToDb(newAccount, connection);
        }

        Console.WriteLine($"[AccountManager] Yeni hesap oluşturuldu: {username} (ID: {newAccount.ID})");
        return newAccount;
    }

    public static void Getaccountinfo(int id)
    {
        var account = LoadAccount(id);
        if (account != null)
            Console.WriteLine($"isim: {account.Username}\n ID: {account.ID}\n avatarid : {account.Avatarid} \n pushtoken : {account.FBNToken} \n colorid: {account.Namecolorid}\n  son giriş: {account.LastLogin} \n Dil: {account.Dil} \n clubid: {account.Clubid}\n club name: {account.ClubName}");
    }

    public static AccountData LoadAccount(int id)
    {
        return AccountCache.Load(id);
    }

    public static void DeleteNotfications()
    {
        foreach (AccountData account in AccountCache.GetAllAccounts())
        {
            lock (account.SyncLock)
            {
                account.Notfications.Clear();
            }
        }
        SaveAccounts();
        Console.WriteLine("Hesapların bildirimleri silindi.");
    }

    public static List<AccountData> GetTop100Players()
    {
        return AccountCache.GetAllAccounts()
            .Where(a => !a.Banned)
            .OrderByDescending(a => a.Trophy)
            .Take(100)
            .ToList();
    }

    public static int GetPlayerRank(int playerid)
    {
        var sortedPlayers = AccountCache.GetAllAccounts()
            .Where(a => !a.Banned)
            .OrderByDescending(a => a.Trophy)
            .ToList();

        int rank = sortedPlayers.FindIndex(a => a.ID == playerid) + 1;
        return rank;
    }

    public static bool CheckMail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        string normalizedEmail = email.Trim().ToLower();
        return AccountCache.GetAllAccounts().Any(a => !string.IsNullOrEmpty(a.Email) && a.Email.Trim().ToLower() == normalizedEmail);
    }

    public static AccountData FindAccountByEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        string normalizedEmail = email.Trim().ToLower();
        return AccountCache.GetAllAccounts().FirstOrDefault(a => !string.IsNullOrEmpty(a.Email) && a.Email.Trim().ToLower() == normalizedEmail);
    }

    public static AccountData FindAccountByUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return null;
        string normalizedName = username.Trim().ToLower();
        return AccountCache.GetAllAccounts().FirstOrDefault(a => !string.IsNullOrEmpty(a.Username) && a.Username.Trim().ToLower() == normalizedName);
    }
}
