using System.Reflection.Metadata;

public static class ClubMessageHandler
{
    public static void Handle(Session session, byte[] message)
    {
        Console.WriteLine("club message handler iss run");
        ByteBuffer readbuffer = new ByteBuffer();
        readbuffer.WriteBytes(message, true);
        int _ = readbuffer.ReadInt();


        string accountıd = readbuffer.ReadString();
        string Message = readbuffer.ReadString();
        readbuffer.Dispose();


        AccountManager.AccountData account = AccountCache.Load(accountıd);
        var club = ClubManager.LoadClub(account.Clubid);
        if (club == null)
        {
            Console.WriteLine("club null");
            return;
        } 

        Console.WriteLine($"{account.Username} adlı kullanıcı {club.ClubName ?? "PORNO"} adlı kulube {Message} mesajını gönderdi");
          ClubMessage clubMessage = new ClubMessage
          {
              messageFlags = ClubMessageFlags.None,
              SenderName = account.Username,
              SenderId = account.AccountId,
              SenderAvatarID = account.Avatarid,
              Content = Message,
              Timestamp = DateTime.Now
          };
        club.Messages.Add(clubMessage);
      //  ClubManager.Save();
        ByteBuffer memberbuffer = new ByteBuffer();
                memberbuffer.WriteInt((int)MessageType.GetClubMessage);
                memberbuffer.WriteByte((byte)clubMessage.messageFlags);
                memberbuffer.WriteString(account.AccountId);
                memberbuffer.WriteString(account.Username);
                memberbuffer.WriteInt(account.Avatarid);
                memberbuffer.WriteString(account.clubRole.ToString());
                memberbuffer.WriteString(Message);
                byte[] messsage = memberbuffer.ToArray();
                memberbuffer.Dispose();
        foreach(var member in club.Members)
        {
            if(SessionManager.IsOnline(member.Accountid))
            {
                Session membersesion = SessionManager.GetSession(member.Accountid);
                membersesion.Send(messsage);
            }
        }
        

    }
}