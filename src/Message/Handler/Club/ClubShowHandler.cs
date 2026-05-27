[PacketHandler(MessageType.ClubShowRequest)]
public static class ClubShowHandler
{
    public static void Handle(Session session, byte[] message)
    {
        ByteBuffer read = ByteBufferPool.Get();
        read.WriteBytes(message, true);

    
        
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
           club=club
        };
        if(club.ID == session.Account?.Clubid)
        {
          //  response.Messages.AddRange(club.Messages);
        }
       
        session.Send(response);
    }
    
}
