[PacketHandler(MessageType.JoinTeamRequest)]
public static class JoinTeamHandler
{
    public static void Handle(Session session,byte[] message)
    {
        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(message, true);
        
        var request = new JoinTeamRequestPacket();
        request.Deserialize(read);
        
        int code = request.TeamId;
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

        var acccount = session.Account;
        if (acccount == null) return;
        Lobby.AddPlayers(acccount);

        var broadcastPacket = new SendTeamMessageResponsePacket
        {
            Flags = TeamMessageFlags.HasSystem,
            EventType = TeamEventType.JoinMessage,
            SenderName = acccount.Username,
            SenderId = acccount.ID
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
        var response = new JoinTeamResponsePacket
        {
             TeamId = Lobby.ID
        };
        
        lock (Lobby.SyncLock)
        {
             response.Messages.AddRange(Lobby.Messages);
        }
        
        session.Send(response);
        session.TeamID = Lobby.ID;  

        // Görev İlerlemesi - Takıma Katılma
        QuestManager.CheckQuestProgress(acccount, Quest.MissionType.JoinTeam);
    }
} 
