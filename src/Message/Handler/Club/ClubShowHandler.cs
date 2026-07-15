[PacketHandler(MessageType.ClubShowRequest)]
public class ClubShowHandler : IGameMessage
{
    public void Handle(Session session, byte[]? data)
    {
        var request = data.DeserializePacket<ClubShowRequestPacket>();

        int clubid = request.ClubId;

        var club = ClubCache.Load(clubid);
        if (club == null)
        {
            MessageCodeManager.Send(session, MessageCodeManager.Message.NotAClub);
            return;
        }

        var response = new ClubShowResponsePacket { club = club };
        if (club.ID == session.Account?.Clubid)
        {
            //  response.Messages.AddRange(club.Messages);
        }

        session.Send(response);
    }
}
