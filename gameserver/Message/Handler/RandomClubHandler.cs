public static class RandomClubHandler
{
    public static void Handle(Session session)
    {
        ByteBuffer buffer = new ByteBuffer();
        buffer.WriteInt((int)MessageType.GetRandomClubResponse);

         var randomclubs = ClubManager.RandomList(10);
        buffer.WriteInt(randomclubs.Count);

        foreach (var rclub in randomclubs)
        {
            buffer.WriteInt(rclub.ClubId);
            buffer.WriteString(rclub.ClubName);
            buffer.WriteString(rclub.Clubaciklama);
            buffer.WriteInt(rclub.TotalKupa ?? 0);
            buffer.WriteInt(rclub.Members.Count);
        }
        byte[] veri = buffer.ToArray();
        buffer.Dispose();
        session.Send(veri);
    }
}