[PacketHandler(MessageType.ClubCreateRequest)]
public static class ClubCreateHandler
{
    public static void Handle(Session session, byte[] messsage)
    {
        try

        {

            ByteBuffer read = ByteBufferPool.Get();
            read.WriteBytes(messsage, true);

            var request = new ClubCreateRequestPacket();
            request.Deserialize(read);
            
            string? ClubName = request.ClubName;
            string? ClubAciklama = request.ClubDescription;
            int Avatarıd = request.AvatarId;
            int State = request.State;
            string? Region = request.Region;

            if (session.Account == null) return;
            AccountManager.AccountData account = session.Account;
            if (account.Clubid == 0)
            {
                // İsim validasyonu
                if (string.IsNullOrWhiteSpace(ClubName) || ClubName.Length < 3 || ClubName.Length > 30)
                {
                    MessageCodeManager.Send(session, MessageCodeManager.Message.ClubUnusableName);
                    Logger.errorslog($"[ClubEditHandler] Geçersiz kulüp adı: {ClubName}");
                    return;
                }

                if (ClubAciklama?.Length > 200)
                {
                    MessageCodeManager.Send(session, MessageCodeManager.Message.ClubUnusableDescription);
                    Logger.errorslog($"[ClubEditHandler] Geçersiz kulüp açıklaması");
                    return;
                }

                // Avatar ID validasyonu
                if (Avatarıd < 1 || Avatarıd > 26)
                {
                    MessageCodeManager.Send(session, MessageCodeManager.Message.InvalidAvatar);
                    Logger.errorslog($"[ClubEditHandler] Geçersiz avatar ID: {Avatarıd}");
                    return;
                }

                var club = ClubManager.CreateClub(ClubName, ClubAciklama, Avatarıd, account.ID, State, Region);

                var response = new ClubCreateResponsePacket
                {
                    ClubId = club.ID,
                    ClubName = club.Name,
                    ClubDescription = club.Description,
                    TotalTrophies = club.TotalTrophy,
                };
                response.Messages.AddRange(club.Messages ?? new List<ClubMessage>());
                response.Members.AddRange(club.Members ?? new List<ClubMember>());
                
                session.Send(response);
                Console.WriteLine("create club data Gönderildi");

            }
            else
            {
                MessageCodeManager.Send(session, MessageCodeManager.Message.AlreadyInClub);
                Logger.errorslog($"[ClubEditHandler] oyuncu zaten bir kulüpte");
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine("club create hata: " + ex.Message);
        }
    }
    
}
