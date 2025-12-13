public static class KickMemberHandler
{
    public static void Handle(Session session, byte[] message)
    {
        ByteBuffer read = new ByteBuffer();
        read.WriteBytes(message, true);

        int type = read.ReadInt();
        string targetid = read.ReadString();

        AccountManager.AccountData account = AccountCache.Load(session.AccountId);
        if (account == null)
        {
            NotficationSender.Send(session, new Notfication
            {
                Id = 11,
                Title = "Başarısız",
                Message = "hesap bulunamadı"
            });
            return;
        }
        var club = ClubCache.Load(account.Clubid);
        if (club == null)
        {
            NotficationSender.Send(session, new Notfication
            {
                Id = 11,
                Title = "Başarısız",
                Message = "aradığınız club bulunamadı...",
                iconid = 3
            });
            return;
        }

        bool kicked = ClubManager.KickMember(club.ClubId, session.AccountId, targetid);
        if (kicked)
        {

            ByteBuffer buffer = new ByteBuffer();
            buffer.WriteInt((int)MessageType.KickMemberinClubResponse);
            buffer.WriteString(targetid);
            byte[] bytes = buffer.ToArray();
            buffer.Dispose();
            session.Send(bytes);
            if (SessionManager.IsOnline(targetid))
            {
                var targetsesion = SessionManager.GetSession(targetid);
                targetsesion.Send(bytes);
            }





        }
        else
        {
            NotficationSender.Send(session, new Notfication
            {
                Id = 11,
                Title = "Başarısız",
                Message = "yetkiniz bulunmamaktadır",
                iconid = 2
            });
        }
    }
}