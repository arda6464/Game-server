[PacketHandler(MessageType.GetRandomClubRequest)]
public class RandomClubHandler : IGameMessage
{
    public void Handle(Session session, byte[]? data)
    {
        var randomclubs = ClubManager.RandomList(10);

        var response = new RandomClubResponsePacket();
        response.Clubs.AddRange(randomclubs);
        session.Send(response);
    }
}
