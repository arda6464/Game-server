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
                        ProgressionManager.Normalize(account);
                        account.SeasonsData ??= new List<SeasonHistoryEntry>();
                        SeasonManager.EnsureAccountSeasonState(account);

                        if (account.ID >= maxAccountId)
                            maxAccountId = account.ID + 1;

                        AccountCache.Cache(account);
                    }
                }
            }
        }

        Console.WriteLine($"[AccountManager] {AccountCache.Count()} hesap yüklendi.");
    }

    private static void SaveAccountToDb(AccountData account, SqliteConnection connection, SqliteTransaction? transaction = null)
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
            command.Transaction = transaction;
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
                try
                {
                    foreach (var account in AccountCache.GetAllAccounts())
                    {
                        SaveAccountToDb(account, connection, transaction);
                    }
                    transaction.Commit();
                }
                catch
                {
                    try { transaction.Rollback(); } catch { }
                    throw;
                }
            }
        }
    }

    public static AccountData CreateAccount(string dil, string username = "arda64best")
    {
        var newAccount = new AccountData
        {
            ID = maxAccountId,
            Username = username,
            CountryCode = dil,
            Country = CountryHelper.GetCountryName(dil),
            Premium = 0,
            Avatarid = 1,
            Namecolorid = 1,
            Level = 1,
            Experience = 0,
            SeasonId = 1,
            SeasonStartTrophy = 0,
            SeasonPeakTrophy = 0,
            SeasonWins = 0,
            SeasonLosses = 0,
            SeasonMatchesPlayed = 0,
            SeasonRewardClaimed = false,
            Token = TokenManager.GenerateNumericToken(),
            LastLogin = DateTime.Now,
            Clubid = 0,
            Trophy = 0,
            Email = null,
            Password = null,
            CreatedAt = DateTime.Now
        };

        maxAccountId++;
        AccountCache.Cache(newAccount);

        Notification notification = new Notification
        {
            type = NotificationTypes.NotificationType.banner,
            Title = "ID Sistemi Güncellendi!",
            Message = "Artık tüm işlemleriniz sadece sayısal ID üzerinden yapılmaktadır.",
            ButtonText = "Tamam",
            IsViewed = false
        };
        newAccount.Notifications.Add(notification);

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
            Console.WriteLine($"isim: {account.Username}\n ID: {account.ID}\n avatarid : {account.Avatarid} \n pushtoken : {account.FBNToken} \n colorid: {account.Namecolorid}\n  son giriş: {account.LastLogin} \n Dil: {account.Country} \n clubid: {account.Clubid}\n club name: {account.ClubName}");
    }

    public static AccountData LoadAccount(int id)
    {
        return AccountCache.Load(id);
    }

    public static void DeleteNotifications()
    {
        foreach (AccountData account in AccountCache.GetAllAccounts())
        {
            lock (account.SyncLock)
            {
                account.Notifications.Clear();
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
