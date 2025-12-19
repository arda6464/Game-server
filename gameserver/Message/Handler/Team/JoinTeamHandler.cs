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
            MessageCodeManager.Send(session, MessageCodeManager.Message.AlreadyİnTeam);
            return;
        }

        Lobby Lobby = LobbyManager.GetLobby(code);
        if (Lobby == null)
        {
            MessageCodeManager.Send(session, MessageCodeManager.Message.NotATeam); 
            return;
        } 

        var acccount = AccountCache.Load(session.AccountId);
        if (acccount == null) return;
        Lobby.AddPlayers(acccount);
        ByteBuffer buffer1 = new ByteBuffer();
        buffer1.WriteInt((int)MessageType.SendTeamMessageResponse);
        TeamMessage teamMessage = new TeamMessage
        {
            messageFlags = TeamMessageFlags.HasSystem,
         eventType= TeamEventType.JoinMessage,
            SenderName = acccount.Username,
            SenderId = acccount.AccountId
        };
        buffer1.WriteByte((byte)teamMessage.messageFlags); // Flag önce yazılmalı!
        buffer1.WriteInt((int)teamMessage.eventType);
        buffer1.WriteString(teamMessage.SenderName);
        buffer1.WriteString(teamMessage.SenderId ?? "");
        byte[] response = buffer1.ToArray();
        buffer1.Dispose();
                    
        foreach(var member in Lobby.Players)
        {
            if (SessionManager.IsOnline(member.AccountId))
            {
                Session session1 = SessionManager.GetSession(member.AccountId);
                session1.Send(response);
            }
        }
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