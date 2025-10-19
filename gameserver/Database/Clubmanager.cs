using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

public enum ClubRole
{
    Member,
    CoLeader,
    Leader
}

public class ClubMember
{
    public string?  AccountName { get; set; }
    public string? Accountid { get; set; }
    public ClubRole Role { get; set; }
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
    public int ClubId { get; set; }
    public string? ClubName { get; set; }
     public string? Clubaciklama { get; set; }
     public int ClubAvatarID { get; set;}
    public int? TotalKupa { get; set; }
    public List<ClubMember> Members { get; set; } = new List<ClubMember>();
     public List<ClubMessage> Messages { get; set; } = new List<ClubMessage>(); // Kulüp mesajları
}
public class ClubMessage
{
    public string? SenderId { get; set; }
    public string? SenderName { get; set; }
    public int SenderAvatarID { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Content { get; set; }
}

public static class ClubManager
{
    public static Dictionary<int, Club> Clubs = new Dictionary<int, Club>();
    private static string filePath = "clubs.json";
    private static int lastClubId = 1;

    #region Load/Save
    public static void Allclubload()
    {
        Console.WriteLine("kulüpler yükleniyor...");
        if (File.Exists(filePath))
        {
            var json = File.ReadAllText(filePath);
            Clubs = JsonConvert.DeserializeObject<Dictionary<int, Club>>(json) ?? new Dictionary<int, Club>();
            lastClubId = Clubs.Keys.Count > 0 ? Clubs.Keys.Max() + 1 : 1;
            Console.WriteLine($"[ClubManager] {Clubs.Count} kulüp yüklendi.");
        }
        else
        {
            Logger.errorslog($"{filePath} dosyası bulunamadı. yeni olşturuluyor...");
            File.Create(filePath).Close();

        }
    }
    public static Club LoadClub(int clubId)
    {
        // sanırım bu kontrole gerek yok  if (clubId == -1) return null;
        if (Clubs.TryGetValue(clubId, out var club))
        {
            return club;
        }

        Console.WriteLine($"[ClubManager] ClubId {clubId} bulunamadı.");
        return null;
    }

    public static void Save()
    {
        var json = JsonConvert.SerializeObject(Clubs, Formatting.Indented);
        File.WriteAllText(filePath, json);
    }
    #endregion

    #region Kulüp oluşturma
    public static Club CreateClub(string name, string aciklama, int Avatarid, string leaderAccountId)
    {
        var leaderAccount = AccountManager.LoadAccount(leaderAccountId);
        if (leaderAccount == null) return null;

        var club = new Club
        {
            ClubId = lastClubId++,
            ClubName = name,
            Clubaciklama = aciklama,
            ClubAvatarID = Avatarid,
            Members = new List<ClubMember>
            {
                new ClubMember { AccountName = leaderAccount.Username, Accountid = leaderAccount.AccountId, Role = ClubRole.Leader }
            }

        };

        Clubs[club.ClubId] = club;
        Save();
        leaderAccount.Clubid = club.ClubId;
        leaderAccount.clubRole = ClubRole.Leader;
        Logger.genellog($"Club oluşturuldu: name: {club.ClubName} des: {club.Clubaciklama} id: {club.ClubId}");

        return club;
    }
    #endregion

