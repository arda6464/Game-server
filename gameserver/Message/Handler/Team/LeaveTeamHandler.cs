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

        ByteBuffer buffer = new ByteBuffer();

        buffer.WriteInt((int)MessageType.LeaveTeamResponse);
        buffer.WriteBool(isleave);
        byte[] lobby = buffer.ToArray();
        buffer.Dispose();
        session.Send(lobby);
       
    
    }
}