[PacketHandler(MessageType.KickMemberinClubRequest)]
public class KickMemberHandler : IGameMessage
{
    public void Handle(Session session, byte[]? data)
    {
        var request = data.DeserializePacket<KickMemberRequestPacket>();

        int targetid = request.TargetId;

        if (session.Account == null)
            return;

        var club = ClubCache.Load(session.Account.Clubid);
        if (club == null)
        {
            MessageCodeManager.Send(session, MessageCodeManager.Message.NotAClub);
            return;
        }

        bool kicked = club.KickMember(session.ID, targetid);
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
            MessageCodeManager.Send(session, MessageCodeManager.Message.InvalidTransaction);
        }
    }
}
