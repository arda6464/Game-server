using System.Collections.Generic;
using Logic;

public class AuthLoginResponsePacket : IPacket
{
    public AccountManager.AccountData? Account { get; set; }
    public int ConnectionToken { get; set; }
    public Club Club { get; set; }
    public List<Club>? RandomClubs { get; set; }
    public long NextQuestRefreshTime { get; set; }
    public long NextSeasonalQuestRefreshTime { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        buffer.WriteVarInt((int)MessageType.AuthLoginResponse);
        buffer.WriteVarInt(ConnectionToken); // UDP Handshake Token

        // --- Account Data ---
        #region  Account data
        buffer.WriteVarInt(Account.ID); // Sayısal ID
        buffer.WriteVarString(Account.Username);
        buffer.WriteVarInt(Account.Avatarid);
        buffer.WriteVarInt(Account.Namecolorid);
        buffer.WriteVarInt(Account.Trophy);
        buffer.WriteVarInt(Account.Level);
        buffer.WriteVarInt(Account.Clubid);
        buffer.WriteVarInt(Account.Premium);
        buffer.WriteVarInt(Account.Gems);
        buffer.WriteVarInt(Account.Coins);
        buffer.WriteBool(Account.Muted);
        if(Account.Muted)
        {
            int second = (int)Math.Max(0, (Account.MutedEndTime - DateTime.UtcNow).TotalSeconds);
            buffer.WriteVarInt(second);
        }
        buffer.WriteVarInt(Account.Roles.Count);
        foreach (var role in Account.Roles)
        {
            buffer.WriteVarString(role.ToString());
        }
        #endregion

        #region  Notfication Settings
        // settings data (1 byte for flags)
        byte settingFlags = 0;
        if (Account.SendOnlineBestFriendNotification) settingFlags |= 1 << 0;
        if (Account.SendNewEventNotification) settingFlags |= 1 << 1;
        if (Account.SendInviteNotification) settingFlags |= 1 << 2;
        if (Account.SendClaimRewardNotification) settingFlags |= 1 << 3;

        buffer.WriteByte(settingFlags);

        #endregion

        // --- Club Data ---
        #region  Club
        if (Club == null)
        {
            buffer.WriteVarInt(0); // ClubId
        }
        else
        {
            buffer.WriteVarInt(Club.ClubId);
            buffer.WriteVarString(Club.ClubName);
            buffer.WriteVarString(Club.Clubaciklama);
            buffer.WriteVarInt(Club.TotalKupa ?? 0);
            buffer.WriteVarInt(Club.Members.Count);
            buffer.WriteVarInt(Club.Messages.Count);

            lock (Club.SyncLock)
            {
                foreach (var message in Club.Messages)
                {
                    buffer.WriteVarInt((int)message.messageFlags);
                    switch ((ClubMessageFlags)message.messageFlags)
                    {
                        case ClubMessageFlags.None:
                            buffer.WriteVarInt((int)message.MessageId);
                            buffer.WriteVarInt(message.SenderId);
                            buffer.WriteVarString(message.SenderName);
                            buffer.WriteVarInt(message.SenderAvatarID);
                            buffer.WriteVarInt((int)Account.clubRole);
                            buffer.WriteVarString(message.Content);
                            break;
                        case ClubMessageFlags.HasSystem:
                            buffer.WriteVarInt((int)message.eventType);
                            buffer.WriteVarString(message.ActorName);
                            buffer.WriteVarInt(message.ActorID);
                            break;
                        case ClubMessageFlags.HasTarget:
                            buffer.WriteVarInt((int)message.eventType);
                            buffer.WriteVarString(message.ActorName);
                            buffer.WriteVarInt(message.ActorID);
                            buffer.WriteVarString(message.TargetName);
                            break;
                    }
                }
            }
            buffer.WriteVarInt(Club.Members.Count);
        }

        foreach (var member in (Club?.Members ?? new List<ClubMember>()))
        {
            buffer.WriteVarInt(member.ID); // Sayısal ID
            buffer.WriteVarString(member.AccountName);
            buffer.WriteVarString(member.Role.ToString());
            buffer.WriteVarInt(member.NameColorID);
            buffer.WriteVarInt(member.AvatarID);
            buffer.WriteBool(SessionManager.IsOnline(member.ID));
        }

        // --- Random Clubs (10) ---
        buffer.WriteVarInt(RandomClubs.Count);
        foreach (var rclub in RandomClubs)
        {
            buffer.WriteVarInt(rclub.ClubId);
            buffer.WriteVarString(rclub.ClubName);
            buffer.WriteVarString(rclub.Clubaciklama);
            buffer.WriteVarInt(rclub.TotalKupa ?? 0);
            buffer.WriteVarInt(rclub.Members.Count);
        }
        #endregion

        #region  Friends
        // --- Friends ---
        buffer.WriteVarInt(Account.Friends.Count);
        foreach (var friend in Account.Friends)
        {
            buffer.WriteVarInt(friend.ID);
            buffer.WriteVarInt(friend.AvatarId);
            buffer.WriteVarString(friend.Username);
            buffer.WriteVarInt(friend.NameColorID);
            buffer.WriteBool(friend.IsBestFriend);
            buffer.WriteVarInt(friend.Trophy);
            buffer.WriteBool(SessionManager.IsOnline(friend.ID));
        }

        lock (Account.SyncLock)
        {
            buffer.WriteVarInt(Account.Requests.Count);
            foreach (var request in Account.Requests)
            {
                buffer.WriteVarInt(request.ID); // Sayısal ID
                buffer.WriteVarInt(request.AvatarId);
                buffer.WriteVarString(request.Username);
            }
        #endregion

            #region  Quest
            // --- Quests ---
            buffer.WriteVarLong(NextQuestRefreshTime);
            buffer.WriteVarLong(NextSeasonalQuestRefreshTime);

            buffer.WriteVarInt(Account.Quests.Count);
            foreach (var quest in Account.Quests)
            {
                buffer.WriteVarInt((byte)quest.ID);
                buffer.WriteVarInt((byte)quest.Type);
                buffer.WriteVarInt((byte)quest.Target);
                buffer.WriteVarInt(quest.CurrentGoal);
                buffer.WriteVarInt((byte)quest.RewardType);
                buffer.WriteVarInt(quest.Goal);
                buffer.WriteBool(quest.IsDailyQuest);
                buffer.WriteBool(quest.IsPremium);
                buffer.WriteBool(quest.IsCompleted);
            }
            #endregion
            
            
            // --- ULTRA-OPTIMIZED DYNAMIC CONFIG (CİMRİ MODE) ---
            // Sadece gerekli veriler (ID, Değer, Kalan Saniye) gönderilir.
            
            var dynamicConfig = DynamicConfigManager.Config;
            
            // 1. Aktif Etkinlikler
           /* buffer.WriteVarInt(dynamicConfig.ActiveEvents.Count);
            foreach ( var evt in dynamicConfig.ActiveEvents)
            {
                buffer.WriteVarInt((int)evt.Type);    // type                
                // Kalan saniye (Negatif olmamalı)
                
                int remainingSeconds = (int)Math.Max(0, (evt.EndTime - DateTime.UtcNow).TotalSeconds);
                buffer.WriteVarInt(remainingSeconds);
                if(evt.Type ==  EventType.XPMultiplier ||evt.Type ==  EventType.DoubleTrophy) // 2. bir değişkene ihtiyac varmı?!
                {
                    buffer.WriteVarInt(evt.Value);

                }
            }*/
            
            // 2. Sistem Flagleri (Toggles)
            byte systemFlags = 0;
            if (dynamicConfig.IsMatchmakingEnabled) systemFlags |= 1 << 0;
            if (dynamicConfig.IsShopEnabled) systemFlags |= 1 << 1;
            if (dynamicConfig.IsRankSystemEnabled) systemFlags |= 1 << 2;
            
            buffer.WriteByte(systemFlags); // 1 byte
            
            // 3. Custom Errors (Kalıcı Teknik Uyarılar)
            buffer.WriteVarInt(dynamicConfig.CustomErrors.Count);
            foreach(var ce in dynamicConfig.CustomErrors)
            {
                buffer.WriteVarString(ce.Title);
                buffer.WriteVarString(ce.Message);
            }
    
        }
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
