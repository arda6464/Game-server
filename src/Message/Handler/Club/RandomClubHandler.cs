[PacketHandler(MessageType.GetRandomClubRequest)]
public static class RandomClubHandler
{
    public static void Handle(Session session)
    {
        var randomclubs = ClubManager.RandomList(10);
        
        var response = new RandomClubResponsePacket();
        response.Clubs.AddRange(randomclubs);
        session.Send(response);
    }
}