using System;
using System.Linq;
[PacketHandler(MessageType.JoinByInviteRequest)]
public static class JoinByInviteHandler
{
    public static void Handle(Session session, byte[] data)
    {
        if (session.Account == null) return;

        var packet = new JoinByInviteRequestPacket();
        using (ByteBuffer reader = new ByteBuffer())
        {
            reader.WriteBytes(data);
            packet.Deserialize(reader);
        }

        var invite = InviteManager.GetInvite(packet.Token);
        if (invite == null)
        {
            // todo send error message (invalid or expired)
            return;
        }

        if (invite.Type == InviteType.Team)
        {
            int teamId = invite.TargetID;
            // Re-use JoinTeam logic
            var joinPacket = new JoinTeamRequestPacket { TeamId = teamId };
            using (ByteBuffer fakeBuffer = new ByteBuffer())
            {
                // MessageManager artık ID'yi atladığı için buraya ID eklemiyoruz
                fakeBuffer.WriteVarInt(joinPacket.TeamId);
                JoinTeamHandler.Handle(session, fakeBuffer.ToArray());
            }
        }
        else if (invite.Type == InviteType.Friend)
        {
            int targetAccountId = invite.TargetID;
            if (targetAccountId == session.Account.ID) return;

            AccountManager.AccountData account = session.Account;
            AccountManager.AccountData target = AccountCache.Load(targetAccountId);

            if (account != null && target != null)
            {
                lock (account.SyncLock)
                {
                    if (account.Friends.Any(f => f.ID == targetAccountId)) return;
                }

                lock (target.SyncLock)
                {
                    if (target.Requests.Any(r => r.ID == account.ID)) return;

                    FriendInfo info = new FriendInfo
                    {
                        Username = account.Username,
                        ID = account.ID,
                        AvatarId = account.Avatarid,
                        NameColorID = account.Namecolorid
                    };
                    target.Requests.Add(info);

                    if (SessionManager.IsOnline(target.ID))
                    {
                        Session? targetSession = SessionManager.GetSession(target.ID);
                        targetSession?.Send(new FriendRequestAddedPacket { Request = info });
                    }
                }
                Logger.genellog($"[Invite] {account.Username} davet linkiyle {target.Username}'ye arkadaşlık isteği gönderdi.");
            }
        }
    }
}
