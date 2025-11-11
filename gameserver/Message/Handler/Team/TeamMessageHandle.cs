public static class TeamMessageHandler
{
    public static void Handle(Session session,byte[] data)
    {
        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(data, true);
        int _ = read.ReadInt();


        
        string Message = read.ReadString();
        read.Dispose();


        var account = AccountCache.Load(session.AccountId);
        Lobby lobby  = LobbyManager.GetLobby(session.TeamID);
        if (lobby == null)
        {
            Console.WriteLine("lobby null");
            return;
        } 

        Console.WriteLine($"{account.Username} adlı kullanıcı {Message} mesajını gönderdi");

        lobby.Messages.Add(new ClubMessage
        {
            SenderName = account.Username,
            SenderId = account.AccountId,
           SenderAvatarID = account.Avatarid,
            Content = Message,
            Timestamp = DateTime.Now
        });
        
        foreach(var player in lobby.Players)
        {
            if(SessionManager.IsOnline(player.AccountId))
            {
                Session membersesion = SessionManager.GetSession(player.AccountId);
                ByteBuffer memberbuffer = new ByteBuffer();
                memberbuffer.WriteInt((int)MessageType.SendTeamMessageResponse);

                memberbuffer.WriteString(account.AccountId);
                memberbuffer.WriteString(account.Username);
                memberbuffer.WriteInt(account.Avatarid);
                memberbuffer.WriteString("");
                memberbuffer.WriteString(Message);
                byte[] messsage = memberbuffer.ToArray();
                memberbuffer.Dispose();
                membersesion.Send(messsage);
            }
        }
        

    }
}