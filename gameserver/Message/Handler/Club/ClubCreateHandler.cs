public static class ClubCreateHandler
{
    public static void Handle(Session session, byte[] messsage)
    {
        try

        {

            ByteBuffer read = new ByteBuffer();
            read.WriteBytes(messsage, true);
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

                var club = ClubManager.CreateClub(ClubName, ClubAciklama, Avatarıd, account.AccountId);

                using (var buffer = new ByteBuffer())

                {
                    buffer.WriteInt((int)MessageType.ClubCreateResponse);
                    buffer.WriteInt(club.ClubId);
                    buffer.WriteString(club.ClubName);
                    buffer.WriteString(club.Clubaciklama);
                    buffer.WriteInt(club.TotalKupa ?? 0);
                    buffer.WriteInt(club.Messages.Count);
                    foreach (var message in (club?.Messages ?? new List<ClubMessage>()))
                    {
                        buffer.WriteString(message.SenderId);
                        buffer.WriteString(message.SenderName);
                        buffer.WriteInt(message.SenderAvatarID);
                        buffer.WriteString("Üye"); // todo enum send
                        buffer.WriteString(message.Content);
                    }

                    buffer.WriteInt(club.Members.Count);


                    foreach (var member in (club?.Members ?? new List<ClubMember>()))
                    {

                        buffer.WriteString(member.Accountid);
                        buffer.WriteString(member.AccountName);
                        buffer.WriteString(member.Role.ToString());
                        buffer.WriteInt(member.NameColorID);
                        buffer.WriteInt(member.AvatarID);

                    }
                    byte[] response = buffer.ToArray();
                    buffer.Dispose();
                    session.Send(response);
                    Console.WriteLine("create club data Gönderildi");
                }

            }
            else
            {
                MessageCodeManager.Send(session, MessageCodeManager.Message.AlreadyİnClub);
                Logger.errorslog($"[ClubEditHandler] oyuncu zaten bir kulüpte");
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine("club create hata: " + ex.Message);
        }
    }
    
}
