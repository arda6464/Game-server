[PacketHandler(MessageType.KickMemberinClubRequest)]
public static class KickMemberHandler
{
    public static void Handle(Session session, byte[] message)
    {
        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(message, true);

        int type = read.ReadShort();
        
        var request = new KickMemberRequestPacket();
        request.Deserialize(read);
        
        string targetid = request.TargetId;
        read.Dispose();

        if (session.Account == null) return;
        AccountManager.AccountData account = session.Account;
        if (account == null)
        {
           /* NotficationSender.Send(session, new Notfication
            {
                Id = 11,
                Title = "Başarısız",
                Message = "hesap bulunamadı"
            });*/
           // MessageCodeManager.Send(session, )
            return;
        }
        var club = ClubCache.Load(account.Clubid);
        if (club == null)
        {
            MessageCodeManager.Send(session, MessageCodeManager.Message.NotAClub);
            return;
        }

        bool kicked = ClubManager.KickMember(club.ClubId, session.AccountId, targetid);
        if (kicked)
        {

            var response = new KickMemberResponsePacket { TargetId = targetid };
            session.Send(response);
            
            if (SessionManager.IsOnline(targetid))
            {
                var targetsesion = SessionManager.GetSession(targetid);
                targetsesion.Send(response);
            }





        }
        else
        {
            MessageCodeManager.Send(session, MessageCodeManager.Message.İnvalidTransaction);
        }
    }
}