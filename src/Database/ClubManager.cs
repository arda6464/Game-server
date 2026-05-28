using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.Data.Sqlite;
using System.Collections.Concurrent;

public static class ClubManager
{
    public static ConcurrentDictionary<int, Club> Clubs = new ConcurrentDictionary<int, Club>();
    private static int lastClubId = 1;

    #region Load/Save
    public static void Allclubload()
    {
        Console.WriteLine("Kulüpler veritabanından yükleniyor...");

        using (var connection = DatabaseManager.GetConnection())
        {
            connection.Open();

            var selectCmd = connection.CreateCommand();
            selectCmd.CommandText = "SELECT * FROM Clubs";
            using (var reader = selectCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    string jsonData = reader.GetString(reader.GetOrdinal("Data"));
                    var club = JsonConvert.DeserializeObject<Club>(jsonData);

                    if (club != null)
                    {
                        club.ID = reader.GetInt32(reader.GetOrdinal("ID"));
                        club.Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? null : reader.GetString(reader.GetOrdinal("Name"));

                        Clubs[club.ID] = club;
                        if (club.ID >= lastClubId)
                            lastClubId = club.ID + 1;

                        ClubCache.Cache(club);
                    }
                }
            }
        }

        Console.WriteLine($"[ClubManager] {Clubs.Count} kulüp yüklendi.");
    }

    private static void SaveClubToDb(Club club, SqliteConnection connection)
    {
        var upsertQuery = @"
            INSERT INTO Clubs (ID, Name, Data) 
            VALUES (@ID, @Name, @Data) 
            ON CONFLICT(ID) DO UPDATE SET
                Name=excluded.Name, 
                Data=excluded.Data;";

        using (var command = connection.CreateCommand())
        {
            string jsonData;
            lock (club.SyncLock)
            {
                jsonData = JsonConvert.SerializeObject(club);
            }

            command.CommandText = upsertQuery;
            command.Parameters.AddWithValue("@ID", club.ID);
            command.Parameters.AddWithValue("@Name", club.Name ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Data", jsonData);
            command.ExecuteNonQuery();
        }
    }

    public static Club LoadClub(int clubId)
    {

        if (Clubs.TryGetValue(clubId, out Club Club))
        {
            ClubCache.Cache(Club);
            return Club;
        }
        var club = ClubCache.Load(clubId);
        if (club != null) return club;

        Console.WriteLine($"[ClubManager] ClubId {clubId} bulunamadı.");
        return null;
    }

    public static void Save()
    {
        using (var connection = DatabaseManager.GetConnection())
        {
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                foreach (var club in Clubs.Values)
                {
                    SaveClubToDb(club, connection);
                }

                foreach (var cachedClub in ClubCache.GetCachedClubs())
                {
                    if (!Clubs.ContainsKey(cachedClub.Key))
                    {
                        SaveClubToDb(cachedClub.Value, connection);
                    }
                }
                transaction.Commit();
            }
        }
    }
    #endregion

    #region Kulüp oluşturma
    public static Club CreateClub(string name, string aciklama, int Avatarid, int leaderId, int State, string Region = "GB")
    {
        var leaderAccount = AccountCache.Load(leaderId);
        if (leaderAccount == null) return null;

        int clubId = Interlocked.Increment(ref lastClubId);
        var club = new Club
        {
            ID = clubId,
            Name = name,
            Description = aciklama,
            AvatarID = Avatarid,
            OwnerID = leaderAccount.ID,
            MessageIdCounter = 1,
            TotalTrophy = leaderAccount.Trophy,
            Members = new List<ClubMember>
            {
                new ClubMember
                {
                    AccountName = leaderAccount.Username,
                    ID = leaderAccount.ID,
                    Role = ClubRole.Leader,
                    NameColorID = leaderAccount.Namecolorid,
                    AvatarID = leaderAccount.Avatarid
                }
            },
            State = (ClubState)State,
            Region = Region
        };

        Clubs[club.ID] = club;
        ClubCache.Cache(club);

        using (var connection = DatabaseManager.GetConnection())
        {
            connection.Open();
            SaveClubToDb(club, connection);
        }

        leaderAccount.Clubid = club.ID;
        leaderAccount.clubRole = ClubRole.Leader;
        leaderAccount.ClubName = club.Name;

        Logger.genellog($"Club oluşturuldu: name: {club.Name} des: {club.Description} id: {club.ID}");

        return club;
    }
    #endregion

    #region Arama / Rastgele
    static Random random = new Random();
    public static List<Club> RandomList(int count)
    {
        var availableClubs = Clubs.Values.ToList();
        List<Club> tempClubs = new List<Club>(availableClubs);
        List<Club> randomClubs = new List<Club>();

        count = Math.Min(count, tempClubs.Count);

        for (int i = 0; i < count; i++)
        {
            int index = random.Next(tempClubs.Count);
            randomClubs.Add(tempClubs[index]);
            tempClubs.RemoveAt(index);
        }

        return randomClubs;
    }

    public static List<Club> SearchClubs(string query)
    {
        return Clubs.Values
            .Where(c => c.Name != null && c.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
            .ToList();
    }
    #endregion

    #region Kulüp Silme
    public static bool DeleteClub(int clubId)
    {
        if (!Clubs.ContainsKey(clubId)) return false;

        Club club = Clubs[clubId];
        foreach (var member in club.Members)
        {
            var acc = AccountCache.Load(member.ID);
            acc.Clubid = 0;
            acc.ClubName = null;
            acc.clubRole = ClubRole.None;
        }
        Clubs.TryRemove(clubId, out _);
        ClubCache.GetCachedClubs().TryRemove(clubId, out _);


        using (var connection = DatabaseManager.GetConnection())
        {
            connection.Open();
            var deleteCmd = connection.CreateCommand();
            deleteCmd.CommandText = "DELETE FROM Clubs WHERE ID = @ClubId";
            deleteCmd.Parameters.AddWithValue("@ClubId", clubId);
            deleteCmd.ExecuteNonQuery();
        }

        Logger.genellog($"Kulüp silindi: {club.Name} ({clubId})");
        return true;
    }
    #endregion
}
