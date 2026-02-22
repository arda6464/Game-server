[PacketHandler(MessageType.ClubShowRequest)]
public static class ClubShowHandler
{
    public static void Handle(Session session, byte[] message)
    {
        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(message, true);

        int _ = read.ReadShort();
        
        var request = new ClubShowRequestPacket();
        request.Deserialize(read);
        
        int clubid = request.ClubId;

        var club = ClubCache.Load(clubid);
        if (club == null)
        {
            MessageCodeManager.Send(session, MessageCodeManager.Message.NotAClub);
            return;
        }

        var response = new ClubShowResponsePacket
        {
            ClubId = club.ClubId,
            ClubName = club.ClubName,
            ClubDescription = club.Clubaciklama,
            ClubAvatarId = club.ClubAvatarID,
            TotalTrophies = club.TotalKupa ?? 0,
        };
        response.Members.AddRange(club.Members);
        session.Send(response);
    }
    
}