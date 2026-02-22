using System.Reflection.Metadata;

[PacketHandler(MessageType.SendClubMessage)]
public static class ClubMessageHandler
{
    public static void Handle(Session session, byte[] message)
    {
        Console.WriteLine("club message handler iss run");
        ByteBuffer readbuffer = new ByteBuffer();
        readbuffer.WriteBytes(message, true);
        int _ = readbuffer.ReadShort();

        var request = new SendClubMessageRequestPacket();
        request.Deserialize(readbuffer);
        
        string accountıd = request.AccountId;
        string Message = request.Message;
        readbuffer.Dispose();


        AccountManager.AccountData account = AccountCache.Load(accountıd);
        var club = ClubManager.LoadClub(account.Clubid);
        if (club == null)
        {
            Console.WriteLine("club null");
            return;
        }
        if (Message.StartsWith("/"))
        {
            GameinCmd(session, account, Message);
            return;
        }

        Console.WriteLine($"{account.Username} adlı kullanıcı {club.ClubName ?? "PORNO"} adlı kulube {Message} mesajını gönderdi");
        
        ClubMessage clubMessage;
        lock (club.SyncLock)
        {
            clubMessage = new ClubMessage
            {
                MessageId = club.MessageIdCounter++,
                messageFlags = ClubMessageFlags.None,
                SenderName = account.Username,
                SenderId = account.AccountId,
                SenderAvatarID = account.Avatarid,
                Content = Message,
                Timestamp = DateTime.Now
            };
            club.Messages.Add(clubMessage);
        }
        
        // Görev İlerlemesi - Mesaj Gönderme
        QuestManager.CheckQuestProgress(account, Quest.MissionType.SendChatMessage);

        var broadcastPacket = new GetClubMessagePacket
        {
            Message = clubMessage
        };
        
        lock (club.SyncLock)
        {
            foreach (var member in club.Members)
            {
                if (SessionManager.IsOnline(member.Accountid))
                {
                    Session membersesion = SessionManager.GetSession(member.Accountid);
                    membersesion.Send(broadcastPacket);
                }
            }
        }


    }
     public static void GameinCmd(Session session, AccountManager.AccountData account, string message)
    {
        string EntryMessage = "";
         string[] cmd = message.Substring(1).Split(' ');
             //   if (cmd.Length == 0) return;
        switch (cmd[0])
        {
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
               SenderId = account.AccountId,
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