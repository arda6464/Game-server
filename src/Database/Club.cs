using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

public enum ClubRole
{
    None,
    Member,
    CoLeader,
    Leader
}
public enum ClubRequestState
{
    Waiting,
    Accepted,
    Rejected
}
public enum ClubMessageFlags : int
{
    None = 0,
    HasTarget = 1,
    HasSystem = 2,
    Request = 3
}
public enum ClubEventType : byte
{
    JoinMessage,
    LeaveMessage,
    KickMessage,
    CreateMessage,
    EditMessage,

    RoleChangeMessage,
}
public enum ClubState : byte
{
    Open,
    Closed,
    OnlyInvite
}


public class ClubMember
{
    public string? AccountName { get; set; }
    public int ID { get; set; }
    public ClubRole Role { get; set; }
    public int NameColorID { get; set; }
    public int AvatarID { get; set; }
}

public class ClubMemberinfo
{
    public int ClubId { get; set; }
    public string? ClubName { get; set; }
    public string? Clubaciklama { get; set; }
    public int? TotalTrophy { get; set; }
}

public class ClubMessage
{
    public int MessageId { get; set; }
    public ClubMessageFlags messageFlags;
    public ClubEventType eventType;
    public int SenderId { get; set; }

    public string? SenderName { get; set; }
    public int SenderAvatarID { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Content { get; set; }
    public ClubRole Role { get; set; }
    public string? ActorName;
    public int ActorID;
    public string? TargetName;
    public int TargetID;
    public ClubRequestState RequestState;
}

public class Club
{
    [JsonIgnore]
    public int ID { get; set; }
    public int OwnerID { get; set; }
    public int MessageIdCounter { get; set; } = 1;
    [JsonIgnore]
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int AvatarID { get; set; }
    public int TotalTrophy { get; set; }
    public List<ClubMember> Members { get; set; } = new List<ClubMember>();
    public List<ClubMessage> Messages { get; set; } = new List<ClubMessage>();
    public ClubState State { get; set; } = ClubState.Open;
    public int MaxMembers { get; set; } = 50;
    public List<int> PendingInvites { get; set; } = new List<int>();
    public string Region { get; set; } = "Global";

    [JsonIgnore]
    public object SyncLock = new object();

    #region Üye ekleme
    public bool AddMember(int newMemberId)
    {
        var newAccount = AccountCache.Load(newMemberId);
        if (newAccount == null) return false;

        lock (SyncLock)
        {
            if (Members.Count >= MaxMembers)
            {
                return false;
            }

            if (Members.Any(m => m.ID == newMemberId))
            {
                Console.WriteLine("bu oyuncu bu clupte");
                return false;
            }

            Members.Add(new ClubMember
            {
                AccountName = newAccount.Username,
                ID = newAccount.ID,
                Role = ClubRole.Member,
                NameColorID = newAccount.Namecolorid,
                AvatarID = newAccount.Avatarid
            });
        }
        newAccount.clubRole = ClubRole.Member;
        newAccount.Clubid = ID;
        newAccount.ClubName = Name;

        ClubManager.Save();
        AccountManager.SaveAccounts();
        return true;
    }
    #endregion

    #region Mesaj gönderme
    public void SendMessageToClubMembers(ClubMessage message)
    {
        lock (SyncLock)
        {
            message.MessageId = MessageIdCounter++;
            if (message.Timestamp == default || message.Timestamp == DateTime.MinValue)
            {
                message.Timestamp = DateTime.Now;
            }
            Messages.Add(message);

            foreach (var member in Members)
            {
                if (SessionManager.IsOnline(member.ID))
                {
                    var session = SessionManager.GetSession(member.ID);
                    if (session != null)
                    {
                        var packet = new GetClubMessagePacket
                        {
                            Message = message,
                            Role = message.messageFlags == ClubMessageFlags.None
                                ? (Members.FirstOrDefault(m => m.ID == message.SenderId)?.Role ?? ClubRole.Member)
                                : ClubRole.Member
                        };
                        session.Send(packet);
                    }
                }
            }
        }
    }


    #endregion

