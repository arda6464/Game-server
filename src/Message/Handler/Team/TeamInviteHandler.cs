[PacketHandler(MessageType.InviteToTeamRequest)]
public static class TeamInviteHandler
{
    public static void Handle(Session session, byte[] data)
    {
        int accid;
        using (ByteBuffer read = new ByteBuffer())
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


        if (!SessionManager.IsOnline(targetacccount.ID))
        {
            // todo playeroflinemessage.....
            return;
        }
        if (lobby.Players.Count == lobby.MaxPlayers)
        {
            // todo teamfullmessage.........
            return;
        }
        else
        {


            if (SessionManager.IsOnline(targetacccount.ID))
            {
                Session? targetsession = SessionManager.GetSession(targetacccount.ID);


                    var notificationPacket = new TeamInviteNotificationPacket
                    {
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
    public static void ResponseHandle(Session session,byte[] responsedata)
    {
        bool Accept = false;
        int targetacc;
        
        using (ByteBuffer read = new ByteBuffer())
        {
            read.WriteBytes(responsedata);
            
            var response = new TeamInviteResponsePacket();
            response.Deserialize(read);
            
            targetacc = response.InviterId;
            Accept = response.Accept;
        }

        if (!SessionManager.IsOnline(targetacc))
            return;
       Session Invitersession = SessionManager.GetSession(targetacc);
        if (Accept)
        {
            // Replicating logic using packets where possible, or keeping it compatible.
            // Original code creates a ByteBuffer simulating JoinTeamRequest.
            // JoinTeamRequestPacket expects { TeamId }.
            
            var joinPacket = new JoinTeamRequestPacket { TeamId = Invitersession.TeamID };
            
            using (ByteBuffer fakebuffer = new ByteBuffer())
            {
                // MessageManager artık ID'yi atladığı için buraya ID eklemiyoruz
                fakebuffer.WriteVarInt(joinPacket.TeamId);
                byte[] fakebyte = fakebuffer.ToArray();
                JoinTeamHandler.Handle(session, fakebyte);
            }
        }
        else return; // todo messagecode
    }
}
