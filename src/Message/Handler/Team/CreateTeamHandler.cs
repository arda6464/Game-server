[PacketHandler(MessageType.CreateTeamRequest)]
public static class CreateTeamHandler
{
    public static void Handle(Session session)
    {
       

        if (session.TeamID != 0)
        {
            MessageCodeManager.Send(session, MessageCodeManager.Message.AlreadyİnTeam);
            return;
        }
        if (session.Account == null) return;
        var Account = session.Account;
            if (Account == null) return;
        Lobby Lobby = LobbyManager.CreateLobby(Account);

        ByteBuffer buffer = new ByteBuffer();

        session.Send(new CreateTeamResponsePacket { 
            TeamId = Lobby.ID,
             Link = Lobby.Link
         }); 
        session.TeamID = Lobby.ID;   
        
        // Görev İlerlemesi - Takım Kurma
        QuestManager.CheckQuestProgress(Account, Quest.MissionType.CreateTeam);

        var broadcastPacket = new SendTeamMessageResponsePacket
        {
            Flags = TeamMessageFlags.HasSystem,
            EventType = TeamEventType.CreateMessage,
            SenderName = Account.Username,
            SenderId = Account.ID
        };
                 

        lock (Lobby.SyncLock)
        {
            foreach(var member in Lobby.Players)
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