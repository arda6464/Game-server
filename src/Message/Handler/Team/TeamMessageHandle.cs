[PacketHandler(MessageType.SendTeamMessageRequest)]
public static class TeamMessageHandler
{
    public static void Handle(Session session, byte[] data)
    {
        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(data, true);
        
        var request = new SendTeamMessageRequestPacket();
        request.Deserialize(read);
        
        string Message = request.Message;
        read.Dispose();


        if (session.Account == null) return;
        var account = session.Account;
        Lobby lobby = LobbyManager.GetLobby(session.TeamID);
        if (lobby == null)
        {
            Console.WriteLine("lobby null");
            return;
        }
        if (Message.StartsWith("/"))
        {
            GameinCmd(session, account, Message);
            return;
        } 
        

        Console.WriteLine($"{account.Username} adlı kullanıcı {Message} mesajını gönderdi");
        TeamMessage teamMessage = new TeamMessage
        {
            messageFlags = TeamMessageFlags.None,
            MessageId = lobby.MessageIdCounter++,
            SenderName = account.Username,
            SenderId = account.ID,
            SenderAvatarID = account.Avatarid,
            Content = Message,
            Timestamp = DateTime.Now
        };
        lobby.Messages.Add(teamMessage);

        // Görev İlerlemesi - Mesaj Gönderme
        QuestManager.CheckQuestProgress(account, Quest.MissionType.SendChatMessage);

        var broadcastPacket = new SendTeamMessageResponsePacket
        {
            Flags = TeamMessageFlags.None,
            MessageId = teamMessage.MessageId,
      SenderId = account.ID, 
            SenderName = account.Username,
            SenderAvatarId = account.Avatarid,
            Role = "",
            Content = Message
        };

        foreach (var player in lobby.Players)
        {
            if (SessionManager.IsOnline(player.ID))
            {
                Session membersesion = SessionManager.GetSession(player.ID);
                membersesion.Send(broadcastPacket);
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
            default:
                EntryMessage = "komut bulunamadı... yardım için /help komutunu kullanın";
                break;
        }


        var response = new SendTeamMessageResponsePacket
        {
            Flags = TeamMessageFlags.None,
            MessageId = 0,
             SenderId = account.ID,
            SenderName = "SİSTEM",
            SenderAvatarId = account.Avatarid,
            Role = "",
            Content = EntryMessage
        };
        session.Send(response);

           
    }
    
}
