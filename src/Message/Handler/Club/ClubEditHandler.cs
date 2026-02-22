[PacketHandler(MessageType.ClubEditRequest)]
public static class ClubEditHandler
{
    public static void Handle(Session session,byte[] message)
    {
        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(message, true);
        _ = read.ReadShort();

        var request = new ClubEditRequestPacket();
        request.Deserialize(read);
        
        string ClubName = request.ClubName;
        string ClubAciklama = request.ClubDescription;
        int Avatarıd = request.AvatarId;

        if (session.Account == null) return;
        AccountManager.AccountData account = session.Account;

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
                    var response = new ClubEditResponsePacket
                    {
                        ClubName = club.ClubName,
                        ClubDescription = club.Clubaciklama,
                        ClubAvatarId = club.ClubAvatarID,
                        AccountId = account.AccountId
                    };
                    
                    foreach(var clubmember in club.Members)
                    {
                        if(SessionManager.IsOnline(clubmember.Accountid))
                        {
                            Session membersession = SessionManager.GetSession(clubmember.Accountid);
                            membersession.Send(response);
                        }
                           
                            // todo change accountıd to client -> if(accountid == datamanager.playerıd) toast("change club data")
                            }                 
                          
                           
                    }
                   

                    
                }
            }
        }
        
    }
