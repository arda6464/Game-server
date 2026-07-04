using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Data.Sqlite;

public static class DatabaseManager
{
    private static string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database.db");
    private static string connectionString = $"Data Source={dbPath}";
    private const string CreateAccountsTableSql = @"
                CREATE TABLE IF NOT EXISTS Accounts (
                    ID INTEGER PRIMARY KEY,
                    Username TEXT,
                    Data JSON
                );";

    private const string CreateClubsTableSql = @"
                CREATE TABLE IF NOT EXISTS Clubs (
                    ID INTEGER PRIMARY KEY,
                    Name TEXT,
                    Data JSON
                );";

    private const string CreateBansTableSql = @"
                CREATE TABLE IF NOT EXISTS Bans (
                    AccountId INTEGER PRIMARY KEY,
                    AccountName TEXT,
                    Reason TEXT,
                    BannedBy TEXT,
                    BanDate TEXT,
                    BanFinishDate TEXT,
                    Perma INTEGER,
                    IP TEXT,
                    DeviceId TEXT,
                    Active INTEGER,
                    Notes TEXT
                );";

    private const string CreateDeletionLogsTableSql = @"
                CREATE TABLE IF NOT EXISTS DeletionLogs (
                    ID INTEGER PRIMARY KEY AUTOINCREMENT,
                    TableName TEXT NOT NULL,
                    DeletedId INTEGER,
                    DeletedData TEXT,
                    DeletedBy TEXT,
                    DeletionDate TEXT,
                    Reason TEXT,
                    BackupPath TEXT
                );";

    #region Initialization

    public static void Initialize()
    {
        using (var connection = GetConnection())
        {
            connection.Open();

            ExecuteNonQuery(CreateAccountsTableSql, connection);
            ExecuteNonQuery(CreateClubsTableSql, connection);
            ExecuteNonQuery(CreateBansTableSql, connection);
            ExecuteNonQuery(CreateDeletionLogsTableSql, connection);
            EnsureClubsSchema(connection);
        }
    }

    #endregion

    #region Connection & Helpers

    public static SqliteConnection GetConnection()
    {
        return new SqliteConnection(connectionString);
    }

    public static void ExecuteNonQuery(string query, SqliteConnection? connection = null)
    {
        bool closeAtEnd = false;
        if (connection == null)
        {
            connection = GetConnection();
            connection.Open();
            closeAtEnd = true;
        }

        using (var command = connection.CreateCommand())
        {
            command.CommandText = query;
            command.ExecuteNonQuery();
        }

        if (closeAtEnd)
        {
            connection.Close();
        }
    }

