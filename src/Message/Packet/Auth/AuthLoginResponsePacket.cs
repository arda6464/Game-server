using System.Collections.Generic;
using Logic;

public class AuthLoginResponsePacket : IPacket
{
    public AccountManager.AccountData Account { get; set; }
    public int ConnectionToken { get; set; } 
    public Club Club { get; set; }
    public List<Club> RandomClubs { get; set; }
    public long NextQuestRefreshTime { get; set; }
    public long NextSeasonalQuestRefreshTime { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteShort((short)MessageType.AuthLoginResponse);
      //  buffer.WriteInt(ConnectionToken); // UDP Handshake Token
        
        // --- Account Data ---
        buffer.WriteString(Account.AccountId);
        buffer.WriteString(Account.Username);
        buffer.WriteInt(Account.Avatarid);
        buffer.WriteInt(Account.Namecolorid);
        buffer.WriteInt(Account.Trophy);
        buffer.WriteInt(Account.Level);
        buffer.WriteInt(Account.Clubid);
        buffer.WriteInt(Account.Premium);
        buffer.WriteInt(Account.Roles.Count);
        foreach (var role in Account.Roles)
        {
            buffer.WriteString(role.ToString());
        }


    
        // settings data (1 byte for flags)
        byte settingFlags = 0;
        if (Account.SendOnlineBestFriendNotification) settingFlags |= 1 << 0;
        if (Account.SendNewEventNotification)         settingFlags |= 1 << 1;
        if (Account.SendInviteNotification)           settingFlags |= 1 << 2;
        if (Account.SendClaimRewardNotification)      settingFlags |= 1 << 3;

        buffer.WriteByte(settingFlags);
        buffer.WriteInt(Account.Gems);
        buffer.WriteInt(Account.Coins);

        // --- Club Data ---
        if (Club == null)
        {
            buffer.WriteInt(-1); // ClubId
            buffer.WriteString("");
            buffer.WriteString("");
            buffer.WriteInt(1); // TotalKupa
            buffer.WriteInt(0); // Members.Count
            buffer.WriteInt(0); // Messages.Count
            buffer.WriteInt(-1); // Again Members Count check? (Logic in handler was weird: wrote member count twice but second one inside null check block logic?)
            // Replicating exact handler logic:
            // "if (club == null) byteBuffer.WriteInt(-1); else byteBuffer.WriteInt(club.Members.Count);"
        }
        else
        {
            buffer.WriteInt(Club.ClubId);
            buffer.WriteString(Club.ClubName);
            buffer.WriteString(Club.Clubaciklama);
            buffer.WriteInt(Club.TotalKupa ?? 0);
            buffer.WriteInt(Club.Members.Count);
            buffer.WriteInt(Club.Messages.Count);
            
            lock (Club.SyncLock)
            {
                foreach (var message in Club.Messages)
                {
                    buffer.WriteByte((byte)message.messageFlags);
                    switch ((ClubMessageFlags)message.messageFlags)
                    {
                        case ClubMessageFlags.None:
                            buffer.WriteByte((byte)message.MessageId);
                            buffer.WriteString(message.SenderId);
                            buffer.WriteString(message.SenderName);
                            buffer.WriteInt(message.SenderAvatarID);
                            buffer.WriteString("Üye");
                            buffer.WriteString(message.Content);
                            break;
                         case ClubMessageFlags.HasSystem:
                            buffer.WriteInt((int)message.eventType);
                            buffer.WriteString(message.ActorName);
                            buffer.WriteString(message.ActorID ?? "");
                            break;
                        case ClubMessageFlags.HasTarget:
                            buffer.WriteInt((int)message.eventType);
                            buffer.WriteString(message.ActorName);
                            buffer.WriteString(message.ActorID);
                            buffer.WriteString(message.TargetName);
                            break;
                    }
                }
            }
            buffer.WriteInt(Club.Members.Count);
        }

        foreach (var member in (Club?.Members ?? new List<ClubMember>()))
        {
            buffer.WriteString(member.Accountid);
            buffer.WriteString(member.AccountName);
            buffer.WriteString(member.Role.ToString());
            buffer.WriteInt(member.NameColorID);
            buffer.WriteInt(member.AvatarID);
            buffer.WriteBool(SessionManager.IsOnline(member.Accountid));
        }

        // --- Random Clubs (10) ---
        buffer.WriteInt(RandomClubs.Count);
        foreach (var rclub in RandomClubs)
        {
             buffer.WriteInt(rclub.ClubId);
            buffer.WriteString(rclub.ClubName);
            buffer.WriteString(rclub.Clubaciklama);
            buffer.WriteInt(rclub.TotalKupa ?? 0);
            buffer.WriteInt(rclub.Members.Count);
        }

        // --- Friends ---
        buffer.WriteInt(Account.Friends.Count);
        foreach (var friend in Account.Friends)
        {
            buffer.WriteString(friend.Id);
            buffer.WriteInt(friend.AvatarId);
            buffer.WriteString(friend.Username);
            buffer.WriteInt(friend.NameColorID);
            buffer.WriteBool(friend.IsBestFriend);
            buffer.WriteInt(friend.Trophy);
            buffer.WriteBool(SessionManager.IsOnline(friend.Id));
        }
        
        lock (Account.SyncLock)
        {
            buffer.WriteInt(Account.Requests.Count);
            foreach (var request in Account.Requests)
            {
                buffer.WriteString(request.Id);
                buffer.WriteInt(request.AvatarId);
                buffer.WriteString(request.Username);
            }
        
            // --- Quests ---
            buffer.WriteLong(NextQuestRefreshTime);
            buffer.WriteLong(NextSeasonalQuestRefreshTime);
            
            buffer.WriteShort((short)Account.Quests.Count);
            foreach (var quest in Account.Quests)
            {
                buffer.WriteByte((byte)quest.ID);
                buffer.WriteByte((byte)quest.Type);
                buffer.WriteByte((byte)quest.Target);
                buffer.WriteInt(quest.CurrentGoal);
                buffer.WriteByte((byte)quest.RewardType);
                buffer.WriteShort((short)quest.Goal);
                buffer.WriteBool(quest.IsDailyQuest);
                buffer.WriteBool(quest.IsPremium);
                buffer.WriteBool(quest.IsCompleted);
            }
        }
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
