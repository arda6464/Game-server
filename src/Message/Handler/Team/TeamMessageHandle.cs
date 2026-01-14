public static class TeamMessageHandler
{
    public static void Handle(Session session, byte[] data)
    {
        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(data, true);
        int _ = read.ReadInt();



        string Message = read.ReadString();
        read.Dispose();


        var account = AccountCache.Load(session.AccountId);
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
            SenderId = account.AccountId,
            SenderAvatarID = account.Avatarid,
            Content = Message,
            Timestamp = DateTime.Now
        };
        lobby.Messages.Add(teamMessage);

        foreach (var player in lobby.Players)
        {
            if (SessionManager.IsOnline(player.AccountId))
            {
                Session membersesion = SessionManager.GetSession(player.AccountId);
                ByteBuffer memberbuffer = new ByteBuffer();

                memberbuffer.WriteInt((int)MessageType.SendTeamMessageResponse);
                memberbuffer.WriteByte((byte)teamMessage.messageFlags);
                memberbuffer.WriteInt(teamMessage.MessageId);
                memberbuffer.WriteString(account.AccountId);
                memberbuffer.WriteString(account.Username);
                memberbuffer.WriteInt(account.Avatarid);
                memberbuffer.WriteString(""); // Team'de rol yok, boş string
                memberbuffer.WriteString(Message);
                byte[] messsage = memberbuffer.ToArray();
                memberbuffer.Dispose();
                membersesion.Send(messsage);
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


        using (ByteBuffer memberbuffer = new ByteBuffer())
        {
                memberbuffer.WriteInt((int)MessageType.SendTeamMessageResponse);
            memberbuffer.WriteByte((byte)TeamMessageFlags.None);
                memberbuffer.WriteInt(0); // Mesaj ID'si yok
                memberbuffer.WriteString(account.AccountId);
                memberbuffer.WriteString("SİSTEM");
                memberbuffer.WriteInt(account.Avatarid);
                memberbuffer.WriteString(""); // Team'de rol yok, boş string
                memberbuffer.WriteString(EntryMessage);
                byte[] messsage = memberbuffer.ToArray();
                session.Send(messsage);
        }

           
    }
    
}