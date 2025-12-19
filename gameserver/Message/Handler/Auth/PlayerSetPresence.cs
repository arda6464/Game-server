public static class PlayerSetPresence
{
    public enum PresenceScope : byte
    {
        Friend,
        Club,
        Chat
    }
    public enum PresenceState : byte
    {
        Offline,
        Online
    }
    public static void Handle(string acccountId, PresenceState presence)
    {

        var acccount = AccountCache.Load(acccountId);
         if (acccount == null) 
        {
            Logger.errorslog($"Account not found: {acccountId}");
            return;
        }



        if (acccount.Friends.Count != 0)
        {
            ByteBuffer bufer = new ByteBuffer();
            bufer.WriteInt((int)MessageType.Presence);
            bufer.WriteString(acccount.AccountId);
            bufer.WriteByte((byte)presence);
            bufer.WriteByte((byte)PresenceScope.Friend);
            byte[] friendresponse = bufer.ToArray();
            bufer.Dispose();

            foreach (var friend in acccount.Friends)
            {
                if (SessionManager.IsOnline(friend.Id))
                {
                    Session? session = SessionManager.GetSession(friend.Id);
                   
                        session.Send(friendresponse);
                    
                }
            }
        }
        
        if(acccount.Clubid != -1)
        {
            ByteBuffer bufer = new ByteBuffer();
            bufer.WriteInt((int)MessageType.Presence);
            bufer.WriteString(acccount.AccountId);
            bufer.WriteByte((byte)presence);
            bufer.WriteByte((byte)PresenceScope.Club);
            byte[] Clubresponse = bufer.ToArray();
            bufer.Dispose();
            var club = ClubCache.Load(acccount.Clubid);
            if(club == null)
            {
                Logger.errorslog($"[Presence]{acccount.Username}({acccount.AccountId}) adlı hesabın clubune erişilmedi");
                return;
            }
            foreach (var clubmember in club.Members)
            {
                if (clubmember.Accountid == acccount.AccountId) continue;
                if (SessionManager.IsOnline(clubmember.Accountid))
                {
                    Session? session = SessionManager.GetSession(clubmember.Accountid);
                       session.Send(Clubresponse);
                    
                }
            }

        }
     
    }
}