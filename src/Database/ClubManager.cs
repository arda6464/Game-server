using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.Data.Sqlite;
using System.Collections.Concurrent;

public enum ClubRole
{
    Member,
    CoLeader,
    Leader
}
public enum ClubMessageFlags : byte
{
    None = 0,
    HasTarget = 1 << 0,
    HasSystem = 2
}
public enum ClubEventType : byte
{
    JoinMessage,
    LeaveMessage,
    KickMessage,
}

public class ClubMember
{
    public string? AccountName { get; set; }
    public string? Accountid { get; set; }
    public ClubRole Role { get; set; }
    public int NameColorID { get; set; }
    public int AvatarID { get; set; }
}
public class ClubMemberinfo
{
    public int ClubId { get; set; }
    public string? ClubName { get; set; }
    public string? Clubaciklama { get; set; }
    public int? TotalKupa { get; set; }
}

public class Club
{
    [JsonIgnore]
    public int ClubId { get; set; }
    public string? OwnerAccountId { get; set; }
    public int MessageIdCounter { get; set; } = 1;
    [JsonIgnore]
    public string? ClubName { get; set; }
    public string? Clubaciklama { get; set; }
    public int ClubAvatarID { get; set; }
    public int? TotalKupa { get; set; }
    public List<ClubMember> Members { get; set; } = new List<ClubMember>();
    public List<ClubMessage> Messages { get; set; } = new List<ClubMessage>();

    [JsonIgnore]
    public object SyncLock = new object();
}
public class ClubMessage
{
    public int MessageId { get; set; }
    public ClubMessageFlags messageFlags;
    public ClubEventType eventType;
    public string? SenderId { get; set; }
    public string? SenderName { get; set; }
    public int SenderAvatarID { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Content { get; set; }
    public string? ActorName;
    public string? ActorID;
    public string? TargetName;
}

public static class ClubManager
{
    public static ConcurrentDictionary<int, Club> Clubs = new ConcurrentDictionary<int, Club>();
    private static int lastClubId = 1;

    # region Load/Save
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
                        club.ClubId = reader.GetInt32(reader.GetOrdinal("ClubId"));
                        club.ClubName = reader.IsDBNull(reader.GetOrdinal("ClubName")) ? null : reader.GetString(reader.GetOrdinal("ClubName"));

                        Clubs[club.ClubId] = club;
                        if (club.ClubId >= lastClubId)
                            lastClubId = club.ClubId + 1;

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
            INSERT INTO Clubs (ClubId, ClubName, Data) 
            VALUES (@ClubId, @ClubName, @Data) 
            ON CONFLICT(ClubId) DO UPDATE SET
                ClubName=excluded.ClubName, 
                Data=excluded.Data;";

        using (var command = connection.CreateCommand())
        {
            string jsonData;
            lock (club.SyncLock)
            {
                jsonData = JsonConvert.SerializeObject(club);
            }

            command.CommandText = upsertQuery;
            command.Parameters.AddWithValue("@ClubId", club.ClubId);
            command.Parameters.AddWithValue("@ClubName", club.ClubName ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Data", jsonData);
            command.ExecuteNonQuery();
        }
    }

