using System;

[PacketHandler(MessageType.JoinByInviteRequest)]
public class InviteHandler : IGameMessage
{
    public void Handle(Session session, byte[]? data)
    {
        if (session.Account == null)
            return;

        var packet = new JoinByInviteRequestPacket();
        using (ByteBuffer reader = ByteBufferPool.Get())
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
            using (ByteBuffer fakeBuffer = ByteBufferPool.Get())
            {
                // MessageManager artık ID'yi atladığı için buraya ID eklemiyoruz
                fakeBuffer.WriteVarInt(joinPacket.TeamId);
                new JoinTeamHandler().Handle(session, fakeBuffer.ToArray());
            }
        }
        else if (invite.Type == InviteType.Friend)
        {
            int targetAccountId = invite.TargetID;
            if (targetAccountId == session.Account.ID)
                return;

            var target = AccountCache.Load(targetAccountId);
            if (target != null)
            {
                FriendsManager.SendRequest(session.Account, target);
                Logger.genellog(
                    $"[Invite] {session.Account.Username} davet linkiyle {target.Username}'ye arkadaşlık isteği gönderdi."
                );
            }
        }
    }
}
