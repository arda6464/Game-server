public static class JoinedClubHandler
{
    
    public static void Handle(Session session, byte[] message)
    {
        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(message,true);
        int _ = read.ReadInt();

        int Clubıd = read.ReadInt();
        read.Dispose();
         bool isJoined = false;
        var Club = ClubManager.LoadClub(Clubıd);
        AccountManager.AccountData account = AccountCache.Load(session.AccountId);
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
          
            
        ByteBuffer buffer = new ByteBuffer();
        buffer.WriteInt((int)MessageType.JoinClubResponse);
        if (isJoined)
        {


            buffer.WriteInt(Club.ClubId);
            buffer.WriteInt(Club.ClubAvatarID);
            buffer.WriteString(Club.ClubName);
            buffer.WriteString(Club.Clubaciklama);
            buffer.WriteInt(Club.Members.Count);
            foreach (var member in Club.Members)
            {



                buffer.WriteString(member.Accountid);
                buffer.WriteString(member.AccountName);
                buffer.WriteString(member.Role.ToString());
                buffer.WriteInt(member.NameColorID);
                buffer.WriteInt(member.AvatarID);

            }

            buffer.WriteInt(Club.Messages.Count);

            foreach (var clubmessage in Club.Messages)
            {
                buffer.WriteByte((byte)clubmessage.messageFlags);

              switch((ClubMessageFlags)clubmessage.messageFlags)
             {
                case ClubMessageFlags.None:
                 buffer.WriteString(clubmessage.SenderId);
            buffer.WriteString(clubmessage.SenderName);
            buffer.WriteInt(clubmessage.SenderAvatarID);
            buffer.WriteString("Üye"); // todo enum send
            buffer.WriteString(clubmessage.Content);
                    break;
                case ClubMessageFlags.HasSystem:
                    buffer.WriteInt((int)clubmessage.eventType);
                    buffer.WriteString(clubmessage.ActorName);
                    buffer.WriteString(clubmessage.ActorID ??"");
                    break;
                case ClubMessageFlags.HasTarget:
                 buffer.WriteInt((int)clubmessage.eventType);
                    buffer.WriteString(clubmessage.ActorName);
                    buffer.WriteString(clubmessage.ActorID);
                    buffer.WriteString(clubmessage.TargetName);
                    break;
            }
            }
            byte[] veri = buffer.ToArray();
            buffer.Dispose();
            session.Send(veri);

               ByteBuffer buffer1 = new ByteBuffer();
               buffer1.WriteInt((int)MessageType.GetClubMessage);

                 ClubMessage messages = new ClubMessage
                 {
                     messageFlags = ClubMessageFlags.HasSystem,
                     eventType = ClubEventType.JoinMessage,
                     ActorName = account.Username,
                    ActorID = account.AccountId
                   };

                   Club.Messages.Add(messages);


                   buffer1.WriteInt((int)MessageType.GetClubMessage);
                   buffer1.WriteByte((byte)ClubMessageFlags.HasSystem);
                   buffer1.WriteInt((int)ClubEventType.JoinMessage);
               buffer1.WriteString(messages.ActorName);
               buffer1.WriteString(messages.ActorID);
               byte[] response = buffer1.ToArray();
               buffer1.Dispose();
                foreach(var memberrs in Club.Members)
               {
                   if(SessionManager.IsOnline(memberrs.Accountid))
                   {
                       Session session1 = SessionManager.GetSession(memberrs.Accountid);
                       session1.Send(response);
                   }
               }






        }
        else Console.WriteLine("joinclub else döndürüyormuş");
    }
}