    #region Üye ekleme
    public static bool AddMember(int clubId, string newMemberId)
    {
        if (!Clubs.ContainsKey(clubId)) return false;

        var club = Clubs[clubId];

        var newAccount = AccountManager.LoadAccount(newMemberId);
        if (newAccount == null) return false;

        if (club.Members.Any(m => m.Accountid == newMemberId.ToString()))
            return false;

        club.Members.Add(new ClubMember
        {
            AccountName = newAccount.Username,
            Accountid = newAccount.AccountId,
            Role = ClubRole.Member
        });

        Save();
        return true;
    }
    #endregion
   public static List<Club> RandomList(int count)
{
    var availableClubs = Clubs.Values.ToList();
    
    // Random nesnesini static yap veya daha iyisi bir kere oluştur
    Random random = new Random();
    
    // Mevcut kulüplerin kopyasını al ki orijinal liste bozulmasın
    List<Club> tempClubs = new List<Club>(availableClubs);
    List<Club> randomClubs = new List<Club>();

    // count, mevcut kulüp sayısından fazla olamaz
    count = Math.Min(count, tempClubs.Count);

    for (int i = 0; i < count; i++)
    {
        int index = random.Next(tempClubs.Count);
        randomClubs.Add(tempClubs[index]);
        tempClubs.RemoveAt(index); // Seçileni listeden çıkar
    }
    
    return randomClubs;
}
    #region Üye çıkarma
    public static bool RemoveMember(int clubId, string targetMemberId)
    {
        if (!Clubs.ContainsKey(clubId)) return false;

        var club = Clubs[clubId];

        var target = club.Members.FirstOrDefault(m => m.Accountid == targetMemberId);

        if (target == null)
        {
            Logger.errorslog("Oyuncu clubte bulanamadı");
            return false;
        }

        club.Members.Remove(target);
        Save();
        return true;
        Logger.genellog("Oyuncu clubten kicklendi");
    }
    #endregion
    #region Üye Atma
    public static bool KickMember(int clubId, int actorId, int targetMemberId)
    {
        if (!Clubs.ContainsKey(clubId)) return false;

        var club = Clubs[clubId];
        var actor = club.Members.FirstOrDefault(m => m.Accountid == actorId.ToString());
        var target = club.Members.FirstOrDefault(m => m.Accountid == targetMemberId.ToString());

        if (actor == null || target == null) return false;

        // Rol kontrolleri
        if (actor.Role == ClubRole.Member) return false; // Member çıkaramaz
        if (target.Role == ClubRole.Leader) return false; // Leader çıkarılamaz
        if (actor.Role == ClubRole.CoLeader && target.Role == ClubRole.CoLeader) return false; // CoLeader sadece alt role çıkarabilir

        club.Members.Remove(target);
        Save();
        return true;
    }
    #endregion

    #region Rol değiştirme
    public static bool ChangeMemberRole(int clubId, int actorId, int targetMemberId, ClubRole newRole)
    {
        if (!Clubs.ContainsKey(clubId)) return false;

        var club = Clubs[clubId];
        var actor = club.Members.FirstOrDefault(m => m.Accountid == actorId.ToString());
        var target = club.Members.FirstOrDefault(m => m.Accountid == targetMemberId.ToString());

        if (actor == null || target == null) return false;

        if (actor.Role != ClubRole.Leader && actor.Role != ClubRole.CoLeader) return false;
        if (target.Role == ClubRole.Leader && actor.Role != ClubRole.Leader) return false; // Leader rolü sadece Leader değiştirebilir

        target.Role = newRole;
        Save();
        return true;
    }
    #endregion
    // Kulüp içi mesaj gönderme
    public static bool SendMessage(int clubId, int senderAccountId, string content)
    {
        var club = LoadClub(clubId);
        if (club == null) return false;

        var sender = club.Members.FirstOrDefault(m => m.Accountid == senderAccountId.ToString());
        if (sender == null) return false; // Sadece üye mesaj gönderebilir

        club.Messages.Add(new ClubMessage
        {
            SenderName = sender.AccountName,
            SenderId = sender.Accountid,
            Timestamp = DateTime.Now,
            Content = content
        });

        Save();
        return true;
    }
    // Kulüp işlemi yapıldığında:
    public static void BroadcastClubUpdate(string action, object data)
    {
        var message = new
        {
            messageId = 20, // salladım sonra updateyle
            action, // "AddMember", "RemoveMember", "SendMessage"
            data
        };
        /*   Connection connection;
          connection.Send(message);

       string json = JsonConvert.SerializeObject(message);

           foreach (var client in ConnectedClients)
            {
                if (client.IsInClub(clubId))
                    client.Send(json);
            }*/
    }


    // Kulüp mesajlarını listeleme
    public static List<ClubMessage> GetMessages(int clubId)
    {
        var club = LoadClub(clubId);
        if (club == null) return new List<ClubMessage>();

        return club.Messages.OrderBy(m => m.Timestamp).ToList();
    }

    public static bool ChangeClubSettings(int clubid, string acccountId, string name, string aciklama, int Avatarid)
    {
        Club club = ClubManager.LoadClub(clubid);
        AccountManager.AccountData account = AccountCache.Load(acccountId);
        if (account == null) return false;
        if (club == null) return false;
        // if (account.clubRole == ClubRole.Member) return false;
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(aciklama))
        {
            Logger.errorslog("Kulüp adı veya açıklama boş olamaz!");
            return false;
        }
        club.ClubName = name;
        club.Clubaciklama = aciklama;
        club.ClubAvatarID = Avatarid;
        ClubManager.Save();
        Logger.genellog($"Kulüp bilgileri güncellendi: {club.ClubName} ({club.ClubId})");
        return true;

    }
     
   /*  public static Club SearchClub(string name)
    {
        List<Club> founded = new List<Club>();
        foreach(var clubs in Clubs)
        {
            if(string.IsNullOrWhiteSpace(clubs.name))
        }
        return null;
    }*/

}
