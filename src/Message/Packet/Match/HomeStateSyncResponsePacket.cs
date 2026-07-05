using System;
using System.Collections.Generic;
using Logic;

public class HomeStateSyncResponsePacket : IPacket
{
    public AccountData? Account { get; set; }
    public Club? Club { get; set; }
    public List<Club> RandomClubs { get; set; } = new List<Club>();
    public List<OnlinePlayerData> OnlinePlayers { get; set; } = new List<OnlinePlayerData>();
    public List<Quest> Quests { get; set; } = new List<Quest>();
    public long NextQuestRefreshTime { get; set; }
    public long NextSeasonalQuestRefreshTime { get; set; }

    public void Serialize(ByteBuffer buffer)
    {
        var account = Account ?? throw new InvalidOperationException("Home sync response account missing.");

        buffer.WriteVarInt((int)MessageType.HomeStateSyncResponse);
        WriteAccountSection(buffer, account);
        WriteClubSection(buffer, account);
        WriteFriendsSection(buffer, account);
        WriteQuestSection(buffer);
        WriteOnlinePlayersSection(buffer);
        WriteDynamicConfigSection(buffer);
    }

    private void WriteAccountSection(ByteBuffer buffer, AccountData account)
    {
        buffer.WriteVarInt(account.ID);
        buffer.WriteVarString(account.Username ?? "");
        buffer.WriteVarInt(account.Avatarid);
        buffer.WriteVarInt(account.Namecolorid);
        buffer.WriteVarInt(account.Trophy);
        buffer.WriteVarInt(account.Level);
        buffer.WriteVarInt(account.Experience);
        buffer.WriteVarInt(account.Clubid);
        buffer.WriteVarInt(account.Premium);
        buffer.WriteVarInt(account.Gems);
        buffer.WriteVarInt(account.Coins);
        buffer.WriteBool(account.Muted);

        if (account.Muted)
        {
            int mutedSeconds = (int)Math.Max(0, (account.MutedEndTime - DateTime.UtcNow).TotalSeconds);
            buffer.WriteVarInt(mutedSeconds);
        }

        buffer.WriteVarInt(account.Roles.Count);
        foreach (var role in account.Roles)
        {
            buffer.WriteVarString(role.ToString());
        }

        byte notificationFlags = 0;
        if (account.SendOnlineBestFriendNotification) notificationFlags |= 1 << 0;
        if (account.SendNewEventNotification) notificationFlags |= 1 << 1;
        if (account.SendInviteNotification) notificationFlags |= 1 << 2;
        if (account.SendClaimRewardNotification) notificationFlags |= 1 << 3;
        buffer.WriteByte(notificationFlags);

        byte socialFlags = 0;
        if (account.MuteTeamInvites) socialFlags |= 1 << 0;
        if (account.LookingForTeam) socialFlags |= 1 << 1;
        if (account.DoNotDisturb) socialFlags |= 1 << 2;
        buffer.WriteByte(socialFlags);

        int remainingMuteTeamInviteSeconds = (int)Math.Max(0, (account.MuteTeamInviteEndTime.ToUniversalTime() - DateTime.UtcNow).TotalSeconds);
        buffer.WriteVarInt(remainingMuteTeamInviteSeconds);
    }

