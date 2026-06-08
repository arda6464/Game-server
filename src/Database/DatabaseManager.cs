using System;
using System.Collections.Generic;
using System.IO;
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

    public static void Initialize()
    {
        using (var connection = GetConnection())
        {
            connection.Open();

            ExecuteNonQuery(CreateAccountsTableSql, connection);
            ExecuteNonQuery(CreateClubsTableSql, connection);
            ExecuteNonQuery(CreateBansTableSql, connection);
            EnsureClubsSchema(connection);
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
}
