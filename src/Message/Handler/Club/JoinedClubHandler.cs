[PacketHandler(MessageType.JoinClubRequest)]
public static class JoinedClubHandler
{
    
    public static void Handle(Session session, byte[] message)
    {
        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(message,true);
        int _ = read.ReadShort();

        var request = new JoinClubRequestPacket();
        request.Deserialize(read);
        
        int Clubıd = request.ClubId;
        read.Dispose();
         bool isJoined = false;
        var Club = ClubManager.LoadClub(Clubıd);
        if (session.Account == null) return;
        AccountManager.AccountData account = session.Account;
        if (Club == null) return;

        if (Club.Members.Count >= 100)
        {
            MessageCodeManager.Send(session, MessageCodeManager.Message.ClubFull);
            return;
        }


        if (account.Clubid == -1) isJoined = ClubManager.AddMember(Club.ClubId, account.AccountId);
        else
        {
            MessageCodeManager.Send(session, MessageCodeManager.Message.AlreadyİnClub);
            return;
        }
          
            
        if (isJoined)
        {
            var response = new JoinClubResponsePacket
            {
                ClubId = Club.ClubId,
                ClubAvatarId = Club.ClubAvatarID,
                ClubName = Club.ClubName,
                ClubDescription = Club.Clubaciklama,
            };
            response.Members.AddRange(Club.Members);
            response.Messages.AddRange(Club.Messages);
            session.Send(response);

               ClubMessage messages = new ClubMessage
                 {
                     messageFlags = ClubMessageFlags.HasSystem,
                     eventType = ClubEventType.JoinMessage,
                     ActorName = account.Username,
                    ActorID = account.AccountId,
                    MessageId = 0 // Assuming default or generated elsewhere if needed
                   };

                   Club.Messages.Add(messages);

                     var broadcastPacket = new GetClubMessagePacket { Message = messages };

                     foreach(var memberrs in Club.Members)
                {
                   if(SessionManager.IsOnline(memberrs.Accountid))
                   {
                       Session session1 = SessionManager.GetSession(memberrs.Accountid);
                       session1.Send(broadcastPacket);
                   }
               }






        }
        else Console.WriteLine("joinclub else döndürüyormuş");
    }
}