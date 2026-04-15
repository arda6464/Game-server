[PacketHandler(MessageType.LeaveClubRequest)]
public static class LeaveClubHandler
{
    public static void Handle(Session session, byte[] message)
    {
        AccountManager.AccountData account = session.Account;
        if (account == null)
        {
            Logger.errorslog("[LEAVE CLUB]Hesap bulunamadı");
            return;
        }

        var club = ClubManager.LoadClub(account.Clubid);
        bool result = false;

        if (club != null)
        {
            result = ClubManager.RemoveMember(club.ClubId, account.ID);
            
            ClubMessage leaveMessage = new ClubMessage
            {
                messageFlags = ClubMessageFlags.HasSystem,
                eventType = ClubEventType.LeaveMessage,
                ActorName = account.Username,
                ActorID = account.ID,
                MessageId = 0
            };
            
            lock (club.SyncLock)
            {
                club.Messages.Add(leaveMessage);
            }

            var broadcastPacket = new GetClubMessagePacket { Message = leaveMessage };
            foreach (var member in club.Members)
            {
                if (SessionManager.IsOnline(member.ID))
                {
                    Session targetSession = SessionManager.GetSession(member.ID);
                    targetSession?.Send(broadcastPacket);
                }
            }
        }
      
        session.Send(new LeaveClubResponsePacket { Kicked = result });
        account.Clubid = -1;
        account.ClubName = null;
    }
}