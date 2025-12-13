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
    public int ClubId { get; set; }
    public string? ClubName { get; set; }
     public string? Clubaciklama { get; set; }
     public int ClubAvatarID { get; set;}
    public int? TotalKupa { get; set; }
    public List<ClubMember> Members  = new List<ClubMember>();
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
            
            // Cache'e ekle
            foreach (var club in Clubs.Values)
            {
                ClubCache.Cache(club);
            }
            
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
        // Cache'den yükle (daha hızlı)
        var club = ClubCache.Load(clubId);
        if (club != null)
        {
            return club;
        }
        
        // Cache'de yoksa normal dictionary'den yükle
        if (Clubs.TryGetValue(clubId, out club))
        {
            ClubCache.Cache(club); // Tekrar cache'e ekle
            return club;
        }

        Console.WriteLine($"[ClubManager] ClubId {clubId} bulunamadı.");
        return null;
    }

    public static void Save()
    {
        // Cache'den de kulüpleri güncelle
       foreach (var cachedClub in ClubCache.GetCachedClubs())
{
    Clubs[cachedClub.Key] = cachedClub.Value; // güncel veriyi doğrudan yazar
}

        
        var json = JsonConvert.SerializeObject(Clubs, Formatting.Indented);
        File.WriteAllText(filePath, json);
   //     Console.WriteLine("[ClubManager] Kulüpler kaydedildi.");
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
                new ClubMember { AccountName = leaderAccount.Username, Accountid = leaderAccount.AccountId, Role = ClubRole.Leader, NameColorID = leaderAccount.Namecolorid, AvatarID =leaderAccount.Avatarid }
            }

        };

        Clubs[club.ClubId] = club;
        ClubCache.Cache(club); // Cache'e ekle
        Save();
        leaderAccount.Clubid = club.ClubId;
        leaderAccount.clubRole = ClubRole.Leader;
        Logger.genellog($"Club oluşturuldu: name: {club.ClubName} des: {club.Clubaciklama} id: {club.ClubId}");
        leaderAccount.ClubName = club.ClubName;

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
            Role = ClubRole.Member,
            NameColorID = newAccount.Namecolorid,
            AvatarID = newAccount.Avatarid
        });
        newAccount.clubRole = ClubRole.Member;
        newAccount.Clubid = club.ClubId;
        newAccount.ClubName = club.ClubName;
        Console.WriteLine("accounda club name data: " + newAccount.ClubName);
       

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
        var targetAccount = AccountManager.LoadAccount(targetMemberId);
        


        if (target == null)
        {
            Logger.errorslog("Oyuncu clubte bulanamadı");
            return false;
        }

        club.Members.Remove(target);
        Save();
        Logger.genellog("Oyuncu clubten kicklendi");
        if(club.Members.Count == 0)
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
        var actor = club.Members.FirstOrDefault(m => m.Accountid == actorId);
        var target = club.Members.FirstOrDefault(m => m.Accountid == targetMemberId);

        if (actor == null || target == null)
        {
             Logger.errorslog($"[Club Manager] hesaplardan biri kulüp üyesi değil");
            return false;
        } 

        // Rol kontrolleri
        if (actor.Role == ClubRole.Member) return false; // Member çıkaramaz
        if (target.Role == ClubRole.Leader) return false; // Leader çıkarılamaz
        if (actor.Role == ClubRole.CoLeader && target.Role == ClubRole.CoLeader) return false; // CoLeader sadece alt role çıkarabilir

        club.Members.Remove(target);
        var acccount = AccountCache.Load(targetMemberId);
        acccount.Clubid = -1;
        acccount.ClubName= null;
       Notfication notfication= new Notfication
       {
           Id = 12,
           Sender = "Sistem",
           Message = $"{club.ClubName} kulübünden atıldın.",
          Timespam = DateTime.Now
       };
        acccount.inboxesNotfications.Add(notfication);
        if(SessionManager.IsOnline(acccount.AccountId))
        {
            var session = SessionManager.GetSession(acccount.AccountId);
            NotficationSender.Send(session,notfication);
        }
        

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

    public static void MemberDataUpdate(string accid, int clubid)
    {
        var club = Clubs[clubid];
        var member = club.Members.FirstOrDefault(m => m.Accountid == accid);
        if (member == null)
        {
            Logger.errorslog($"[Club Manager] güncellenecek üye bulunamadı. accıd:{accid} clubid:{clubid}");
            return;
        }
        AccountManager.AccountData account = AccountCache.Load(accid);

        if (account == null)
        {
            Logger.errorslog($"[Club Manager] oyuncu bulunamadı. accıd:{accid} clubid:{clubid}");
            return;
        }
        member.Accountid = account.AccountId;
        member.AccountName = account.Username;
        member.AvatarID = account.Avatarid;
        member.NameColorID = account.Namecolorid;

    }
    #region Kulüp Silme
public static bool DeleteClub(int clubId)
{
    if (!Clubs.ContainsKey(clubId))
    {
        Logger.errorslog($"[ClubManager] Silinecek kulüp bulunamadı: {clubId}");
        return false;
    }

    var club = Clubs[clubId];

        Clubs.Remove(clubId);
        ClubCache.GetCachedClubs().TryRemove(clubId, out _);

        // ClubCache.RemoveFromCache(clubId); // Cache'den de kaldır



        Logger.genellog($"Kulüp silindi: {club.ClubName} ({clubId})");
        Save();
        return true;
    
}
#endregion

}
