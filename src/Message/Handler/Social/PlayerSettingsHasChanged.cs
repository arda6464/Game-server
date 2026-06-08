[PacketHandler(MessageType.SocialSettingsHasChanged)]
public static class SocialSettingsHasChanged
{
    public static void Handle(Session session, byte[] data)
    {
        AccountManager.AccountData account = session.Account;
        if (account == null) return;
        if (account.Clubid == 0) return;

        var request = new SocialSettingsHasChangedPacket();
        using (ByteBuffer buffer = ByteBufferPool.Get())
        {
            buffer.WriteBytes(data, true);
            request.Deserialize(buffer);
            buffer.Dispose();
        }

        account.LookingForTeam = request.LookingForTeam;
        account.MuteTeamInvites = request.MuteTeamInvites;
        if (request.MuteTeamInviteEndTime > 0)
            account.MuteTeamInviteEndTime = DateTime.UtcNow.AddSeconds(request.MuteTeamInviteEndTime);
        else
            account.MuteTeamInviteEndTime = DateTime.MaxValue; // 0 = her zaman aktif / limit yok anlamında
        account.DoNotDisturb = request.DoNotDisturb;

        request.Success = true;
        request.Account = account;
        using (ByteBuffer buffer = ByteBufferPool.Get())
        {
            request.Serialize(buffer);
            buffer.Dispose();
        }
        session.Send(request);

        var updatePacket = new OnlinePlayerstateHasChangedPacket
        {
            PlayerId = account.ID,
            LookingForTeam = account.LookingForTeam,
            DisturbMode = account.DoNotDisturb
        };
        foreach (var friend in account.Friends)
        {
            if (SessionManager.IsOnline(friend.ID))
            {
                Session? friendSession = SessionManager.GetSession(friend.ID);
                if (friendSession != null)
                {
                    friendSession.Send(updatePacket);
                }
            }
        }
        Club club = ClubManager.LoadClub(account.Clubid);
        if (club?.Members == null) return;

        foreach (var clubMember in club.Members)
        {
            if (SessionManager.IsOnline(clubMember.ID))
            {
                Session? memberSession = SessionManager.GetSession(clubMember.ID);
                if (memberSession != null)
                {
                    memberSession.Send(updatePacket);
                }
            }
        }












    }
}
