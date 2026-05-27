using System.Reflection.Metadata;

[PacketHandler(MessageType.SendClubMessage)]
public static class ClubMessageHandler
{
    public static void Handle(Session session, byte[] message)
    {
        Console.WriteLine("club message handler iss run");
        ByteBuffer readbuffer = ByteBufferPool.Get();
        readbuffer.WriteBytes(message, true);

        var request = new SendClubMessageRequestPacket();
        request.Deserialize(readbuffer);
        
        int accountıd = session.Account.ID;
        string Message = request.Message;
        readbuffer.Dispose();


        AccountManager.AccountData account = AccountCache.Load(accountıd);
        var club = ClubManager.LoadClub(account.Clubid);
        if (club == null)
        {
            Console.WriteLine("club null");
            return;
        }
        if (session.Logic.IsMuted())
        {
          // todo toast
          //  session.Send(muteResponse);
          Console.WriteLine("oyuncu muteli");
            return;
        }

        if (Message.StartsWith("/"))
        {
            GameinCmd(session, account, Message);
            return;
        }

        Console.WriteLine($"{account.Username} adlı kullanıcı {club.Name ?? "PORNO"} adlı kulube {Message} mesajını gönderdi");
        
        ClubMessage clubMessage = new ClubMessage
        {
            messageFlags = ClubMessageFlags.None,
            SenderName = account.Username,
            SenderId = account.ID,
            SenderAvatarID = account.Avatarid,
            Content = Message,
            Timestamp = DateTime.Now
        };
        
        // Görev İlerlemesi - Mesaj Gönderme
        QuestManager.CheckQuestProgress(account, Quest.MissionType.SendChatMessage);

        club.SendMessageToClubMembers(clubMessage);
    }
     public static void GameinCmd(Session session, AccountManager.AccountData account, string message)
    {
        string EntryMessage = "";
         string[] cmd = message.Substring(1).Split(' ');
             //   if (cmd.Length == 0) return;
        switch (cmd[0])
        {
            case "help":
                EntryMessage = "Mevcut komutlar: /help, /clubid, /status, /firebase";
                break;
            case "ping":
                EntryMessage = "pong!";
                break;
            case "myid":
                EntryMessage = $" senin id:{account.ID}";
                break;
            case "clubid":
                EntryMessage = $" senin club id:{account.Clubid}";
                break;
            case "status":
                EntryMessage = $"Çevrimiçi oyuncu sayısı: {SessionManager.GetCount}\n Server sürümü: {Config.Instance.ServerVersion}\n"; // todo....
                break;
            case "firebase":
            EntryMessage = $"token: {account.FBNToken ?? "null..."}";
                break;
            default:
                EntryMessage = "komut bulunamadı... yardım için /help komutunu kullanın";
                break;
        }


        var response = new GetClubMessagePacket
        {
            Message = new ClubMessage
            {
               messageFlags = ClubMessageFlags.None,
               SenderId = account.ID+1,
               SenderName = "SİSTEM",
               SenderAvatarID = account.Avatarid,
               Content = EntryMessage,
               // MessageId and Timestamp might need defaults if not crucial for system message display
               MessageId = 0, 
               Timestamp = DateTime.Now
            }
        };
        session.Send(response);

           
    }
}