    private void WriteClubSection(ByteBuffer buffer, AccountData account)
    {
        if (Club == null)
        {
            buffer.WriteVarInt(0);
            return;
        }

        buffer.WriteVarInt(Club.ID);
        buffer.WriteVarInt(Club.AvatarID);
        buffer.WriteVarString(Club.Name ?? "");
        buffer.WriteVarString(Club.Description ?? "");
        buffer.WriteVarInt(Club.TotalTrophy);
        buffer.WriteVarInt((int)Club.State);
        buffer.WriteVarString(Club.Region ?? "");
        buffer.WriteVarInt((int)account.clubRole);

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
                        buffer.WriteVarString(message.SenderName ?? "");
                        buffer.WriteVarInt(message.SenderAvatarID);
                        buffer.WriteVarInt((int)account.clubRole);
                        buffer.WriteVarString(message.Content ?? "");
                        break;
                    case ClubMessageFlags.HasSystem:
                        buffer.WriteVarInt((int)message.eventType);
                        buffer.WriteVarString(message.ActorName ?? "");
                        buffer.WriteVarInt(message.ActorID);
                        break;
                    case ClubMessageFlags.HasTarget:
                        buffer.WriteVarInt((int)message.eventType);
                        buffer.WriteVarString(message.ActorName ?? "");
                        buffer.WriteVarString(message.TargetName ?? "");
                        buffer.WriteVarInt(message.TargetID);
                        break;
                    case ClubMessageFlags.Request:
                        buffer.WriteVarInt(message.MessageId);
                        buffer.WriteVarInt(message.ActorID);
                        buffer.WriteVarString(message.ActorName ?? "");
                        buffer.WriteVarString(message.Content ?? "");
                        buffer.WriteVarInt(message.SenderAvatarID);
                        buffer.WriteVarInt((int)message.RequestState);
                        break;
                }
            }
        }

        foreach (var member in Club.Members)
        {
            buffer.WriteVarInt(member.ID);
            buffer.WriteVarString(member.AccountName ?? "");
            buffer.WriteVarInt((int)member.Role);
            buffer.WriteVarInt(member.NameColorID);
            buffer.WriteVarInt(member.AvatarID);
            buffer.WriteBool(SessionManager.IsOnline(member.ID));
        }

        buffer.WriteVarInt(RandomClubs.Count);
        foreach (var randomClub in RandomClubs)
        {
            buffer.WriteVarInt(randomClub.ID);
            buffer.WriteVarString(randomClub.Name ?? "");
            buffer.WriteVarString(randomClub.Description ?? "");
            buffer.WriteVarInt(randomClub.TotalTrophy);
            buffer.WriteVarInt(randomClub.Members.Count);
            buffer.WriteVarInt(randomClub.AvatarID);
            buffer.WriteVarInt((int)randomClub.State);
            buffer.WriteVarString(randomClub.Region ?? "");
        }
    }

    private void WriteFriendsSection(ByteBuffer buffer, AccountData account)
    {
        buffer.WriteVarInt(account.Friends.Count);
        foreach (var friend in account.Friends)
        {
            buffer.WriteVarInt(friend.ID);
            buffer.WriteVarInt(friend.AvatarId);
            buffer.WriteVarString(friend.Username ?? "");
            buffer.WriteVarInt(friend.NameColorID);
            buffer.WriteBool(friend.IsBestFriend);
            buffer.WriteVarInt(friend.Trophy);
            buffer.WriteBool(SessionManager.IsOnline(friend.ID));
        }

        lock (account.SyncLock)
        {
            buffer.WriteVarInt(account.Requests.Count);
            foreach (var request in account.Requests)
            {
                buffer.WriteVarInt(request.ID);
                buffer.WriteVarInt(request.AvatarId);
                buffer.WriteVarString(request.Username ?? "");
            }
        }
    }

    private void WriteQuestSection(ByteBuffer buffer)
    {
        buffer.WriteVarLong(NextQuestRefreshTime);
        buffer.WriteVarLong(NextSeasonalQuestRefreshTime);

        buffer.WriteVarInt(Quests.Count);
        foreach (var quest in Quests)
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
    }

    private void WriteOnlinePlayersSection(ByteBuffer buffer)
    {
        buffer.WriteVarInt(OnlinePlayers.Count);
        foreach (var player in OnlinePlayers)
        {
            buffer.WriteVarInt(player.ID);
            buffer.WriteVarString(player.Username ?? "");
            buffer.WriteVarInt(player.AvatarId);
            buffer.WriteVarInt(player.NameColorID);
            buffer.WriteVarInt(player.Trophy);
            buffer.WriteBool(player.LookingForTeam);
            buffer.WriteBool(player.DisturbMode);
            buffer.WriteBool(player.IsFriend);
            buffer.WriteBool(player.IsClubMember);
        }
    }

    private void WriteDynamicConfigSection(ByteBuffer buffer)
    {
        var dynamicConfig = DynamicConfigManager.Config;

        byte systemFlags = 0;
        if (dynamicConfig.IsMatchmakingEnabled) systemFlags |= 1 << 0;
        if (dynamicConfig.IsShopEnabled) systemFlags |= 1 << 1;
        if (dynamicConfig.IsRankSystemEnabled) systemFlags |= 1 << 2;
        buffer.WriteByte(systemFlags);

        buffer.WriteVarInt(dynamicConfig.CustomErrors.Count);
        foreach (var customError in dynamicConfig.CustomErrors)
        {
            buffer.WriteVarString(customError.Title ?? "");
            buffer.WriteVarString(customError.Message ?? "");
        }
    }

    public void Deserialize(ByteBuffer buffer)
    {
        throw new NotImplementedException();
    }
}
