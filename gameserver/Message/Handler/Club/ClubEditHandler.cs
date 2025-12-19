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
            // İsim validasyonu
            if (string.IsNullOrWhiteSpace(ClubName) || ClubName.Length < 3 || ClubName.Length > 30)
            {
                MessageCodeManager.Send(session, MessageCodeManager.Message.ClubUnusableName);
                Logger.errorslog($"[ClubEditHandler] Geçersiz kulüp adı: {ClubName}");
                return;
            }
            
            if (string.IsNullOrWhiteSpace(ClubAciklama) || ClubAciklama.Length > 200)
            {
                MessageCodeManager.Send(session, MessageCodeManager.Message.ClubUnusableDescription);
                Logger.errorslog($"[ClubEditHandler] Geçersiz kulüp açıklaması");
                return;
            }
            
            // Avatar ID validasyonu
            if (Avatarıd < 1 || Avatarıd > 10)
            {
                MessageCodeManager.Send(session, MessageCodeManager.Message.İnvalidAvatar);
                Logger.errorslog($"[ClubEditHandler] Geçersiz avatar ID: {Avatarıd}");
                return;
            }
            
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