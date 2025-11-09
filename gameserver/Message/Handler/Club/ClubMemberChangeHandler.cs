public static class ClubMemberChangeHandler
{
    public static void Handle(Session session, byte[] message)
    {
        try
        {
            ByteBuffer read = new ByteBuffer();
            read.WriteBytes(message, true);

            int type = read.ReadInt();
            string targetid = read.ReadString();
            short status = read.ReadShort();

            // Hesap kontrolleri
            AccountManager.AccountData targetAccount = AccountCache.Load(targetid);
            var myAccount = AccountCache.Load(session.AccountId);

            // Temel kontroller
            if (targetAccount == null || myAccount == null)
            {
                SendNotification(session, "Hesap bulunamadı!");
                return;
            }

            // Aynı kulüpte mi kontrolü
            if (targetAccount.Clubid != myAccount.Clubid)
            {
                SendNotification(session, "Aynı kulüpte değilsiniz!");
                return;
            }

            // Yetki kontrolü - sadece lider ve yardımcı liderler işlem yapabilir
            if (myAccount.clubRole != ClubRole.Leader && myAccount.clubRole != ClubRole.CoLeader)
            {
                SendNotification(session, "Bu işlem için yetkiniz yok!");
                return;
            }

            // Kendi kendine işlem yapamaz
            if (targetAccount.AccountId == session.AccountId)
            {
                SendNotification(session, "Kendi rolünüzü değiştiremezsiniz!");
                return;
            }

            switch (status)
            {
                case 1: // Yükseltme
                    HandlePromotion(session, targetAccount, myAccount);
                    break;
                case -1: // Düşürme
                    HandleDemotion(session, targetAccount, myAccount);
                    break;
                default:
                    SendNotification(session, "Geçersiz işlem!");
                    break;
            }
        }
        catch (Exception ex)
        {
            // Hata loglama
            SendNotification(session, "İşlem sırasında hata oluştu!");
        }
    }

    private static void HandlePromotion(Session session, AccountManager.AccountData targetAccount, 
                                      AccountManager.AccountData myAccount)
    {
        // Sadece lider yardımcı lider atayabilir veya liderlik devredebilir
        if (myAccount.clubRole != ClubRole.Leader)
        {
            SendNotification(session, "Sadece kulüp lideri rol değişikliği yapabilir!");
            return;
        }

        if (targetAccount.clubRole == ClubRole.Member)
        {
            // Üyeyi yardımcı liders yükselt
            ClubManager.ChangeMemberRole(targetAccount.Clubid, session.AccountId, 
                                       targetAccount.AccountId, ClubRole.CoLeader);
            SendNotification(session, "Üye yardımcı lider yapıldı!");
        }
        else if (targetAccount.clubRole == ClubRole.CoLeader)
        {
            // Yardımcı lidere liderlik devret
            ClubManager.ChangeMemberRole(targetAccount.Clubid, session.AccountId, 
                                       targetAccount.AccountId, ClubRole.Leader);
            ClubManager.ChangeMemberRole(targetAccount.Clubid, session.AccountId, 
                                       session.AccountId, ClubRole.CoLeader);
            SendNotification(session, "Liderlik devredildi!");
        }
        else
        {
            SendNotification(session, "Geçersiz yükseltme işlemi!");
        }
    }

    private static void HandleDemotion(Session session, AccountManager.AccountData targetAccount, 
                                     AccountManager.AccountData myAccount)
    {
        // Sadece lider rol düşürebilir
        if (myAccount.clubRole != ClubRole.Leader)
        {
            SendNotification(session, "Sadece kulüp lideri rol düşürebilir!");
            return;
        }

        if (targetAccount.clubRole == ClubRole.Member)
        {
            SendNotification(session, "Bu oyuncu zaten en düşük seviyede!");
        }
        else if (targetAccount.clubRole == ClubRole.CoLeader)
        {
            // Yardımcı lideri üyeye düşür
            ClubManager.ChangeMemberRole(targetAccount.Clubid, session.AccountId, 
                                       targetAccount.AccountId, ClubRole.Member);
            SendNotification(session, "Yardımcı lider üyeliğe düşürüldü!");
        }
        else if (targetAccount.clubRole == ClubRole.Leader)
        {
            SendNotification(session, "Lideri düşüremezsiniz!");
        }
    }

    private static void SendNotification(Session session, string message)
    {
        Notification notification = new Notification
        {
          Id = 11,
          Title = "Reddedildi",
           Message = message,
        };
        NotificationSender.Send(session, notification);
    }
}