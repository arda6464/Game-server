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
              //  SendNotification(session, "Hesap bulunamadı!");
                return;
            }

            // Aynı kulüpte mi kontrolü
            if (targetAccount.Clubid != myAccount.Clubid)
            {
                MessageCodeManager.Send(session, MessageCodeManager.Message.MemberNotİnClub);
                return;
            }

            // Yetki kontrolü - sadece lider ve yardımcı liderler işlem yapabilir
            if (myAccount.clubRole != ClubRole.Leader && myAccount.clubRole != ClubRole.CoLeader)
            {
               MessageCodeManager.Send(session, MessageCodeManager.Message.NoAuthorityClub);
                return;
            }

            // Kendi kendine işlem yapamaz
            if (targetAccount.AccountId == session.AccountId)
            {
                MessageCodeManager.Send(session, MessageCodeManager.Message.ThisYou);
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
                    MessageCodeManager.Send(session, MessageCodeManager.Message.İnvalidTransaction);
                    break;
            }
        }
        catch (Exception ex)
        {
            // Hata loglama
           MessageCodeManager.Send(session, MessageCodeManager.Message.GeneralError);
        }
    }

    private static void HandlePromotion(Session session, AccountManager.AccountData targetAccount, 
                                      AccountManager.AccountData myAccount)
    {
        // Sadece lider yardımcı lider atayabilir veya liderlik devredebilir
        if (myAccount.clubRole != ClubRole.Leader)
        {
           MessageCodeManager.Send(session, MessageCodeManager.Message.JustClubOwnerChange);
            return;
        }

        if (targetAccount.clubRole == ClubRole.Member)
        {
            // Üyeyi yardımcı liders yükselt
            ClubManager.ChangeMemberRole(targetAccount.Clubid, session.AccountId,
                                       targetAccount.AccountId, ClubRole.CoLeader);
            MessageCodeManager.Send(session, MessageCodeManager.Message.ClubRoleUpdateCoOwner);
        }
        else if (targetAccount.clubRole == ClubRole.CoLeader)
        {
            // Yardımcı lidere liderlik devret
            ClubManager.ChangeMemberRole(targetAccount.Clubid, session.AccountId,
                                       targetAccount.AccountId, ClubRole.Leader);
            ClubManager.ChangeMemberRole(targetAccount.Clubid, session.AccountId,
                                       session.AccountId, ClubRole.CoLeader);
            MessageCodeManager.Send(session, MessageCodeManager.Message.ClubRoleDoOwner);
        }
        else
            MessageCodeManager.Send(session, MessageCodeManager.Message.İnvalidTransaction);
    }

    private static void HandleDemotion(Session session, AccountManager.AccountData targetAccount, 
                                     AccountManager.AccountData myAccount)
    {
        // Sadece lider rol düşürebilir
        /*if (myAccount.clubRole != ClubRole.Leader)
        {
             MessageCodeManager.Send(session, MessageCodeManager.Message.ClubRoleDoOwner);
            return;
        }*/

        if (targetAccount.clubRole == ClubRole.Member)
        {
             MessageCodeManager.Send(session, MessageCodeManager.Message.MemberAlreadyLowest);
        }
        else if (targetAccount.clubRole == ClubRole.CoLeader)
        {
            // Yardımcı lideri üyeye düşür
            ClubManager.ChangeMemberRole(targetAccount.Clubid, session.AccountId, 
                                       targetAccount.AccountId, ClubRole.Member);
            MessageCodeManager.Send(session, MessageCodeManager.Message.ClubRoleLowerCoOwner);
        }
        else if (targetAccount.clubRole == ClubRole.Leader)
        {
           MessageCodeManager.Send(session, MessageCodeManager.Message.CannotLowerOwner);
        }
    }
}