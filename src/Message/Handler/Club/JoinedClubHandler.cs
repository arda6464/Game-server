[PacketHandler(MessageType.JoinClubRequest)]
public static class JoinedClubHandler
{
    public static void Handle(Session session, byte[] message)
    {
        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(message, true);

        var request = new JoinClubRequestPacket();
        request.Deserialize(read);
        
        int clubId = request.ClubId;
        read.Dispose();

        if (session.Account == null) return;
        AccountManager.AccountData account = session.Account;
        
        var club = ClubManager.LoadClub(clubId);
        if (club == null) return;

        if (club.Members.Count >= 100)
        {
            MessageCodeManager.Send(session, MessageCodeManager.Message.ClubFull);
            return;
        }

        if (account.Clubid != -1)
        {
            MessageCodeManager.Send(session, MessageCodeManager.Message.AlreadyİnClub);
            return;
        }

        bool isJoined = ClubManager.AddMember(club.ClubId, account.ID);
            
        if (isJoined)
        {
            var response = new JoinClubResponsePacket
            {
                ClubId = club.ClubId,
                ClubAvatarId = club.ClubAvatarID,
                ClubName = club.ClubName,
                ClubDescription = club.Clubaciklama,
            };
            response.Members.AddRange(club.Members);
            response.Messages.AddRange(club.Messages);
            session.Send(response);

            ClubMessage joinMessage = new ClubMessage
            {
                messageFlags = ClubMessageFlags.HasSystem,
                eventType = ClubEventType.JoinMessage,
                ActorName = account.Username,
                ActorID = account.ID,
                MessageId = 0
            };

            lock (club.SyncLock)
            {
                club.Messages.Add(joinMessage);
            }

            var broadcastPacket = new GetClubMessagePacket { Message = joinMessage };

            foreach (var member in club.Members)
            {
                if (SessionManager.IsOnline(member.ID))
                {
                    Session targetSession = SessionManager.GetSession(member.ID);
                    targetSession?.Send(broadcastPacket);
                }
            }
        }
        else 
        {
            Console.WriteLine("joinclub else döndürüyormuş");
        }
    }
}
