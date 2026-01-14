public static class LeaveTeamHandler
{
     public static void Handle(Session session,Byte[] data)
    {
         ByteBuffer read = new ByteBuffer();
        read.WriteBytes(data, true);
        int type = read.ReadInt();
        read.Dispose();

        if (session.TeamID == 0)
        {
            Console.WriteLine("oyuncu bir takımda değil zaten?");
            return;
        }
         
     bool isleave = LobbyManager.LeaveTeam(session.TeamID,session.AccountId);
        if (!isleave) return;
        ByteBuffer buffer = new ByteBuffer();

        buffer.WriteInt((int)MessageType.LeaveTeamResponse);
        buffer.WriteBool(isleave);
        byte[] lobby = buffer.ToArray();
        buffer.Dispose();
        session.Send(lobby);
       
          ByteBuffer buffer1 = new ByteBuffer();
        buffer1.WriteInt((int)MessageType.SendTeamMessageResponse);
        var acccount = AccountCache.Load(session.AccountId);
        Lobby loby = LobbyManager.GetLobby(session.TeamID);
        if (loby == null)
        {
            Console.WriteLine($"leaveteam: {session.AccountId} ID'li oyuncunun ayrılmak istediği takım null");
            return;
        }
         session.TeamID = 0;
        TeamMessage teamMessage = new TeamMessage
        {
            messageFlags = TeamMessageFlags.HasSystem,
         eventType= TeamEventType.LeaveMessage,
            SenderName = acccount.Username,
            SenderId = acccount.AccountId
        };
        buffer1.WriteByte((byte)teamMessage.messageFlags); // Flag önce yazılmalı!
        buffer1.WriteInt((int)teamMessage.eventType);
        buffer1.WriteString(teamMessage.SenderName);
        buffer1.WriteString(teamMessage.SenderId ?? "");
        byte[] response = buffer1.ToArray();
        buffer1.Dispose();
                    
        foreach(var member in loby.Players)
        {
            if (SessionManager.IsOnline(member.AccountId))
            {
                Session session1 = SessionManager.GetSession(member.AccountId);
                session1.Send(response);
            }
        }
    
    }
}