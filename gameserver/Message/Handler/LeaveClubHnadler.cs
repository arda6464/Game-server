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
        if (Kicked) account.Clubid = -1;

       
        
    }
}