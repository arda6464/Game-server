[PacketHandler(MessageType.JoinClubRequest)]
public static class JoinedClubHandler
{
    public static void Handle(Session session, byte[] message)
    {
        ByteBuffer read = ByteBufferPool.Get();
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

        if (account.Clubid != 0)
        {
            MessageCodeManager.Send(session, MessageCodeManager.Message.AlreadyInClub);
            return;
        }
        if (club.State == ClubState.OnlyInvite)
        {
            if(club.PendingInvites.Contains(account.ID))
            {
                MessageCodeManager.Send(session, MessageCodeManager.Message.AlreadyRequestClub);
                return;
            }

               club.PendingInvites.Add(session.ID);
            ClubMessage clubMessage = new ClubMessage
            {
                messageFlags = ClubMessageFlags.Request,
                ActorID = account.ID,
                ActorName = account.Username,
                Content = request.jointext,
                RequestState = ClubRequestState.Waiting,
                SenderAvatarID = account.Avatarid
            };
            club.SendMessageToClubMembers(clubMessage);
           MessageCodeManager.Send(session, MessageCodeManager.Message.SendClubJoinRequest );
           Console.WriteLine("Kulübe katılma isteği başarıyla alındı");
        }
        else
        {
            bool isJoined = club.AddMember(account.ID);

            if (isJoined)
            {
                var response = new JoinClubResponsePacket
                {
                    Club = club
                };

                session.Send(response);

                ClubMessage joinMessage = new ClubMessage
                {
                    messageFlags = ClubMessageFlags.HasSystem,
                    eventType = ClubEventType.JoinMessage,
                    ActorName = account.Username,
                    ActorID = account.ID
                };
                club.SendMessageToClubMembers(joinMessage);

            }
            else
            {
                Console.WriteLine("joinclub else döndürüyormuş");
            }

        }



    }
}