    public static Club LoadClub(int clubId)
    {
        var club = ClubCache.Load(clubId);
        if (club != null) return club;
        
        if (Clubs.TryGetValue(clubId, out club))
        {
            ClubCache.Cache(club);
            return club;
        }

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
    public static Club CreateClub(string name, string aciklama, int Avatarid, string leaderAccountId)
    {
        var leaderAccount = AccountCache.Load(leaderAccountId);
        if (leaderAccount == null) return null;

        int clubId = System.Threading.Interlocked.Increment(ref lastClubId);
        var club = new Club
        {
            ClubId = clubId,
            ClubName = name,
            Clubaciklama = aciklama,
            ClubAvatarID = Avatarid,
            OwnerAccountId = leaderAccount.AccountId,
            MessageIdCounter = 1,
            TotalKupa = leaderAccount.Trophy,
            Members = new List<ClubMember>
            {
                new ClubMember { AccountName = leaderAccount.Username, Accountid = leaderAccount.AccountId, Role = ClubRole.Leader, NameColorID = leaderAccount.Namecolorid, AvatarID =leaderAccount.Avatarid }
            }
        };

        Clubs[club.ClubId] = club;
        ClubCache.Cache(club);
        
        using (var connection = DatabaseManager.GetConnection())
        {
            connection.Open();
            SaveClubToDb(club, connection);
        }

        leaderAccount.Clubid = club.ClubId;
        leaderAccount.clubRole = ClubRole.Leader;
        leaderAccount.ClubName = club.ClubName;
        
        Logger.genellog($"Club oluşturuldu: name: {club.ClubName} des: {club.Clubaciklama} id: {club.ClubId}");

        return club;
    }
    #endregion

    #region Üye ekleme
    public static bool AddMember(int clubId, string newMemberId)
    {
        if (!Clubs.ContainsKey(clubId)) return false;

        var club = Clubs[clubId];
        var newAccount = AccountCache.Load(newMemberId);
        if (newAccount == null) return false;

        lock (club.SyncLock)
        {
            if (club.Members.Any(m => m.Accountid == newMemberId))
            {
                Console.WriteLine("bu oyuncu bu clupte");
                return false;
            }

            club.Members.Add(new ClubMember
            {
                AccountName = newAccount.Username,
                Accountid = newAccount.AccountId,
                Role = ClubRole.Member,
                NameColorID = newAccount.Namecolorid,
                AvatarID = newAccount.Avatarid
            });
        }
        newAccount.clubRole = ClubRole.Member;
        newAccount.Clubid = club.ClubId;
        newAccount.ClubName = club.ClubName;

        Save();
        return true;
    }
    #endregion
    
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

    #region Üye çıkarma
    public static bool RemoveMember(int clubId, string targetMemberId)
    {
        if (!Clubs.ContainsKey(clubId)) return false;

        var club = Clubs[clubId];
        lock (club.SyncLock)
        {
            var target = club.Members.FirstOrDefault(m => m.Accountid == targetMemberId);

            if (target == null)
            {
                Logger.errorslog("Oyuncu clubte bulanamadı");
                return false;
            }

            club.Members.Remove(target);
        }
        Save();
        Logger.genellog("Oyuncu clubten kicklendi");
        if (club.Members.Count == 0)
        {
            DeleteClub(club.ClubId);
        }
        return true;
    }
    #endregion

    #region Üye Atma
    public static bool KickMember(int clubId, string actorId, string targetMemberId)
    {
        if (!Clubs.ContainsKey(clubId)) return false;

        var club = Clubs[clubId];
        lock (club.SyncLock)
        {
            var actor = club.Members.FirstOrDefault(m => m.Accountid == actorId);
            var target = club.Members.FirstOrDefault(m => m.Accountid == targetMemberId);

            if (actor == null || target == null)
            {
                Logger.errorslog($"[Club Manager] hesaplardan biri kulüp üyesi değil");
                return false;
            }

            if (actor.Role == ClubRole.Member) return false;
            if (target.Role == ClubRole.Leader) return false;
            if (actor.Role == ClubRole.CoLeader && target.Role == ClubRole.CoLeader) return false;

            club.Members.Remove(target);
        }
        var acccount = AccountCache.Load(targetMemberId);
        acccount.Clubid = -1;
        acccount.ClubName = null;
        
        Notfication notfication = new Notfication
        {
            type =  NotficationTypes.NotficationType.Inbox,
            Sender = "Sistem",
            Message = $"{club.ClubName} kulübünden atıldın.",
            Timespam = DateTime.Now
        };
        acccount.inboxesNotfications.Add(notfication);
        
        if (SessionManager.IsOnline(acccount.AccountId))
        {
            var session = SessionManager.GetSession(acccount.AccountId);
            NotficationSender.Send(session, notfication);
        }
        
        Save();
        return true;
    }
    #endregion

    #region Rol değiştirme
    public static bool ChangeMemberRole(int clubId, string actorId, string targetMemberId, ClubRole newRole)
    {
        if (!Clubs.ContainsKey(clubId)) return false;

        var club = Clubs[clubId];
        var actor = club.Members.FirstOrDefault(m => m.Accountid == actorId);
        var target = club.Members.FirstOrDefault(m => m.Accountid == targetMemberId);

        if (actor == null || target == null) return false;

        if (actor.Role != ClubRole.Leader && actor.Role != ClubRole.CoLeader) return false;
        if (target.Role == ClubRole.Leader && actor.Role != ClubRole.Leader) return false;

        target.Role = newRole;
        Save();
        return true;
    }
    #endregion

    public static void SendMessage(int clubId, string senderAccountId, string content)
    {
        var club = LoadClub(clubId);
        if (club == null) return;

        lock (club.SyncLock)
        {
            var sender = club.Members.FirstOrDefault(m => m.Accountid == senderAccountId);
            if (sender == null) return;

            club.Messages.Add(new ClubMessage
            {
                MessageId = club.MessageIdCounter++,
                messageFlags = ClubMessageFlags.None,
                SenderName = sender.AccountName,
                SenderId = sender.Accountid,
                Timestamp = DateTime.Now,
                Content = content
            });
        }

        Save();
    }

    public static bool ChangeClubSettings(int clubid, string acccountId, string name, string aciklama, int Avatarid)
    {
        Club club = ClubManager.LoadClub(clubid);
        if (club == null) return false;
        
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(aciklama))
        {
            Logger.errorslog("Kulüp adı veya açıklama boş olamaz!");
            return false;
        }
        
        lock (club.SyncLock)
        {
            club.ClubName = name;
            club.Clubaciklama = aciklama;
            club.ClubAvatarID = Avatarid;
        }
        ClubManager.Save();
        Logger.genellog($"Kulüp bilgileri güncellendi: {club.ClubName} ({club.ClubId})");
        return true;
    }

