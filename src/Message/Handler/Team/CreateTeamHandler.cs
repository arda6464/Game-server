public static class CreateTeamHandler
{
    public static void Handle(Session session)
    {
       

        if (session.TeamID != 0)
        {
            MessageCodeManager.Send(session, MessageCodeManager.Message.AlreadyİnTeam);
            return;
        }
            var Account = AccountCache.Load(session.AccountId);
            if (Account == null) return;
        Lobby Lobby = LobbyManager.CreateLobby(Account);

        ByteBuffer buffer = new ByteBuffer();

        buffer.WriteInt((int)MessageType.CreateTeamResponse);
        buffer.WriteInt(Lobby.ID);
        byte[] lobby = buffer.ToArray();
        buffer.Dispose();
        session.Send(lobby);
        session.TeamID = Lobby.ID;       
        ByteBuffer buffer1 = new ByteBuffer();
        buffer1.WriteInt((int)MessageType.SendTeamMessageResponse);
        TeamMessage teamMessage = new TeamMessage
        {
            messageFlags = TeamMessageFlags.HasSystem,
         eventType= TeamEventType.CreateMessage,
            SenderName = Account.Username,
            SenderId = Account.AccountId
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
    }
}