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
        var Club = ClubManager.LoadClub(account.Clubid);
        bool Kicked = false;

        if (Club != null)
        {
            Kicked = ClubManager.RemoveMember(Club.ClubId, account.AccountId);
        }
      
        session.Send(new LeaveClubResponsePacket { Kicked = Kicked });
        account.Clubid = -1;
        account.ClubName = null;


          ClubMessage messages = new ClubMessage
          {
              messageFlags = ClubMessageFlags.HasSystem,
              eventType = ClubEventType.LeaveMessage,
              ActorName = account.Username,
             ActorID = account.AccountId,
             MessageId = 0
            };
        Club.Messages.Add(messages);
       
        var broadcastPacket = new GetClubMessagePacket { Message = messages };
        
             foreach(var member in Club.Members)
            {
                if(SessionManager.IsOnline(member.Accountid))
                {
                    Session session1 = SessionManager.GetSession(member.Accountid);
                    session1.Send(broadcastPacket);
                }
            }
       

       
        
    }
}