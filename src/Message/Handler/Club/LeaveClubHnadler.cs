public static class LeaveClubHandler
{
    public static void Handle(Session session, byte[] message)
    {
        AccountManager.AccountData account = AccountCache.Load(session.AccountId);
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
      
            ByteBuffer buffer = new ByteBuffer();
            buffer.WriteInt((int)MessageType.LeaveClubResponse);
        buffer.WriteBool(Kicked);
        byte[] veri = buffer.ToArray();
        buffer.Dispose();
        session.Send(veri);
        account.Clubid = -1;
        account.ClubName = null;


         ByteBuffer buffer1 = new ByteBuffer();
            buffer1.WriteInt((int)MessageType.GetClubMessage);

              ClubMessage messages = new ClubMessage
              {
                  messageFlags = ClubMessageFlags.HasSystem,
                  eventType = ClubEventType.LeaveMessage,
                  ActorName = account.Username,
                 ActorID = account.AccountId
                };
        Club.Messages.Add(messages);
       
                buffer1.WriteInt((int)MessageType.GetClubMessage);
                buffer1.WriteByte((byte)ClubMessageFlags.HasSystem);
                buffer1.WriteInt((int)ClubEventType.LeaveMessage);
            buffer1.WriteString(messages.ActorName);
            buffer1.WriteString(messages.ActorID);
            byte[] response = buffer1.ToArray();
            buffer1.Dispose();
             foreach(var member in Club.Members)
            {
                if(SessionManager.IsOnline(member.Accountid))
                {
                    Session session1 = SessionManager.GetSession(member.Accountid);
                    session.Send(response);
                }
            }
       

       
        
    }
}