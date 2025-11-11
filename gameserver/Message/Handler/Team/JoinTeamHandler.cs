public static class JoinTeamHandler
{
    public static void Handle(Session session,byte[] message)
    {
        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(message, true);
        int type = read.ReadInt();
        int code = read.ReadInt();
        read.Dispose();

        if (session.TeamID != 0)
        {
            Console.WriteLine("zaten aktif bir team'de");
            return;
        }

        Lobby Lobby = LobbyManager.GetLobby(code);
        if (Lobby == null)
        {
            Notification notification = new Notification // todo client already message send
            {
                Id = 11,
                Title = "Başarısız",
                Message = "Takım bulunamadı!",
                iconid = 4
            };
            NotificationSender.Send(session, notification); 
            return;
        } 

        var acccount = AccountCache.Load(session.AccountId);
        if (acccount == null) return;
        Lobby.AddPlayers(acccount);
        ByteBuffer buffer = new ByteBuffer();

        buffer.WriteInt((int)MessageType.JoinTeamResponse);
        buffer.WriteInt(Lobby.ID);
        buffer.WriteInt(Lobby.Messages.Count);
        foreach(var teammessage in Lobby.Messages)
        {
            buffer.WriteString(teammessage.SenderId);
                buffer.WriteString(teammessage.SenderName);
                buffer.WriteInt(teammessage.SenderAvatarID);
                buffer.WriteString(" ");
                buffer.WriteString(teammessage.Content);
        }
        byte[] lobby = buffer.ToArray(); 
        buffer.Dispose();
        session.Send(lobby);
        session.TeamID = Lobby.ID;       
    }
} 