public static class CreateTeamHandler
{
    public static void Handle(Session session,byte[] message)
    {
        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(message, true);
        int type = read.ReadInt();
        read.Dispose();

        if (session.TeamID != 0)
        {
            Console.WriteLine("zaten aktif bir team'de");
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
    }
}