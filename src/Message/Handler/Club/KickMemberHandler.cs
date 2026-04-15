[PacketHandler(MessageType.KickMemberinClubRequest)]
public static class KickMemberHandler
{
    public static void Handle(Session session, byte[] message)
    {
        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(message, true);
        
        var request = new KickMemberRequestPacket();
        request.Deserialize(read);
        
        int targetid = request.TargetId;
        read.Dispose();

        if (session.Account == null) return;
        
        var club = ClubCache.Load(session.Account.Clubid);
        if (club == null)
        {
            MessageCodeManager.Send(session, MessageCodeManager.Message.NotAClub);
            return;
        }

        bool kicked = ClubManager.KickMember(club.ClubId, session.ID, targetid);
        if (kicked)
        {
            var response = new KickMemberResponsePacket { TargetId = targetid };
            session.Send(response);
            
            if (SessionManager.IsOnline(targetid))
            {
                var targetsession = SessionManager.GetSession(targetid);
                targetsession.Send(response);
            }
        }
        else
        {
            MessageCodeManager.Send(session, MessageCodeManager.Message.İnvalidTransaction);
        }
    }
}