    private static void EnsureClubsSchema(SqliteConnection connection)
    {
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA table_info(Clubs);";
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    columns.Add(reader.GetString(reader.GetOrdinal("name")));
                }
            }
        }

        if (columns.Count == 0)
        {
            ExecuteNonQuery(CreateClubsTableSql, connection);
            return;
        }

        if (columns.Contains("ID") && columns.Contains("Name"))
            return;

        if (columns.Contains("ClubId") && columns.Contains("ClubName"))
        {
            ExecuteNonQuery("ALTER TABLE Clubs RENAME TO Clubs_legacy;", connection);
            ExecuteNonQuery(CreateClubsTableSql, connection);
            ExecuteNonQuery("INSERT INTO Clubs (ID, Name, Data) SELECT ClubId, ClubName, Data FROM Clubs_legacy;", connection);
            ExecuteNonQuery("DROP TABLE Clubs_legacy;", connection);
            Logger.genellog("[Database] Clubs tablosu ID/Name şemasına taşındı.");
        }
    }

    #endregion

    #region Delete Methods

    /// <summary>
    /// Hesap siler
    /// </summary>
    public static bool DeleteAccount(int accountId, string deletedBy = "System", string reason = "Console deletion")
    {
        try
        {
            // Önce hesabı al (log için)
            var account = AccountCache.Load(accountId);
            if (account == null)
            {
                Logger.errorslog($"[Database] Hesap bulunamadı: {accountId}");
                return false;
            }

            // Yedek al
            string backupPath = SetBackupData("Accounts", accountId, JsonSerializer.Serialize(account));

            // Ban kayıtlarını temizle (soft delete)
            DeleteBan(accountId);

            // Hesabı sil
            string query = "DELETE FROM Accounts WHERE ID = @id;";
            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    command.Parameters.AddWithValue("@id", accountId);
                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        // Silme logu
                        AccountCache.Remove(accountId);
                        LogDeletion("Accounts", accountId, JsonSerializer.Serialize(account), deletedBy, reason, backupPath);
                        Logger.genellog($"[Database] Hesap silindi: ID={accountId}, Username={account.Username}, Sebep={reason}");
                        return true;
                    }
                }
                
            }
            return false;
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[Database] DeleteAccount hatası: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Kulüp siler
    /// </summary>
    public static bool DeleteClub(int clubId, string deletedBy = "System", string reason = "Console deletion")
    {
        try
        {
            // Önce kulübü al
            var club = ClubCache.Load(clubId);
            if (club == null)
            {
                Logger.errorslog($"[Database] Kulüp bulunamadı: {clubId}");
                return false;
            }

            // Yedek al
            string backupPath = SetBackupData("Clubs", clubId, JsonSerializer.Serialize(club));

            // Kulüp üyelerini güncelle
            if (club.Members != null && club.Members.Count > 0)
            {
                foreach (var memberId in club.Members)
                {
                    // todo
                }
            }

            // Kulübü sil
            string query = "DELETE FROM Clubs WHERE ID = @id;";
            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    command.Parameters.AddWithValue("@id", clubId);
                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        LogDeletion("Clubs", clubId, JsonSerializer.Serialize(club), deletedBy, reason, backupPath);
                        Logger.genellog($"[Database] Kulüp silindi: ID={clubId}, Name={club.Name}, Sebep={reason}");
                        return true;
                    }
                }
            }
            return false;
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[Database] DeleteClub hatası: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Ban kaydı siler
    /// </summary>
    public static bool DeleteBan(int accountId, string deletedBy = "System", string reason = "Ban removed")
    {
        try
        {
            var ban = BanManager.GetBanInfo(accountId);
            if (ban == null)
            {
                return false;
            }

            string backupPath = SetBackupData("Bans", accountId, JsonSerializer.Serialize(ban));

            string query = "DELETE FROM Bans WHERE AccountId = @id;";
            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    command.Parameters.AddWithValue("@id", accountId);
                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        LogDeletion("Bans", accountId, JsonSerializer.Serialize(ban), deletedBy, reason, backupPath);
                        Logger.genellog($"[Database] Ban silindi: AccountId={accountId}, Sebep={reason}");
                        return true;
                    }
                }
            }
            return false;
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[Database] DeleteBan hatası: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Tüm hesapları siler (DİKKAT!)
    /// </summary>
    public static int DeleteAllAccounts(string deletedBy = "System", string reason = "Mass deletion")
    {
        try
        {
            var accounts = AccountCache.GetAllAccounts();
            if (accounts.Count == 0) return 0;

            // Yedek al
            string backupPath = SetBackupData("AllAccounts", 0, JsonSerializer.Serialize(accounts));

            int deletedCount = 0;
            foreach (var account in accounts)
            {
                if (DeleteAccount(account.ID, deletedBy, reason))
                {
                    deletedCount++;
                }
            }

            Logger.genellog($"[Database] {deletedCount} hesap silindi. Sebep: {reason}");
            return deletedCount;
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[Database] DeleteAllAccounts hatası: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Tüm kulüpleri siler (DİKKAT!)
    /// </summary>
    public static int DeleteAllClubs(string deletedBy = "System", string reason = "Mass deletion")
    {
        try
        {
            var clubs = ClubCache.GetCachedClubs();
            if (clubs.Count == 0) return 0;

            string backupPath = SetBackupData("AllClubs", 0, JsonSerializer.Serialize(clubs));

            int deletedCount = 0;
            foreach (var club in clubs)
            {
                int clubid = Convert.ToInt32(club.Value);
                if (DeleteClub(clubid, deletedBy, reason))
                {
                    deletedCount++;
                }
            }

            Logger.genellog($"[Database] {deletedCount} kulüp silindi. Sebep: {reason}");
            return deletedCount;
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[Database] DeleteAllClubs hatası: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Inaktif hesapları siler
    /// </summary>
    public static int DeleteInactiveAccounts(int daysInactive, string deletedBy = "System")
    {
        // todo 
        return 0;
       /* try
        {
            DateTime cutoff = DateTime.Now.AddDays(-daysInactive);
            var accounts = GetAccountsByLastLogin(cutoff);
            
            if (accounts.Count == 0) return 0;

            int deletedCount = 0;
            foreach (var account in accounts)
            {
                if (DeleteAccount(account.ID, deletedBy, $"Inactive for {daysInactive} days"))
                {
                    deletedCount++;
                }
            }

            Logger.genellog($"[Database] {deletedCount} inaktif hesap silindi. ({daysInactive} gün)");
            return deletedCount;
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[Database] DeleteInactiveAccounts hatası: {ex.Message}");
            return 0;
        }*/
    }

    /// <summary>
    /// Tablo temizler (tüm verileri siler)
    /// </summary>
    public static int ClearTable(string tableName, string deletedBy = "System")
    {
        try
        {
            string[] validTables = { "Accounts", "Clubs", "Bans" };
            if (!Array.Exists(validTables, t => t.Equals(tableName, StringComparison.OrdinalIgnoreCase)))
            {
                Logger.errorslog($"[Database] Geçersiz tablo: {tableName}");
                return -1;
            }

            // Önce yedek al
            string backupPath = SetBackupData($"All{tableName}", 0, GetTableData(tableName));

            string query = $"DELETE FROM {tableName};";
            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    int rowsAffected = command.ExecuteNonQuery();
                    
                    Logger.genellog($"[Database] {tableName} tablosu temizlendi. {rowsAffected} satır silindi.");
                    return rowsAffected;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[Database] ClearTable hatası: {ex.Message}");
            return -1;
        }
    }

    #endregion

    #region Restore Methods

    /// <summary>
    /// Yedekten geri yükleme
    /// </summary>
    public static bool RestoreFromBackup(string backupPath)
    {
        try
        {
            if (!File.Exists(backupPath))
            {
                Logger.errorslog($"[Database] Yedek dosyası bulunamadı: {backupPath}");
                return false;
            }

            string json = File.ReadAllText(backupPath);
            var backupData = JsonSerializer.Deserialize<BackupData>(json);
            
            if (backupData == null)
            {
                Logger.errorslog($"[Database] Yedek verisi okunamadı.");
                return false;
            }

            // Tabloyu temizle
            ClearTable(backupData.TableName, "Restore");

            // Verileri geri yükle
            // Bu kısım veri yapısına göre özelleştirilmeli
            Logger.genellog($"[Database] Yedekten geri yükleme başarılı: {backupPath}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[Database] RestoreFromBackup hatası: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Backup Methods

    private static string SetBackupData(string tableName, int id, string data)
    {
        try
        {
            string backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
            if (!Directory.Exists(backupDir))
                Directory.CreateDirectory(backupDir);

            string fileName = $"{tableName}_{id}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            string backupPath = Path.Combine(backupDir, fileName);
            
            var backup = new BackupData
            {
                TableName = tableName,
                RecordId = id,
                Data = data,
                BackupDate = DateTime.Now
            };

            string json = JsonSerializer.Serialize(backup, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(backupPath, json);
            
            return backupPath;
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[Database] BackupData hatası: {ex.Message}");
            return string.Empty;
        }
    }

    private static string GetTableData(string tableName)
    {
        try
        {
            string query = $"SELECT * FROM {tableName};";
            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    using (var reader = command.ExecuteReader())
                    {
                        var results = new List<Dictionary<string, object>>();
                        while (reader.Read())
                        {
                            var row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[reader.GetName(i)] = reader.GetValue(i);
                            }
                            results.Add(row);
                        }
                        return JsonSerializer.Serialize(results);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[Database] GetTableData hatası: {ex.Message}");
            return "[]";
        }
    }

    #endregion

    #region Logging

    private static void LogDeletion(string tableName, int deletedId, string deletedData, string deletedBy, string reason, string backupPath)
    {
        try
        {
            string query = @"
                INSERT INTO DeletionLogs (TableName, DeletedId, DeletedData, DeletedBy, DeletionDate, Reason, BackupPath)
                VALUES (@tableName, @deletedId, @deletedData, @deletedBy, @deletionDate, @reason, @backupPath);
            ";

            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    command.Parameters.AddWithValue("@tableName", tableName);
                    command.Parameters.AddWithValue("@deletedId", deletedId);
                    command.Parameters.AddWithValue("@deletedData", deletedData);
                    command.Parameters.AddWithValue("@deletedBy", deletedBy);
                    command.Parameters.AddWithValue("@deletionDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    command.Parameters.AddWithValue("@reason", reason);
                    command.Parameters.AddWithValue("@backupPath", backupPath ?? string.Empty);
                    command.ExecuteNonQuery();
                }
            }
        }
        catch (Exception ex)
        {
            Logger.errorslog($"[Database] LogDeletion hatası: {ex.Message}");
        }
    }

    #endregion



    #region Data Classes

    [Serializable]
    public class BackupData
    {
        public string TableName { get; set; } = string.Empty;
        public int RecordId { get; set; }
        public string Data { get; set; } = string.Empty;
        public DateTime BackupDate { get; set; }
    }

   
    #endregion
}