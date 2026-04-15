using System;
using System.IO;
using Microsoft.Data.Sqlite;

public static class DatabaseManager
{
    private static string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database.db");
    private static string connectionString = $"Data Source={dbPath}";

    public static void Initialize()
    {
        using (var connection = GetConnection())
        {
            connection.Open();

            // Accounts tablosu (Minimalist)
            var createAccountsTable = @"
                CREATE TABLE IF NOT EXISTS Accounts (
                    ID INTEGER PRIMARY KEY,
                    Username TEXT,
                    Data JSON
                );";

            // Clubs tablosu (Minimalist)
            var createClubsTable = @"
                CREATE TABLE IF NOT EXISTS Clubs (
                    ClubId INTEGER PRIMARY KEY,
                    ClubName TEXT,
                    Data JSON
                );";

            // Bans tablosu
            var createBansTable = @"
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

            ExecuteNonQuery(createAccountsTable, connection);
            ExecuteNonQuery(createClubsTable, connection);
            ExecuteNonQuery(createBansTable, connection);
        }
    }

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
}