    #region  Üye data update
    public static void MemberDataUpdate(string accid, int clubid)
    {
        if (!Clubs.TryGetValue(clubid, out var club)) return;
        var member = club.Members.FirstOrDefault(m => m.Accountid == accid);
        if (member == null) return;

        AccountManager.AccountData account = AccountCache.Load(accid);
        if (account == null) return;

        lock (club.SyncLock)
        {
            member.Accountid = account.AccountId;
            member.AccountName = account.Username;
            member.AvatarID = account.Avatarid;
            member.NameColorID = account.Namecolorid;
        }
        
        Save();
    }
    #endregion

    #region Kulüp Silme
    public static bool DeleteClub(int clubId)
    {
        if (!Clubs.ContainsKey(clubId)) return false;

        var club = Clubs[clubId];
        Clubs.TryRemove(clubId, out _);
        ClubCache.GetCachedClubs().TryRemove(clubId, out _);

        using (var connection = DatabaseManager.GetConnection())
        {
            connection.Open();
            var deleteCmd = connection.CreateCommand();
            deleteCmd.CommandText = "DELETE FROM Clubs WHERE ClubId = @ClubId";
            deleteCmd.Parameters.AddWithValue("@ClubId", clubId);
            deleteCmd.ExecuteNonQuery();
        }

        Logger.genellog($"Kulüp silindi: {club.ClubName} ({clubId})");
        return true;
    }
    #endregion

    #region  Mesaj bulma
    public static ClubMessage GetCLubMessage(Club club, int messageid)
    {
        return club.Messages.Find(m => m.MessageId == messageid);
    }
    #endregion
}


