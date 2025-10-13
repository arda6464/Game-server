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

        if (Club.Members.Count == 100)
        {
            Console.WriteLine("katılamaz dolu "); // todo notfication warn
        }
        if (account.Clubid == -1) isJoined = ClubManager.AddMember(Club.ClubId, account.AccountId);
        else
            Console.WriteLine("bu kişi zaten clubte");
        ByteBuffer buffer = new ByteBuffer();
        buffer.WriteInt((int)MessageType.JoinClubResponse);
        if(isJoined)
        {
            account.Clubid = Club.ClubId;
            buffer.WriteInt(Club.ClubId);
            buffer.WriteInt(Club.ClubAvatarID);
            buffer.WriteString(Club.ClubName);
            buffer.WriteString(Club.Clubaciklama);
            buffer.WriteInt(Club.Members.Count);
            buffer.WriteInt(Club.Messages.Count);

            foreach (var clubmessage in Club.Messages)
            {
                buffer.WriteString(clubmessage.SenderId);
                buffer.WriteString(clubmessage.SenderName);
                buffer.WriteInt(clubmessage.SenderAvatarID);
                buffer.WriteString("Üye");
                buffer.WriteString(clubmessage.Content);
            }
            byte[] veri = buffer.ToArray();
            buffer.Dispose();
            session.Send(veri);
        }
    }
}