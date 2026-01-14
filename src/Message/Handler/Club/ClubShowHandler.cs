public static class ClubShowHandler
{
    public static void Handle(Session session, byte[] message)
    {
        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(message, true);

        int type = read.ReadInt();
        int clubid = read.ReadInt();

        var club = ClubCache.Load(clubid);
        if (club == null)
        {
            MessageCodeManager.Send(session, MessageCodeManager.Message.NotAClub);
            return;
        }
        ByteBuffer buffer = new ByteBuffer();
        buffer.WriteInt((int)MessageType.ClubShowResponse);

        buffer.WriteInt(club.ClubId);
        buffer.WriteString(club.ClubName);
        buffer.WriteString(club.Clubaciklama);
        buffer.WriteInt(club.ClubAvatarID);
        buffer.WriteInt(club.TotalKupa ?? 0);
        buffer.WriteInt(club.Members.Count);
        foreach (var member in club.Members)
        {
            buffer.WriteString(member.Accountid);
            buffer.WriteString(member.AccountName);
            // buffer.WriteInt(member.)
            buffer.WriteString(member.Role.ToString());
            buffer.WriteInt(member.NameColorID);
            buffer.WriteInt(member.AvatarID);
        }
        byte[] data = buffer.ToArray();
        buffer.Dispose();
        session.Send(data);
    }
    
}