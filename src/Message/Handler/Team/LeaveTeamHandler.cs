[PacketHandler(MessageType.LeaveTeamRequest)]
public static class LeaveTeamHandler
{
     public static void Handle(Session session,Byte[] data)
    {
         ByteBuffer read = new ByteBuffer();
        read.WriteBytes(data, true);
        read.Dispose();

        if (session.TeamID == 0)
        {
            Console.WriteLine("oyuncu bir takımda değil zaten?");
            return;
        }
         
     bool isleave = LobbyManager.LeaveTeam(session.TeamID,session.ID);
        if (!isleave) return;

        session.Send(new LeaveTeamResponsePacket { Success = isleave });
       
        if (session.Account == null) return;
        var acccount = session.Account;
        Lobby loby = LobbyManager.GetLobby(session.TeamID); // Wait, if I leave, can I still get lobby? session.TeamID is not reset yet in original code?
        // Original code: Lobby loby = LobbyManager.GetLobby(session.TeamID); after LeaveTeam called and before session.TeamID = 0;
        
        if (loby == null)
        {
            Console.WriteLine($"leaveteam: {session.ID} ID'li oyuncunun ayrılmak istediği takım null");
             session.TeamID = 0; // Ensure reset
            return;
        }
         session.TeamID = 0;

        var broadcastPacket = new SendTeamMessageResponsePacket
        {
            Flags = TeamMessageFlags.HasSystem,
            EventType = TeamEventType.LeaveMessage,
            SenderName = acccount.Username,
            SenderId = acccount.ID
        };
                    
        lock (loby.SyncLock)
        {
            foreach(var member in loby.Players)
            {
                if (SessionManager.IsOnline(member.ID))
                {
                    Session session1 = SessionManager.GetSession(member.ID);
                    session1.Send(broadcastPacket);
                }
            }
        }
    
    }
}
