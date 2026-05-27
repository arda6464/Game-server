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
            result = club.RemoveMember(account.ID);
            
            ClubMessage leaveMessage = new ClubMessage
            {
                messageFlags = ClubMessageFlags.HasSystem,
                eventType = ClubEventType.LeaveMessage,
                ActorName = account.Username,
                ActorID = account.ID
            };
            club.SendMessageToClubMembers(leaveMessage);
        }
      
        session.Send(new LeaveClubResponsePacket { Kicked = result });
        account.Clubid = 0;
        account.ClubName = null;
    }
}