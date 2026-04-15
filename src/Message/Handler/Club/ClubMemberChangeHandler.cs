[PacketHandler(MessageType.MemberToLowerRequest)]
public static class ClubMemberChangeHandler
{
    public static void Handle(Session session, byte[] message)
    {
        try
        {
            ByteBuffer read = new ByteBuffer();
            read.WriteBytes(message, true);
            
            var request = new ClubMemberChangeRequestPacket();
            request.Deserialize(read);
            
            int targetid = request.TargetId;
            int status = request.Status;

            if (session.Account == null) return;
            var myAccount = session.Account;
            AccountManager.AccountData targetAccount = AccountCache.Load(targetid);

            if (targetAccount == null || myAccount == null) return;

            if (targetAccount.Clubid != myAccount.Clubid)
            {
                MessageCodeManager.Send(session, MessageCodeManager.Message.MemberNotİnClub);
                return;
            }

            if (myAccount.clubRole != ClubRole.Leader && myAccount.clubRole != ClubRole.CoLeader)
            {
               MessageCodeManager.Send(session, MessageCodeManager.Message.NoAuthorityClub);
                return;
            }

            if (targetAccount.ID == session.ID)
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
           MessageCodeManager.Send(session, MessageCodeManager.Message.GeneralError);
        }
    }

    private static void HandlePromotion(Session session, AccountManager.AccountData targetAccount, 
                                      AccountManager.AccountData myAccount)
    {
        if (myAccount.clubRole != ClubRole.Leader)
        {
           MessageCodeManager.Send(session, MessageCodeManager.Message.JustClubOwnerChange);
            return;
        }

        if (targetAccount.clubRole == ClubRole.Member)
        {
            ClubManager.ChangeMemberRole(targetAccount.Clubid, session.ID,
                                       targetAccount.ID, ClubRole.CoLeader);
            MessageCodeManager.Send(session, MessageCodeManager.Message.ClubRoleUpdateCoOwner);
        }
        else if (targetAccount.clubRole == ClubRole.CoLeader)
        {
            ClubManager.ChangeMemberRole(targetAccount.Clubid, session.ID,
                                       targetAccount.ID, ClubRole.Leader);
            ClubManager.ChangeMemberRole(targetAccount.Clubid, session.ID,
                                       session.ID, ClubRole.CoLeader);
            MessageCodeManager.Send(session, MessageCodeManager.Message.ClubRoleDoOwner);
        }
        else
            MessageCodeManager.Send(session, MessageCodeManager.Message.İnvalidTransaction);
    }

    private static void HandleDemotion(Session session, AccountManager.AccountData targetAccount, 
                                     AccountManager.AccountData myAccount)
    {
        if (targetAccount.clubRole == ClubRole.Member)
        {
             MessageCodeManager.Send(session, MessageCodeManager.Message.MemberAlreadyLowest);
        }
        else if (targetAccount.clubRole == ClubRole.CoLeader)
        {
            ClubManager.ChangeMemberRole(targetAccount.Clubid, session.ID, 
                                       targetAccount.ID, ClubRole.Member);
            MessageCodeManager.Send(session, MessageCodeManager.Message.ClubRoleLowerCoOwner);
        }
        else if (targetAccount.clubRole == ClubRole.Leader)
        {
           MessageCodeManager.Send(session, MessageCodeManager.Message.CannotLowerOwner);
        }
    }
}
