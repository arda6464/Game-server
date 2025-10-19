public static class ClubEditHandler
{
    public static void Handle(Session session,byte[] message)
    {
        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(message, true);
        _ = read.ReadInt();

        string ClubName = read.ReadString();
        string ClubAciklama = read.ReadString();
        int Avatarıd = read.ReadInt();

        AccountManager.AccountData account = AccountCache.Load(session.AccountId);
        if (account.Clubid == -1)
        {
            // TODO NAME CONTROL?!
            if (!string.IsNullOrWhiteSpace(ClubName) || string.IsNullOrWhiteSpace(ClubAciklama))
                ClubManager.CreateClub(ClubName, ClubAciklama, Avatarıd, account.AccountId);
        }
        else
        {
            var club = ClubManager.LoadClub(account.Clubid);
            {
                bool change = ClubManager.ChangeClubSettings(account.Clubid, account.AccountId, ClubName, ClubAciklama, Avatarıd);
                
                if(change)
                {
                    
                    foreach(var clubmember in club.Members)
                    {
                        if(SessionManager.IsOnline(clubmember.Accountid))
                        {
                            Session membersession = SessionManager.GetSession(clubmember.Accountid);
                            using (var buffer = new ByteBuffer())

                           {
                                           buffer.WriteInt((int)MessageType.ClubEditResponse);
 
                            buffer.WriteString(club.ClubName);
                            buffer.WriteString(club.Clubaciklama);
                            buffer.WriteInt(club.ClubAvatarID);
                            buffer.WriteString(account.AccountId);
                            membersession.Send(buffer.ToArray());
                        }
                           
                            // todo change accountıd to client -> if(accountid == datamanager.playerıd) toast("change club data")
                            }                 
                          
                           
                    }
                   

                    
                }
            }
        }
        
    }
}