    #region Üye çıkarma
    public bool RemoveMember(int targetMemberId)
    {
        lock (SyncLock)
        {
            var target = Members.FirstOrDefault(m => m.ID == targetMemberId);

            if (target == null)
            {
                Logger.errorslog("Oyuncu clubte bulanamadı");
                return false;
            }

            Members.Remove(target);
        }
        ClubManager.Save();
        AccountManager.SaveAccounts();
        Logger.genellog("Oyuncu clubten kicklendi");
        if (Members.Count == 0)
        {
            ClubManager.DeleteClub(ID);
        }
        return true;
    }
    #endregion

    #region Üye Atma
    public bool KickMember(int actorId, int targetMemberId) // actor atan kişi target atılan
    {
        lock (SyncLock)
        {
            var actor = Members.FirstOrDefault(m => m.ID == actorId);
            var target = Members.FirstOrDefault(m => m.ID == targetMemberId);

            if (actor == null || target == null)
            {
                Logger.errorslog($"[Club] hesaplardan biri kulüp üyesi değil");
                return false;
            }

            if (actor.Role == ClubRole.Member) return false;
            if (target.Role == ClubRole.Leader) return false;
            if (actor.Role == ClubRole.CoLeader && target.Role == ClubRole.CoLeader) return false;

            Members.Remove(target);
        }
        var acccount = AccountCache.Load(targetMemberId);
        var actotraccount = AccountCache.Load(actorId);
        if (acccount != null)
        {
            acccount.Clubid = 0;
            acccount.ClubName = null;
            acccount.clubRole = ClubRole.None;

            Notfication notfication = new Notfication
            {
                type = NotficationTypes.NotficationType.Inbox,
                Sender = "Sistem",
                Message = $"{Name} kulübünden atıldın.",
                Timespam = DateTime.Now
            };
            acccount.inboxesNotfications.Add(notfication);

            if (SessionManager.IsOnline(acccount.ID))
            {
                var session = SessionManager.GetSession(acccount.ID);
                NotficationSender.Send(session, notfication);
            }
            ClubMessage kickmessage = new ClubMessage
            {
                messageFlags = ClubMessageFlags.HasTarget,
                ActorID = actotraccount.ID,
                ActorName = actotraccount.Username,
                eventType = ClubEventType.KickMessage,
                TargetName = acccount.Username,
                 TargetID = acccount.ID
            };
            SendMessageToClubMembers(kickmessage);
        }

        ClubManager.Save();
        return true;
    }
    #endregion

    #region Rol değiştirme
    public bool ChangeMemberRole(int actorId, int targetMemberId, ClubRole newRole)
    {
        var actor = Members.FirstOrDefault(m => m.ID == actorId);
        var target = Members.FirstOrDefault(m => m.ID == targetMemberId);

        if (actor == null || target == null) return false;

        if (actor.Role != ClubRole.Leader && actor.Role != ClubRole.CoLeader) return false;
        if (target.Role == ClubRole.Leader && actor.Role != ClubRole.Leader) return false;

        target.Role = newRole;
        ClubManager.Save();
        var targetAccount = AccountCache.Load(targetMemberId);
        if (targetAccount != null)
        {
            targetAccount.clubRole = newRole;
            AccountManager.SaveAccounts();
        }
        return true;
    }
    #endregion

    #region Kulüp ayarları
    public bool ChangeClubSettings(int accid, string name, string aciklama, int Avatarid, int State, string? Region = "GB")
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(aciklama))
        {
            Logger.errorslog("Kulüp adı veya açıklama boş olamaz!");
            return false;
        }

        lock (SyncLock)
        {
            Name = name;
            Description = aciklama;
            AvatarID = Avatarid;
            this.State = (ClubState)State;
            this.Region = Region;
        }
        ClubManager.Save();
        Logger.genellog($"Kulüp bilgileri güncellendi: {Name} ({ID})");
        return true;
    }
    #endregion

    #region Üye data update
    public void MemberDataUpdate(int playerid)
    {
        var member = Members.FirstOrDefault(m => m.ID == playerid);
        if (member == null) return;

        AccountManager.AccountData account = AccountCache.Load(playerid);
        if (account == null) return;

        lock (SyncLock)
        {
            member.AccountName = account.Username;
            member.AvatarID = account.Avatarid;
            member.NameColorID = account.Namecolorid;
        }

        ClubManager.Save();
    }
    #endregion

    #region Mesaj bulma
    public ClubMessage GetCLubMessage(int messageid)
    {
        return Messages.Find(m => m.MessageId == messageid);
    }
    #endregion
}
