public static class TeamInviteHandler
{
    public static void Handle(Session session, byte[] data)
    {
        string accid;
        using (ByteBuffer read = new ByteBuffer())
        {
            read.WriteBytes(data);
            read.ReadInt();
            accid = read.ReadString();
        }
        var targetacccount = AccountCache.Load(accid);
        if (targetacccount == null) return; // todo messagecode (aslında gerek varmı?)


        //todo: oyuncu takım davetlerini kabul ediyormu?
        var acccount = AccountCache.Load(session.AccountId);
        if (session.TeamID == 0) CreateTeamHandler.Handle(session);


        Lobby lobby = LobbyManager.GetLobby(session.TeamID);


        if (!SessionManager.IsOnline(targetacccount.AccountId))
        {
            // todo playeroflinemessage.....
            return;
        }
        if (lobby.Players.Count == lobby.MaxPlayers)
        {
            // todo teamfullmessage.........
            return;
        }
        else
        {
            Session? targetsession = SessionManager.GetSession(targetacccount.AccountId);
            using (ByteBuffer buffer = new ByteBuffer())
            {
                buffer.WriteInt((int)MessageType.InviteToTeamRequest);
                buffer.WriteString(acccount.Username);
                buffer.WriteString(acccount.AccountId);
                buffer.WriteInt(acccount.Avatarid);
                buffer.WriteInt(acccount.Trophy);
                buffer.WriteByte((byte)lobby.Players.Count);
                buffer.WriteByte((byte)lobby.MaxPlayers);
                targetsession?.Send(buffer.ToArray());
            }
        }
    }
    public static void ResponseHandle(Session session,byte[] responsedata)
    {
        bool Accept = false;
        string targetacc;
        using (ByteBuffer read = new ByteBuffer())
        {
            read.WriteBytes(responsedata);
            read.ReadInt();
            targetacc = read.ReadString();
            Accept = read.ReadBool();
        }

        if (!SessionManager.IsOnline(targetacc))
            return;
       Session Invitersession = SessionManager.GetSession(targetacc);
        if (Accept)
        {
            using (ByteBuffer fakebuffer = new ByteBuffer())

            {
            fakebuffer.WriteInt(23232); // fake id
            fakebuffer.WriteInt(Invitersession.TeamID);
            byte[] fakebyte = fakebuffer.ToArray();

            JoinTeamHandler.Handle(session,fakebyte);         
                
            }
        }
        else return; // todo messagecode
    }
}