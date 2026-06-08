[PacketHandler(MessageType.InvitePlayerTeamRequest)]
public static class TeamInviteHandler
{
    public static void Handle(Session session, byte[] data)
    {
        int accid;
        using (ByteBuffer read = ByteBufferPool.Get())
        {
            read.WriteBytes(data);
            accid = read.ReadVarInt();
        }
        var targetacccount = AccountCache.Load(accid);
        if (targetacccount == null) return; // todo messagecode (aslında gerek varmı?)


        //todo: oyuncu takım davetlerini kabul ediyormu?
        if (session.Account == null) return;
        var acccount = session.Account;
        if (session.TeamID == 0) CreateTeamHandler.Handle(session);


        Lobby lobby = LobbyManager.GetLobby(session.TeamID);

        var responsepacket = new InvitePlayerToTeamResponsePacket();
        if (!SessionManager.IsOnline(targetacccount.ID))
        {
            responsepacket.Sended = false;
            responsepacket.ErrorCode = TeamErrorCode.TargetOffline;
            responsepacket.Serialize(ByteBufferPool.Get());
            session.Send(responsepacket);
            return;
        }
        else if (lobby.Players.Count == lobby.MaxPlayers)
        {
            responsepacket.Sended = false;
            responsepacket.ErrorCode = TeamErrorCode.TeamFull;
            responsepacket.Serialize(ByteBufferPool.Get());
            session.Send(responsepacket);
            return;
        }
        else
        {
            responsepacket.Sended = false;
           
            responsepacket.Serialize(ByteBufferPool.Get());
            session.Send(responsepacket);


            if (SessionManager.IsOnline(targetacccount.ID))
            {
                Session? targetsession = SessionManager.GetSession(targetacccount.ID);


                var notificationPacket = new TeamInviteNotificationPacket
                {
                     TeamID = lobby.ID,
                    SenderName = acccount.Username,
                    SenderId = acccount.ID,
                    SenderAvatarId = acccount.Avatarid,
                    SenderTrophy = acccount.Trophy,
                    CurrentPlayers = lobby.Players.Count,
                    MaxPlayers = lobby.MaxPlayers
                };
                targetsession?.Send(notificationPacket);
            }
            else
            {// todo if notfi almak istemiyorsa
                if (NotificationPolicyManager.CanSendNotification(targetacccount, NotificationPolicyManager.NotificationType.Invite))
                {
                    AndroidNotficationManager.SendNotification($"Davet!", $"{targetacccount.Username} sizi  takıma davet etti!", targetacccount.FBNToken);
                    NotificationPolicyManager.UpdateCooldown(targetacccount, NotificationPolicyManager.NotificationType.Invite);
                }
            }

        }
    }
    public static void ResponseHandle(Session session, byte[] responsedata)
    {
        bool Accept = false;
        int teamid = 0;

        using (ByteBuffer read = ByteBufferPool.Get())
        {
            read.WriteBytes(responsedata);

            var response = new TeamInviteResponsePacket();
            response.Deserialize(read);

            teamid = response.TeamId;
            Accept = response.Accept;
        }


        if (Accept)
        {

            Lobby lobby = LobbyManager.GetLobby(teamid);
            if (lobby == null)
            {
                // todo messagecode (takım bulunamadı)
                return;
            }
            if (lobby.RequestedPlayerIds.Contains(session.Account.ID))
            {
                
                ByteBuffer fakeBuffer = ByteBufferPool.Get();
                fakeBuffer.WriteVarInt(teamid);
                JoinTeamHandler.Handle(session, fakeBuffer.ToArray());
                fakeBuffer.Dispose();
                lock (lobby.SyncLock)
                {
                    lobby.RequestedPlayerIds.Remove(session.Account.ID);
                }
            }
        }

        else return; // todo messagecode
    }
}
